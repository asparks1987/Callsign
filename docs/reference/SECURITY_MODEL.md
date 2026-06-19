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

## Tier safety model

Free, Pro, and Advanced must all preserve the local-first safety model.

- Free is limited to voice/callsign identity and visible Start menu app launch.
- Pro can add full Windows, WSL, and Linux control only with policy checks, visible approval prompts, and audit logging.
- Advanced can add specialized commands and recipes only if dangerous, external, secret, or destructive actions are blocked or explicitly approved.

Paid tiers must not require hidden data collection for the core experience. Cloud transcription, telemetry, licensing checks, and paid services must be disclosed and opt-in where they affect user data.

## Closed-source boundary

Private paid-tier material belongs in `/closed-source/`, which is ignored by git. Public security docs should describe the safety model, but proprietary implementation details should not be tracked in the open-source repo.

## Callsign canon alignment

The Alpha v1 line aims for Windows Voice Access parity, but v1.0 remains intentionally narrow: wake, identity verification, animated overlay with live readout, and Start menu app launch.

Security rules for the overlay and transcript readout:

- The overlay is a user-visible listening cue, not authorization.
- The wake word alone never permits command execution.
- Transcript text shown below `callsign.gif` must not bypass the identity gate.
- The overlay must hide when a session completes, cancels, times out, locks out, or listening stops.
- Future browser, file, WSL, Linux, and system control features require stronger approvals, policy checks, and audit trails than v1.0 app launch.
