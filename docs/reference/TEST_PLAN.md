# Test Plan

## Goals

The Callsign alpha must prove three things:

1. A new user can set up the app.
2. Voice identity gates the session correctly.
3. The app can launch a visible installed application reliably.

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
- Docs and the public site describe the current behavior.

