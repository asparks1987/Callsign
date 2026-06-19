# Voice Pipeline

## Purpose

Define the audio, wake, identity, transcription, and interruption path without allowing transcript text or model output to bypass authorization state.

## Pipeline

```text
microphone
  -> device/format normalization
  -> bounded audio frames
  -> openWakeWord detector
  -> WakeWordDetected event
  -> overlay/session start
  -> identity capture
  -> speaker/callsign evaluation
  -> command capture
  -> speech-to-text
  -> bounded intent parser
  -> action validation
```

## Wake stage

- openWakeWord is the canonical Alpha wake source.
- The detector consumes bounded frames.
- The configured model and expected sample format are checked at startup.
- A textual transcript containing `Callsign`, `call sign`, or a homophone cannot create a wake event.
- Scores and thresholds may be logged as sanitized numeric telemetry.
- The UI must distinguish `microphone active`, `wake detector ready`, and `session awake`.

## Identity stage

Inputs may include:

- Active profile.
- Enrollment state.
- Captured post-wake audio.
- Spoken callsign transcript.
- Speaker-embedding comparison, when enabled.
- Quality/confidence indicators.

Rules:

- The identity utterance cannot also be a command.
- Missing enrollment blocks the identity transition.
- A low-quality sample asks for a retry; it does not silently lower the threshold.
- Repeated failures enter a bounded lockout.
- Thresholds are configuration with tested ranges, not magic constants scattered through code.
- The UI can explain failure without exposing biometric internals or raw audio.

## Command stage

- Begins only after identity success.
- Uses a separate capture window.
- Produces a bounded transcript and normalized intent.
- Rejects unsupported action classes.
- Includes cancellation and silence handling.
- Does not retain raw command audio by default.

## Interruption

Required stop intents:

- `stop`
- `stop now`
- `cancel`
- `pause`

A local hotkey or UI control should provide an independent stop path.

Stop behavior:

1. Cancel capture and pending adapter work.
2. Clear authorization state.
3. Hide or transition the overlay.
4. Record a redacted terminal event.
5. Return to idle.

## Latency budgets

Initial engineering targets, subject to measurement:

| Segment | Target |
|---|---:|
| Wake event to overlay state | p95 under 300 ms |
| Overlay to identity prompt/readout | p95 under 500 ms |
| Identity utterance end to decision | p95 under 1.5 s |
| Command utterance end to parsed intent | p95 under 2.0 s |
| Parsed app intent to visible Start action | p95 under 750 ms |

A release may choose different measured budgets, but it must publish them and distinguish local from cloud-provider latency.

## Error classes

- `AUDIO_DEVICE_UNAVAILABLE`
- `AUDIO_FORMAT_UNSUPPORTED`
- `WAKE_MODEL_MISSING`
- `WAKE_RUNTIME_NOT_READY`
- `IDENTITY_NOT_ENROLLED`
- `IDENTITY_SAMPLE_LOW_QUALITY`
- `IDENTITY_MISMATCH`
- `IDENTITY_TIMEOUT`
- `TRANSCRIPTION_UNAVAILABLE`
- `TRANSCRIPTION_TIMEOUT`
- `NO_SPEECH`
- `SESSION_CANCELLED`

See [ERROR_CATALOG.md](ERROR_CATALOG.md).

## Test requirements

- Recorded deterministic fixtures for audio framing.
- Wake positive/negative fixtures.
- Transcript-not-wake tests.
- Noisy, silent, clipped, and wrong-device tests.
- Identity match/mismatch/threshold-boundary tests.
- Command capture cannot start before identity.
- Cancellation at every stage.
- Long-running soak test for audio-buffer growth.
- Sanitized logs contain no raw audio.
