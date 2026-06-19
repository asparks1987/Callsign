# Windows Automation Strategy

## Goal

For `v1.0 alpha`, Callsign should launch installed Windows apps in a visible, understandable way from the background service.

The v1.0 path is:

1. Service hears `Callsign`.
2. Service verifies the user's callsign.
3. User asks for an installed app.
4. Callsign opens Start search.
5. Callsign types the app name.
6. Callsign launches the matching app.

All Alpha v1 features are free and remain free until at least beta.

## Why keep it visible

Visible automation is easier to trust and easier to stop.

Callsign should not hide the fact that it is interacting with the desktop.

## v1.0 strategy

- Use the Start menu search experience for app launching.
- Open common shell destinations such as Settings, File Explorer, This PC, Recycle Bin, and user folders through visible shell-backed launches.
- Keep the app launch target obvious in the UI and service status.
- Keep the user able to cancel or reset the session.
- Avoid arbitrary shell execution.
- Reject paths, URLs, shell text, secrets, and non-launch automation.

## Future automation

Browser control, dictation helpers, system control, and file search are future work and should not be treated as current alpha scope.

## Safety rules

- Do not rely on hidden windows.
- Do not use background-only automation for the v1.0 launch path.
- Do not use raw coordinates when a visible, semantic path is available.
- Do not bypass the wake word, callsign identity, policy, or audit model.

