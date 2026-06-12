# Data Model

## Overview

Callsign keeps the alpha data model small and local.

Main data types:

- User profile.
- User settings.
- Voice enrollment state.
- Session state.
- Launch history.

## User profile

Profiles live under:

```text
%LOCALAPPDATA%/Callsign/Profiles/<callsign>/settings.json
```

The current profile shape includes:

- Callsign.
- Display name.
- Optional email and department fields.
- Notes.
- Local voice enrollment state.
- Last launched app.

## Voice enrollment state

Voice enrollment is currently stored with the profile settings.

Suggested fields:

```json
{
  "voice_enrollment_status": "Enrolled",
  "voice_samples_recorded": 3,
  "voice_samples_required": 3,
  "voice_enrolled_utc": "2026-06-12T00:00:00Z"
}
```

## Session state

The alpha session state is mostly runtime-only:

- Idle.
- Waiting for identity.
- Waiting for command.
- Ready to launch.
- Launching.
- Completed.
- Locked out.

## Launch history

Useful launch metadata can be recorded locally for debugging and user confidence:

- Profile callsign.
- App name.
- Timestamp.
- Success or failure.

## Retention

- Profile data: keep until the user deletes it.
- Launch history: keep locally until the user clears it.
- Voice enrollment metadata: keep locally until reset.
- Secrets: do not store.

