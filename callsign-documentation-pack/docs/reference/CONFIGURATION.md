# Configuration

## Principles

- Safe defaults.
- Explicit scope.
- Typed/versioned schema.
- No secret values in ordinary settings files.
- Invalid values fail closed and remain recoverable.
- UI labels match configuration names and docs.

## Configuration layers

Highest precedence first:

1. Temporary command/test override.
2. Per-profile setting.
3. Per-user application setting.
4. Packaged default.

Environment variables may be used for developer diagnostics but should not become an undocumented production configuration channel.

## Core settings

### Runtime

- Enable/disable listening.
- Service or per-user fallback mode.
- Startup behavior.
- Active profile.
- Session deadlines.
- Lockout attempts/duration.
- Overlay display and monitor.

### Audio/wake

- Input device ID.
- Sample format.
- Wake model ID/path selected from packaged assets.
- Wake threshold within tested bounds.
- Diagnostic audio-level display.

### Identity

- Provider/model version.
- Threshold profile.
- Required enrollment sample count.
- Raw-sample retention flag.
- Retry/lockout policy.

### Transcription

- Local/cloud provider.
- Model/language.
- Network opt-in.
- Timeout.
- Maximum utterance duration.

### Data and logs

- Log level.
- Retention days/file count.
- Audit enabled/required.
- Diagnostic export location.
- Telemetry opt-in.

### Actions

- Enabled release capabilities.
- Approved file-search roots.
- Browser preference.
- Allowlisted aliases.
- Policy mode; users may tighten, not silently weaken, blocked rules.

## Secrets

API keys and tokens, if a future provider requires them, use an OS-protected secret store. They never appear in profile JSON, logs, screenshots, exports, or command-line arguments.

## Validation

- Enumerated values.
- Bounds.
- Path canonicalization.
- Version compatibility.
- Cross-field invariants.
- Unknown fields handled according to schema policy.
- A backup before migration.

## Repair/reset

Provide:

- Restore safe defaults.
- Repair packaged wake runtime.
- Rebuild app index.
- Clear logs.
- Reset enrollment.
- Reset profile.
- Full local-data reset.

Each operation states what it changes.
