using System.Text.Json;
using Callsign.UI.Models;

namespace Callsign.UI.Services;

public sealed class ProfileStore
{
    private const string ProfilesFolderName = "Profiles";
    private const string SettingsFileName = "settings.json";
    private const string LegacyFileName = "profile.json";

    private readonly string _rootPath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public ProfileStore()
    {
        _rootPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            ProfilesFolderName);

        Directory.CreateDirectory(_rootPath);
    }

    public IReadOnlyList<UserProfile> GetProfiles()
    {
        if (!Directory.Exists(_rootPath))
            return Array.Empty<UserProfile>();

        var profiles = new List<UserProfile>();
        foreach (var profileDirectory in Directory.EnumerateDirectories(_rootPath))
        {
            var profile = ReadProfileFromDirectory(profileDirectory);
            if (profile != null)
                profiles.Add(profile);
        }

        return profiles
            .OrderBy(profile => profile.Callsign, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public UserProfile? Load(string callsign)
    {
        var file = ResolveProfilePath(callsign);
        return ReadProfile(file);
    }

    public void Save(UserProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (string.IsNullOrWhiteSpace(profile.Callsign))
            throw new ArgumentException("Callsign is required.", nameof(profile));

        var callsignFolder = ResolveCallsSignFolder(profile.Callsign);
        Directory.CreateDirectory(callsignFolder);

        var now = DateTime.UtcNow;
        profile.UpdatedUtc = now;
        if (profile.CreatedUtc == default)
            profile.CreatedUtc = now;

        profile.Callsign = NormalizeCallsign(profile.Callsign);
        var json = JsonSerializer.Serialize(profile, _jsonOptions);
        File.WriteAllText(Path.Combine(callsignFolder, SettingsFileName), json);
    }

    public void Delete(string callsign)
    {
        var callsignFolder = ResolveCallsSignFolder(callsign);
        if (Directory.Exists(callsignFolder))
            Directory.Delete(callsignFolder, recursive: true);
    }

    public bool Exists(string callsign) => Directory.Exists(ResolveCallsSignFolder(callsign));

    public string ResolveCallsSignFolder(string callsign)
    {
        var safeCallsign = SanitizeFolderName(callsign);
        return Path.Combine(_rootPath, safeCallsign);
    }

    private string ResolveProfilePath(string callsign) =>
        Path.Combine(ResolveCallsSignFolder(callsign), SettingsFileName);

    private UserProfile? ReadProfile(string profileFile)
    {
        if (!File.Exists(profileFile))
            return null;

        try
        {
            var raw = File.ReadAllText(profileFile);
            var profile = JsonSerializer.Deserialize<UserProfile>(raw, _jsonOptions);
            if (profile == null)
                return null;

            profile.Settings ??= new UserSettings();
            if (string.IsNullOrWhiteSpace(profile.Settings.DashboardTitle))
                profile.Settings.DashboardTitle = "Callsign";
            if (string.IsNullOrWhiteSpace(profile.Settings.VoiceEnrollmentStatus))
                profile.Settings.VoiceEnrollmentStatus = "Not enrolled";
            profile.Settings.VoiceSamplesRequired = Math.Max(1, profile.Settings.VoiceSamplesRequired);
            profile.Settings.VoiceSamplesRecorded = Math.Max(0, profile.Settings.VoiceSamplesRecorded);
            profile.Callsign = NormalizeCallsign(Path.GetFileName(Path.GetDirectoryName(profileFile)));
            return profile;
        }
        catch
        {
            return null;
        }
    }

    private UserProfile? ReadProfileFromDirectory(string profileDirectory)
    {
        var callsign = Path.GetFileName(profileDirectory);
        if (string.IsNullOrWhiteSpace(callsign))
            return null;

        var primary = Path.Combine(profileDirectory, SettingsFileName);
        var legacy = Path.Combine(profileDirectory, LegacyFileName);

        return ReadProfile(primary) ?? ReadProfile(legacy);
    }

    private string SanitizeFolderName(string callsign)
    {
        var normalized = NormalizeCallsign(callsign);
        foreach (var ch in Path.GetInvalidPathChars())
        {
            normalized = normalized.Replace(ch, '-');
        }

        foreach (var ch in Path.GetInvalidFileNameChars())
        {
            normalized = normalized.Replace(ch, '-');
        }

        return string.IsNullOrWhiteSpace(normalized) ? throw new ArgumentException("Callsign resolves to an empty folder name.") : normalized;
    }

    private static string NormalizeCallsign(string? callsign) =>
        callsign?.Trim().ToLowerInvariant() ?? string.Empty;
}
