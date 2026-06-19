# Threat Model

## Scope

This threat model covers the current Callsign alpha: local profile storage, voice enrollment, identity confirmation, visible app launch, and the background service that owns wake and session orchestration.

## Assets

- User control over the desktop.
- Local profile data.
- Voice enrollment state.
- Launch history.
- UI status and session state.

## Actors

### Benign user

Wants the assistant to open apps and help with routine desktop work.

### Confused or ambiguous user

May say a command that is unclear or incomplete.

### Malicious local process

May try to tamper with profiles, spoof identity, or interfere with the visible session.

### Malicious webpage or document

May try to influence future automation features through prompt injection.

## Trust boundaries

```text
User speech -> identity check -> session state machine -> visible launch action -> local profile storage
```

The most important boundary is between user identity and command capture. No launch should occur until the enrolled callsign is matched.

## Threats and mitigations

### Threat: wrong identity

Mitigation:

- Require wake word plus enrolled callsign.
- Lock out after timeout or repeated mismatch.
- Make the session status visible.

### Threat: accidental launch

Mitigation:

- Keep the app name visible in the UI.
- Require explicit launch intent.
- Provide cancel and reset controls.

### Threat: profile tampering

Mitigation:

- Store profiles locally.
- Keep the data model minimal.
- Validate profile paths and callsigns.

### Threat: hidden or confusing automation

Mitigation:

- Use visible launch paths.
- Avoid hidden background actions in alpha.
- Keep the UI status text obvious.

### Threat: future prompt injection

Mitigation:

- Treat observed content as data.
- Keep policy outside the model.
- Add safety rules before broader automation lands.

## Residual risks

- Voice recognition can mishear.
- A nearby person can still speak a similar callsign.
- A user may approve a bad action if the product grows beyond the current alpha.
- Future cloud features may add privacy risk unless they stay opt-in.

## Callsign canon alignment

The Alpha v1 line is intended to compete with Windows Voice Access-level functionality, but Callsign's threat model is stricter because it adds wake plus identity verification before command execution.

Threat considerations added by the wake overlay and live readout:

- False wake must not trigger action; it may only show the overlay and request identity.
- Misheard identity must not be treated as a command.
- The text readout is diagnostic feedback and must not become an alternate authorization path.
- A stuck overlay or stale transcript could confuse the user, so terminal session states must hide the overlay and clear active readout.

