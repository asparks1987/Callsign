# Roadmap

## Release ladder

All Alpha v1 features are free and remain free until at least beta.

The Alpha v1 line is the path to practical Windows Voice Access parity while keeping Callsign's identity model and visible action model.

| Release | Focus | Goal |
|---|---|---|
| v1.0 alpha | Wake + identity + overlay + Start menu launch | Service wakeword, verified callsign gate, topmost overlay readout, and visible Start menu launch. |
| v1.1 alpha | Dictation | Speak text locally and review before insertion or copy. |
| v1.2 alpha | Browser control | Open, search, and navigate visible browser flows with explicit boundaries. |
| v1.3 alpha | System control and file search | Broad Windows control, WSL/Linux workflow expansion, and Explorer-backed search actions. |
| Beta or later | Packaging and monetization | Revisit Pro/Advanced packaging and support model after alpha parity targets are stable. |

## v1.0 alpha (current)

- Account and callsign setup
- Voice sample recording and playback
- Background service listening
- `Callsign` wake word detection for `Callsign` and `call sign`
- Overlay activation at wake with live readout
- Callsign identity verification before launch
- Visible Start menu app launch
- Stop/cancel/timeout/lockout safety

## Exit criteria

- Fresh install can complete service startup and profile enrollment.
- Wake is visible, audible enough, and synchronized with overlay lifecycle.
- `callsign.gif` is shown at wake and hidden only in terminal states.
- Start menu launch succeeds on common installed apps.
- Wrong/missing identity never launches.
- Safe terminal behavior is visible to users.

## v1.1 alpha

- Dictation visible review mode.
- Clear typed insertion/copy actions.
- Explicit failure and stop handling.

## v1.2 alpha

- Browser open/search/navigation flows.
- External action boundaries (purchases, messages, uploads, etc).

## v1.3 alpha

- Windows control workflows and safe policy checks.
- WSL and Linux control bridge.
- File and folder search with Explorer open path.

## Alpha parity hold point

Before leaving Alpha v1, product should be compared against Windows Voice Access for reliability and usability of daily command set and visibility behavior.

## Beta or later

- Paid tier model and packaging boundaries.
- Signed installer and update/rollback story.
- Support and telemetry options with opt-in only.
