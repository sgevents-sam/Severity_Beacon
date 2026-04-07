using System.Text.Json;
using SeverityBeacon.Gui.Models;

namespace SeverityBeacon.Gui.Services;

public class ProfileStore
{
    private sealed class AppState
    {
        public string LastUsedProfileName { get; set; } = string.Empty;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _profilesPath;
    private readonly string _appStatePath;

    public ProfileStore()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directoryPath = Path.Combine(appDataPath, "SeverityBeacon");
        Directory.CreateDirectory(directoryPath);
        _profilesPath = Path.Combine(directoryPath, "profiles.json");
        _appStatePath = Path.Combine(directoryPath, "app-state.json");
    }

    public async Task<IReadOnlyList<BeaconProfile>> LoadProfilesAsync()
    {
        if (!File.Exists(_profilesPath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_profilesPath);
        var profiles = await JsonSerializer.DeserializeAsync<List<BeaconProfile>>(stream, JsonOptions);
        return profiles ?? [];
    }

    public async Task SaveProfileAsync(BeaconProfile profile)
    {
        var profiles = (await LoadProfilesAsync()).ToList();
        var existingIndex = profiles.FindIndex(existing => string.Equals(existing.Name, profile.Name, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
        {
            profiles[existingIndex] = profile;
        }
        else
        {
            profiles.Add(profile);
        }

        await SaveAllAsync(profiles.OrderBy(existing => existing.Name).ToList());
    }

    public async Task DeleteProfileAsync(string profileName)
    {
        var profiles = (await LoadProfilesAsync())
            .Where(profile => !string.Equals(profile.Name, profileName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        await SaveAllAsync(profiles);
    }

    public async Task<string?> LoadLastUsedProfileNameAsync()
    {
        if (!File.Exists(_appStatePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(_appStatePath);
        var appState = await JsonSerializer.DeserializeAsync<AppState>(stream, JsonOptions);
        return string.IsNullOrWhiteSpace(appState?.LastUsedProfileName)
            ? null
            : appState.LastUsedProfileName;
    }

    public async Task SaveLastUsedProfileNameAsync(string? profileName)
    {
        var appState = new AppState
        {
            LastUsedProfileName = profileName?.Trim() ?? string.Empty
        };

        await using var stream = File.Create(_appStatePath);
        await JsonSerializer.SerializeAsync(stream, appState, JsonOptions);
    }

    private async Task SaveAllAsync(List<BeaconProfile> profiles)
    {
        await using var stream = File.Create(_profilesPath);
        await JsonSerializer.SerializeAsync(stream, profiles, JsonOptions);
    }
}
