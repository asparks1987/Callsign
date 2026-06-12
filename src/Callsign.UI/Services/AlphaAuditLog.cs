using System.Text.Json;
using Callsign.UI.Models;

namespace Callsign.UI.Services;

public sealed class AlphaAuditLog
{
    private readonly ProfileStore _profileStore;

    public AlphaAuditLog(ProfileStore profileStore)
    {
        _profileStore = profileStore;
    }

    public bool TryRecordStartMenuLaunch(UserProfile profile, string appName, out string? warning)
    {
        warning = null;
        if (string.IsNullOrWhiteSpace(profile.Callsign) || string.IsNullOrWhiteSpace(appName))
            return true;

        try
        {
            var folder = _profileStore.ResolveCallsSignFolder(profile.Callsign);
            Directory.CreateDirectory(folder);

            var entry = new
            {
                event_type = "alpha.start_menu_launch",
                timestamp_utc = DateTime.UtcNow,
                callsign = profile.Callsign,
                app_name = appName.Trim(),
                launch_path = "start_menu_search"
            };

            File.AppendAllText(
                Path.Combine(folder, "alpha-audit.jsonl"),
                JsonSerializer.Serialize(entry) + Environment.NewLine);
            return true;
        }
        catch (Exception ex)
        {
            warning = $"Launch succeeded, but the local alpha audit log could not be written: {ex.Message}";
            return false;
        }
    }
}
