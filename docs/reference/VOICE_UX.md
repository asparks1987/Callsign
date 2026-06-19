# Voice UX

## Goal

Callsign should feel calm, premium, direct, and visible. The v1.0 alpha voice experience is deliberately narrow:

1. Say `Callsign` or `call sign`.
2. See `callsign.gif` appear above the desktop.
3. Read what Callsign heard below the animation.
4. Say your callsign.
5. Speak the app you want to launch.
6. Watch the app open through a visible Start menu flow.

All Alpha v1 features are free and remain free until at least beta.

## v1.0 voice interaction modes

### Wake mode

The background service waits for the wake word through openWakeWord audio detection.

Examples:

- `Callsign`
- `Call sign`

Transcription mistakes such as `paul sign` or `wall sign` can be shown as diagnostics after audio capture, but they do not wake the service by themselves.

### Overlay mode

When wake is detected, Callsign shows the animated overlay and readout.
If speech is currently arriving but no final transcript has landed yet, the overlay should show an animated live hearing cue such as `Hearing your callsign...` or `Hearing your command...` instead of staying blank.
While that hearing cue is active, the overlay should also have a subtle pulse or glow so the user can see that Callsign is actively listening, a small live badge such as `LIVE` should switch on so the state is obvious at a glance, and a compact mic activity meter should reflect incoming audio instead of leaving the user guessing whether the microphone is alive.
The overlay should also show a compact caption strip for the last heard transcript beneath the readout, with confidence when available. After a command completes, the overlay may also append the most recent system action so the user can see what actually ran without leaving the voice session.
The overlay should also show an authority line that makes it obvious whether the authoritative user runtime is hearing audio or whether the app is only in preview/listener mode.
When wake detection is uncertain, the overlay should also show a wake-candidate line with score and threshold when available, so the user can tell whether Callsign heard speech but did not clear wake.

It should also show a compact recent speech history beneath that caption, and the Session tab should keep the same recent history plus a dedicated speech cue line that mirrors whether Callsign is hearing speech or waiting for the next turn, and shows transcript confidence when a transcript is present, so the user can review what Callsign heard without hunting through logs. The Session tab should also keep a live `Last heard` row and command row synchronized with the runtime snapshot, and the visible-controls overlay should follow the same fresh runtime cue when the service is active.
The Session tab should also show the same wake-candidate line so wake misses are visible in both the live overlay and the control surface.

The Dictation tab should mirror that same speech cue pattern so the user can see when dictation is hearing speech versus waiting for the next turn, without making them look at the Session tab to know what is happening.
The Browser tab should also mirror the live voice cue and last-heard row so browser control feels like part of the same visible voice session, not a separate silent panel. The System tab should also show which action is selected and the last action that actually ran so the user can see the current system target before it runs and confirm what finished. The System and Files tabs should stay equally live and visible while Callsign is listening. The Files tab should also show the selected result immediately so the user can see which file or folder is ready to open in Explorer.

The visible-controls overlay should also include the live voice cue line so numbered targets and active listening state stay visible together when the user is navigating by voice.

Required readout examples:

- `Callsign heard. Say your callsign.`
- `Hearing your callsign...`
- `Heard: womprat`
- `Hearing your command...`
- `Command: open Notepad`
- `Launching Notepad...`
- `Wake candidate: heard 'call sign' (23% / 35%) but it stayed below threshold.`

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
- `click browser target`
- `click browser back`
- `click search results`
- `click system volume up`
- `click volume up`
- `show numbers`
- `show visible controls`
- `hide visible controls`
- `click 1`
- `click 2`
- `click 3`

This is part of the broader Windows Voice Access parity target: the user should be able to name the thing they can see and have Callsign move to it or activate it visibly.
The overlay should follow focus as the user navigates so the highlighted number stays in sync with the active control.
The visible-controls overlay should also name the currently focused control at the top so the user can hear and see which target is active.
The visible-controls overlay should keep the same live voice cue language used by the Session tab so the user can tell at a glance that Callsign is still listening while they choose a target.

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

## Follow-up voice modes

- `v1.1 alpha`: dictation mode for visible text capture, with safe text actions like copy, paste, cut, undo, redo, go to start, go to end, go to line start, go to line end, go to paragraph start, go to paragraph end, select to start, select to end, select to line start, select to line end, select to paragraph start, select to paragraph end, delete to start, delete to end, delete to line start, delete to line end, delete to paragraph start, delete to paragraph end, select previous word, select next word, select previous sentence, select next sentence, delete previous word, delete previous sentence, replace previous word, replace previous sentence, replace previous paragraph, replace all, new line, new paragraph, delete word, punctuation, clear, and select-all.
- `v1.2 alpha`: browser control mode for visible navigation and search, including back, forward, refresh, new tab, close tab, focus address bar, find in page, find next, find previous, zoom in, zoom out, zoom reset, scroll up, scroll down, scroll to top, scroll to bottom, and visible URL/search entry.
- `v1.3 alpha`: system control mode, including safe desktop controls such as volume up, volume down, mute, show desktop, and file search results opened through Windows Explorer.

The full Alpha v1 line is the path to Windows Voice Access parity.

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
