# Roadmap

## Release ladder

All Alpha v1 features are free and remain free until at least beta.

The current public scope is v1.0 alpha:

| Release | Focus | Goal |
|---|---|---|
| v1.0 alpha | Wake + identity + overlay + Start menu launch | Service wakeword, verified callsign gate, topmost overlay readout, and visible Start menu launch. |

## v1.0 alpha

- Account and callsign setup
- Voice sample recording and playback
- Background service listening
- `Callsign` wake word detection for `Callsign` and `call sign`
- Overlay activation at wake with live readout
- Callsign identity verification before launch
- Visible Start menu app launch
- Stop, cancel, timeout, and lockout safety

## Exit criteria

- Fresh install can complete service startup and profile enrollment.
- Wake is visible, audible enough, and synchronized with overlay lifecycle.
- `callsign.gif` is shown at wake and hidden only in terminal states.
- Start menu launch succeeds on common installed apps.
- Wrong or missing identity never launches.
- Safe terminal behavior is visible to users.

## Future ideas

Later work such as dictation, browser control, system control, and file search should be tracked separately and should not be treated as current alpha scope.

