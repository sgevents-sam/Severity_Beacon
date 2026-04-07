namespace SeverityBeacon.Models;

public class ProblemResults
{
    public List<ProblemResult> Result { get; set; } = [];
}

public class ProblemResult
{
    public string Eventid { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Objectid { get; set; } = string.Empty;
    public string Clock { get; set; } = string.Empty;
    public string Ns { get; set; } = string.Empty;
    public string REventid { get; set; } = string.Empty;
    public string RClock { get; set; } = string.Empty;
    public string RNs { get; set; } = string.Empty;
    public string Correlationid { get; set; } = string.Empty;
    public string Userid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Acknowledged { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string CauseEventid { get; set; } = string.Empty;
    public string Opdata { get; set; } = string.Empty;
    public string Suppressed { get; set; } = string.Empty;
}
