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
    EntitlementRequired,
    SignatureRequired,
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

public enum CallsignCommandVisibilityRequirement
{
    VisibleRequired,
    VisiblePreferred,
    BackgroundAllowedWithApproval
}

public enum CallsignCommandPrivacyImpact
{
    None,
    WindowTitleOrProcess,
    UiText,
    Clipboard,
    FilePath,
    FileContents,
    ScreenshotOrOcr,
    ExternalData
}

public enum CallsignCommandApprovalRequirement
{
    None,
    AskWhenAmbiguous,
    RequireApproval,
    RequireFreshIdentity,
    Blocked
}

public enum CallsignCommandVerificationStrategy
{
    None,
    VisibleStatus,
    StateCheck,
    UiAutomationCheck,
    UserConfirmation
}

public enum CallsignPolicyDecision
{
    Allow,
    Deny,
    RequireApproval,
    RequireFreshIdentity,
    BlockedDangerousAction
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
    bool EnabledByDefault = true,
    string? Category = null,
    CallsignCommandVisibilityRequirement VisibilityRequirement = CallsignCommandVisibilityRequirement.VisibleRequired,
    bool Reversible = true,
    CallsignCommandPrivacyImpact PrivacyImpact = CallsignCommandPrivacyImpact.None,
    CallsignCommandApprovalRequirement ApprovalRequirement = CallsignCommandApprovalRequirement.None,
    string? HelpText = null,
    IReadOnlyList<string>? Examples = null,
    CallsignCommandVerificationStrategy VerificationStrategy = CallsignCommandVerificationStrategy.VisibleStatus);

public sealed record CallsignPolicyEvaluationResult(
    CallsignPolicyDecision Decision,
    string Reason,
    CallsignCommandRiskTier RiskTier,
    CallsignCommandApprovalRequirement ApprovalRequirement,
    bool AuditRequired = true,
    bool VisibleActionRequired = true);

public sealed record CallsignCommandExecutionContext(
    string PackId,
    string CommandId,
    string Transcript,
    string NormalizedCommand,
    string ArgumentText,
    string? Callsign,
    DateTimeOffset RequestedUtc,
    CancellationToken CancellationToken);

public enum CallsignFollowUpStepKind
{
    Command,
    Wait
}

public sealed record CallsignFollowUpStep(
    CallsignFollowUpStepKind Kind,
    string Value = "",
    int DurationMilliseconds = 0);

public sealed record CallsignCommandExecutionResult(
    bool Succeeded,
    string Message,
    string? VisibleAction = null,
    string? AuditEvent = null,
    CallsignPolicyDecision? PolicyDecision = null,
    CallsignCommandApprovalRequirement? PolicyApprovalRequirement = null,
    CallsignCommandRiskTier? PolicyRiskTier = null,
    bool? PolicyVisibleActionRequired = null,
    IReadOnlyList<CallsignFollowUpStep>? FollowUpSteps = null);

public sealed record CallsignEntitlementState(IReadOnlyCollection<CallsignPackTier> EnabledTiers)
{
    public static CallsignEntitlementState FreeOnly { get; } = new(new[] { CallsignPackTier.Free });

    public static CallsignEntitlementState AllTiers { get; } = new(Enum.GetValues<CallsignPackTier>());

    public bool Allows(CallsignPackTier tier) =>
        tier == CallsignPackTier.Free
        || EnabledTiers.Any(enabled => enabled == tier);

    public static CallsignEntitlementState FromTierNames(IEnumerable<string>? tierNames)
    {
        if (tierNames == null)
            return FreeOnly;

        var parsed = new HashSet<CallsignPackTier> { CallsignPackTier.Free };
        foreach (var tierName in tierNames)
        {
            if (string.IsNullOrWhiteSpace(tierName))
                continue;

            if (Enum.TryParse<CallsignPackTier>(tierName.Trim(), true, out var tier))
                parsed.Add(tier);
        }

        return new CallsignEntitlementState(parsed.ToArray());
    }
}

public sealed record CallsignCommandResolution(
    string PackId,
    string PackDisplayName,
    string PackVersion,
    CallsignPackTier Tier,
    CallsignPackLoadStatus LoadStatus,
    bool IsCommunity,
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
    string? SignatureStatus = null,
    bool IsCommunity = false,
    bool RequiresSignature = false,
    string? MinimumCallsignVersion = null,
    string? SourceUri = null);

public sealed record CallsignPackInfo(
    string PackId,
    string DisplayName,
    string Version,
    CallsignPackTier Tier,
    CallsignPackLoadStatus LoadStatus,
    string AssemblyPath,
    int CommandCount,
    string Message,
    DateTimeOffset LoadedUtc,
    bool IsCommunity = false,
    bool WasImported = false,
    string? SignatureStatus = null,
    bool RequiresSignature = false);

public sealed record CallsignPackImportResult(
    bool Succeeded,
    string Message,
    string? SourcePath = null,
    string? InstalledPath = null,
    string? PackId = null,
    CallsignPackLoadStatus? LoadStatus = null);

public sealed record CallsignPackState(
    IReadOnlyCollection<string> DisabledPackIds,
    IReadOnlyCollection<string> DisabledAssemblyPaths)
{
    public CallsignPackState(IReadOnlyCollection<string> DisabledPackIds)
        : this(DisabledPackIds, Array.Empty<string>())
    {
    }

    public static CallsignPackState Empty { get; } = new(Array.Empty<string>());
}

public interface ICallsignCommandPack
{
    CallsignPackDescriptor Descriptor { get; }

    IReadOnlyList<CallsignCommandDefinition> Commands { get; }

    ValueTask<CallsignCommandExecutionResult> ExecuteAsync(CallsignCommandExecutionContext context);
}
