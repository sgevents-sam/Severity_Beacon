namespace SeverityBeacon;

public readonly record struct BeaconLog(DateTime Timestamp, string Message)
{
    public override string ToString()
    {
        return $"{Timestamp:T}: {Message}";
    }
}
