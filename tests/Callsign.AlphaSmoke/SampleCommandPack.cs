using Callsign.Extensions;

namespace Callsign.AlphaSmoke;

public static class PackTestSupport
{
    public static CallsignCommandRegistry CreateRegistry()
    {
        var root = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", "packs", Guid.NewGuid().ToString("N"));
        return new CallsignCommandRegistry(root);
    }
}

public sealed class SampleCommandPack : ICallsignCommandPack
{
    private static readonly CallsignCommandDefinition[] SampleCommands =
    [
        new CallsignCommandDefinition(
            CommandId: "sample-echo",
            DisplayName: "Echo sample text",
            VoicePhrases: new[] { "sample pack echo", "sample pack say" },
            Description: "Echoes the spoken argument text back through the pack contract.",
            Kind: CallsignCommandKind.Extension,
            Tier: CallsignPackTier.Free,
            RiskTier: CallsignCommandRiskTier.Observe,
            VisibleAction: true)
    ];

    public CallsignPackDescriptor Descriptor { get; } = new(
        PackId: "sample-pack",
        DisplayName: "Sample Pack",
        Version: "1.0.0",
        Tier: CallsignPackTier.Free,
        Description: "Sample smoke-test extension pack.",
        SignatureStatus: "dev");

    public IReadOnlyList<CallsignCommandDefinition> Commands => SampleCommands;

    public ValueTask<CallsignCommandExecutionResult> ExecuteAsync(CallsignCommandExecutionContext context)
    {
        var message = $"Executed {Descriptor.PackId}/{context.CommandId} with '{context.ArgumentText}'.";
        return ValueTask.FromResult(new CallsignCommandExecutionResult(true, message, VisibleAction: context.ArgumentText, AuditEvent: $"sample-pack:{context.CommandId}"));
    }
}
