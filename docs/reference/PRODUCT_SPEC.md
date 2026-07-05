# Product Specification

## Product name

Callsign

## One-line description

Callsign is an open-source, service-first desktop voice assistant for Windows: say `Callsign`, verify the caller, then execute visible actions.

## Product thesis

Desktop voice control should be powerful enough for real work and visible enough to trust.

Callsign solves for that by combining:

- an MIT-licensed Free core,
- local profile and voice enrollment,
- audio-path wake detection,
- callsign identity verification,
- animated wake overlay with live readout,
- visible action execution,
- and a strict boundary for future paid extension libraries.

## Current alpha boundary

The current public release gate is `v1.0 alpha`.

v1.0 is complete only when a fresh user can install Callsign, create a profile, enroll voice, say the wake word, verify their callsign, see the overlay/readout, and launch an installed app through a visible Start menu flow.

The repo already contains early surfaces for the broader Alpha v1 direction, including command routing, browser launch helpers, system control helpers, file search, visible control overlays, dictation, and extension packs. Those surfaces are v1.x work unless they are explicitly listed as v1.0 release criteria.

The long-term Free-core objective is practical parity with stable Windows 11 Voice Access while preserving Callsign's wake, callsign identity, visibility, policy, and audit model. The canonical checklist lives in `VOICE_ACCESS_PARITY_MATRIX.md`.

## Canon flow

```text
Callsign -> identity verification -> command -> visible action
```

Transcript text alone must never bypass wake detection or identity verification.

## Alpha v1 release ladder

All Alpha v1 capabilities are free and remain free until at least beta.

Alpha versioning starts at `0.0.3a` and uses two tracks:

- Major milestones: `0.0.3a`, `0.0.4a`, `0.0.5a`, `0.1.0a`, and `1.0.0a`.
- `0.0.01a` and higher for micro stabilizing patches between milestones
- `1.0.0a` for the first public Alpha parity release

| Release | Scope |
|---|---|
| `v1.0 alpha` | Service wake detection, callsign verification, overlay + live readout, and visible Start menu app launch. |
| `v1.1 alpha` | Dictation with visible review before insertion, copy, paste, or other text action. |
| `v1.2 alpha` | Browser control for visible open, search, navigation, and bounded browser workflows. |
| `v1.3 alpha` | System control for Windows, WSL, and Linux, including file search results shown or opened through Explorer. |
| `v1.4 alpha` | Voice Access parity hardening, command discovery, update splash, manual walkthroughs, and release-candidate installer/site verification. |

The 100% parity target for `VOICE_ACCESS_PARITY_MATRIX.md` is the release gating criterion for `1.0.0a`; all earlier versions keep the flow intentionally additive.

## Free tier requirements

The Free tier is the public open-source product.

It must:

- install and run without a paid account,
- keep the v1.0 wake/identity/overlay/launch path usable,
- keep user profile and enrollment state local by default,
- expose understandable status and failure states,
- provide stop, cancel, timeout, and lockout behavior,
- and remain independent from closed-source extensions.

## Future commercial tiers

Future Pro and Advanced capabilities may be delivered as closed-source extension libraries.

The planned shape is:

- Pro: deeper Windows, browser, WSL, Linux, workflow, and system control.
- Advanced: specialized command catalogs, recipes, diagnostics, and power-user automation.

These tiers must not weaken the open core. Paid code can expand what Callsign knows how to do, but it must still pass identity, policy, approval, visibility, and audit expectations.

## v1.0 interaction

1. User creates a local account/profile and callsign.
2. User records and reviews samples.
3. User activates voice identity.
4. Service runs as background listener.
5. User says `Callsign` or `call sign`.
6. `callsign.gif` overlay appears above other windows.
7. Overlay shows live wake, identity, command, and launch readout.
8. User says enrolled callsign.
9. Identity verification succeeds.
10. User says an installed app launch request.
11. App launches through visible Start menu flow.
12. Session exits safely on completion, cancel, timeout, or lockout.

## v1.0 scope rules

- Wake is detected by openWakeWord/audio path, not transcript text alone.
- Identity must pass before command capture.
- Command scope stays to installed app launch targets and safe shell-backed destinations.
- No arbitrary shell execution.
- No hidden automation.
- No password, payment, security setting, or external submission actions.
- Installer, service startup, runtime health, and smoke behavior are part of release quality.

## Non-goals for v1.0 alpha

- Full desktop command parity.
- Browser workflow execution.
- Dictation into arbitrary apps.
- WSL/Linux control.
- Paid-feature requirements.
- Closed-source logic inside the open-source core.
