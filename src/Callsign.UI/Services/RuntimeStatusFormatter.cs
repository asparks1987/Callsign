namespace Callsign.UI.Services;

public static class RuntimeStatusFormatter
{
    public static string FormatOwnershipProof(RuntimeStateSnapshot? snapshot, int runningServiceProcessCount)
    {
        var processCount = runningServiceProcessCount < 0
            ? "unknown"
            : runningServiceProcessCount.ToString();

        if (snapshot == null)
            return $"Runtime owner: no service snapshot yet; Callsign.Service process count={processCount}.";

        var role = string.IsNullOrWhiteSpace(snapshot.RuntimeRole)
            ? "unknown role"
            : snapshot.RuntimeRole.Trim();
        var authority = string.IsNullOrWhiteSpace(snapshot.RuntimeAuthorityStatus)
            ? "unknown authority"
            : snapshot.RuntimeAuthorityStatus.Trim();
        var pid = snapshot.CurrentProcessId.HasValue
            ? snapshot.CurrentProcessId.Value.ToString()
            : "unknown";
        var started = snapshot.ProcessStartedUtc.HasValue
            ? snapshot.ProcessStartedUtc.Value.ToLocalTime().ToString("g")
            : "unknown";
        var freshness = DateTime.UtcNow - snapshot.UpdatedUtc.ToUniversalTime() <= TimeSpan.FromSeconds(30)
            ? "fresh"
            : "stale";
        var mutexNote = string.Equals(role, "user-runtime", StringComparison.OrdinalIgnoreCase)
            ? "duplicate user-runtime launches exit through Local\\Callsign.UserRuntime"
            : "supervisor cannot own the user microphone session";

        return $"Runtime owner: {authority}; role={role}; PID={pid}; started={started}; snapshot={freshness}; Callsign.Service process count={processCount}; {mutexNote}.";
    }

    public static string FormatHearingProof(RuntimeStateSnapshot snapshot)
    {
        var canHearAudio = snapshot.CanHearAudio.HasValue
            ? snapshot.CanHearAudio.Value ? "true" : "false"
            : "unknown";
        var device = string.IsNullOrWhiteSpace(snapshot.ActiveMicrophoneDeviceName)
            ? "unknown microphone"
            : snapshot.ActiveMicrophoneDeviceName.Trim();
        var packetAge = snapshot.SecondsSinceLastAudioPacket.HasValue
            ? $"{snapshot.SecondsSinceLastAudioPacket.Value:0.0}s"
            : "unknown";
        var packetState = HasRecentAudioPacket(snapshot)
            ? "recent audio packets"
            : snapshot.SecondsSinceLastAudioPacket.HasValue
                ? "no recent audio packets"
                : "packet timing unavailable";
        var authority = string.IsNullOrWhiteSpace(snapshot.RuntimeAuthorityStatus)
            ? string.IsNullOrWhiteSpace(snapshot.RuntimeRole) ? "unknown authority" : snapshot.RuntimeRole.Trim()
            : snapshot.RuntimeAuthorityStatus.Trim();
        var mode = string.IsNullOrWhiteSpace(snapshot.ModeDescription)
            ? "mode unknown"
            : snapshot.ModeDescription.Trim();

        return $"Runtime proof: CanHearAudio={canHearAudio}; mic={device}; packet age={packetAge}; {packetState}; authority={authority}; mode={mode}.";
    }

    public static string FormatMicLevel(RuntimeStateSnapshot snapshot)
    {
        if (snapshot.LastMicrophoneLevelState == null)
            return "Microphone telemetry unavailable.";

        if (snapshot.CanHearAudio == false && snapshot.IsListening)
            return HasRecentAudioPacket(snapshot)
                ? "Runtime is receiving microphone packets, but speech is below the active threshold."
                : "Runtime running but no microphone audio packets are arriving.";

        if (snapshot.CanHearAudio == true && string.Equals(snapshot.RuntimeAuthorityStatus, "authoritative-user-runtime", StringComparison.OrdinalIgnoreCase))
            return $"Microphone level: {snapshot.LastMicrophoneLevelState}. Authoritative runtime is hearing audio.";

        return snapshot.LastWakeWordScore.HasValue && snapshot.WakeWordThreshold.HasValue
            ? snapshot.LastWakeWordScore.Value >= snapshot.WakeWordThreshold.Value
                ? $"Microphone level: {snapshot.LastMicrophoneLevelState}. Wake candidate passed threshold."
                : $"Microphone level: {snapshot.LastMicrophoneLevelState}. Wake candidate heard but below threshold."
            : $"Microphone level: {snapshot.LastMicrophoneLevelState}.";
    }

    public static bool HasRecentAudioPacket(RuntimeStateSnapshot snapshot) =>
        snapshot.SecondsSinceLastAudioPacket.HasValue
            && snapshot.SecondsSinceLastAudioPacket.Value <= 2.5;

    public static string FormatAuthority(RuntimeStateSnapshot? runtimeSnapshot, bool isListening, bool usingLocalPreviewListener)
    {
        if (runtimeSnapshot != null)
        {
            if (!string.IsNullOrWhiteSpace(runtimeSnapshot.RuntimeAuthorityStatus))
            {
                var status = runtimeSnapshot.RuntimeAuthorityStatus.Trim();
                return string.Equals(status, "authoritative-user-runtime", StringComparison.OrdinalIgnoreCase)
                    ? runtimeSnapshot.CanHearAudio == true
                        ? "Authoritative user runtime hearing audio"
                        : "Authoritative user runtime running but silent"
                    : status;
            }

            return runtimeSnapshot.IsListening
                ? runtimeSnapshot.CanHearAudio == true
                    ? "Background service hearing audio"
                    : "Background service running but silent"
                : "Background service idle";
        }

        if (isListening)
            return usingLocalPreviewListener
                ? "Local preview listener"
                : "Authoritative user runtime";

        return "Idle";
    }
}
