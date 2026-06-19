# Product Specification

## Product name

Callsign

## One-line description

Callsign is an open-source, service-first voice assistant for Windows voice workflows: say `Callsign`, verify the caller, then execute visible desktop actions.

## Mission

Be the practical Windows Voice Access-level control layer with:

- Apple-level usability polish
- Strong identity safety
- Visible overlay and command feedback
- Open-source core for trust and inspectability

The open-source Free tier is the public product.
It should be the piece that most directly rivals built-in Windows voice controls for day-to-day use.

Later paid tiers are not part of the current alpha scope and should not change the public promise today.

## Canon flow

```text
Callsign -> identity verification -> command -> visible action
```

Transcript text alone must never bypass wake and identity gate.

## Alpha v1 release line

All Alpha v1 capabilities are free and remain free until at least beta.

The open-source Free tier is the public face of Callsign and should be the part users compare to Windows Voice Access first.
It is the layer that should be polished, approachable, and good enough for real work on its own.

The Alpha v1 line remains service-first and visible:

| Release | Scope |
|---|---|
| v1.0 alpha | Service wake, callsign verification, overlay + live readout, and visible Start menu app launch. |

Everything beyond v1.0 stays in future planning until the current scope is complete and stable.

## v1.0 alpha interaction

The v1.0 alpha MVP is intentionally narrow:

1. User creates account and callsign.
2. User records and reviews samples.
3. User activates voice identity.
4. Service runs as background listener.
5. User says `Callsign` or `call sign`.
6. `callsign.gif` overlay appears above other windows.
7. Overlay shows live readout of wake, identity, command, and launch phases.
8. User says enrolled callsign.
9. Identity verification succeeds.
10. User says install command (app launch intent).
11. App launches through visible Start menu flow.
12. Session exits safely or returns on cancel/timeout/lockout.

## v1.0 scope rules

- Open-source source and local profile storage for identity state.
- Wake is detected by openWakeWord (audio path) and never transcript-only text.
- Identity must pass before command capture.
- Command scope stays to app launch targets that are clearly installed app names.
- No arbitrary shell execution in alpha.
- Startup/installer/service/smoke behavior remains part of v1.0 pass criteria.

## Pricing canon during alpha

- All Alpha v1 features are free.
- No alpha feature is retroactively paywalled.
- Future packaging may change later.
- The current scope should not shape the v1.0 public promise.

## Tier and upgrade architecture

The current product is the Free tier.
The app should install Free first, prove the user can create a profile and run visible commands, and keep the public repo focused on that experience.

Future tier planning lives in `docs/reference/TIER_ARCHITECTURE.md`, but it is not part of the v1.0 delivery scope.

## Non-goals for v1.0 alpha

- Full command parity in one release.
- Silent command execution.
- Hidden background automation.
- Closed-source logic in the open-source core.
