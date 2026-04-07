namespace SeverityBeacon;

public sealed class BeaconManualOverrideSession : IDisposable
{
    private readonly TheBeacon _beacon;

    public BeaconManualOverrideSession(string beaconSerialPort, Action<string>? stateChanged = null)
    {
        _beacon = new TheBeacon(beaconSerialPort, "#000000", 0, null, stateChanged);
    }

    public void SetColor(string hexColor)
    {
        _beacon.SetBeaconColour(hexColor);
    }

    public void SetSeverity(SeverityOption option)
    {
        _beacon.SendBeaconIssue(option);
    }

    public void Dispose()
    {
        _beacon.Dispose();
    }
}
