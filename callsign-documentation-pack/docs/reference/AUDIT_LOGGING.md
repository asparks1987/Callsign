# Audit Logging

## Purpose

Provide a local, reviewable record of safety-relevant decisions and actions without turning the audit log into a transcript, surveillance feed, or secret store.

## Event classes

- Session start/terminal state.
- Wake event metadata.
- Identity decision metadata.
- Parsed intent class.
- Policy decision.
- Approval request/result.
- Tool/action invocation.
- Verification result.
- Cancellation/timeout/lockout.
- Configuration change.
- Profile/enrollment reset.
- Installer/update/security event.

## Event schema

See `schemas/audit-event.schema.json`.

Required concepts:

- Event version and ID.
- Timestamp.
- Correlation/session IDs.
- Component and version.
- Event type.
- Outcome.
- Risk tier.
- Redacted subject/target classification.
- Policy rule ID.
- Verification summary.
- Structured error.
- Privacy/redaction markers.

## Never log by default

- Raw audio.
- Full transcript.
- Passwords, tokens, 2FA, payment, wallet, or clipboard data.
- Screenshots.
- Full UI trees.
- Full file contents.
- Complete sensitive paths.
- Model prompts/responses containing observed data.
- Unredacted stack traces for export.

## Redaction

Redaction occurs before serialization. Do not write sensitive content and redact it later.

Use:

- Stable local opaque IDs.
- Basename or path class instead of full path.
- Length/hash where useful and safe.
- Allowlisted structured fields.
- Explicit `redaction_applied` markers.

Do not use unsalted hashes of low-entropy callsigns as anonymization.

## Integrity

Alpha logs are diagnostic, not tamper-proof legal records. Future stronger integrity may use chained hashes and signed checkpoints, but must not be marketed before implemented.

## Access

- Local user can inspect and clear according to retention rules.
- Export requires preview and redaction.
- Upload is opt-in.
- UI avoids displaying sensitive event fields by default.
- Future support bundles include manifests and explicit user selection.

## Failure

An audit-write failure:

- Produces a visible health warning.
- Must not silently bypass required policy/audit gates for state-changing tools.
- May allow low-risk observe-only behavior under an explicitly documented degraded mode.
- Uses bounded fallback diagnostics.

## Tests

- Schema validation.
- Secret canary never appears.
- Path/transcript redaction.
- Rotation.
- Disk full.
- Permission denied.
- Concurrent writers.
- Correlation continuity.
- Cancellation and denied action events.
