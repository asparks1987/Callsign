namespace Callsign.UI.Services;

public enum AlphaSessionState
{
    Idle,
    WaitingForIdentity,
    WaitingForCommand,
    ReadyToLaunch,
    Launching,
    Completed,
    LockedOut
}

public sealed class AlphaSessionStateMachine
{
    public const string AudioWakeDetectorSource = "audio-wake-detector";
    public const string ManualUiSource = "manual-ui";
    public const string ScriptedTranscriptControlSource = "scripted-transcript-control";

    private readonly TimeSpan _commandTimeout;
    private readonly TimeSpan _lockoutDuration;

    public AlphaSessionStateMachine(TimeSpan? commandTimeout = null, TimeSpan? lockoutDuration = null)
    {
        _commandTimeout = commandTimeout ?? TimeSpan.FromSeconds(15);
        _lockoutDuration = lockoutDuration ?? TimeSpan.FromSeconds(20);
        Reset();
    }

    public AlphaSessionState State { get; private set; }
    public string StatusMessage { get; private set; } = "Idle.";
    public string? VerifiedCallsign { get; private set; }
    public DateTimeOffset? VerifiedCallsignUtc { get; private set; }
    public string? PendingCommand { get; private set; }
    public string? PendingApp { get; private set; }
    public string? LastWakeTransitionSource { get; private set; }
    public DateTime StateEnteredUtc { get; private set; }
    public DateTime? LockedUntilUtc { get; private set; }

    public void Reset()
    {
        State = AlphaSessionState.Idle;
        StatusMessage = "Idle.";
        VerifiedCallsign = null;
        VerifiedCallsignUtc = null;
        PendingCommand = null;
        PendingApp = null;
        LastWakeTransitionSource = null;
        StateEnteredUtc = DateTime.UtcNow;
        LockedUntilUtc = null;
    }

    public void DetectWakeWord(string? source = AudioWakeDetectorSource)
    {
        Tick();
        if (State == AlphaSessionState.LockedOut)
        {
            StatusMessage = GetLockedOutMessage();
            return;
        }

        State = AlphaSessionState.WaitingForIdentity;
        StateEnteredUtc = DateTime.UtcNow;
        LastWakeTransitionSource = NormalizeWakeTransitionSource(source);
        StatusMessage = "Wake word detected. Say your callsign to continue.";
    }

    public bool TryVerifyIdentity(string spokenCallsign, string enrolledCallsign, bool voiceEnrolled, out string message)
    {
        Tick();
        if (State != AlphaSessionState.WaitingForIdentity)
        {
            message = "Wake word first.";
            return false;
        }

        if (!voiceEnrolled)
        {
            message = "Voice is not enrolled for this profile.";
            Cancel(message);
            return false;
        }

        var spoken = Normalize(spokenCallsign);
        var enrolled = Normalize(enrolledCallsign);
        if (string.IsNullOrWhiteSpace(spoken) || string.IsNullOrWhiteSpace(enrolled))
        {
            message = "A callsign is required.";
            Cancel(message);
            return false;
        }

        if (!string.Equals(spoken, enrolled, StringComparison.OrdinalIgnoreCase))
        {
            LockedUntilUtc = DateTime.UtcNow.Add(_lockoutDuration);
            State = AlphaSessionState.LockedOut;
            StateEnteredUtc = DateTime.UtcNow;
            StatusMessage = $"Identity mismatch. Locked out for {_lockoutDuration.TotalSeconds:0} seconds.";
            message = StatusMessage;
            return false;
        }

        VerifiedCallsign = enrolled;
        VerifiedCallsignUtc = DateTimeOffset.UtcNow;
        State = AlphaSessionState.WaitingForCommand;
        StateEnteredUtc = DateTime.UtcNow;
        StatusMessage = $"Identity verified for {enrolled}. Speak the task.";
        message = StatusMessage;
        return true;
    }

    public bool TryVerifyIdentity(CallsignIdentityResult identity, string enrolledCallsign, bool voiceEnrolled, bool requireBiometric, out string message)
    {
        Tick();
        if (State != AlphaSessionState.WaitingForIdentity)
        {
            message = "Wake word first.";
            return false;
        }

        if (!voiceEnrolled)
        {
            message = "Voice is not enrolled for this profile.";
            Cancel(message);
            return false;
        }

        if (!identity.Accepted)
        {
            if (string.Equals(identity.RejectReason, "identity_mismatch", StringComparison.OrdinalIgnoreCase))
            {
                LockedUntilUtc = DateTime.UtcNow.Add(_lockoutDuration);
                State = AlphaSessionState.LockedOut;
                StateEnteredUtc = DateTime.UtcNow;
                StatusMessage = $"Identity mismatch. Locked out for {_lockoutDuration.TotalSeconds:0} seconds.";
                message = StatusMessage;
                return false;
            }

            StatusMessage = identity.RetryPrompt ?? "Say your callsign again.";
            message = StatusMessage;
            return false;
        }

        if (requireBiometric && identity.Biometric?.Accepted != true)
        {
            StatusMessage = identity.Biometric == null
                ? "Voice biometric proof is required before command capture."
                : $"Voice biometric rejected: {identity.Biometric.RejectReason ?? "biometric_mismatch"}";
            message = StatusMessage;
            return false;
        }

        VerifiedCallsign = Normalize(identity.MatchedVariant ?? enrolledCallsign);
        VerifiedCallsignUtc = DateTimeOffset.UtcNow;
        State = AlphaSessionState.WaitingForCommand;
        StateEnteredUtc = DateTime.UtcNow;
        StatusMessage = $"Identity verified for {VerifiedCallsign}. Speak the task.";
        message = StatusMessage;
        return true;
    }

    public bool TryCaptureCommand(string commandText, out string message)
    {
        Tick();
        if (State != AlphaSessionState.WaitingForCommand)
        {
            message = "Verify identity first.";
            return false;
        }

        var command = commandText.Trim();
        if (string.IsNullOrWhiteSpace(command))
        {
            message = "Speak the command before continuing.";
            return false;
        }

        PendingCommand = command;
        PendingApp = InferAppName(command);
        State = AlphaSessionState.ReadyToLaunch;
        StateEnteredUtc = DateTime.UtcNow;
        StatusMessage = string.IsNullOrWhiteSpace(PendingApp)
            ? "Command captured. Choose an app name to launch."
            : $"Command captured for '{PendingApp}'. Ready to launch.";
        message = StatusMessage;
        return true;
    }

    public bool TryBeginLaunch(string appName, out string message)
    {
        Tick();
        if (State != AlphaSessionState.ReadyToLaunch)
        {
            message = "Capture the command first.";
            return false;
        }

        var target = string.IsNullOrWhiteSpace(appName) ? PendingApp : appName.Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
            message = "Enter an app name to launch.";
            return false;
        }

        PendingApp = target;
        State = AlphaSessionState.Launching;
        StateEnteredUtc = DateTime.UtcNow;
        StatusMessage = $"Launching {target} from Start menu.";
        message = StatusMessage;
        return true;
    }

    public void CompleteLaunch()
    {
        State = AlphaSessionState.Completed;
        StateEnteredUtc = DateTime.UtcNow;
        StatusMessage = "Launch complete.";
    }

    public void FailLaunch(string reason)
    {
        PendingApp = null;
        State = AlphaSessionState.ReadyToLaunch;
        StateEnteredUtc = DateTime.UtcNow;
        StatusMessage = reason;
    }

    public void Cancel(string reason)
    {
        State = AlphaSessionState.Idle;
        StateEnteredUtc = DateTime.UtcNow;
        StatusMessage = reason;
        PendingCommand = null;
        PendingApp = null;
        VerifiedCallsign = null;
        VerifiedCallsignUtc = null;
        LastWakeTransitionSource = null;
    }

    public void Tick()
    {
        if (State == AlphaSessionState.LockedOut)
        {
            if (LockedUntilUtc.HasValue && DateTime.UtcNow >= LockedUntilUtc.Value)
                Reset();
            return;
        }

        if (State is AlphaSessionState.Idle or AlphaSessionState.Completed or AlphaSessionState.Launching)
            return;

        if (DateTime.UtcNow - StateEnteredUtc <= _commandTimeout)
            return;

        LockedUntilUtc = DateTime.UtcNow.Add(_lockoutDuration);
        State = AlphaSessionState.LockedOut;
        StateEnteredUtc = DateTime.UtcNow;
        StatusMessage = $"Session timed out. Locked out for {_lockoutDuration.TotalSeconds:0} seconds.";
    }

    public TimeSpan? GetLockoutRemaining()
    {
        if (State != AlphaSessionState.LockedOut || !LockedUntilUtc.HasValue)
            return null;

        var remaining = LockedUntilUtc.Value - DateTime.UtcNow;
        return remaining <= TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    public bool HasFreshIdentity(TimeSpan freshness)
    {
        if (string.IsNullOrWhiteSpace(VerifiedCallsign) || !VerifiedCallsignUtc.HasValue)
            return false;

        if (freshness <= TimeSpan.Zero)
            return true;

        return DateTimeOffset.UtcNow - VerifiedCallsignUtc.Value <= freshness;
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return string.Join(
            ' ',
            value.ToLowerInvariant()
                .Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string NormalizeWakeTransitionSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return AudioWakeDetectorSource;

        return source.Trim();
    }

    private string GetLockedOutMessage()
    {
        var remaining = GetLockoutRemaining();
        if (remaining == null)
            return "Locked out.";

        return $"Locked out for {Math.Ceiling(remaining.Value.TotalSeconds):0} seconds.";
    }

    private static string? InferAppName(string command)
    {
        var normalized = command.Trim();
        var prefixes = new[]
        {
            "launch the application called ",
            "launch the application named ",
            "launch the app called ",
            "launch the app named ",
            "launch application ",
            "launch app ",
            "launch the application ",
            "launch the app ",
            "open the application called ",
            "open the application named ",
            "open the app called ",
            "open the app named ",
            "open application ",
            "open app ",
            "open the application ",
            "open the app ",
            "open up ",
            "open up the app ",
            "open up the application ",
            "launch ",
            "open ",
            "start ",
            "run "
        };
        foreach (var prefix in prefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return TrimPoliteSuffix(normalized[prefix.Length..].Trim());
        }

        return TrimPoliteSuffix(normalized);
    }

    private static string TrimPoliteSuffix(string value)
    {
        foreach (var suffix in new[] { " please", " thanks", " thank you" })
        {
            if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return value[..^suffix.Length].Trim();
        }

        return value;
    }
}
