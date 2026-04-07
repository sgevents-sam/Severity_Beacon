namespace SeverityBeacon;

public class BeaconRuntimeOptions
{
    public required Uri ZabbixUrl { get; init; }
    public required string ApiToken { get; init; }
    public required string BeaconSerialPort { get; init; }
    public required string HostGroupId { get; init; }
    public required Dictionary<string, SeverityOption> SeverityOptions { get; init; }
    public int QueryIntervalSeconds { get; init; } = 15;
    public string ZeroProblemsBeaconHex { get; init; } = "#01FF01";
    public int ClearBeaconAfter { get; init; } = 9;
    public string? DeskLampBeaconHex { get; init; }
}
