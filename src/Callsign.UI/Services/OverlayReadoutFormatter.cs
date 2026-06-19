using Callsign.UI.Models;

namespace Callsign.UI.Services;

public static class OverlayReadoutFormatter
{
    public static string FormatPhase(AlphaSessionState state) =>
        state switch
        {
            AlphaSessionState.WaitingForIdentity => "Identity",
            AlphaSessionState.WaitingForCommand => "Command",
            AlphaSessionState.ReadyToLaunch => "Ready",
            AlphaSessionState.Launching => "Launching",
            _ => "Listening"
        };

    public static string FormatReadout(
        AlphaSessionState state,
        string? transcript = null,
        float? transcriptConfidence = null,
        string? verifiedCallsign = null,
        string? pendingCommand = null,
        string? pendingApp = null,
        string? identityRetryPrompt = null,
        bool speechActive = false,
        string? dictationTranscript = null,
        bool dictationActive = false)
    {
        var heard = string.IsNullOrWhiteSpace(transcript) ? null : transcript.Trim();
        var dictation = string.IsNullOrWhiteSpace(dictationTranscript) ? null : dictationTranscript.Trim();
        var heardWithConfidence = heard != null && transcriptConfidence.HasValue
            ? $"Heard: {heard} ({transcriptConfidence.Value:P0})"
            : heard != null
                ? $"Heard: {heard}"
                : null;
        var commandWithConfidence = heard != null && transcriptConfidence.HasValue
            ? $"Command: {heard} ({transcriptConfidence.Value:P0})"
            : heard != null
                ? $"Command: {heard}"
                : null;

        return state switch
        {
            _ when dictationActive && dictation != null => $"Dictation: {dictation}",
            _ when dictationActive && speechActive => "Hearing dictation...",
            _ when dictationActive => "Dictation is waiting for speech.",
            AlphaSessionState.WaitingForIdentity when heardWithConfidence != null => heardWithConfidence,
            AlphaSessionState.WaitingForIdentity when speechActive => "Hearing your callsign...",
            AlphaSessionState.WaitingForIdentity => "Callsign heard. Say your callsign.",
            AlphaSessionState.WaitingForCommand when commandWithConfidence != null => commandWithConfidence,
            AlphaSessionState.WaitingForCommand when speechActive && !string.IsNullOrWhiteSpace(verifiedCallsign) => "Hearing your command...",
            AlphaSessionState.WaitingForCommand when !string.IsNullOrWhiteSpace(verifiedCallsign) => "Identity confirmed. Say the command.",
            AlphaSessionState.WaitingForCommand => identityRetryPrompt ?? "Identity confirmed. Say the command.",
            AlphaSessionState.ReadyToLaunch when !string.IsNullOrWhiteSpace(pendingCommand) => $"Command: {pendingCommand.Trim()}",
            AlphaSessionState.ReadyToLaunch when commandWithConfidence != null => commandWithConfidence,
            AlphaSessionState.ReadyToLaunch => "Command ready.",
            AlphaSessionState.Launching when !string.IsNullOrWhiteSpace(pendingApp) => $"Launching {pendingApp.Trim()}...",
            AlphaSessionState.Launching when !string.IsNullOrWhiteSpace(pendingCommand) => $"Launching {pendingCommand.Trim()}...",
            AlphaSessionState.Launching when heard != null => $"Launching {heard}...",
            AlphaSessionState.Launching => "Launching...",
            _ when speechActive => "Hearing speech...",
            _ when heardWithConfidence != null => heardWithConfidence,
            _ => "Listening."
        };
    }
}
