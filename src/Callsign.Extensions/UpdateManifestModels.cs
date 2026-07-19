namespace Callsign.Extensions;

public sealed record CallsignUpdateManifest(
    string Version,
    string InstallerUrl,
    string InstallerSha256,
    long InstallerSizeBytes,
    string ReleaseNotes,
    IReadOnlyList<CallsignUpdateCommandChange>? AddedCommands = null,
    IReadOnlyList<CallsignUpdateCommandChange>? ChangedCommands = null,
    IReadOnlyList<CallsignUpdateCommandChange>? RemovedCommands = null,
    IReadOnlyList<CallsignUpdateExtensionChange>? ExtensionPackChanges = null,
    string? SplashSummary = null,
    DateTimeOffset? PublishedUtc = null,
    IReadOnlyList<CallsignUpdateFeatureChange>? FeatureHighlights = null);

public sealed record CallsignUpdateCommandChange(
    string CommandId,
    string DisplayName,
    string Category,
    string Summary,
    CallsignPackTier Tier = CallsignPackTier.Free);

public sealed record CallsignUpdateExtensionChange(
    string PackId,
    string DisplayName,
    string Version,
    CallsignPackTier Tier,
    string Summary,
    string? SignatureStatus = null,
    bool IsCommunity = false);

public sealed record CallsignUpdateFeatureChange(
    string FeatureId,
    string DisplayName,
    string Category,
    string Summary);
