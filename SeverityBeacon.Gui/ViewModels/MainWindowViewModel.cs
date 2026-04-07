using System.Collections.ObjectModel;
using System.Text;
using Avalonia.Media;
using Avalonia.Threading;
using SeverityBeacon;
using SeverityBeacon.Gui.Models;
using SeverityBeacon.Gui.Services;

namespace SeverityBeacon.Gui.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private const string DefaultZeroProblemsHex = "#01FF01";
    private const string DefaultDeskLampHex = "#FFFFFF";
    private static readonly Dictionary<string, SeverityOption> DefaultSeverityOptions = SeverityDefaults.CreateSeverityOptions();

    private readonly SeverityBeaconService _service = new(new HttpClient());
    private readonly ProfileStore _profileStore = new();
    private BeaconManualOverrideSession? _manualOverrideSession;
    private CancellationTokenSource? _pollingCancellation;
    private Task? _pollingTask;
    private string _zabbixUrl = string.Empty;
    private string _apiToken = string.Empty;
    private string _zeroProblemsHex = DefaultZeroProblemsHex;
    private string _deskLampHex = DefaultDeskLampHex;
    private string _manualOverrideHex = "#FFFFFF";
    private string _statusMessage = "Idle";
    private string _logText = "Choose a saved profile or open settings to configure one.";
    private string _lastError = string.Empty;
    private string _profileName = string.Empty;
    private string _currentBeaconHex = "#000000";
    private string? _pendingHostGroupId;
    private string? _activeOverrideSeverityName;
    private int _queryIntervalSeconds = 15;
    private int _clearBeaconAfter = 9;
    private bool _isRunning;
    private bool _isDeskLampEnabled;
    private bool _isManualOverrideEnabled;
    private bool _isSettingsOpen;
    private bool _isUpdatingDeskLampColor;
    private bool _isUpdatingManualOverrideColor;
    private bool _hasInitialized;
    private string? _selectedSerialPort;
    private HostGroupItemViewModel? _selectedHostGroup;
    private ProfileItemViewModel? _selectedProfile;
    private Color _deskLampColor = Colors.White;
    private Color _manualOverrideColor = Colors.White;
    private Color _currentBeaconColor = Colors.Black;

    public MainWindowViewModel()
    {
        ResetSeverityRules(CloneDefaultSeverityOptions());
    }

    public ObservableCollection<string> SerialPorts { get; } = [];
    public ObservableCollection<HostGroupItemViewModel> HostGroups { get; } = [];
    public ObservableCollection<ProfileItemViewModel> Profiles { get; } = [];
    public ObservableCollection<SeverityRuleViewModel> SeverityRules { get; } = [];

    public string ZabbixUrl
    {
        get => _zabbixUrl;
        set => SetProperty(ref _zabbixUrl, value);
    }

    public string ApiToken
    {
        get => _apiToken;
        set => SetProperty(ref _apiToken, value);
    }

    public string ZeroProblemsHex
    {
        get => _zeroProblemsHex;
        set => SetProperty(ref _zeroProblemsHex, value);
    }

    public string DeskLampHex
    {
        get => _deskLampHex;
        set
        {
            if (!SetProperty(ref _deskLampHex, value))
            {
                return;
            }

            if (_isUpdatingDeskLampColor)
            {
                return;
            }

            if (TryParseHexColor(value, out var color))
            {
                _isUpdatingDeskLampColor = true;
                DeskLampColor = color;
                _isUpdatingDeskLampColor = false;
            }
        }
    }

    public Color DeskLampColor
    {
        get => _deskLampColor;
        set
        {
            if (!SetProperty(ref _deskLampColor, value))
            {
                return;
            }

            if (_isUpdatingDeskLampColor)
            {
                return;
            }

            _isUpdatingDeskLampColor = true;
            DeskLampHex = ToHex(value);
            _isUpdatingDeskLampColor = false;
        }
    }

    public string ManualOverrideHex
    {
        get => _manualOverrideHex;
        set
        {
            if (!SetProperty(ref _manualOverrideHex, value))
            {
                return;
            }

            if (_isUpdatingManualOverrideColor)
            {
                return;
            }

            if (TryParseHexColor(value, out var color))
            {
                _isUpdatingManualOverrideColor = true;
                ManualOverrideColor = color;
                _isUpdatingManualOverrideColor = false;
                _ = ApplyManualColorIfLiveAsync();
            }
        }
    }

    public Color ManualOverrideColor
    {
        get => _manualOverrideColor;
        set
        {
            if (!SetProperty(ref _manualOverrideColor, value))
            {
                return;
            }

            if (_isUpdatingManualOverrideColor)
            {
                return;
            }

            _isUpdatingManualOverrideColor = true;
            ManualOverrideHex = ToHex(value);
            _isUpdatingManualOverrideColor = false;
            _ = ApplyManualColorIfLiveAsync();
        }
    }

    public Color CurrentBeaconColor
    {
        get => _currentBeaconColor;
        private set => SetProperty(ref _currentBeaconColor, value);
    }

    public string CurrentBeaconHex
    {
        get => _currentBeaconHex;
        private set => SetProperty(ref _currentBeaconHex, value);
    }

    public string? ActiveOverrideSeverityName
    {
        get => _activeOverrideSeverityName;
        private set
        {
            if (SetProperty(ref _activeOverrideSeverityName, value))
            {
                RaisePropertyChanged(nameof(HasActiveOverride));
                RaisePropertyChanged(nameof(ActiveOverrideDisplay));
            }
        }
    }

    public bool HasActiveOverride => !string.IsNullOrWhiteSpace(ActiveOverrideSeverityName);

    public string ActiveOverrideDisplay => HasActiveOverride
        ? $"Active override: {ActiveOverrideSeverityName}"
        : string.Empty;

    public int QueryIntervalSeconds
    {
        get => _queryIntervalSeconds;
        set => SetProperty(ref _queryIntervalSeconds, value);
    }

    public int ClearBeaconAfter
    {
        get => _clearBeaconAfter;
        set => SetProperty(ref _clearBeaconAfter, value);
    }

    public bool IsDeskLampEnabled
    {
        get => _isDeskLampEnabled;
        set => SetProperty(ref _isDeskLampEnabled, value);
    }

    public bool IsManualOverrideEnabled
    {
        get => _isManualOverrideEnabled;
        private set
        {
            if (SetProperty(ref _isManualOverrideEnabled, value))
            {
                RaisePropertyChanged(nameof(IsManualOverrideInactive));
            }
        }
    }

    public bool IsManualOverrideInactive => !IsManualOverrideEnabled;

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set => SetProperty(ref _isSettingsOpen, value);
    }

    public string ProfileName
    {
        get => _profileName;
        set => SetProperty(ref _profileName, value);
    }

    public ProfileItemViewModel? SelectedProfile
    {
        get => _selectedProfile;
        set => SetProperty(ref _selectedProfile, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string LogText
    {
        get => _logText;
        private set => SetProperty(ref _logText, value);
    }

    public string LastError
    {
        get => _lastError;
        private set
        {
            if (SetProperty(ref _lastError, value))
            {
                RaisePropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(LastError);

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                RaisePropertyChanged(nameof(PollingToggleLabel));
            }
        }
    }

    public string PollingToggleLabel => IsRunning ? "Stop Polling" : "Start Polling";

    public string? SelectedSerialPort
    {
        get => _selectedSerialPort;
        set => SetProperty(ref _selectedSerialPort, value);
    }

    public HostGroupItemViewModel? SelectedHostGroup
    {
        get => _selectedHostGroup;
        set => SetProperty(ref _selectedHostGroup, value);
    }

    public async Task InitializeAsync()
    {
        if (_hasInitialized)
        {
            return;
        }

        _hasInitialized = true;
        await RefreshProfilesAsync();
        await RefreshSerialPortsAsync();
        SetCurrentBeaconHex("#000000");

        var lastUsedProfileName = await _profileStore.LoadLastUsedProfileNameAsync();
        if (string.IsNullOrWhiteSpace(lastUsedProfileName))
        {
            return;
        }

        SelectedProfile = Profiles.FirstOrDefault(profile =>
            string.Equals(profile.Profile.Name, lastUsedProfileName, StringComparison.OrdinalIgnoreCase));

        if (SelectedProfile == null)
        {
            return;
        }

        await LoadSelectedProfileAsync();
        await StartAsync();
    }

    public Task RefreshSerialPortsAsync()
    {
        try
        {
            LastError = string.Empty;
            var ports = _service.GetBeaconDevices();
            SerialPorts.Clear();
            foreach (var port in ports)
            {
                SerialPorts.Add(port);
            }

            if (!string.IsNullOrWhiteSpace(SelectedSerialPort) && SerialPorts.Contains(SelectedSerialPort))
            {
                return Task.CompletedTask;
            }

            if (!string.IsNullOrWhiteSpace(_selectedProfile?.Profile.SerialPort) && SerialPorts.Contains(_selectedProfile.Profile.SerialPort))
            {
                SelectedSerialPort = _selectedProfile.Profile.SerialPort;
            }
            else
            {
                SelectedSerialPort = SerialPorts.FirstOrDefault();
            }

            StatusMessage = SerialPorts.Count == 0 ? "No beacon ports found" : "Ports refreshed";
        }
        catch (Exception exception)
        {
            SetError(exception);
        }

        return Task.CompletedTask;
    }

    public async Task RefreshHostGroupsAsync()
    {
        try
        {
            LastError = string.Empty;
            if (!Uri.TryCreate(ZabbixUrl, UriKind.Absolute, out var zabbixUri))
            {
                throw new InvalidOperationException("Enter a valid Zabbix URL before loading host groups.");
            }

            if (string.IsNullOrWhiteSpace(ApiToken))
            {
                throw new InvalidOperationException("Enter the Zabbix API token before loading host groups.");
            }

            StatusMessage = "Loading host groups";
            var hostGroups = await _service.GetHostGroupsAsync(zabbixUri, ApiToken);

            HostGroups.Clear();
            foreach (var hostGroup in hostGroups.OrderBy(group => group.Name))
            {
                HostGroups.Add(new HostGroupItemViewModel
                {
                    Id = hostGroup.Groupid,
                    Name = hostGroup.Name
                });
            }

            if (!string.IsNullOrWhiteSpace(_pendingHostGroupId))
            {
                SelectedHostGroup = HostGroups.FirstOrDefault(group => group.Id == _pendingHostGroupId) ?? HostGroups.FirstOrDefault();
                _pendingHostGroupId = null;
            }
            else
            {
                SelectedHostGroup ??= HostGroups.FirstOrDefault();
            }

            StatusMessage = HostGroups.Count == 0 ? "No host groups returned" : "Host groups refreshed";
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
    }

    public async Task RefreshProfilesAsync()
    {
        try
        {
            var profiles = await _profileStore.LoadProfilesAsync();
            Profiles.Clear();
            foreach (var profile in profiles.OrderBy(profile => profile.Name))
            {
                Profiles.Add(new ProfileItemViewModel { Profile = profile });
            }
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
    }

    public async Task SaveProfileAsync()
    {
        try
        {
            LastError = string.Empty;
            if (string.IsNullOrWhiteSpace(ProfileName))
            {
                throw new InvalidOperationException("Enter a profile name before saving.");
            }

            var profile = new BeaconProfile
            {
                Name = ProfileName.Trim(),
                ZabbixUrl = ZabbixUrl.Trim(),
                ApiToken = ApiToken.Trim(),
                HostGroupId = SelectedHostGroup?.Id ?? _pendingHostGroupId ?? string.Empty,
                HostGroupName = SelectedHostGroup?.Name,
                SerialPort = SelectedSerialPort,
                QueryIntervalSeconds = QueryIntervalSeconds,
                ClearBeaconAfter = ClearBeaconAfter,
                ZeroProblemsHex = ZeroProblemsHex.Trim(),
                IsDeskLampEnabled = IsDeskLampEnabled,
                DeskLampHex = DeskLampHex.Trim(),
                SeverityRules = SeverityRules.Select(rule => rule.ToProfileEntry()).ToList()
            };

            await _profileStore.SaveProfileAsync(profile);
            await _profileStore.SaveLastUsedProfileNameAsync(profile.Name);
            await RefreshProfilesAsync();
            SelectedProfile = Profiles.FirstOrDefault(item => string.Equals(item.Profile.Name, profile.Name, StringComparison.OrdinalIgnoreCase));
            StatusMessage = "Profile saved";
            AppendLog($"Saved profile '{profile.Name}'.");
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
    }

    public async Task LoadSelectedProfileAsync()
    {
        try
        {
            LastError = string.Empty;
            if (SelectedProfile == null)
            {
                return;
            }

            ApplyProfile(SelectedProfile.Profile);
            await _profileStore.SaveLastUsedProfileNameAsync(SelectedProfile.Profile.Name);
            StatusMessage = "Profile loaded";
            AppendLog($"Loaded profile '{SelectedProfile.Profile.Name}'.");

            if (Uri.TryCreate(ZabbixUrl, UriKind.Absolute, out _) && !string.IsNullOrWhiteSpace(ApiToken))
            {
                await RefreshHostGroupsAsync();
            }

            await RefreshSerialPortsAsync();
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
    }

    public async Task DeleteSelectedProfileAsync()
    {
        try
        {
            LastError = string.Empty;
            if (SelectedProfile == null)
            {
                throw new InvalidOperationException("Select a profile to delete.");
            }

            var profileName = SelectedProfile.Profile.Name;
            await _profileStore.DeleteProfileAsync(profileName);
            var deletedWasLastUsed = string.Equals(
                await _profileStore.LoadLastUsedProfileNameAsync(),
                profileName,
                StringComparison.OrdinalIgnoreCase);
            SelectedProfile = null;
            await RefreshProfilesAsync();
            if (deletedWasLastUsed)
            {
                await _profileStore.SaveLastUsedProfileNameAsync(Profiles.FirstOrDefault()?.Profile.Name);
            }
            StatusMessage = "Profile deleted";
            AppendLog($"Deleted profile '{profileName}'.");
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
    }

    public async Task TogglePollingAsync()
    {
        if (IsRunning)
        {
            await StopAsync();
            return;
        }

        await StartAsync();
    }

    public async Task StartAsync()
    {
        try
        {
            LastError = string.Empty;
            await ClearManualOverrideSessionAsync();

            if (!Uri.TryCreate(ZabbixUrl, UriKind.Absolute, out var zabbixUri))
            {
                throw new InvalidOperationException("A valid Zabbix URL is required.");
            }

            if (string.IsNullOrWhiteSpace(ApiToken))
            {
                throw new InvalidOperationException("A Zabbix API token is required.");
            }

            if (string.IsNullOrWhiteSpace(SelectedSerialPort))
            {
                throw new InvalidOperationException("Select a serial port for the beacon.");
            }

            if (SelectedHostGroup == null)
            {
                throw new InvalidOperationException("Select a host group before starting.");
            }

            var options = new BeaconRuntimeOptions
            {
                ZabbixUrl = zabbixUri,
                ApiToken = ApiToken.Trim(),
                BeaconSerialPort = SelectedSerialPort,
                HostGroupId = SelectedHostGroup.Id,
                SeverityOptions = SeverityRules.ToDictionary(rule => rule.Name, rule => rule.ToSeverityOption(), StringComparer.OrdinalIgnoreCase),
                QueryIntervalSeconds = QueryIntervalSeconds,
                ZeroProblemsBeaconHex = ZeroProblemsHex.Trim(),
                ClearBeaconAfter = ClearBeaconAfter,
                DeskLampBeaconHex = IsDeskLampEnabled ? DeskLampHex.Trim() : null
            };

            _pollingCancellation = new CancellationTokenSource();
            IsRunning = true;
            StatusMessage = "Polling";
            ActiveOverrideSeverityName = null;
            AppendLog($"Starting polling for host group {SelectedHostGroup.DisplayName} on port {SelectedSerialPort}.");

            _pollingTask = Task.Run(async () =>
            {
                try
                {
                    await _service.RunPollingAsync(
                        options,
                        log => AppendLog(log.ToString()),
                        UpdateCurrentBeacon,
                        _pollingCancellation.Token);
                }
                catch (TaskCanceledException)
                {
                    AppendLog("Polling stopped.");
                }
                catch (Exception exception)
                {
                    Dispatcher.UIThread.Post(() => SetError(exception));
                }
                finally
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        IsRunning = false;
                        StatusMessage = "Idle";
                    });
                }
            });

            await Task.CompletedTask;
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
    }

    public async Task StopAsync()
    {
        try
        {
            StatusMessage = "Stopping";
            _pollingCancellation?.Cancel();
            if (_pollingTask != null)
            {
                await _pollingTask;
            }

            if (!string.IsNullOrWhiteSpace(SelectedSerialPort))
            {
                await _service.SetStaticColorAsync(SelectedSerialPort, "#000000", UpdateCurrentBeacon);
            }

            SetCurrentBeaconHex("#000000");
            StatusMessage = "Stopped";
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
    }

    public async Task SendManualOverrideColorAsync()
    {
        try
        {
            LastError = string.Empty;
            if (!IsManualOverrideEnabled)
            {
                await SetManualOverrideEnabledAsync(true);
            }
            await EnsureManualOverrideSessionAsync();
            _manualOverrideSession!.SetColor(ManualOverrideHex.Trim());
            ActiveOverrideSeverityName = null;
            AppendLog($"Manual override colour set to {NormalizeHex(ManualOverrideHex)}.");
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
    }

    public async Task ToggleSeverityOverrideAsync(string severityName)
    {
        try
        {
            LastError = string.Empty;
            if (!IsManualOverrideEnabled)
            {
                await SetManualOverrideEnabledAsync(true);
            }
            await EnsureManualOverrideSessionAsync();

            if (string.Equals(ActiveOverrideSeverityName, severityName, StringComparison.OrdinalIgnoreCase))
            {
                _manualOverrideSession!.SetColor(ManualOverrideHex.Trim());
                ActiveOverrideSeverityName = null;
                AppendLog($"Manual severity override '{severityName}' cleared.");
                return;
            }

            var rule = SeverityRules.FirstOrDefault(item => string.Equals(item.Name, severityName, StringComparison.OrdinalIgnoreCase));
            if (rule == null)
            {
                throw new InvalidOperationException($"Severity '{severityName}' is not configured.");
            }

            _manualOverrideSession!.SetSeverity(rule.ToSeverityOption());
            ActiveOverrideSeverityName = severityName;
            AppendLog($"Manual severity override '{severityName}' applied.");
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
    }

    public async Task SetManualOverrideEnabledAsync(bool enabled)
    {
        try
        {
            LastError = string.Empty;

            if (enabled)
            {
                await EnsureManualOverrideSessionAsync();
                IsManualOverrideEnabled = true;
                ActiveOverrideSeverityName = null;
                _manualOverrideSession!.SetColor(ManualOverrideHex.Trim());
                AppendLog($"Manual override enabled at {NormalizeHex(ManualOverrideHex)}.");
                return;
            }

            if (_manualOverrideSession != null)
            {
                _manualOverrideSession.SetColor("#000000");
            }

            await ClearManualOverrideSessionAsync();
            IsManualOverrideEnabled = false;
            SetCurrentBeaconHex("#000000");
            StatusMessage = "Idle";
            AppendLog("Manual override disabled.");
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
    }

    private async Task EnsureManualOverrideSessionAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedSerialPort))
        {
            throw new InvalidOperationException("Select a serial port before using manual override.");
        }

        if (IsRunning)
        {
            await StopAsync();
        }

        if (_manualOverrideSession != null)
        {
            return;
        }

        _manualOverrideSession = _service.CreateManualOverrideSession(SelectedSerialPort, UpdateCurrentBeacon);
        IsManualOverrideEnabled = true;
        StatusMessage = "Manual override";
    }

    private Task ClearManualOverrideSessionAsync()
    {
        _manualOverrideSession?.Dispose();
        _manualOverrideSession = null;
        ActiveOverrideSeverityName = null;
        IsManualOverrideEnabled = false;
        return Task.CompletedTask;
    }

    private async Task ApplyManualColorIfLiveAsync()
    {
        if (!IsManualOverrideEnabled || HasActiveOverride)
        {
            return;
        }

        try
        {
            await EnsureManualOverrideSessionAsync();
            _manualOverrideSession!.SetColor(ManualOverrideHex.Trim());
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
    }

    private void ApplyProfile(BeaconProfile profile)
    {
        ProfileName = profile.Name;
        ZabbixUrl = profile.ZabbixUrl;
        ApiToken = profile.ApiToken;
        QueryIntervalSeconds = profile.QueryIntervalSeconds;
        ClearBeaconAfter = profile.ClearBeaconAfter;
        ZeroProblemsHex = profile.ZeroProblemsHex;
        IsDeskLampEnabled = profile.IsDeskLampEnabled;
        DeskLampHex = string.IsNullOrWhiteSpace(profile.DeskLampHex) ? DefaultDeskLampHex : profile.DeskLampHex;
        SelectedSerialPort = profile.SerialPort;
        _pendingHostGroupId = profile.HostGroupId;
        SelectedHostGroup = HostGroups.FirstOrDefault(group => group.Id == profile.HostGroupId);

        var severityMap = profile.SeverityRules.Count > 0
            ? profile.SeverityRules.ToDictionary(
                entry => entry.Name,
                entry => new SeverityOption(entry.State1, entry.SeverityValue, entry.State2, entry.Interval1, entry.Interval2),
                StringComparer.OrdinalIgnoreCase)
            : CloneDefaultSeverityOptions();

        ResetSeverityRules(severityMap);
    }

    public void RestoreAllColorDefaults()
    {
        ZeroProblemsHex = DefaultZeroProblemsHex;
        RestoreDeskLampDefault();
        ResetSeverityRules(CloneDefaultSeverityOptions());
        AppendLog("Restored all colour defaults.");
    }

    public void RestoreDeskLampDefault()
    {
        DeskLampHex = DefaultDeskLampHex;
    }

    public void RestoreAllClearDefault()
    {
        ZeroProblemsHex = DefaultZeroProblemsHex;
    }

    public void RestoreSeverityDefault(string severityName)
    {
        if (!DefaultSeverityOptions.TryGetValue(severityName, out var defaultOption))
        {
            return;
        }

        var rule = SeverityRules.FirstOrDefault(item => string.Equals(item.Name, severityName, StringComparison.OrdinalIgnoreCase));
        rule?.ApplySeverityOption(defaultOption);
    }

    private void ResetSeverityRules(Dictionary<string, SeverityOption> severityOptions)
    {
        SeverityRules.Clear();
        foreach (var severity in severityOptions.OrderByDescending(entry => entry.Value.ZabbixSeverityValue))
        {
            SeverityRules.Add(new SeverityRuleViewModel(severity.Key, severity.Value));
        }
    }

    private void UpdateCurrentBeacon(string hexColor)
    {
        Dispatcher.UIThread.Post(() => SetCurrentBeaconHex(hexColor));
    }

    private void SetCurrentBeaconHex(string hexColor)
    {
        CurrentBeaconHex = NormalizeHex(hexColor);
        if (TryParseHexColor(CurrentBeaconHex, out var color))
        {
            CurrentBeaconColor = color;
        }
    }

    private void AppendLog(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var builder = new StringBuilder(LogText);
            if (!string.IsNullOrWhiteSpace(LogText))
            {
                builder.AppendLine();
            }

            builder.Append(message);
            LogText = builder.ToString();
        });
    }

    private void SetError(Exception exception)
    {
        LastError = exception.Message;
        IsRunning = false;
        StatusMessage = "Attention needed";
        AppendLog($"Error: {exception.Message}");
    }

    private static bool TryParseHexColor(string? hex, out Color color)
    {
        color = Colors.White;
        if (string.IsNullOrWhiteSpace(hex))
        {
            return false;
        }

        var normalized = NormalizeHex(hex);
        return Color.TryParse(normalized, out color);
    }

    private static string NormalizeHex(string hex)
    {
        var trimmed = hex.Trim();
        return trimmed.StartsWith('#') ? trimmed.ToUpperInvariant() : $"#{trimmed.ToUpperInvariant()}";
    }

    private static string ToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static Dictionary<string, SeverityOption> CloneDefaultSeverityOptions()
    {
        return DefaultSeverityOptions.ToDictionary(
            entry => entry.Key,
            entry => new SeverityOption(
                entry.Value.BeaconHexColourState1,
                entry.Value.ZabbixSeverityValue,
                entry.Value.BeaconHexColourState2,
                entry.Value.BeaconChangeStateInterval1,
                entry.Value.BeaconChangeStateInterval2),
            StringComparer.OrdinalIgnoreCase);
    }
}
