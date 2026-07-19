namespace Callsign.Extensions;

public static class CallsignCommandPolicy
{
    public static CallsignPolicyEvaluationResult Evaluate(
        CallsignCommandDefinition command,
        bool identityVerified,
        bool freshIdentityVerified = false)
    {
        var visibleSurfaceRequired = RequiresVisibleSurface(command);

        if (command.ApprovalRequirement == CallsignCommandApprovalRequirement.Blocked
            || command.RiskTier == CallsignCommandRiskTier.DangerousOrBlocked)
        {
            return new CallsignPolicyEvaluationResult(
                CallsignPolicyDecision.BlockedDangerousAction,
                "The command is blocked by Callsign policy.",
                command.RiskTier,
                CallsignCommandApprovalRequirement.Blocked,
                VisibleActionRequired: true);
        }

        if (!identityVerified)
        {
            return new CallsignPolicyEvaluationResult(
                CallsignPolicyDecision.RequireFreshIdentity,
                "Callsign identity must be verified before commands can run.",
                command.RiskTier,
                CallsignCommandApprovalRequirement.RequireFreshIdentity,
                VisibleActionRequired: visibleSurfaceRequired);
        }

        if (command.ApprovalRequirement == CallsignCommandApprovalRequirement.RequireFreshIdentity && !freshIdentityVerified)
        {
            return new CallsignPolicyEvaluationResult(
                CallsignPolicyDecision.RequireFreshIdentity,
                "This command requires a fresh callsign identity check.",
                command.RiskTier,
                command.ApprovalRequirement,
                VisibleActionRequired: visibleSurfaceRequired);
        }

        if (command.ApprovalRequirement == CallsignCommandApprovalRequirement.AskWhenAmbiguous
            || command.ApprovalRequirement == CallsignCommandApprovalRequirement.RequireApproval
            || command.RiskTier == CallsignCommandRiskTier.ExternalSideEffect
            || command.VisibilityRequirement == CallsignCommandVisibilityRequirement.BackgroundAllowedWithApproval
            || RequiresPrivacyApproval(command.PrivacyImpact))
        {
            var approvalRequirement = command.ApprovalRequirement == CallsignCommandApprovalRequirement.AskWhenAmbiguous
                ? CallsignCommandApprovalRequirement.AskWhenAmbiguous
                : CallsignCommandApprovalRequirement.RequireApproval;
            return new CallsignPolicyEvaluationResult(
                CallsignPolicyDecision.RequireApproval,
                command.ApprovalRequirement == CallsignCommandApprovalRequirement.AskWhenAmbiguous
                    ? "This command needs a visible choice before it can run."
                    : "This command requires explicit user approval before it can run.",
                command.RiskTier,
                approvalRequirement,
                VisibleActionRequired: visibleSurfaceRequired);
        }

        return new CallsignPolicyEvaluationResult(
            CallsignPolicyDecision.Allow,
            "Command allowed.",
            command.RiskTier,
            command.ApprovalRequirement,
            VisibleActionRequired: visibleSurfaceRequired);
    }

    private static bool RequiresVisibleSurface(CallsignCommandDefinition command) =>
        command.VisibleAction
        || command.VisibilityRequirement == CallsignCommandVisibilityRequirement.VisibleRequired
        || command.VisibilityRequirement == CallsignCommandVisibilityRequirement.BackgroundAllowedWithApproval
        || command.ApprovalRequirement is CallsignCommandApprovalRequirement.AskWhenAmbiguous
            or CallsignCommandApprovalRequirement.RequireApproval
            or CallsignCommandApprovalRequirement.RequireFreshIdentity
            or CallsignCommandApprovalRequirement.Blocked
        || command.RiskTier == CallsignCommandRiskTier.ExternalSideEffect
        || command.RiskTier == CallsignCommandRiskTier.DangerousOrBlocked
        || RequiresPrivacyApproval(command.PrivacyImpact);

    private static bool RequiresPrivacyApproval(CallsignCommandPrivacyImpact privacyImpact) =>
        privacyImpact is CallsignCommandPrivacyImpact.Clipboard
            or CallsignCommandPrivacyImpact.FileContents
            or CallsignCommandPrivacyImpact.ScreenshotOrOcr
            or CallsignCommandPrivacyImpact.ExternalData;
}
