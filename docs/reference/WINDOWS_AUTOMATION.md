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
- Keep the app launch target obvious in the UI/service status.
- Keep the user able to cancel or reset the session.
- Avoid arbitrary shell execution.
- Reject paths, URLs, shell text, secrets, and non-launch automation.

## v1.3 system control strategy

Richer desktop automation belongs in `v1.3 alpha`. It comes after the first launch release, dictation, and browser control are reliable.

Future layers may include:

- UI automation for app controls.
- Safe text entry helpers.
- Safe system actions such as volume up, volume down, mute, and show desktop.
- WSL/Linux workflow commands.
- File and folder search.
- More precise app adapters.

## File search in v1.3

File search is part of system control because it reveals local filesystem context.

Required behavior:

- Search approved local scopes only.
- Prefer packaged `fzf.exe` for ranked filename/folder matching, with a visible built-in fallback warning if fzf is unavailable.
- Show matching results visibly.
- Open selected or best-match results through Windows Explorer.
- Do not read, upload, summarize, modify, delete, or submit file contents without a later explicit feature and policy approval.

## Safety rules

- Do not rely on hidden windows.
- Do not use background-only automation for the v1.0 launch path.
- Do not use raw coordinates when a visible, semantic path is available.
- Do not bypass the wake word, callsign identity, policy, or audit model.

