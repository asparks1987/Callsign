# Error Catalog

Errors are stable machine-readable codes with safe user messages and internal diagnostic context.

## Voice and runtime

| Code | User-facing meaning | Default recovery |
|---|---|---|
| `AUDIO_DEVICE_UNAVAILABLE` | No usable microphone | Select/reconnect device |
| `AUDIO_PERMISSION_DENIED` | Microphone access blocked | Open Windows privacy settings |
| `AUDIO_FORMAT_UNSUPPORTED` | Device/runtime format mismatch | Choose device or repair runtime |
| `WAKE_MODEL_MISSING` | Wake model absent | Run repair/reinstall |
| `WAKE_MODEL_HASH_MISMATCH` | Model failed integrity check | Reinstall trusted artifact |
| `WAKE_RUNTIME_NOT_READY` | Wake helper failed | View logs/repair |
| `IDENTITY_NOT_ENROLLED` | Active profile needs enrollment | Open enrollment |
| `IDENTITY_SAMPLE_LOW_QUALITY` | Capture cannot be evaluated | Retry |
| `IDENTITY_MISMATCH` | Profile not confirmed | Retry within policy |
| `IDENTITY_LOCKED_OUT` | Too many failures | Wait/reset per policy |
| `SESSION_TIMEOUT` | Current phase expired | Wake again |
| `SESSION_CANCELLED` | User/system cancelled | No action |
| `TRANSCRIPTION_UNAVAILABLE` | Provider not ready | Retry/change provider |
| `NO_SPEECH` | No usable speech detected | Retry |

## Profile/storage

| Code | Meaning | Recovery |
|---|---|---|
| `PROFILE_INVALID` | Profile failed schema/validation | Repair or recreate |
| `PROFILE_NOT_FOUND` | Active profile missing | Select another |
| `PROFILE_VERSION_UNSUPPORTED` | Newer/incompatible schema | Update or recover read-only |
| `STORAGE_ACCESS_DENIED` | Local data cannot be read/written | Fix permissions |
| `STORAGE_FULL` | Disk cannot accept data | Free space |
| `ENROLLMENT_DATA_CORRUPT` | Identity artifact invalid | Re-enroll |

## Action

| Code | Meaning | Recovery |
|---|---|---|
| `INTENT_UNSUPPORTED` | Not in this release | Explain scope |
| `TARGET_INVALID` | Path/URL/shell/unsafe target | Ask for plain supported target |
| `TARGET_NOT_FOUND` | No installed match | Try another name |
| `TARGET_AMBIGUOUS` | Multiple matches | Confirm/select |
| `ACTION_CANCELLED` | Cancelled before completion | No action |
| `ACTION_TIMEOUT` | Adapter timed out | Retry/handoff |
| `VERIFICATION_FAILED` | Action result not confirmed | Show uncertainty |
| `INTEGRITY_BOUNDARY` | Elevated target cannot be controlled | Human handoff |
| `POLICY_DENIED` | Safety rule blocked action | Explain rule |
| `APPROVAL_REQUIRED` | Specific confirmation needed | Ask exact question |

## Protocol/system

- `IPC_UNAVAILABLE`
- `IPC_UNAUTHENTICATED`
- `VERSION_MISMATCH`
- `SCHEMA_INVALID`
- `MESSAGE_TOO_LARGE`
- `RATE_LIMITED`
- `DEPENDENCY_MISSING`
- `DEPENDENCY_VERSION_MISMATCH`
- `AUDIT_UNAVAILABLE`
- `OVERLAY_UNAVAILABLE`
- `UNEXPECTED_INTERNAL_ERROR`

## Error rules

- Never expose a stack trace in the active voice flow.
- Never claim success after `VERIFICATION_FAILED`.
- Keep messages actionable and brief.
- Preserve detailed local diagnostics with redaction.
- Unknown exceptions map to a safe code and correlation ID.
