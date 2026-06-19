# Voice UX

## Goal

Callsign should feel calm, visible, and easy to stop.

The user should never wonder:

- whether Callsign is listening,
- what Callsign heard,
- whether identity passed,
- what command is pending,
- or how to cancel.

All Alpha v1 features are free and remain free until at least beta.

## v1.0 voice flow

1. Say `Callsign` or `call sign`.
2. See `callsign.gif` appear above the desktop.
3. Read the live hearing cue or transcript below the animation.
4. Say your enrolled callsign.
5. Wait for identity confirmation.
6. Speak the installed app you want to launch.
7. Watch the app open through visible Start menu flow.

## Wake mode

The background service waits for the wake word through openWakeWord/audio detection.

Examples:

- `Callsign`
- `Call sign`

Transcription mistakes can be shown as diagnostics after audio capture, but transcript text alone must not wake the service.

## Overlay mode

The overlay is a visible state cue.

It should show:

- wake phase
- identity phase
- command phase
- current transcript or hearing cue
- current launch/stop result
- authority/status information when useful

Required readout examples:

- `Callsign heard. Say your callsign.`
- `Hearing your callsign...`
- `Heard: womprat`
- `Hearing your command...`
- `Command: open Notepad`
- `Launching Notepad...`

The overlay must not steal focus, block input, or prevent the user from stopping the session.

## Identity mode

The post-wake identity utterance is identity-only.

Examples:

- `Alpha`
- `Jordan`
- `womprat`

Commands in the same utterance must not execute. If identity fails or times out, no launch occurs.

## v1.0 command mode

After identity is confirmed, v1.0 accepts installed app launch requests.

Examples:

- `launch Notepad`
- `open Calculator`
- `start Visual Studio Code`
- `open Settings`
- `open File Explorer`
- `open Downloads`
- `open Documents`

The launch path should stay visible and understandable.

## Visible control mode

The setup app and overlay should remain understandable to users who rely on visible UI.

Current and v1.x command routing can support visible-control concepts such as:

- `show numbers`
- `show visible controls`
- `hide visible controls`
- `click 1`
- `click 2`
- `click display name`
- `click train voice identity`

This supports the long-term accessibility goal, but it should not weaken the v1.0 release gate.

## v1.x voice modes

Planned Alpha v1 extensions:

- v1.1: dictation with visible review before text actions
- v1.2: browser control with visible navigation and approval boundaries
- v1.3: system control, WSL/Linux workflows, and file search with visible results

## Response style

Keep active-session responses short.

Good:

- `Identity confirmed. Say the app name.`
- `Launching Notepad.`
- `Session cancelled.`

Avoid long explanations while the user is in the middle of a voice flow.

## Stop and recovery

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

- Support keyboard interaction for setup screens.
- Keep session state visible.
- Show live text while the user is speaking.
- Show a recent speech history in the Session tab.
- Allow voice enrollment reset and retry.
- Keep failures readable: mic, wake runtime, identity runtime, model, service, or launch failure.
