namespace SeverityBeacon.Gui.Models;

public class BeaconProfile
{
    public string Name { get; set; } = string.Empty;
    public string ZabbixUrl { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
    public string HostGroupId { get; set; } = string.Empty;
    public string? HostGroupName { get; set; }
    public string? SerialPort { get; set; }
    public int QueryIntervalSeconds { get; set; } = 15;
    public int ClearBeaconAfter { get; set; } = 9;
    public string ZeroProblemsHex { get; set; } = "#01FF01";
    public bool IsDeskLampEnabled { get; set; }
    public string DeskLampHex { get; set; } = "#FFFFFF";
    public List<SeverityProfileEntry> SeverityRules { get; set; } = [];
}
