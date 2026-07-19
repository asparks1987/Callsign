# Roadmap

## Release ladder

All Alpha v1 features are free and remain free until at least beta.

| Release | Focus | Goal |
|---|---|---|
| `v1.0 alpha` | Wake + identity + overlay + Start menu launch | Prove the core visible session model end to end. |
| `v1.1 alpha` | Dictation with review | Let users dictate text while keeping insertion/copy actions explicit and visible. |
| `v1.2 alpha` | Browser control | Add visible open/search/navigation and bounded browser workflows. |
| `v1.3 alpha` | System control and file search | Add Windows, WSL, Linux, and Explorer-backed file search workflows with policy gates. |
| `v1.4 alpha` | Voice Access parity hardening | Close the parity matrix, polish overlays, expand tests, replay imported-pack and update splashes, and publish a release-candidate installer/site update. |
| `Beta or later` | Packaging and extension libraries | Decide Pro/Advanced packaging, entitlement, signed libraries, and paid command catalogs. |

## Alpha versioning model

The project continues with alpha prerelease numbering that supports both major milestone jumps and micro patches:

- Start the line at `0.0.3a`.
- Use micro follow-ups like `0.0.01a`, `0.0.02a`, `0.0.03a`, etc. for fixes and polish.
- Use major alpha revisions like `0.0.1a`, `0.0.2a`, `0.0.4a`, `0.0.5a`, and onward for larger alpha capability clusters, with `0.0.3a` remaining the documented starting baseline.
- The first public alpha target remains `1.0.0a`.

### Targeted milestone map

| Version | Intent |
|---|---|
| `0.0.3a` | Baseline command registry hardening plus Packs UI import flow, with visible update check/in-flight install plumbing complete. |
| `0.0.01a` | Urgent fixes and polish between alpha milestones. |
| `0.0.1a` | Larger alpha capability cluster: command families, UI surfaces, and policy/verification expansion. |
| `0.0.2a` | Follow-on alpha capability cluster or hardening slice before the next major jump. |
| `0.0.4a` | Further extension command families and policy/verification hardening. |
| `0.0.5a` | Stability hardening and regression fixes for wake, identity, and session reliability. |
| `0.1.0a` | Major alpha milestone before first public parity release. |
| `1.0.0a` | First public alpha release with all required free Voice Access parity categories in place. |

The plan is to keep all parity work within `0.0.xa` and `1.0.0a` until all
`VOICE_ACCESS_PARITY_MATRIX.md` categories are complete and evidence-backed.

## Version progression

Current alpha planning uses:

- micro steps like `0.0.01a`, `0.0.02a`, `0.0.03a`, ...
- major milestone steps like `0.0.1a`, `0.0.2a`, `0.0.3a`, `0.0.4a`, `0.0.5a`, `0.0.6a`, `0.1.0a`, ...
- with the public alpha target at `1.0.0a`.

The schedule starts at `0.0.3a` and increments as needed.

| Version | Purpose |
|---|---|
| `0.0.3a` | First public internal baseline with extension registry and UI integration in place. |
| `0.0.01a` | Fast patch release used for urgent fixes between micro milestones. |
| `0.0.1a` | Major alpha slice used for a larger command-family or UI capability cluster. |
| `0.0.2a` | Another major alpha slice used for additional capability clusters or hardening. |
| `0.0.4a` | Additional parity command families and integration hardening. |
| `0.0.5a` | Stability and packaging cleanup before broader feature expansion. |
| `0.1.0a` | Major alpha milestone before first public parity release. |
| `1.0.0a` | First public alpha release milestone, including all required free parity surface for Voice Access v1 target. |

## v1.0 alpha

v1.0 is the current public release gate.

Required:

- account and callsign setup
- voice sample recording and playback
- background service listening
- `Callsign` / `call sign` wake detection from audio
- overlay activation at wake with live readout
- callsign identity verification before command capture
- visible Start menu app launch
- stop, cancel, timeout, and lockout safety
- runtime state visible in the setup/monitoring UI

## v1.1 alpha

Shipped as part of the current alpha line:

- dictation capture after identity
- visible text review
- explicit insert/copy/paste controls
- no silent text entry into sensitive fields
- clear cancel/reset behavior

## v1.2 alpha

Shipped as part of the current alpha line:

- visible browser open/search/navigation
- safe URL and search parsing
- clear boundary around form submission, uploads, messages, and external side effects
- approval prompts for higher-risk browser actions

## v1.3 alpha

Shipped as part of the current alpha line:

- Windows system control through visible, policy-governed adapters
- WSL and Linux command/control paths that are not arbitrary hidden shell execution
- file search with results shown to the user
- Explorer-backed open/reveal behavior
- stronger audit and approval coverage for state-changing commands

## v1.4 alpha

Shipped as part of the current alpha line:

- close every category in `VOICE_ACCESS_PARITY_MATRIX.md`
- add command discovery for `what can I say`
- show update-splash command deltas and feature highlights from the latest manifest
- replay imported-pack splash narration after community extension import
- harden imported community pack review, disable, enable, and rollback behavior
- complete manual Voice Access parity walkthroughs
- build, publish, and verify the release-candidate installer from the public website

## Commercial roadmap

The public Free core remains open-source.

Future Pro and Advanced tiers may add closed-source extension libraries for:

- deeper command catalogs
- recipes and diagnostics
- advanced workflow automation
- specialized Windows, WSL, Linux, and browser controls
- continuously updated paid capability packs

Closed-source material belongs in `/closed-source/` and must not become a dependency for Free.

## Exit criteria for v1.0

- Fresh install can complete service startup and profile enrollment.
- Wake is visible and synchronized with overlay lifecycle.
- `callsign.gif` appears at wake and hides only in terminal states.
- Start menu launch succeeds for common installed apps.
- Wrong, missing, stale, or weak identity never launches.
- Stop/cancel/timeout/lockout behavior is visible and testable.

