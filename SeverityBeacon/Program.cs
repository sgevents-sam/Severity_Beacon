using Cocona;
using Cocona.Builder;
using SeverityBeacon;

var builder = CoconaApp.CreateBuilder();
var app = builder.Build();
var httpClient = new HttpClient();
var service = new SeverityBeaconService(httpClient);

app.AddCommand(async (
    [Argument(Description = "URL of Zabbix Server")] Uri zabbixUrl,
    [Argument(Description = "Zabbix user API token")] string apiToken,
    [Option(['c'], Description = "Beacon Serial Port")] string? beaconSerialPort,
    [Option(['g'], Description = "Provide a HostGroup_ID to filter severity queries")] string? hostGroup,
    [Option(['s'], Description = """
                                 Severities to filter by with colour or flashing state:
                                 Usage: -s [disaster,#FF0000] (static)
                                        -s [disaster,#FF0000,#00FF00,125] (flashing between 2 colours each 125ms)
                                        -s [disaster,#FFA501,#000000,3000,500] (flashing between 2 colours, wait 3000ms before state 1 -> state 2, then 500ms before state 2 -> state 1)
                                 Severity Values: disaster, high, average, warning, information, "not classified"
                                 default = -s "[disaster,#FF0000,#0000FF,125]", -s "[high,#FF00000,#000000,125]" -s "[average,#FFA501,#000000,3000,500]" -s "[warning,#FFFF01,#000000,3000,500]"
                                 """)] string[]? severity,
    [Option(['r'], Description = "Query interval in seconds, default = 15")] int? queryInterval,
    [Option(['z'], Description = "Hex colour of zero problems state, default = #018001")] string? zeroProblemsHex,
    [Option(['x'], Description = "After x number of queries with successive OK results, turn off the beacon, default = 9")] int? clearBeaconAfter) =>
{
    var severityOptions = severity != null
        ? SeverityOptionParser.Parse(severity)
        : SeverityDefaults.CreateSeverityOptions();

    var selectedHostGroup = hostGroup ?? await PromptForHostGroupAsync(service, zabbixUrl, apiToken);
    var selectedBeacon = beaconSerialPort ?? PromptForBeacon(service.GetBeaconDevices());

    using var cancellationTokenSource = new CancellationTokenSource();
    Console.CancelKeyPress += (_, args) =>
    {
        Console.WriteLine("Application is shutting down, please wait");
        args.Cancel = true;
        cancellationTokenSource.Cancel();
    };

    Console.Clear();
    Console.WriteLine("Zabbix poll started");

    try
    {
        await service.RunPollingAsync(
            new BeaconRuntimeOptions
            {
                ZabbixUrl = zabbixUrl,
                ApiToken = apiToken,
                BeaconSerialPort = selectedBeacon,
                HostGroupId = selectedHostGroup,
                SeverityOptions = severityOptions,
                QueryIntervalSeconds = queryInterval ?? 15,
                ZeroProblemsBeaconHex = zeroProblemsHex ?? "#01FF01",
                ClearBeaconAfter = clearBeaconAfter ?? 9
            },
            log => Console.WriteLine(log),
            null,
            cancellationTokenSource.Token);
    }
    catch (TaskCanceledException)
    {
    }
});

app.Run();

static string PromptForBeacon(IReadOnlyList<string> availableBeacons)
{
    if (availableBeacons.Count == 0)
    {
        throw new InvalidOperationException("No serial beacon devices were found.");
    }

    Console.Clear();
    Console.WriteLine("Please select beacon:");
    for (var index = 0; index < availableBeacons.Count; index++)
    {
        Console.WriteLine($"{index}> {availableBeacons[index]}");
    }

    while (true)
    {
        var requestedId = Console.ReadLine()?.Trim();
        if (int.TryParse(requestedId, out var parsed) && parsed >= 0 && parsed < availableBeacons.Count)
        {
            Console.WriteLine($"Beacon set as device: {availableBeacons[parsed]}");
            return availableBeacons[parsed];
        }

        Console.WriteLine("ID entered as incorrect, please try again");
    }
}

static async Task<string> PromptForHostGroupAsync(SeverityBeaconService service, Uri zabbixUrl, string apiToken)
{
    Console.WriteLine($"No HostGroup ID provided, obtaining the list from {zabbixUrl}");
    var hostGroups = await service.GetHostGroupsAsync(zabbixUrl, apiToken);
    if (hostGroups.Count == 0)
    {
        throw new InvalidOperationException("No host groups were returned from Zabbix.");
    }

    foreach (var hostGroup in hostGroups)
    {
        Console.WriteLine($"- {hostGroup.Name} - Group ID: {hostGroup.Groupid}");
    }

    Console.WriteLine("Please enter a host group id from the list");

    while (true)
    {
        var requestedId = Console.ReadLine()?.Trim();
        var selected = hostGroups.FirstOrDefault(a => a.Groupid == requestedId);
        if (selected != null)
        {
            Console.Clear();
            Console.WriteLine($"Filtering by HostGroup \"{selected.Name}\"");
            return selected.Groupid;
        }

        Console.WriteLine("ID entered as incorrect, please try again");
    }
}
