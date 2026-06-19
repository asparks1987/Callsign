# Product Specification

## Product name

Callsign

## One-line description

Callsign is an open-source, service-first voice-control layer for Windows voice workflows: say `Callsign`, verify the caller, then execute visible desktop actions.

## Mission

Be the practical Windows Voice Access-level control layer with:

- Apple-level usability polish
- Strong identity safety
- Visible overlay and command feedback
- Open-source core for trust and inspectability

The open-source Free tier is the public product.
It should be the piece that most directly rivals built-in Windows voice controls for day-to-day use.
Pro and Advanced are the paid expansion tiers that let Callsign keep growing without turning the core into a paywalled toy.

## Canon flow

```text
Callsign -> identity verification -> command -> visible action
```

Transcript text alone must never bypass wake and identity gate.

## Alpha v1 release line

All Alpha v1 capabilities are free and remain free until at least beta.

The open-source Free tier is the public face of Callsign and should be the part users compare to Windows Voice Access first.
It is the layer that should be polished, approachable, and good enough for real work on its own.

Pro and Advanced should be treated as paid growth tiers:

- Pro extends the platform into deeper Windows, WSL, and Linux control.
- Advanced stays flexible for rapidly added power-user commands, recipes, and specialized workflows.

The Alpha v1 line remains service-first and visible:

| Release | Scope |
|---|---|
| v1.0 alpha | Service wake, callsign verification, overlay + live readout, and visible Start menu app launch. |
| v1.1 alpha | Dictation with visible review and explicit insertion/copy controls. |
| v1.2 alpha | Browser open/search/navigation with safe external boundaries. |
| v1.3 alpha | Windows, WSL, and Linux system workflows; file search opened through Explorer. |
| Beta or later | Paid packaging and continuously expanding Pro/Advanced feature growth. |

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

## Follow-up alpha releases

### v1.1 dictation

Visible dictated text first, with explicit user confirmation before insertion or copy.

### v1.2 browser control

Open a browser target, search web content, and navigate visible pages.
Submission actions remain constrained in alpha policy.

### v1.3 system control and file search

Safe Windows/WSL/Linux control and Explorer-based file search results.
File search is visible and action-transparent; no content reading/uploading by default.

## Pricing canon during alpha

- All Alpha v1 features are free.
- No alpha feature is retroactively paywalled.
- Beta or later may split Free/Pro/Advanced packaging.
- The paid tiers should be designed so new commands and capabilities can be added continuously as the product evolves.

## Tier and upgrade architecture

The tier source of truth is `docs/reference/TIER_ARCHITECTURE.md`.

The product is split by capability layer:

- Free is the MIT-licensed open-source parity layer for everyday Windows voice control.
- Pro is the paid upgrade layer for deeper Windows, WSL, Linux, browser, workflow, and system control.
- Advanced is the paid specialist layer for the fastest-moving command library, recipes, diagnostics, and power-user packs.

The app should install Free first, prove the user can create a profile and run visible commands, then offer an in-app Pro upgrade that loads signed private command packs. Free must not require private code, and Pro entitlement must not bypass policy.

## Non-goals for v1.0 alpha

- Full command parity in one release.
- Silent command execution.
- Hidden background automation.
- Closed-source logic in the open-source core.
