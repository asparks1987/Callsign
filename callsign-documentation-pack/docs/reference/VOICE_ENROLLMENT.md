# Voice Enrollment

## Scope

Voice enrollment prepares the active local profile for the Alpha voice/callsign gate. It does not create an operating-system credential or a guarantee that only one human can pass.

## Data classes

- Profile identifier.
- Callsign text.
- Enrollment status.
- Sample count and quality metadata.
- Enrollment timestamp and schema version.
- Optional speaker embedding or model reference.
- Raw audio samples, if retained by configuration.
- Calibration and threshold metadata.

Each class requires an explicit storage, retention, deletion, export, and logging rule.

## Capture requirements

- Use the selected microphone and show its identity.
- Record a bounded duration and supported format.
- Reject silence, clipping, excessive noise, and captures below the minimum duration.
- Require samples from separate button holds or explicit turns.
- Never fabricate enrollment from a transcript alone.
- Do not mix samples from multiple profiles.
- Use a temporary path and atomic commit.
- Clean temporary data after cancellation or failure.

## Sample review

The user can:

- Play the latest sample.
- Delete and repeat a sample.
- See a simple quality explanation.
- Reset the complete enrollment.
- Inspect whether raw samples are retained.
- Delete retained samples without guessing file paths.

## Model and threshold rules

- Version the embedding/model identifier.
- Record the threshold configuration version.
- Calibrate with representative positive and negative fixtures.
- Do not silently lower thresholds after failures.
- Separate `sample quality insufficient` from `identity mismatch`.
- Make migration behavior explicit when models change.
- Re-enroll when compatibility cannot be guaranteed.

## Storage

See [STORAGE_AND_RETENTION.md](STORAGE_AND_RETENTION.md). Recommended layout:

```text
%LOCALAPPDATA%\Callsign\Profiles\<profile-id>\
  settings.json
  enrollment\
    manifest.json
    samples\        # only when configured to retain raw samples
    embedding.bin   # format and protection must be documented
```

Do not use the callsign string as an unvalidated path.

## Deletion

Reset must remove or invalidate:

- Enrollment status.
- Embedding/model artifact.
- Retained raw samples.
- Calibration metadata tied to the profile.
- Cached identity decisions.
- Any derived index that could still identify the profile.

Emit a minimal audit event without copying biometric data.

## Tests

- Fresh enrollment.
- Cancel mid-sample.
- Silence, noise, clipping, short sample.
- Wrong device.
- Device removed during capture.
- Multiple profiles.
- Reset and reenroll.
- Model-version migration.
- Corrupt manifest.
- Permission denied.
- No raw audio in logs.
