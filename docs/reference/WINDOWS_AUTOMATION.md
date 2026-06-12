# Windows Automation Strategy

## Goal

Callsign should launch installed Windows apps in a visible, understandable way.

For alpha, the current visible path is:

1. Open Start search.
2. Type the app name.
3. Launch the matching app.

## Why keep it visible

Visible automation is easier to trust and easier to stop.

Callsign should not hide the fact that it is interacting with the desktop.

## Current alpha strategy

- Use the Start menu search experience for app launching.
- Keep the app launch target obvious in the UI.
- Keep the user able to cancel or reset the session.
- Avoid arbitrary shell execution.

## Future automation strategy

Later phases can add richer desktop automation, but only after the alpha is reliable.

Future layers may include:

- UI automation for app controls.
- Safe text entry helpers.
- File workflow helpers.
- More precise app adapters.

## Safety rules

- Do not rely on hidden windows.
- Do not use background-only automation for the alpha launch path.
- Do not use raw coordinates when a visible, semantic path is available later.

