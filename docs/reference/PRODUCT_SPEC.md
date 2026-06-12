# Product Specification

## Product name

Callsign

## One-line description

Callsign is a Windows-first voice assistant that wakes on the word `Callsign`, confirms the user's callsign, and helps launch installed apps and complete routine desktop tasks in a visible way.

## Product thesis

Callsign should feel like a helpful desktop companion, not a hidden macro recorder.

The public promise is simple:

1. The user speaks the wake word.
2. Callsign confirms the user by callsign.
3. Callsign shows what it is about to do.
4. Callsign performs the action in a visible, stoppable flow.

## Alpha v1 interaction flow

The current alpha is focused on onboarding and app launch:

1. User creates an account in the setup UI.
2. User records a few voice samples.
3. The profile is marked as voice-enrolled.
4. The user says `Callsign`.
5. The user says their callsign or username.
6. Callsign verifies identity against the enrolled profile.
7. The user speaks a command such as `launch Notepad`.
8. Callsign resolves the app name through a visible Start menu search flow.
9. Callsign launches the app and records what happened.

If identity fails, the command window closes and nothing launches.

## Current MVP scope

- Windows setup and onboarding UI.
- User account creation and profile storage.
- Voice sample recording and enrollment state.
- Wake word plus callsign identity flow.
- Visible app launch through the Start menu.
- Clear status messages and stop/reset controls.

## Explicit non-goals for alpha

- Arbitrary shell execution.
- Password, 2FA, or payment entry.
- Hidden or minimized automation.
- Remote control or background surveillance.
- Silent email, upload, or submission actions.
- Linux as a baseline target before the Windows alpha is stable.

## Product principles

### 1. The user stays in command

Callsign only acts after the user has clearly asked it to.

### 2. Identity comes first

The user must say the wake word and their callsign before command capture begins.

### 3. Visibility beats cleverness

The user should be able to see what Callsign is doing, stop it, and understand the result.

### 4. Open source core, freemium future

The core desktop experience should remain free and open source. Future premium offerings, if any, must not undermine the local, visible workflow.

## Success metrics

- A new user can create an account without help.
- A new user can enroll voice in a few clear steps.
- The app can launch a common installed application reliably.
- The user can stop or reset the session at any time.
- The app does not proceed when identity fails.

## Open product questions

- How much of the voice enrollment should be guided by the app versus spoken prompts later?
- Should the next phase focus on background service reliability or broader app launch support?
- What freemium extras, if any, should stay optional after the core alpha?

