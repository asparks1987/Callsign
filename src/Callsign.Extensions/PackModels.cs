namespace Callsign.Extensions;

public enum CallsignPackTier
{
    Free,
    Pro,
    Advanced
}

public enum CallsignPackLoadStatus
{
    Loaded,
    Disabled,
    MissingAssembly,
    MissingPackType,
    DuplicatePackId,
    InvalidPack,
    LoadFailure
}

public enum CallsignCommandRiskTier
{
    Observe,
    LocalReversible,
    LocalStateChange,
    ExternalSideEffect,
    DangerousOrBlocked
}

public enum CallsignCommandKind
{
    StartMenuLaunch,
    Browser,
    FileSearch,
    Dictation,
    SystemControl,
    UiAction,
    Extension
}

public sealed record CallsignCommandDefinition(
    string CommandId,
    string DisplayName,
    IReadOnlyList<string> VoicePhrases,
    string Description,
    CallsignCommandKind Kind,
    CallsignPackTier Tier,
    CallsignCommandRiskTier RiskTier,
    bool VisibleAction = true,
    string? Target = null,
    bool EnabledByDefault = true);

public sealed record CallsignCommandExecutionContext(
    string PackId,
    string CommandId,
    string Transcript,
    string NormalizedCommand,
    string ArgumentText,
    string? Callsign,
    DateTimeOffset RequestedUtc,
    CancellationToken CancellationToken);

public sealed record CallsignCommandExecutionResult(
    bool Succeeded,
    string Message,
    string? VisibleAction = null,
    string? AuditEvent = null);

public sealed record CallsignCommandResolution(
    string PackId,
    string PackDisplayName,
    string PackVersion,
    CallsignPackTier Tier,
    CallsignPackLoadStatus LoadStatus,
    string CommandId,
    string CommandDisplayName,
    string ArgumentText,
    CallsignCommandDefinition Definition);

public sealed record CallsignPackDescriptor(
    string PackId,
    string DisplayName,
    string Version,
    CallsignPackTier Tier,
    string Description,
    string? SignatureStatus = null);

public sealed record CallsignPackInfo(
    string PackId,
    string DisplayName,
    string Version,
    CallsignPackTier Tier,
    CallsignPackLoadStatus LoadStatus,
    string AssemblyPath,
    int CommandCount,
    string Message,
    DateTimeOffset LoadedUtc);

public sealed record CallsignPackState(
    IReadOnlyCollection<string> DisabledPackIds)
{
    public static CallsignPackState Empty { get; } = new(Array.Empty<string>());
}

public interface ICallsignCommandPack
{
    CallsignPackDescriptor Descriptor { get; }

    IReadOnlyList<CallsignCommandDefinition> Commands { get; }

    ValueTask<CallsignCommandExecutionResult> ExecuteAsync(CallsignCommandExecutionContext context);
}
