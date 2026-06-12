# Threat Model

## Scope

This threat model covers the current Callsign alpha: local profile storage, voice enrollment, identity confirmation, visible app launch, and the future background voice service.

## Assets

- User control over the desktop.
- Local profile data.
- Voice enrollment state.
- Launch history.
- UI status and session state.
- Any future audit logs or recipe data.

## Actors

### Benign user

Wants the assistant to open apps and help with routine desktop work.

### Confused or ambiguous user

May say a command that is unclear or incomplete.

### Malicious local process

May try to tamper with profiles, spoof identity, or interfere with the visible session.

### Malicious webpage or document

May try to influence future automation features through prompt injection.

### Compromised model or service

May produce unsafe suggestions if the assistant grows beyond the current alpha.

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

