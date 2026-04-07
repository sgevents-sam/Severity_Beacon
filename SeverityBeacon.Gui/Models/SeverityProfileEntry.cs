namespace SeverityBeacon.Gui.Models;

public class SeverityProfileEntry
{
    public string Name { get; set; } = string.Empty;
    public int SeverityValue { get; set; }
    public string State1 { get; set; } = string.Empty;
    public string? State2 { get; set; }
    public int? Interval1 { get; set; }
    public int? Interval2 { get; set; }
}
