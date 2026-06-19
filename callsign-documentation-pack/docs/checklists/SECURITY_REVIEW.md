# Security and Privacy Review Checklist

## Scope

- [ ] Exact feature, release, actors, and assets.
- [ ] Current versus target behavior.
- [ ] Trust boundaries.
- [ ] Data-flow diagram.

## Authorization

- [ ] Wake/session/identity requirements.
- [ ] Policy outside model.
- [ ] Approval scope and expiry.
- [ ] Unknown inputs fail closed.
- [ ] Context is revalidated.

## Sensitive data

- [ ] Data inventory.
- [ ] Local/network flow.
- [ ] Storage/retention/deletion.
- [ ] Log/export redaction.
- [ ] No raw secrets/audio/screenshots by default.
- [ ] Provider disclosure and opt-in.

## Execution

- [ ] Typed input.
- [ ] No shell interpolation.
- [ ] Path/selector normalization.
- [ ] Privilege boundary.
- [ ] Cancellation.
- [ ] Verification.
- [ ] Audit.
- [ ] Rollback/handoff.

## Threats

- [ ] Prompt injection.
- [ ] Replay/spoofing where relevant.
- [ ] Race/context change.
- [ ] Local malicious process.
- [ ] Tampered dependency/artifact.
- [ ] Denial of service.
- [ ] Compound side effects.
- [ ] Misleading success.

## Evidence

- [ ] Unit/integration.
- [ ] Denied behavior.
- [ ] Fuzz/bounds.
- [ ] Secret canary.
- [ ] Manual visible test.
- [ ] Threat model and ADR updated.
