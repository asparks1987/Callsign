# Test Plan

## Goals

The Callsign alpha must prove three things:

1. A new user can set up the app.
2. Voice identity gates the session correctly.
3. The app can launch a visible installed application reliably.
4. The documented dictation, browsing, and file-search tools work end to end.

## Test layers

### Unit tests

Targets:

- Callsign validation.
- Profile storage.
- Voice enrollment state.
- Session state machine transitions.
- App name parsing.
- Error and lockout handling.

### UI tests

Targets:

- Create account flow.
- Save and reload profile flow.
- Record sample flow.
- Train and reset voice flow.
- Wake, verify, capture, launch flow.

### Launch tests

Targets:

- Start menu search opens visibly.
- App name is typed into search.
- Matching installed app launches.
- Failure path leaves the session in a safe state.

### Dictation tests

Targets:

- Dictation mode starts and stops visibly.
- Speech is transcribed into visible text.
- Missing microphone, silence, and transcription failure are readable.
- Copy and paste actions keep the dictated text user-visible.

### Browser tests

Targets:

- Web address or search phrase resolves to a visible browser target.
- The default browser opens without hidden automation.
- Invalid local paths or shell text are rejected.
- Search phrases produce a useful search URL.

### File search tests

Targets:

- The intended local file scope is searched.
- Matching results are shown in the UI.
- Empty and access-denied states are readable.
- Selected results can be opened or acted on.

### Safety tests

Targets:

- Identity mismatch blocks launch.
- Missing enrollment blocks launch.
- Timeout blocks launch.
- Cancel returns to idle.
- Stop/reset clears the session.

## Alpha acceptance tests

### Test: first-run setup

Expected:

- User can create an account.
- The profile persists after restart.
- The profile folder path is shown correctly.

### Test: voice enrollment

Expected:

- User can record samples.
- Training reflects the enrollment state.
- Reset returns the profile to not enrolled.

### Test: wake and verify

Expected:

- Saying `Callsign` moves the session into identity wait.
- Saying the correct callsign allows command capture.
- Saying the wrong callsign locks out the session.

### Test: launch app

Expected:

- A spoken command such as `launch Notepad` resolves the app name.
- The app launches through Start search.
- The launch result is shown in the UI.

### Test: timeout and cancel

Expected:

- If the user waits too long, the session locks out.
- Cancel returns to idle.
- Reset clears the current session state.

## Release gate

A release candidate cannot ship unless:

- The onboarding UI works from a clean profile store.
- Voice enrollment is saved and restored.
- Identity failure prevents launch.
- App launch works on a common installed app.
- Dictation exposes transcribed text and clear error states.
- Browser and file search work on the documented paths.
- Docs and the public site describe the current behavior.

## Tier acceptance checks

### Free

Expected:

- Voice/callsign recognition gates all spoken launch commands.
- Start menu app launch works for common installed apps.
- Free rejects paths, URLs, shells, command text, and non-launch automation.

### Pro

Expected:

- Windows, WSL, and Linux control actions pass policy evaluation before execution.
- Risky desktop actions show readable intent and require explicit approval.
- Audit logs record approved, denied, failed, and cancelled actions.

### Advanced

Expected:

- Advanced-only command packs are unavailable without the Advanced tier gate.
- Dangerous, external, secret, or destructive commands are blocked unless explicitly approved by policy.
- Recipe workflows show intent, approval points, and audit output before execution.

### Documentation and site drift

Expected:

- Root `burndown.md` exists and describes Free, Pro, and Advanced.
- `docs/reference/BURNDOWN.md` points to the canonical burndown.
- No user-facing docs use deprecated middle-tier wording.
- `docs/index.html` mentions Free, Pro, and Advanced after regeneration.
- `.gitignore` includes `/closed-source/`.
