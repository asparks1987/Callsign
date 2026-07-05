using System.Text.Json;
using Callsign.UI.Models;

namespace Callsign.UI.Services;

public sealed class AlphaAuditLog
{
    private readonly ProfileStore _profileStore;
    private const string AuditFileName = "alpha-audit.jsonl";

    public AlphaAuditLog(ProfileStore profileStore)
    {
        _profileStore = profileStore;
    }

    public bool TryRecordStartMenuLaunch(
        UserProfile profile,
        string appName,
        out string? warning,
        string? launchPath = null,
        bool visibleStartMenuPath = true)
    {
        return TryRecordCommand(
            profile,
            eventType: "alpha.command_execution",
            actionName: "start_menu_launch",
            status: "succeeded",
            out warning,
            commandFamily: "start_menu",
            actionTarget: appName,
            launchPath: string.IsNullOrWhiteSpace(launchPath) ? "start_menu_search" : launchPath,
            success: true,
            verificationMethod: visibleStartMenuPath ? "visible_start_menu" : "state_check",
            verificationSummary: visibleStartMenuPath
                ? "Launch used the visible Start menu search path."
                : "Launch completed outside the visible Start menu path and must not be counted as Start menu parity proof.");
    }

    public bool TryRecordCommand(
        UserProfile profile,
        string eventType,
        string actionName,
        string status,
        out string? warning,
        string? commandFamily = null,
        string? actionTarget = null,
        string? launchPath = null,
        string? details = null,
        bool? success = null,
        string? correlationId = null,
        string? verificationMethod = null,
        string? verificationSummary = null)
    {
        warning = null;
        if (string.IsNullOrWhiteSpace(profile.Callsign) || string.IsNullOrWhiteSpace(actionName))
            return true;

        var verificationPerformed = !string.IsNullOrWhiteSpace(verificationMethod)
            || !string.IsNullOrWhiteSpace(verificationSummary);
        return TryWriteRecord(profile.Callsign, new
        {
            event_type = eventType,
            timestamp_utc = DateTime.UtcNow,
            correlation_id = string.IsNullOrWhiteSpace(correlationId)
                ? $"audit_{Guid.NewGuid():N}"
                : correlationId,
            callsign = profile.Callsign,
            command_family = commandFamily,
            action_name = actionName,
            status,
            action_target = actionTarget,
            launch_path = launchPath,
            details,
            success,
            verification = new
            {
                performed = verificationPerformed,
                method = verificationMethod ?? "not_recorded",
                summary = verificationSummary ?? (success == true ? "Command reached its visible execution path." : "Verification was not recorded.")
            },
            audit_source = "ui_client"
        }, out warning);
    }

    private bool TryWriteRecord(string callsign, object entry, out string? warning)
    {
        warning = null;
        try
        {
            var folder = _profileStore.ResolveCallsSignFolder(callsign);
            Directory.CreateDirectory(folder);

            File.AppendAllText(
                Path.Combine(folder, AuditFileName),
                JsonSerializer.Serialize(entry) + Environment.NewLine);
            return true;
        }
        catch (Exception ex)
        {
            warning = $"Audit write failed: {ex.Message}";
            return false;
        }
    }
}
