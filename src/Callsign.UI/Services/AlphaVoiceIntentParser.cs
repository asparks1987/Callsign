namespace Callsign.UI.Services;

public enum AlphaVoiceIntentKind
{
    None,
    StartMenuLaunch,
    Browser,
    FileSearch,
    Dictation,
    SystemControl,
    UiNavigation,
    UiAction,
    ExtensionCommand
}

public sealed record AlphaVoiceIntent(
    bool ContainsCallsign,
    string NormalizedCommand,
    AlphaVoiceIntentKind Kind,
    string Target,
    BrowserOpenTarget BrowserTarget = BrowserOpenTarget.Default,
    string ArgumentText = "",
    string PackId = "");

public static class AlphaVoiceIntentParser
{
    public static AlphaVoiceIntent ParseVerifiedTranscript(string transcript, string wakeWord, string callsign)
    {
        var normalizedCommand = AlphaVoiceTranscriptParser.NormalizeLaunchCommand(
            AlphaVoiceTranscriptParser.ExtractCommandFromTranscript(transcript, wakeWord, callsign));
        var containsCallsign = AlphaVoiceTranscriptParser.ContainsSpeechPhrase(transcript, callsign);

        if (string.IsNullOrWhiteSpace(normalizedCommand))
            return new AlphaVoiceIntent(containsCallsign, string.Empty, AlphaVoiceIntentKind.None, string.Empty);

        if (AlphaCommandRouter.TryRouteUiNavigation(normalizedCommand, out var uiTarget))
            return new AlphaVoiceIntent(containsCallsign, normalizedCommand, AlphaVoiceIntentKind.UiNavigation, uiTarget);

        if (AlphaCommandRouter.TryRoute(normalizedCommand, out var route))
        {
            var kind = route.Kind switch
            {
                AlphaCommandKind.Browser => AlphaVoiceIntentKind.Browser,
                AlphaCommandKind.FileSearch => AlphaVoiceIntentKind.FileSearch,
                AlphaCommandKind.Dictation => AlphaVoiceIntentKind.Dictation,
                AlphaCommandKind.SystemControl => AlphaVoiceIntentKind.SystemControl,
                AlphaCommandKind.UiAction => AlphaVoiceIntentKind.UiAction,
                _ => AlphaVoiceIntentKind.None
            };

            return new AlphaVoiceIntent(containsCallsign, normalizedCommand, kind, route.Target, route.BrowserTarget);
        }

        if (Callsign.Extensions.CallsignCommandRegistry.Shared.TryResolve(normalizedCommand, out var command))
        {
            return new AlphaVoiceIntent(
                containsCallsign,
                normalizedCommand,
                AlphaVoiceIntentKind.ExtensionCommand,
                command.CommandId,
                BrowserOpenTarget.Default,
                command.ArgumentText,
                command.PackId);
        }

        var appName = AlphaVoiceTranscriptParser.InferAppName(normalizedCommand);
        if (string.IsNullOrWhiteSpace(appName))
            return new AlphaVoiceIntent(containsCallsign, normalizedCommand, AlphaVoiceIntentKind.None, string.Empty);

        return new AlphaVoiceIntent(
            containsCallsign,
            normalizedCommand,
            AlphaVoiceIntentKind.StartMenuLaunch,
            StartMenuLauncher.ResolveAppName(appName));
    }
}
