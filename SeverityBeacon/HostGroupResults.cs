namespace SeverityBeacon;

public class HostGroupResults
{
    public List<HostGroupResult> Result { get; set; } = [];
}

public class HostGroupResult
{
    public string Groupid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Flags { get; set; } = string.Empty;
    public string Uuid { get; set; } = string.Empty;
}
