using System.Text.Json;
using Callsign.Extensions;

namespace Callsign.UI.Services;

public enum VoiceShortcutActionKind
{
    Command,
    Wait
}

public sealed record VoiceShortcutAction(
    VoiceShortcutActionKind Kind,
    string Value = "",
    int DurationMilliseconds = 0);

public sealed record VoiceShortcutDefinition(
    string ShortcutId,
    string Title,
    string WhenISay,
    string Group,
    bool Enabled,
    IReadOnlyList<VoiceShortcutAction> Actions,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);

public sealed record VoiceShortcutSaveResult(
    bool Succeeded,
    string Message,
    VoiceShortcutDefinition? Shortcut = null);

public static class VoiceShortcutConstants
{
    public const string PackId = "voice-shortcuts";
    public const string PackDisplayName = "Voice Shortcuts";
    public const int MaxActionsPerShortcut = 8;
    public const int MinWaitMilliseconds = 100;
    public const int MaxWaitMilliseconds = 30000;
}

public sealed class VoiceShortcutStore
{
    private const string ShortcutFolderName = "VoiceShortcuts";
    private const string ShortcutFileName = "shortcuts.json";

    private readonly string _rootPath;
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public VoiceShortcutStore(string? rootPath = null)
    {
        _rootPath = rootPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            ShortcutFolderName);
        _filePath = Path.Combine(_rootPath, ShortcutFileName);
        Directory.CreateDirectory(_rootPath);
    }

    public IReadOnlyList<VoiceShortcutDefinition> GetShortcuts()
    {
        try
        {
            if (!File.Exists(_filePath))
                return Array.Empty<VoiceShortcutDefinition>();

            var shortcuts = JsonSerializer.Deserialize<List<VoiceShortcutDefinition>>(File.ReadAllText(_filePath), _jsonOptions);
            if (shortcuts == null)
                return Array.Empty<VoiceShortcutDefinition>();

            return shortcuts
                .Select(Normalize)
                .Where(shortcut => shortcut != null)
                .Cast<VoiceShortcutDefinition>()
                .OrderBy(shortcut => shortcut.Title, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            return Array.Empty<VoiceShortcutDefinition>();
        }
    }

    public VoiceShortcutDefinition CreateDraft() =>
        new(
            ShortcutId: $"shortcut-{Guid.NewGuid():N}",
            Title: string.Empty,
            WhenISay: string.Empty,
            Group: "General",
            Enabled: true,
            Actions: Array.Empty<VoiceShortcutAction>(),
            CreatedUtc: DateTimeOffset.UtcNow,
            UpdatedUtc: DateTimeOffset.UtcNow);

    public VoiceShortcutSaveResult Save(VoiceShortcutDefinition shortcut, Func<string, bool>? commandValidator = null)
    {
        var shortcuts = GetShortcuts().ToList();
        var normalized = Normalize(shortcut);
        if (normalized == null)
            return new VoiceShortcutSaveResult(false, "The shortcut could not be normalized.");

        var validation = Validate(
            normalized,
            shortcuts.Where(existing => !string.Equals(existing.ShortcutId, normalized.ShortcutId, StringComparison.OrdinalIgnoreCase)),
            commandValidator);
        if (!validation.Succeeded)
            return validation;

        var existingIndex = shortcuts.FindIndex(existing => string.Equals(existing.ShortcutId, normalized.ShortcutId, StringComparison.OrdinalIgnoreCase));
        var createdUtc = existingIndex >= 0
            ? shortcuts[existingIndex].CreatedUtc
            : (normalized.CreatedUtc == default ? DateTimeOffset.UtcNow : normalized.CreatedUtc);
        normalized = normalized with
        {
            CreatedUtc = createdUtc,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        if (existingIndex >= 0)
            shortcuts[existingIndex] = normalized;
        else
            shortcuts.Add(normalized);

        Persist(shortcuts);
        return new VoiceShortcutSaveResult(true, "Voice shortcut saved.", normalized);
    }

    public bool Delete(string shortcutId, out string message)
    {
        message = "Voice shortcut deleted.";
        if (string.IsNullOrWhiteSpace(shortcutId))
        {
            message = "Select a shortcut first.";
            return false;
        }

        var shortcuts = GetShortcuts().ToList();
        var removed = shortcuts.RemoveAll(shortcut => string.Equals(shortcut.ShortcutId, shortcutId, StringComparison.OrdinalIgnoreCase));
        if (removed == 0)
        {
            message = "The selected shortcut was not found.";
            return false;
        }

        Persist(shortcuts);
        return true;
    }

    public bool SetEnabled(string shortcutId, bool enabled, out string message)
    {
        message = enabled ? "Voice shortcut enabled." : "Voice shortcut disabled.";
        if (string.IsNullOrWhiteSpace(shortcutId))
        {
            message = "Select a shortcut first.";
            return false;
        }

        var shortcuts = GetShortcuts().ToList();
        var index = shortcuts.FindIndex(shortcut => string.Equals(shortcut.ShortcutId, shortcutId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            message = "The selected shortcut was not found.";
            return false;
        }

        shortcuts[index] = shortcuts[index] with
        {
            Enabled = enabled,
            UpdatedUtc = DateTimeOffset.UtcNow
        };
        Persist(shortcuts);
        return true;
    }

    public static VoiceShortcutSaveResult Validate(
        VoiceShortcutDefinition shortcut,
        IEnumerable<VoiceShortcutDefinition>? otherShortcuts = null,
        Func<string, bool>? commandValidator = null)
    {
        if (string.IsNullOrWhiteSpace(shortcut.Title))
            return new VoiceShortcutSaveResult(false, "Shortcut title is required.");

        if (string.IsNullOrWhiteSpace(shortcut.WhenISay))
            return new VoiceShortcutSaveResult(false, "The 'When I say' phrase is required.");

        if (shortcut.Actions == null || shortcut.Actions.Count == 0)
            return new VoiceShortcutSaveResult(false, "Add at least one shortcut action.");

        if (shortcut.Actions.Count > VoiceShortcutConstants.MaxActionsPerShortcut)
            return new VoiceShortcutSaveResult(false, $"Voice shortcuts are limited to {VoiceShortcutConstants.MaxActionsPerShortcut} actions.");

        var normalizedPhrase = AlphaVoiceTranscriptParser.NormalizeSpeechText(shortcut.WhenISay);
        if (string.IsNullOrWhiteSpace(normalizedPhrase))
            return new VoiceShortcutSaveResult(false, "The shortcut phrase must contain letters or numbers.");

        foreach (var action in shortcut.Actions)
        {
            switch (action.Kind)
            {
                case VoiceShortcutActionKind.Command:
                    if (string.IsNullOrWhiteSpace(action.Value))
                        return new VoiceShortcutSaveResult(false, "Each command action needs a spoken command.");
                    if (string.Equals(AlphaVoiceTranscriptParser.NormalizeSpeechText(action.Value), normalizedPhrase, StringComparison.OrdinalIgnoreCase))
                        return new VoiceShortcutSaveResult(false, "A voice shortcut cannot run itself as a command step.");
                    if (commandValidator != null && !commandValidator(action.Value))
                        return new VoiceShortcutSaveResult(false, $"Shortcut command step could not be resolved by Callsign: '{action.Value}'.");
                    break;
                case VoiceShortcutActionKind.Wait:
                    if (action.DurationMilliseconds < VoiceShortcutConstants.MinWaitMilliseconds
                        || action.DurationMilliseconds > VoiceShortcutConstants.MaxWaitMilliseconds)
                    {
                        return new VoiceShortcutSaveResult(
                            false,
                            $"Wait actions must be between {VoiceShortcutConstants.MinWaitMilliseconds} and {VoiceShortcutConstants.MaxWaitMilliseconds} milliseconds.");
                    }
                    break;
            }
        }

        if (otherShortcuts != null)
        {
            var otherShortcutList = otherShortcuts.ToArray();
            foreach (var other in otherShortcutList)
            {
                if (string.Equals(AlphaVoiceTranscriptParser.NormalizeSpeechText(other.WhenISay), normalizedPhrase, StringComparison.OrdinalIgnoreCase))
                    return new VoiceShortcutSaveResult(false, "Another voice shortcut already uses that spoken phrase.");
            }

            if (HasShortcutCycle(shortcut, otherShortcutList))
                return new VoiceShortcutSaveResult(false, "Voice shortcuts cannot call each other in a loop.");
        }

        return new VoiceShortcutSaveResult(true, "Voice shortcut is valid.", shortcut);
    }

    private static bool HasShortcutCycle(VoiceShortcutDefinition shortcut, IReadOnlyCollection<VoiceShortcutDefinition> otherShortcuts)
    {
        var allShortcuts = otherShortcuts
            .Append(shortcut)
            .Select(Normalize)
            .Where(candidate => candidate != null)
            .Cast<VoiceShortcutDefinition>()
            .ToArray();
        var startId = shortcut.ShortcutId;
        if (string.IsNullOrWhiteSpace(startId))
            return false;

        var phraseToShortcutId = allShortcuts
            .Select(candidate => new
            {
                candidate.ShortcutId,
                Phrase = AlphaVoiceTranscriptParser.NormalizeSpeechText(candidate.WhenISay)
            })
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.ShortcutId) && !string.IsNullOrWhiteSpace(candidate.Phrase))
            .GroupBy(candidate => candidate.Phrase, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(candidate => candidate.ShortcutId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var commandEdges = allShortcuts.ToDictionary(
            candidate => candidate.ShortcutId,
            candidate => candidate.Actions
                .Where(action => action.Kind == VoiceShortcutActionKind.Command)
                .Select(action => AlphaVoiceTranscriptParser.NormalizeSpeechText(action.Value))
                .Where(command => !string.IsNullOrWhiteSpace(command))
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return VisitsStart(startId);

        bool VisitsStart(string shortcutId)
        {
            if (!visited.Add(shortcutId))
                return false;

            active.Add(shortcutId);
            if (commandEdges.TryGetValue(shortcutId, out var commands))
            {
                foreach (var command in commands)
                {
                    if (!phraseToShortcutId.TryGetValue(command, out var nextShortcutIds))
                        continue;

                    foreach (var nextShortcutId in nextShortcutIds)
                    {
                        if (string.Equals(nextShortcutId, startId, StringComparison.OrdinalIgnoreCase))
                            return true;
                        if (active.Contains(nextShortcutId))
                            return true;
                        if (VisitsStart(nextShortcutId))
                            return true;
                    }
                }
            }

            active.Remove(shortcutId);
            return false;
        }
    }

    private void Persist(IEnumerable<VoiceShortcutDefinition> shortcuts)
    {
        Directory.CreateDirectory(_rootPath);
        File.WriteAllText(
            _filePath,
            JsonSerializer.Serialize(
                shortcuts
                    .Select(Normalize)
                    .Where(shortcut => shortcut != null)
                    .Cast<VoiceShortcutDefinition>()
                    .OrderBy(shortcut => shortcut.Title, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                _jsonOptions));
    }

    private static VoiceShortcutDefinition? Normalize(VoiceShortcutDefinition? shortcut)
    {
        if (shortcut == null)
            return null;

        var actions = (shortcut.Actions ?? Array.Empty<VoiceShortcutAction>())
            .Select(action => action.Kind switch
            {
                VoiceShortcutActionKind.Wait => new VoiceShortcutAction(
                    VoiceShortcutActionKind.Wait,
                    string.Empty,
                    Math.Clamp(action.DurationMilliseconds, VoiceShortcutConstants.MinWaitMilliseconds, VoiceShortcutConstants.MaxWaitMilliseconds)),
                _ => new VoiceShortcutAction(
                    VoiceShortcutActionKind.Command,
                    action.Value?.Trim() ?? string.Empty,
                    0)
            })
            .ToArray();

        return shortcut with
        {
            ShortcutId = string.IsNullOrWhiteSpace(shortcut.ShortcutId) ? $"shortcut-{Guid.NewGuid():N}" : shortcut.ShortcutId.Trim(),
            Title = shortcut.Title?.Trim() ?? string.Empty,
            WhenISay = shortcut.WhenISay?.Trim() ?? string.Empty,
            Group = string.IsNullOrWhiteSpace(shortcut.Group) ? "General" : shortcut.Group.Trim(),
            Actions = actions,
            CreatedUtc = shortcut.CreatedUtc == default ? DateTimeOffset.UtcNow : shortcut.CreatedUtc,
            UpdatedUtc = shortcut.UpdatedUtc == default ? DateTimeOffset.UtcNow : shortcut.UpdatedUtc
        };
    }
}

public sealed class VoiceShortcutCommandPack : ICallsignCommandPack
{
    private readonly Dictionary<string, VoiceShortcutDefinition> _shortcuts;

    public VoiceShortcutCommandPack(IEnumerable<VoiceShortcutDefinition> shortcuts)
    {
        var shortcutList = shortcuts.ToArray();
        _shortcuts = shortcutList
            .Where(shortcut => shortcut.Enabled)
            .Where(shortcut => VoiceShortcutStore.Validate(
                shortcut,
                shortcutList.Where(other => !string.Equals(other.ShortcutId, shortcut.ShortcutId, StringComparison.OrdinalIgnoreCase))).Succeeded)
            .ToDictionary(shortcut => shortcut.ShortcutId, StringComparer.OrdinalIgnoreCase);
    }

    public CallsignPackDescriptor Descriptor { get; } = new(
        PackId: VoiceShortcutConstants.PackId,
        DisplayName: VoiceShortcutConstants.PackDisplayName,
        Version: "1.4.0-alpha",
        Tier: CallsignPackTier.Free,
        Description: "Local voice shortcuts composed from existing Callsign-visible commands and bounded waits.",
        SignatureStatus: "local",
        IsCommunity: false,
        RequiresSignature: false);

    public IReadOnlyList<CallsignCommandDefinition> Commands =>
        _shortcuts.Values
            .OrderBy(shortcut => shortcut.Title, StringComparer.OrdinalIgnoreCase)
            .Select(shortcut => new CallsignCommandDefinition(
                CommandId: shortcut.ShortcutId,
                DisplayName: shortcut.Title,
                VoicePhrases: new[] { shortcut.WhenISay },
                Description: $"Run the '{shortcut.Title}' voice shortcut from group '{shortcut.Group}'.",
                Kind: CallsignCommandKind.Extension,
                Tier: CallsignPackTier.Free,
                RiskTier: CallsignCommandRiskTier.LocalReversible,
                VisibleAction: true,
                Category: "Voice shortcuts",
                VisibilityRequirement: CallsignCommandVisibilityRequirement.VisibleRequired,
                Reversible: true,
                PrivacyImpact: CallsignCommandPrivacyImpact.None,
                ApprovalRequirement: CallsignCommandApprovalRequirement.None,
                HelpText: $"Voice shortcut '{shortcut.Title}' runs {shortcut.Actions.Count} saved action(s) through the normal Callsign visible command pipeline.",
                Examples: new[] { shortcut.WhenISay },
                VerificationStrategy: CallsignCommandVerificationStrategy.VisibleStatus))
            .ToArray();

    public ValueTask<CallsignCommandExecutionResult> ExecuteAsync(CallsignCommandExecutionContext context)
    {
        if (!_shortcuts.TryGetValue(context.CommandId, out var shortcut))
        {
            return ValueTask.FromResult(new CallsignCommandExecutionResult(false, "The selected voice shortcut is no longer available."));
        }

        var followUpSteps = shortcut.Actions
            .Select(action => action.Kind switch
            {
                VoiceShortcutActionKind.Wait => new CallsignFollowUpStep(
                    CallsignFollowUpStepKind.Wait,
                    DurationMilliseconds: action.DurationMilliseconds),
                _ => new CallsignFollowUpStep(
                    CallsignFollowUpStepKind.Command,
                    Value: action.Value)
            })
            .ToArray();

        return ValueTask.FromResult(new CallsignCommandExecutionResult(
            true,
            $"Voice shortcut '{shortcut.Title}' requested.",
            VisibleAction: $"voice-shortcut:{shortcut.ShortcutId}",
            AuditEvent: $"voice_shortcut:{shortcut.ShortcutId}",
            FollowUpSteps: followUpSteps));
    }

    public static string FormatActionSummary(VoiceShortcutAction action) =>
        action.Kind switch
        {
            VoiceShortcutActionKind.Wait => $"Wait {action.DurationMilliseconds} ms",
            _ => $"Say: {action.Value}"
        };
}
