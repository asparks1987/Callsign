# Security Model

## Security objective

Preserve the user's control of the desktop while providing a visible voice path to a small set of supported actions.

## Security boundaries

```text
ambient audio
  -> wake detector
  -> session state
  -> active-profile identity gate
  -> bounded intent
  -> policy/approval
  -> adapter
  -> verification
```

Future model planning sits before policy and never becomes an authorization boundary.

## Trusted computing base

Minimize trust to:

- Signed/known Callsign UI and runtime binaries.
- Local profile/storage adapters.
- Session state machine.
- Wake and identity adapters within their documented limits.
- Policy engine.
- Tool validation and execution server.
- Audit/redaction pipeline.
- Operating-system user boundary.

Do not treat:

- Speech transcripts.
- Model output.
- Web/document content.
- File names.
- UI text.
- Tool descriptions.
- Screenshots.
- Clipboard contents.
- Network responses.

as trusted instructions.

## Identity model

Alpha's voice/callsign gate reduces accidental activation and attempts to bind a session to the active profile. It is not equivalent to Windows authentication.

Controls:

- Separate wake, identity, and command turns.
- Active enrolled profile required.
- Bounded attempts and lockout.
- Configured threshold and model version.
- Visible status.
- Session-scoped authorization.
- Clear fallback and reset.
- No privilege elevation.

## Action controls

- Narrow release-specific intent parser.
- Strict input schemas and size limits.
- Deny unsupported intents.
- Visible target.
- Policy outside model.
- Specific approval.
- Semantic automation first.
- Postcondition verification.
- Cancellation.
- Redacted audit trail.

## Blocked behavior

- Arbitrary command/shell/script execution.
- Credential, password, 2FA, payment, or wallet entry.
- Admin/UAC and security settings.
- Permanent deletion.
- Software install/uninstall.
- Hidden external side effects.
- Remote desktop/control.
- Stealth, evasion, surveillance, or exfiltration.
- Silent cloud upload of observed content.

## Local storage protection

- Use per-user directories and ACLs.
- Avoid world-writable locations.
- Validate profile IDs and file names.
- Write atomically.
- Version schemas.
- Redact logs.
- Consider OS-protected encryption for sensitive derived identity data.
- Do not store secrets.
- Never trust local files merely because they are local; validate every load.

## IPC

Before UI/service IPC is considered hardened:

- Bind to the interactive user.
- Authenticate peers.
- Define message schemas and limits.
- Prevent replay where relevant.
- Reject unauthenticated action requests.
- Tie action requests to current session state.
- Log peer/version mismatch safely.

## Supply chain

- Pin and hash bundled runtimes and models.
- Track licenses.
- Generate artifact manifests.
- Scan dependencies and binaries.
- Sign releases.
- Reproduce or document builds.
- Do not repair by silently downloading unpinned executables.

## Security verification

- Static analysis.
- Dependency review.
- Schema fuzzing.
- State-machine negative tests.
- Policy regression suite.
- IPC abuse tests.
- Path traversal/reparse tests.
- Installer/service privilege tests.
- Log-secret scanning.
- Manual review of every new action class.
