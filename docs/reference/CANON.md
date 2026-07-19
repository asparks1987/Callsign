# Callsign Canon Book

## Mission

Callsign is an open-source, Windows-first desktop voice assistant for visible, identity-gated computer control.

The project exists to make voice control feel powerful without making the user trust hidden automation. Every user-facing session follows the same structure:

```text
Callsign -> identity verification -> command -> visible action
```

## Product promise

The user can always see:

- when Callsign is listening,
- what Callsign thinks it heard,
- whether identity has been accepted,
- what action is about to happen,
- and how to stop or reset the session.

The wake word opens a session.
The enrolled callsign authorizes command capture.
The command becomes action only through a visible, policy-governed path.

## Open-source canon

The Free tier is the public trust layer of Callsign.

It includes the setup app, local profile/enrollment flow, background runtime, wake/session state, overlay/readout, visible Start menu launch path, public contracts, docs, and tests for the open core.

The Free core must be useful without a paid account and must not depend on private code. Alpha v1 features remain free until at least beta.

The public website should sell this plainly: Callsign is inspectable voice control for people who want a desktop assistant they can understand, fork, audit, and stop.

## Closed-source extension canon

Callsign is also being built with a future commercial extension layer in mind.

Future Pro and Advanced capabilities may ship as closed-source extension libraries. Those libraries can add deeper command catalogs, recipes, diagnostics, browser workflows, Windows/WSL/Linux control, and specialized automation while keeping the core runtime open.

The boundary is strict:

- Free remains open-source and independently useful.
- Proprietary tier material belongs in `/closed-source/`, which is ignored by git.
- Paid extensions must not bypass identity, visibility, policy, approval, or audit rules.
- Closed-source libraries expand the ceiling; they do not replace the public foundation.

## Alpha v1 release line

Alpha v1 is a release line, not a single oversized drop.

| Release | Public promise |
|---|---|
| `v1.0 alpha` | Background service wake detection, callsign identity verification, `callsign.gif` overlay with live readout, and visible Start menu app launch. |
| `v1.1 alpha` | Dictation with visible review before text insertion or clipboard actions. |
| `v1.2 alpha` | Browser control for visible open/search/navigation and safe bounded browser tasks. |
| `v1.3 alpha` | System control for Windows, WSL, and Linux, including file search results shown or opened through Explorer. |

All Alpha v1 features are free until at least beta.

## Current implementation shape

Callsign is service-first.

- `src/Callsign.Service` is the runtime authority for wake detection, session state, identity gate, runtime status snapshots, and launch orchestration.
- `src/Callsign.UI` is setup, onboarding, monitoring, voice enrollment, overlay/readout, and user-visible control.
- The current public release gate remains v1.0: wake, identity, overlay, transcript/readout, and visible Start menu app launch.
- The repo also contains early v1.x surfaces for browser launch, file search, system control, visible control routing, and richer command parsing. These should be documented as forward path unless proven as release-ready.

## Required v1.0 interaction model

1. The user creates a local profile and callsign.
2. The user records voice samples and marks the profile as enrolled.
3. The background runtime listens for the wake cue through audio detection.
4. The user says `Callsign` or `call sign`.
5. `callsign.gif` appears with a live readout.
6. The user says their callsign.
7. Identity is accepted or rejected visibly.
8. If accepted, the user says an installed app launch request.
9. Callsign launches the app through visible Start menu flow.
10. Completion, cancel, timeout, lockout, and failure states are visible.

## Overlay canon

The overlay is a user-facing state cue, not an authorization path.

It must:

- appear when wake is detected,
- stay visible through identity and command capture,
- show live text or a hearing cue below the animation,
- show what runtime state owns the session,
- avoid stealing focus,
- and hide only when the session completes, cancels, times out, locks out, or stops.

Readout examples:

- `Callsign heard. Say your callsign.`
- `Hearing your callsign...`
- `Heard: womprat`
- `Identity confirmed. Say the app name.`
- `Hearing your command...`
- `Command: open Notepad`
- `Launching Notepad...`

## Safety canon

No action can skip identity.

- Transcript-only wake is ignored.
- Wake alone never executes commands.
- Misheard identity repeats, rejects, times out, or locks out.
- The user must have visible stop and reset paths.
- Hidden or minimized-window action is not part of the MVP.
- Arbitrary shell execution is not part of the product.
- Sensitive data and external side effects require explicit user approval.

## Platform direction

Windows is the practical launch platform.

WSL and Linux control are v1.x extension paths. They should be built through visible, policy-governed adapters rather than raw hidden execution.

## UX bar

Callsign should feel:

- clear enough for someone who needs accessibility-first voice control,
- calm enough for daily use,
- powerful enough to grow toward advanced command catalogs,
- and trustworthy because it is visible, identity-first, and open at the core.

## Public site canon

The public site under `docs/` should sell two ideas together:

- Callsign is open-source voice control you can inspect and stop.
- Callsign has a future closed-source extension-library path for advanced paid capabilities.

Internal implementation details belong in reference docs. The homepage should lead with user trust, open-source usefulness, alpha scope, and the honest commercial boundary.
