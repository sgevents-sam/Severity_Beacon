namespace SeverityBeacon.Gui.ViewModels;

public class HostGroupItemViewModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string DisplayName => $"{Name} ({Id})";
}
