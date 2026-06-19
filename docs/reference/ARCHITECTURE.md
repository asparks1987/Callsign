# Architecture

## Overview

Callsign is service-first.

The UI is for setup, onboarding, monitoring, voice enrollment, overlay/readout, and visible controls. The background runtime is the authority for wake detection, identity gating, session state, and action orchestration.

The core session contract is:

```text
Callsign -> identity verification -> command -> visible action
```

## Current implementation

### Setup and monitoring app

`src/Callsign.UI` currently owns the user-facing alpha surface:

- local profile creation and selection
- callsign settings
- voice sample capture and playback
- voice identity training controls
- runtime repair helpers
- runtime state monitoring
- wake overlay and readout UI
- visible control overlays and UI navigation helpers
- command parsing/routing surfaces for the v1.x path

### Background service

`src/Callsign.Service` provides the service runtime:

- microphone/runtime ownership
- wake detection orchestration
- session state transitions
- callsign identity gate integration
- visible Start menu launch execution
- runtime snapshot writes for UI synchronization
- overlay readout serialization
- health and diagnostics data for the monitoring UI

Transcript text is treated as command input only after a dedicated wake event and identity confirmation.

## v1.0 release architecture

v1.0 is intentionally narrow:

- openWakeWord/audio-path wake detection
- callsign identity verification
- `callsign.gif` overlay with live readout
- visible Start menu launch for installed apps and safe shell-backed destinations
- explicit stop/cancel/timeout/lockout states

No arbitrary shell execution exists in the v1.0 alpha core.

## v1.x extension architecture

The codebase is moving toward richer adapters:

- dictation with visible review
- browser open/search/navigation
- system control helpers
- file search with visible results
- Windows, WSL, and Linux control paths
- command catalogs and recipes

These adapters must stay behind the same runtime gates: wake, identity, policy, visibility, approval, and audit.

## Open-source boundary

The Free runtime, setup UI, profile/enrollment flow, overlay, public contracts, docs, tests, and basic everyday commands belong in the MIT-licensed repo.

Future Pro and Advanced extension libraries may be closed-source, but they should plug into the open runtime rather than replace it.

## Data and privacy posture

Treat these as sensitive:

- process names
- window titles
- UI text
- file paths
- microphone state
- screenshots
- clipboard contents
- profile data
- voice enrollment metadata

The default alpha posture is local-first. Do not send desktop observations or file contents to cloud models unless the user explicitly opts in.
