namespace Callsign.UI.Services;

public enum UserRuntimeOwnershipState
{
    Started,
    AlreadyRunningAuthoritative,
    AlreadyRunningNonAuthoritative,
    Unavailable
}

public sealed record UserRuntimeOwnershipDecision(
    UserRuntimeOwnershipState State,
    string Message);

public static class RuntimeOwnershipService
{
    public static UserRuntimeOwnershipDecision EvaluateStart(
        bool runtimeExeExists,
        RuntimeStateSnapshot? runtimeSnapshot,
        int runningProcessCount)
    {
        if (!runtimeExeExists)
            return new UserRuntimeOwnershipDecision(
                UserRuntimeOwnershipState.Unavailable,
                "Installed user runtime was not found; using local preview listener.");

        var runtimeIsFresh = runtimeSnapshot != null
            && string.Equals(runtimeSnapshot.RuntimeRole, "user-runtime", StringComparison.OrdinalIgnoreCase)
            && runtimeSnapshot.IsListening
            && DateTime.UtcNow - runtimeSnapshot.UpdatedUtc.ToUniversalTime() <= TimeSpan.FromSeconds(15);
        var runtimeCanHearAudio = runtimeIsFresh && runtimeSnapshot?.CanHearAudio == true;

        if (runtimeCanHearAudio && runningProcessCount > 0)
        {
            return new UserRuntimeOwnershipDecision(
                UserRuntimeOwnershipState.AlreadyRunningAuthoritative,
                "Background user runtime is already authoritative and hearing audio.");
        }

        if (runningProcessCount > 0 && runtimeSnapshot != null)
        {
            if (runtimeSnapshot.CanHearAudio == false)
            {
                return new UserRuntimeOwnershipDecision(
                    UserRuntimeOwnershipState.AlreadyRunningNonAuthoritative,
                    "Background user runtime is already running but not hearing microphone audio yet. Use the Session tab to verify the listener or restart the runtime.");
            }

            return new UserRuntimeOwnershipDecision(
                UserRuntimeOwnershipState.AlreadyRunningAuthoritative,
                "Background user runtime is already running as the authoritative listener. Watch the Session tab for fresh user-runtime status.");
        }

        return new UserRuntimeOwnershipDecision(
            UserRuntimeOwnershipState.Started,
            "Requested background user runtime start. Watch the Session tab for authoritative user-runtime status.");
    }
}
