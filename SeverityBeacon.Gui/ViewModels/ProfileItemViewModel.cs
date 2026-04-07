using SeverityBeacon.Gui.Models;

namespace SeverityBeacon.Gui.ViewModels;

public class ProfileItemViewModel
{
    public required BeaconProfile Profile { get; init; }
    public string DisplayName => Profile.Name;
}
