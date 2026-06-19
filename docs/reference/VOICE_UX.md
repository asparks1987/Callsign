# Voice UX

## Goal

Callsign should feel calm, premium, direct, and visible.
The v1.0 alpha voice experience is deliberately narrow:

1. Say `Callsign` or `call sign`.
2. See `callsign.gif` appear above the desktop.
3. Read what Callsign heard below the animation.
4. Say your callsign.
5. Speak the app you want to launch.
6. Watch the app open through a visible Start menu flow.

All Alpha v1 features are free and remain free until at least beta.

The Free tier is the public open-source core.
It is the part that should most closely reach parity with built-in Windows voice tools while feeling better organized, more visible, and more trustworthy.

The tier architecture and upgrade model are defined in `TIER_ARCHITECTURE.md`.

## v1.0 voice interaction modes

### Wake mode

The background service waits for the wake word through openWakeWord audio detection.

Examples:

- `Callsign`
- `Call sign`

Transcription mistakes can be shown as diagnostics after audio capture, but they do not wake the service by themselves.

### Overlay mode

When wake is detected, Callsign shows the animated overlay and readout.
If speech is currently arriving but no final transcript has landed yet, the overlay should show a live hearing cue such as `Hearing your callsign...` or `Hearing your command...` instead of staying blank.
The overlay should also keep a subtle pulse or glow while audio is active, show a compact `LIVE` badge, and mirror the current runtime state so the user can see that Callsign is actively listening.

The overlay must show:

- the wake phase
- the identity phase
- the command phase
- the last heard transcript when available
- the current launch or stop result
- an authority line that makes it obvious whether the runtime is hearing audio or only previewing

Required readout examples:

- `Callsign heard. Say your callsign.`
- `Hearing your callsign...`
- `Heard: womprat`
- `Hearing your command...`
- `Command: open Notepad`
- `Launching Notepad...`

The overlay must never steal focus, block input, or prevent the user from stopping the session.

### Visible control mode

Setup and control surfaces should be voice-addressable by label wherever possible.
When Callsign shows visible controls, it should paint numbered badges over the live UI and keep the currently focused control highlighted so the user can see what is active.

Examples:

- `click callsign`
- `click active account`
- `click display name`
- `click repair wakeword`
- `click train voice identity`
- `show numbers`
- `show visible controls`
- `hide visible controls`
- `click 1`
- `click 2`
- `click 3`

This is part of the current Windows voice-control parity target: the user should be able to name the thing they can see and have Callsign move to it or activate it visibly.

### Identity mode

The service listens for the user's enrolled callsign.

Examples:

- `Alpha`
- `Jordan`
- `womprat`

The identity phrase is identity-only. Commands in the same utterance must not execute.

### Launch command mode

After identity is confirmed, the service listens for an installed app launch request.

Examples:

- `launch Notepad`
- `open Calculator`
- `start Visual Studio Code`
- `open Settings`
- `open File Explorer`
- `open Downloads`
- `open Documents`

For `v1.0 alpha`, Start menu app launch is the only required voice action.

## Recovery mode

If identity fails or times out, the assistant should clearly tell the user to try again.

If wake readiness fails, the configuration UI should point the tester to `Repair Wakeword` first. Manual PowerShell commands remain a fallback for advanced troubleshooting.

Examples:

- `I did not match that callsign. Try again.`
- `The session timed out. Say Callsign to start over.`

## Response style

Keep responses short while the user is working.

Good:

- `Identity confirmed. Say the app name.`
- `Launching Notepad.`
- `Session cancelled.`

Avoid long explanations during the active flow.

## Interruption

Required stop phrases:

- `Stop`
- `Stop now`
- `Cancel`
- `Pause`

Required behavior:

1. Stop pending actions.
2. Clear the session.
3. Hide the overlay.
4. Show a visible status update.
5. Return to idle.

## Accessibility considerations

- Support keyboard interaction for all setup screens.
- Keep the session state visible.
- Make the current phase obvious.
- Show the animated listening state on wake.
- Show live text readout while the user is speaking.
- Show a short recent speech history in the Session tab.
- Allow voice enrollment to be retried.

## Future voice modes

Dictation, browser control, and system control are future work and should stay out of the current v1.0 promise.

