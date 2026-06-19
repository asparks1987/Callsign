# Threat Model

## Scope

This threat model covers the Callsign alpha: local profile storage, voice enrollment, identity confirmation, visible app launch, and the background service that owns wake and session orchestration.

It also names the future threat boundary for Pro and Advanced closed-source extension libraries.

## Assets

- User control over the desktop.
- Local profile data.
- Voice enrollment state.
- Runtime/session state.
- Launch history.
- UI status and overlay readout.
- Future extension manifests and command catalogs.

## Actors

### Benign user

Wants the assistant to open apps and help with routine desktop work.

### Confused or ambiguous user

May say a command that is unclear, incomplete, or unsafe.

### Nearby speaker

May accidentally or intentionally say the wake word or a similar callsign.

### Malicious local process

May try to tamper with profiles, spoof runtime state, interfere with the visible session, or modify future extension files.

### Malicious webpage or document

May try to influence browser, file, or system-control features through prompt injection.

### Malicious extension

Future closed-source libraries could try to bypass identity, policy, audit, or visibility unless extension loading is constrained.

## Trust boundaries

```text
User speech -> wake detection -> identity check -> session state machine -> policy/approval -> visible action -> local profile storage
```

The most important boundary is between identity and command capture. No action should occur until the enrolled callsign is matched.

Future extension libraries add another boundary:

```text
Signed extension manifest -> policy evaluation -> runtime adapter -> visible action
```

## Threats and mitigations

### Threat: wrong identity

Mitigation:

- Require wake word plus enrolled callsign.
- Treat identity utterance as identity-only.
- Lock out after timeout or repeated mismatch.
- Make the session status visible.

### Threat: accidental launch

Mitigation:

- Keep the app name visible in the UI.
- Require explicit launch intent.
- Provide cancel and reset controls.
- Reject ambiguous or unsafe launch strings.

### Threat: profile tampering

Mitigation:

- Store profiles locally.
- Keep the data model minimal.
- Validate profile paths and callsigns.
- Normalize Windows paths before policy checks.

### Threat: hidden or confusing automation

Mitigation:

- Use visible launch paths.
- Avoid hidden background actions in alpha.
- Keep the UI status text obvious.
- Require verification for fallback automation.

### Threat: future prompt injection

Mitigation:

- Treat observed content as untrusted data.
- Keep policy outside the model.
- Require approval for external side effects.
- Do not let webpages, documents, screenshots, clipboard contents, or UI text override user intent.

### Threat: unsafe closed-source extension

Mitigation:

- Keep Free independent from private code.
- Require signed manifests before loading future extension libraries.
- Route all extension commands through identity, policy, approval, visibility, and audit.
- Keep proprietary material in `/closed-source/` during local development and out of the public repo.

## Residual risks

- Voice recognition can mishear.
- A nearby person can still speak a similar callsign.
- A user may approve a bad action as command surfaces grow.
- Future cloud features may add privacy risk unless they stay opt-in.
- Closed-source extensions require strong signing, review, and policy gates before release.

## Callsign canon alignment

The Alpha v1 line is intended to compete with Windows Voice Access-level functionality, but Callsign's threat model is stricter because it adds wake plus identity verification before command execution.

Threat considerations added by the wake overlay and live readout:

- False wake must not trigger action; it may only show the overlay and request identity.
- Misheard identity must not be treated as a command.
- The text readout is diagnostic feedback and must not become an alternate authorization path.
- A stuck overlay or stale transcript could confuse the user, so terminal session states must hide the overlay and clear active readout.
