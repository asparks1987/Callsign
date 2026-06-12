# Voice UX

## Goal

Callsign should feel calm, direct, and visible. The alpha voice experience is simple:

1. Say `Callsign`.
2. Say your callsign.
3. Speak the app you want to launch.
4. Watch the app open through a visible Start menu flow.

## Voice interaction modes

### Wake mode

The assistant waits for the wake word.

Examples:

- `Callsign`
- `Callsign, open Notepad`

### Identity mode

The assistant listens for the user's enrolled callsign.

Examples:

- `Alpha`
- `Jordan`

### Command mode

The assistant listens for the task after identity is confirmed.

Examples:

- `launch Notepad`
- `open Calculator`
- `start Visual Studio Code`

For the Free tier, the main supported action is launching installed apps from the Start menu. Full Windows, WSL, and Linux control belongs in Pro. Specialized command packs, recipes, diagnostics, and power-user workflows belong in Advanced.

### Recovery mode

If identity fails or times out, the assistant should clearly tell the user to try again.

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
3. Show a visible status update.
4. Return to idle.

## Accessibility considerations

- Support keyboard interaction for all setup screens.
- Keep the session state visible.
- Make the current phase obvious.
- Allow voice enrollment to be retried.
