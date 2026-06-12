# Security Model

## Summary

Callsign operates a user desktop, so safety has to stay visible and local. The current alpha is intentionally narrow: account setup, voice enrollment, identity confirmation, and launching installed apps through the Start menu.

## Security goals

- Preserve user control.
- Make actions visible.
- Require identity before command capture.
- Avoid hidden automation.
- Avoid credential handling.
- Keep local data local by default.
- Make it easy to stop or reset the session.

## V1 identity controls

- Wake word plus callsign verification must happen before a command is acted on.
- If the user does not match the enrolled callsign, no launch occurs.
- Alpha voice identity is voice-only.
- Other biometrics can be reserved for later without affecting the alpha flow.

## What is trusted

- The local setup app.
- The local profile store.
- The session state machine.
- User-created profile data.

## What is not trusted

- Unverified spoken commands.
- Screen text that might be misleading.
- File names and UI labels that could be ambiguous.
- Future cloud services unless the user opts in.

## Data handling

### Voice enrollment

Voice enrollment state is stored locally with the profile.

Controls:

- Keep enrollment metadata local.
- Do not expose sample data to cloud services by default.
- Allow reset and re-enrollment.

### Profile data

Profile data should stay minimal:

- Callsign.
- Display name.
- Optional contact fields.
- Voice enrollment status.

## Blocked alpha behaviors

- Password or 2FA handling.
- Payments.
- Security setting changes.
- Hidden-window automation.
- Arbitrary shell execution.
- Silent email, upload, or submission actions.
- Remote desktop control.

## Visibility and stop rules

The alpha must provide:

- A clear cancel path.
- A clear reset path.
- Obvious status text.
- No silent completion of external side effects.

## Freemium note

If premium capabilities are added later, they must not weaken the local-first safety model or require hidden data collection for the core experience.

