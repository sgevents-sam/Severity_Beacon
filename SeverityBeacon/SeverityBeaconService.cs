using System.Net.Http.Headers;
using System.Net.Http.Json;
using SeverityBeacon.Models;

namespace SeverityBeacon;

public class SeverityBeaconService(HttpClient httpClient)
{
    public IReadOnlyList<string> GetBeaconDevices()
    {
        return TheBeacon.GetBeaconDevices();
    }

    public async Task<IReadOnlyList<HostGroupResult>> GetHostGroupsAsync(Uri zabbixUrl, string apiToken, CancellationToken cancellationToken = default)
    {
        var rpcRequest = new JsonRpc
        {
            Method = "hostgroup.get"
        };

        var hostGroups = await PostRpcAsync<HostGroupResults>(zabbixUrl, apiToken, rpcRequest, cancellationToken);
        return hostGroups?.Result ?? [];
    }

    public async Task RunPollingAsync(
        BeaconRuntimeOptions options,
        Action<BeaconLog>? log = null,
        Action<string>? stateChanged = null,
        CancellationToken cancellationToken = default)
    {
        ValidateOptions(options);
        using var beacon = new TheBeacon(
            options.BeaconSerialPort,
            options.ZeroProblemsBeaconHex,
            options.ClearBeaconAfter,
            options.DeskLampBeaconHex,
            stateChanged);

        Log(log, $"Connected to beacon on {options.BeaconSerialPort}");

        while (!cancellationToken.IsCancellationRequested)
        {
            var currentSeverity = await GetProblemsAsync(options, log, cancellationToken);
            beacon.SendBeaconIssue(currentSeverity);
            await Task.Delay(TimeSpan.FromSeconds(options.QueryIntervalSeconds), cancellationToken);
        }
    }

    public Task SetStaticColorAsync(string beaconSerialPort, string hexColor, Action<string>? stateChanged = null)
    {
        using var beacon = new TheBeacon(beaconSerialPort, hexColor, 0, null, stateChanged);
        beacon.SetBeaconColour(hexColor);
        return Task.CompletedTask;
    }

    public BeaconManualOverrideSession CreateManualOverrideSession(string beaconSerialPort, Action<string>? stateChanged = null)
    {
        return new BeaconManualOverrideSession(beaconSerialPort, stateChanged);
    }

    private async Task<SeverityOption?> GetProblemsAsync(
        BeaconRuntimeOptions options,
        Action<BeaconLog>? log,
        CancellationToken cancellationToken)
    {
        Log(log, "Obtaining latest problems from Zabbix");

        var hosts = await GetEnabledHostsInHostGroupsAsync(
            options.ZabbixUrl,
            options.ApiToken,
            [options.HostGroupId],
            cancellationToken);

        if (hosts.Count == 0)
        {
            Log(log, "No enabled hosts matched the selected host group");
            return null;
        }

        Log(log, $"{hosts.Count} hosts in host group enabled in Zabbix");

        var rpcRequest = new JsonRpc
        {
            Method = "problem.get"
        };
        rpcRequest.Params.Add("acknowledged", false);
        rpcRequest.Params.Add("suppressed", false);
        rpcRequest.Params.Add("hostids", hosts);
        rpcRequest.Params.Add("severities", options.SeverityOptions.Select(a => a.Value.ZabbixSeverityValue).ToArray());

        var problems = await PostRpcAsync<ProblemResults>(options.ZabbixUrl, options.ApiToken, rpcRequest, cancellationToken);
        var activeProblems = problems?.Result ?? [];

        Log(log, $"{activeProblems.Count} problems active in Zabbix");

        var maximumSeverity = activeProblems
            .Select(problem => int.TryParse(problem.Severity, out var value) ? value : (int?)null)
            .Max();

        if (maximumSeverity == null)
        {
            return null;
        }

        var severity = options.SeverityOptions.FirstOrDefault(a => a.Value.ZabbixSeverityValue == maximumSeverity.Value);
        if (severity.Equals(default(KeyValuePair<string, SeverityOption>)))
        {
            Log(log, $"No beacon mapping found for severity {maximumSeverity.Value}");
            return null;
        }

        Log(log, $"Highest active severity is {severity.Key}");
        return severity.Value;
    }

    private async Task<IReadOnlyList<string>> GetEnabledHostsInHostGroupsAsync(
        Uri zabbixUrl,
        string apiToken,
        IEnumerable<string> hostGroupIds,
        CancellationToken cancellationToken)
    {
        var rpcRequest = new JsonRpc
        {
            Method = "host.get"
        };
        rpcRequest.Params.Add("output", new List<string> { "hostid", "status" });
        rpcRequest.Params.Add("filter", new RequestFilter { status = ["0"] });
        rpcRequest.Params.Add("groupids", hostGroupIds);

        var hosts = await PostRpcAsync<HostResults>(zabbixUrl, apiToken, rpcRequest, cancellationToken);
        return hosts?.Result.Select(host => host.Hostid).ToList() ?? [];
    }

    private async Task<T?> PostRpcAsync<T>(
        Uri zabbixUrl,
        string apiToken,
        JsonRpc rpcRequest,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(zabbixUrl, "/api_jsonrpc.php"))
        {
            Content = JsonContent.Create(rpcRequest)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    private static void ValidateOptions(BeaconRuntimeOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiToken))
        {
            throw new ArgumentException("Zabbix API token is required.");
        }

        if (string.IsNullOrWhiteSpace(options.BeaconSerialPort))
        {
            throw new ArgumentException("A beacon serial port is required.");
        }

        if (string.IsNullOrWhiteSpace(options.HostGroupId))
        {
            throw new ArgumentException("A host group ID is required.");
        }

        if (options.SeverityOptions.Count == 0)
        {
            throw new ArgumentException("At least one severity mapping is required.");
        }
    }

    private static void Log(Action<BeaconLog>? log, string message)
    {
        if (log == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        log(new BeaconLog(DateTime.Now, message));
    }
}
