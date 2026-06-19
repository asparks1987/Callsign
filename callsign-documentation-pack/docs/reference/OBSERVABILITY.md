# Observability

## Goals

Help users and developers determine whether microphone, wake model, identity, service, overlay, and action adapters are healthy without exposing private content.

## Health model

Components report:

- `healthy`
- `degraded`
- `unavailable`
- `disabled`
- `unknown`

Key checks:

- UI version.
- Service process and mode.
- IPC compatibility.
- Microphone availability and selected device.
- Audio level activity.
- Wake model/runtime presence and hash.
- Wake loop freshness.
- Active profile/enrollment readiness.
- Overlay connection.
- Transcription provider.
- Action adapter readiness.
- Disk/log health.
- Packaged dependency versions.

## Runtime snapshot

The UI consumes the redacted runtime snapshot schema. It must distinguish stale data from current health.

## Logs

Recommended channels:

- `runtime`: lifecycle and structured errors.
- `voice`: readiness and durations, no raw audio/transcript.
- `policy`: rule IDs and outcomes.
- `audit`: user-impacting decisions and verified actions.
- `installer`: staging, service, shortcut, manifest, rollback.
- `site-build`: documentation generation.

## Metrics

Local engineering metrics may include:

- Wake attempts and accepted events.
- Identity outcomes by generic reason.
- State durations.
- Cancellation/timeout counts.
- Adapter success/failure.
- Crash/restart count.
- Queue/buffer sizes.
- Log drop/rotation count.

Telemetry transmission is off by default and requires a separate privacy design.

## Diagnostic bundle

An export flow should:

1. Show exactly which files/fields will be included.
2. Redact usernames, paths, callsigns, transcripts, and identifiers.
3. Exclude raw audio, samples, embeddings, screenshots, clipboard, and secrets.
4. Include version/manifest/hash information.
5. Let the user cancel and inspect the archive.
6. Avoid automatic upload.

## Alerts

User-facing alerts are actionable:

- `Wake runtime missing — run Repair Wakeword.`
- `Service and UI versions do not match — reinstall this version.`
- `No microphone detected — choose a device.`
- `Profile needs enrollment.`
- `Audit storage unavailable — state-changing automation is disabled.`

## Verification

Observability itself has tests for stale snapshot, dropped IPC, disk full, service crash/restart, model mismatch, and redaction.
