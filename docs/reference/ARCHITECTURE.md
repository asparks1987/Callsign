# Architecture

## Overview

Callsign is service-first.
The UI is for setup and monitoring; the background runtime is the voice session owner.

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
- identity gate integration
- Start menu launch execution
- runtime snapshot writes for UI synchronization
- overlay readout serialization for visible feedback

Transcript text is treated as a command channel only after a dedicated wake event and identity confirmation.

## Overlay and status

`callsign.gif` is a UI-only cue, driven by runtime session state.

Behavior:

- appears on wake transition
- stays on-screen through identity and command capture
- hides on completion, cancel, timeout, lockout, or explicit stop

## v1.0 scope

v1.0 only launches installed apps by visible Start menu path.
No arbitrary shell execution exists in the alpha core.

## Future scope

Later features such as dictation, browser control, system control, and file search should be documented separately and should not be treated as current alpha scope.

## Safety and platform direction

The architecture is intentionally local-first and open-source:

- user-visible actions and stop controls
- no hidden background side effects
- no paywall in Alpha v1
- the public core stays stable while future work is planned separately

## Core contract

The Free runtime, policy engine, session state, overlay, command registry contracts, and everyday command set belong in the MIT-licensed repo.

All commands must still pass the same identity, policy, visibility, and audit pipeline.

