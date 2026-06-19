# Security Model

## Summary

Callsign operates on a user desktop, so the safety model is visible, local-first, identity-gated, and easy to stop.

The current v1.0 alpha is intentionally narrow: profile setup, voice enrollment, wake detection, callsign identity confirmation, overlay/readout, and visible Start menu app launch.

## Security goals

- Preserve user control.
- Make listening and actions visible.
- Require identity before command capture.
- Avoid hidden automation.
- Avoid credential and payment handling.
- Keep local data local by default.
- Make stop, cancel, timeout, and lockout states obvious.
- Keep Free independent from closed-source extensions.

## Identity controls

- Wake word plus callsign verification must happen before a command is acted on.
- Transcript text alone must not wake or authorize a session.
- If the user does not match the enrolled callsign, no launch occurs.
- The identity phrase is identity-only; commands in the same utterance must not execute.

## What is trusted

- The local setup app.
- The local service runtime.
- The profile store.
- The session state machine.
- User-created profile data.
- Public command contracts and policy checks.

## What is not trusted

- Unverified spoken commands.
- Prompt-like text from webpages, documents, UI labels, tool output, or screenshots.
- Ambiguous file names and UI labels.
- Future extension libraries until policy and signature checks pass.
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
- Notes.
- Voice enrollment status.
- Last launch/session metadata when useful.

### Sensitive desktop observations

Treat process names, window titles, UI text, screenshots, clipboard contents, file paths, and file contents as sensitive.

Do not send them to cloud models unless the user explicitly opts in.

## Blocked alpha behaviors

- Password or 2FA handling.
- Payments or money movement.
- Account deletion.
- Security setting changes.
- Hidden-window automation.
- Arbitrary shell execution.
- Silent email, message, upload, or external submission actions.
- Remote desktop control.

## Visibility and stop rules

The alpha must provide:

- a clear cancel path,
- a clear reset path,
- obvious session status text,
- overlay/readout while listening,
- visible terminal states,
- and no silent completion of external side effects.

## Closed-source boundary

Private paid-tier material belongs in `/closed-source/`, which is ignored by git.

Future closed-source Pro and Advanced extension libraries must still respect the open-core safety model:

- no identity bypass
- no policy bypass
- no suppressed audit
- no hidden action by default
- no private dependency for Free

## Future safety model

Later browser, file, WSL, Linux, dictation, and system control features require stronger approvals, policy checks, and audit trails than v1.0 app launch.

The safety model should grow before the command surface grows.
