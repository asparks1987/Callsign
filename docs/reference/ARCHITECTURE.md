# Architecture

## Overview

Callsign is service-first. The UI is for setup and monitoring; the background runtime is the voice session owner.

The canonical v1.0 path is:

- openWakeWord wake detection
- service-driven identity verification
- command capture and execution
- visible Start menu action

## Setup surface

The WinForms app lets users:

- create and select local profiles
- record and replay samples
- train callsign identity
- manage runtime repair and logs
- monitor service state, microphone health, and recent speech readout

This app is intentionally not the final execution owner.

## Runtime

`src/Callsign.Service` provides:

- microphone capture and wake detection
- session state machine (`wake -> identity -> command`)
- identity/biometric gate integration
- Start menu launch execution
- runtime snapshot writes for UI synchronization
- overlay readout serialization for visible feedback

Transcript text is treated as a command channel only after a dedicated wake event and identity confirmation.

## Overlay and status

`callsign.gif` is a UI-only cue, driven by runtime session state.

Behavior:

- appears on wake transition
- stays on-screen through identity + command capture
- hides on completion, cancel, timeout, lockout, or explicit stop

## v1.0 scope

v1.0 only launches installed apps by visible Start menu path.
No arbitrary shell execution exists in alpha core.

## v1.1, v1.2, v1.3 direction

- v1.1 adds dictation.
- v1.2 adds visible browser flows.
- v1.3 expands to system and filesystem control with stronger policy rules.

## Safety and platform direction

The architecture is intentionally local-first, open-source core:

- user-visible actions and stop controls
- no hidden background side effects
- no paywall in Alpha v1
- Pro/Advanced expansion happens after the alpha parity core is stable

The Free tier should remain the stable, inspectable core that matches the public promise.
Pro and Advanced should be able to grow continuously above that core with new commands, workflows, and deeper control paths, without forcing the open-source experience to become dependent on private logic.

## Tier architecture

The Free runtime, policy engine, session state, overlay, command registry contracts, and everyday command set belong in the MIT-licensed repo.

The paid layers should plug into the same runtime through signed command packs:

- Free commands ship with the open-source app.
- Pro command packs unlock deeper Windows, WSL, Linux, browser, workflow, and system control.
- Advanced command packs unlock specialized recipes, diagnostics, admin/dev workflows, and power-user automation.

All commands, free or paid, must still pass the same identity, policy, visibility, and audit pipeline.

The detailed installer, entitlement, update, and command-pack boundary is defined in `docs/reference/TIER_ARCHITECTURE.md`.
