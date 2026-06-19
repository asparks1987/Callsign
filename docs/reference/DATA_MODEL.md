# Data Model

## Overview

Callsign keeps the Free alpha data model small, local, and inspectable.

Main data types:

- user profile
- user settings
- voice enrollment state
- runtime/session snapshot
- launch history and diagnostics
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

## Launch history

Useful launch metadata can be recorded locally for debugging and user confidence:

- profile callsign
- app name
- timestamp
- success or failure
- visible verification summary

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
