# Data Model

## Overview

Callsign keeps the Free alpha data model small, local, and inspectable.

Main data types:

- user profile
- user settings
- voice enrollment state
- runtime/session snapshot
- launch history and diagnostics
- update manifest / release feed metadata
- future command/extension metadata

## User profile

Profiles live under:

```text
%LOCALAPPDATA%/Callsign/Profiles/<callsign>/settings.json
```

The current profile shape includes:

- callsign
- display name
- optional email and department fields
- notes
- local voice enrollment state
- last launched app/session metadata

## Voice enrollment state

Voice enrollment is stored with profile settings.

Suggested fields:

```json
{
  "voice_enrollment_status": "Enrolled",
  "voice_samples_recorded": 3,
  "voice_samples_required": 3,
  "voice_enrolled_utc": "2026-06-12T00:00:00Z"
}
```

## Runtime/session snapshot

The runtime snapshot is how the service and UI stay aligned.

Important fields include:

- runtime role and authority
- microphone health
- session state
- last transcript/readout
- overlay visibility
- wake/identity/command timing
- launch result or failure state

Runtime control logs are diagnostic metadata, not transcript storage. Scripted transcript queue/consume events are logged with redacted metadata only: transcript length and a short SHA-256 prefix. Raw transcript text stays in the transient request file until consumed and is not copied into `runtime-control.log`.

Temporary runtime audio lives only under Callsign-managed local folders such as `Logs/segments`, `Runtime/wake-window`, and `Runtime/wake-warmup`. Segment files are deleted after processing when wake diagnostics are off, abandoned current segments are deleted on stop, queued segments are deleted during shutdown, and listener startup prunes stale temporary WAV files older than the bounded retention window. These files are local processing artifacts, not profile data or audit records.

The profile also carries a local update device id so the user can recognize the current installation in the visible Updates surface. That local value is not sent raw in phone-home check-ins. `UpdateCheckService` hashes the profile callsign/account id and the local update device id into short `sha256:` identifiers before posting the check-in payload to the update server.

## Launch history

Useful launch metadata can be recorded locally for debugging and user confidence:

- profile callsign
- app name
- timestamp
- success or failure
- visible verification summary

## Update manifest metadata

Update and release feeds can carry localizable, visible change metadata for the update splash and release packet:

- version
- installer URL
- installer hash and size
- release notes
- added commands
- changed commands
- removed commands
- extension pack changes
- feature highlights
- published timestamp

The update manifest should keep feature highlights separate from command deltas so a release can announce visible UI or behavior changes even when the command catalog itself did not change.

## Future extension metadata

Future Pro and Advanced extension libraries may need metadata such as:

- extension id
- display name
- tier
- version
- signature status
- risk declarations
- command catalog manifest

Entitlement secrets and private implementation details must not be stored in the public repo.

## Retention

- Profile data: keep until the user deletes it.
- Launch history: keep locally until the user clears it.
- Voice enrollment metadata: keep locally until reset.
- Runtime diagnostics: keep bounded and local.
- Secrets: do not store in Free alpha profile data.
