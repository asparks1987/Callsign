# Overlay UX

## Purpose

The overlay is a trust surface. It tells the user that Callsign is active, what phase it is in, what it heard, and how to stop.

## Required behavior

- Appears on accepted wake event.
- Stays topmost without stealing focus.
- Never captures keyboard input by default.
- Does not obscure critical system controls.
- Supports high contrast and text scaling.
- Shows a text state in addition to animation.
- Hides on completion, cancellation, timeout, lockout, service stop, or fatal fault.
- Recovers if the UI process restarts while the service remains alive.

## Visual states

| State | Animation | Text |
|---|---|---|
| Wake detected | active listening | `Callsign heard` |
| Identity | focused pulse | `Say your callsign` |
| Evaluating | restrained progress | `Checking this profile` |
| Command | active listening | `Say the app name` |
| Confirmation | paused/highlight | exact target question |
| Executing | directional/progress | exact visible action |
| Success | brief completion | verified result |
| Failure | calm error | safe next step |
| Cancelled | stop transition | `Session cancelled` |

## Placement

Default:

- Centered near the upper portion of the active display.
- Respect Windows work area and taskbar.
- Remain inside the selected monitor.
- Persist user-adjusted position per display configuration.
- Avoid following focus aggressively during a session.

## Transcript/readout

- Show only the minimum useful text.
- Redact sensitive recognized content.
- Truncate long input with an expansion path in the configuration UI.
- Do not persist the on-screen readout by default.
- Distinguish `Heard`, `Target`, and `Result`.
- Never display secrets if a future recognizer captures them.

## Failure safety

If overlay creation fails:

- Keep the session unauthorized until a visible fallback is available, or cancel.
- Write a structured error.
- Surface the fault in the UI/tray.
- Do not continue into hidden automation.

## Tests

- Multi-monitor and DPI scaling.
- Focus preservation.
- Full-screen app behavior.
- Screen-reader naming.
- High contrast.
- Rapid wake/cancel loops.
- UI restart.
- Service disconnect.
- Very long and non-Latin text.
