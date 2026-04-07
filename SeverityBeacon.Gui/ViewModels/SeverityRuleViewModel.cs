using SeverityBeacon;
using SeverityBeacon.Gui.Models;

namespace SeverityBeacon.Gui.ViewModels;

public class SeverityRuleViewModel : ViewModelBase
{
    private string _state1;
    private string? _state2;
    private int? _interval1;
    private int? _interval2;

    public SeverityRuleViewModel(string name, SeverityOption option)
    {
        Name = name;
        SeverityValue = option.ZabbixSeverityValue;
        _state1 = option.BeaconHexColourState1;
        _state2 = option.BeaconHexColourState2;
        _interval1 = option.BeaconChangeStateInterval1;
        _interval2 = option.BeaconChangeStateInterval2;
    }

    public string Name { get; }
    public int SeverityValue { get; }

    public string State1
    {
        get => _state1;
        set => SetProperty(ref _state1, value);
    }

    public string? State2
    {
        get => _state2;
        set => SetProperty(ref _state2, value);
    }

    public int? Interval1
    {
        get => _interval1;
        set => SetProperty(ref _interval1, value);
    }

    public int? Interval2
    {
        get => _interval2;
        set => SetProperty(ref _interval2, value);
    }

    public SeverityOption ToSeverityOption()
    {
        return new SeverityOption(State1, SeverityValue, EmptyToNull(State2), Interval1, Interval2);
    }

    public SeverityProfileEntry ToProfileEntry()
    {
        return new SeverityProfileEntry
        {
            Name = Name,
            SeverityValue = SeverityValue,
            State1 = State1,
            State2 = EmptyToNull(State2),
            Interval1 = Interval1,
            Interval2 = Interval2
        };
    }

    public void ApplySeverityOption(SeverityOption option)
    {
        State1 = option.BeaconHexColourState1;
        State2 = option.BeaconHexColourState2;
        Interval1 = option.BeaconChangeStateInterval1;
        Interval2 = option.BeaconChangeStateInterval2;
    }

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
