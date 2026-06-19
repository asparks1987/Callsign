# Privacy Model

## Principles

- Collect the minimum data needed for the selected feature.
- Keep data local by default.
- Separate wake processing from post-wake transcription.
- Disclose every network provider and data class.
- Do not use user data for training without separate explicit consent.
- Make retention and deletion operable.
- Redact diagnostics by construction.

## Data inventory

| Data class | Sensitivity | Default location | Default retention |
|---|---|---|---|
| Profile name/callsign | personal | local profile | until profile deletion |
| Enrollment metadata | sensitive | local profile | until reset/deletion |
| Raw enrollment audio | biometric/sensitive | local only if retained | shortest configured period |
| Speaker embedding | biometric/sensitive | local protected store | until reset/deletion/model migration |
| Wake score/audio telemetry | sensitive operational | local logs/metrics | short bounded period |
| Command audio | sensitive | memory | not retained by default |
| Transcript/readout | sensitive | memory/UI | not persisted by default |
| Window/UI/path metadata | sensitive | memory/audit summary | minimized/redacted |
| Audit event | sensitive operational | local | bounded configurable period |
| Crash diagnostics | potentially sensitive | local; opt-in upload | bounded |
| Telemetry | varies | off by default | disclosed per event |

## Cloud providers

A provider integration must show:

- Provider name.
- Data sent.
- Region/endpoint if configurable.
- Retention/training settings known to the project.
- Whether processing is optional.
- Local alternative.
- Failure behavior.
- How to revoke credentials and delete cached data.

Cloud is never implied by a generic `Improve accuracy` toggle.

## Voice data

- Raw audio is not logged.
- Temporary capture files are deleted after use unless retention is explicitly enabled.
- Enrollment reset deletes derived artifacts and samples.
- Sample export is not required for Alpha; if added, it must be explicit and protected.
- Speaker identity data is treated as sensitive biometric-like data even if the product does not make legal biometric-authentication claims.

## Diagnostics

Default diagnostics include:

- Component versions.
- Structured error codes.
- State transitions.
- Durations.
- Boolean readiness.
- Sanitized numeric confidence.
- Redacted target class.

They exclude:

- Full transcript.
- Raw audio.
- Full file paths.
- Clipboard content.
- Screenshots.
- UI text.
- Email addresses or tokens.
- Stack traces containing user paths unless locally viewed by the user.

## User controls

The UI should provide:

- Listening enable/disable.
- Provider disclosure.
- Profile and enrollment deletion.
- Raw sample retention setting, if supported.
- Audit/log location and clear action.
- Diagnostics export with preview and redaction.
- Telemetry opt-in/out.
- Uninstall data-removal option.

## Privacy review

Use [docs/checklists/SECURITY_REVIEW.md](../checklists/SECURITY_REVIEW.md) and [docs/guides/PRIVACY_REVIEW.md](../guides/PRIVACY_REVIEW.md).
