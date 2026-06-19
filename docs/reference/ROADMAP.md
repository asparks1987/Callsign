# Roadmap

## Release ladder

All Alpha v1 features are free and remain free until at least beta.

| Release | Focus | Goal |
|---|---|---|
| `v1.0 alpha` | Wake + identity + overlay + Start menu launch | Prove the core visible session model end to end. |
| `v1.1 alpha` | Dictation with review | Let users dictate text while keeping insertion/copy actions explicit and visible. |
| `v1.2 alpha` | Browser control | Add visible open/search/navigation and bounded browser workflows. |
| `v1.3 alpha` | System control and file search | Add Windows, WSL, Linux, and Explorer-backed file search workflows with policy gates. |
| `Beta or later` | Packaging and extension libraries | Decide Pro/Advanced packaging, entitlement, signed libraries, and paid command catalogs. |

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

Planned:

- dictation capture after identity
- visible text review
- explicit insert/copy/paste controls
- no silent text entry into sensitive fields
- clear cancel/reset behavior

## v1.2 alpha

Planned:

- visible browser open/search/navigation
- safe URL and search parsing
- clear boundary around form submission, uploads, messages, and external side effects
- approval prompts for higher-risk browser actions

## v1.3 alpha

Planned:

- Windows system control through visible, policy-governed adapters
- WSL and Linux command/control paths that are not arbitrary hidden shell execution
- file search with results shown to the user
- Explorer-backed open/reveal behavior
- stronger audit and approval coverage for state-changing commands

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
