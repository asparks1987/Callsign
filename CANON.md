# Callsign Canon Book

## Mission

Callsign is the open-source Windows voice-control killer.

It should feel as approachable as Apple Voice Control, grow toward more power than Talon, and stay visibly safer because every session follows the same identity-first structure:

`Callsign -> identity verification -> command -> visible action`

## Product promise

Callsign is built around a simple promise:

- the user can always see when Callsign is listening,
- the user can always see what Callsign heard,
- the user can always stop the session,
- and the user never has to guess whether a command was authorized.

The wake word only opens a session.
The enrolled callsign unlocks command capture.
The command only becomes action when the product can show the path visibly.

The open-source Free tier is the public foundation of Callsign.
It is the part that should most directly compete with and often surpass built-in Windows voice control for everyday use:

- wake with `Callsign`
- see what was heard
- verify identity before action
- launch visible desktop apps
- stop the session at any time

The paid tiers exist to keep the platform growing:

- Pro should extend into deeper Windows, WSL, and Linux control.
- Advanced should remain the fast-moving tier for new commands, recipes, diagnostics, and specialized power-user workflows.
- The paid tiers should be structured so new capabilities can be added continuously without disturbing the stable open-source core.

## Tier architecture canon

The Free product is the MIT-licensed open-source core. It must be downloadable from GitHub or the public website, install with minimal effort, and remain useful without a paid account.

Free is the Windows Voice Access parity layer: the part of Callsign that should cover everyday voice control better, more visibly, and more safely than the built-in tool.

Pro is the upgrade layer. It should unlock signed private command packs, deeper automation adapters, and a continuously updated command library without changing the Free core into a thin demo.

Advanced is the specialist layer. It should carry the fastest-moving catalog of power-user commands, diagnostics, recipes, admin/dev workflows, and specialized automation packs.

Entitlement decides whether a paid command pack may load. Policy still decides whether a command may run. A paid license must never bypass identity, approval, visibility, or audit rules.

The canonical implementation plan for this boundary lives in `docs/reference/TIER_ARCHITECTURE.md`.

## Why this exists

Current desktop voice tools usually break down in one of three places:

- they hide what they heard,
- they make users trust a black box,
- or they stop short of practical everyday control.

Callsign is the opposite:

- open-source core,
- local-first operation,
- identity-gated sessions,
- visible overlay feedback,
- and a clear release ladder from launch to broader control.

## Alpha v1 release line

All Alpha v1 features are free and remain free until at least beta.

- `v1.0 alpha`: background service wake detection, identity verification, `callsign.gif` overlay, live text readout, and visible Start menu app launch.
- `v1.1 alpha`: dictation with visible review.
- `v1.2 alpha`: browser control with safe external-action boundaries.
- `v1.3 alpha`: Windows, WSL, and Linux system control, including Explorer-backed file search.

Free is the open-source release line the public can use and trust on day one.
Pro and Advanced are the future paid expansion line.

## Windows Voice Access parity line

Alpha v1 is the path to functional parity with built-in Windows voice tools.

The target is not just feature count.
The target is:

- practical daily usefulness,
- readable state changes,
- visible control surfaces,
- and a much stronger trust model than the default platform tools.

The Free tier should be the Windows Voice Access alternative the project is known for.
The paid tiers should provide the deeper, ever-expanding ceiling.

## UX bar

The experience should feel:

- as polished and calm as Apple Voice Control,
- as powerful and extensible as Talon,
- and more trustworthy because identity is required before action.

## Required interaction model

The public alpha flow is fixed and testable:

1. Say `Callsign`.
2. See `callsign.gif` appear on top of everything.
3. Read the live transcript or hearing cue below the animation.
4. Say your enrolled callsign.
5. Wait for identity confirmation.
6. Say the installed app or command.
7. Watch the visible action happen.

Accepted wake variants include `Callsign` and `call sign`.

## Overlay canon

The wake overlay is a user-facing cue, not an authorization path.

It must:

- appear immediately when wake is detected,
- stay visible through identity and command capture,
- show live text readout below the animation,
- show whether the authoritative runtime or preview listener is owning audio,
- remain topmost without stealing focus,
- and hide when the session completes, cancels, times out, or locks out.

Readout examples:

- `Callsign heard. Say your callsign.`
- `Hearing your callsign...`
- `Heard: womprat`
- `Hearing your command...`
- `Command: open Notepad`
- `Launching Notepad...`

## Architecture canon

Callsign is service-first.

The UI is for setup, configuration, and monitoring.
The background runtime is the authority for wake detection, identity gating, and command orchestration.

The alpha v1 runtime is intentionally narrow:

- openWakeWord wake detection,
- strict identity gate,
- visible Start menu launch path,
- and readable telemetry for session, audio, and overlay sync.

## Safety canon

No action can skip identity.

- Transcript-only wake is ignored.
- Misheard identity is treated as a repeat request or rejection.
- Session termination states must remain visible.
- Closed-source features stay out of alpha canon and out of the public repo.

## Platform direction

Windows is the practical alpha launch platform.

WSL and Linux are part of the same product story and should be documented early, but they expand from the same visible, identity-first session model rather than replacing it.

## Public site canon

The public website should sell the open-source core clearly and simply:

- the app is visible,
- the workflow is identity-first,
- the alpha is free,
- and the product is meant to be a credible Windows Voice Control alternative that grows into deeper power.

Internal plumbing stays in contributor docs.
Public pages stay focused on the user story.

## Non-goals for v1.0

- arbitrary shell execution,
- hidden actions or silent completion paths,
- full command parity in the first public MVP,
- paid-feature requirements in Alpha v1.
