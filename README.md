# Callsign

Callsign is an open-source, Windows-first desktop voice assistant for visible, identity-gated computer control.

The project starts with a simple interaction contract:

```text
Callsign -> identity verification -> command -> visible action
```

The Free core is the public product: inspectable, local-first, and designed so users can always see when Callsign is listening, what it heard, and how to stop it.

## Current alpha

The current repo is focused on the Alpha v1 line. v1.0 is the first public MVP:

- create a local profile and callsign
- record and review voice enrollment samples
- run a background service for wake/session state
- detect the `Callsign` wake cue through the audio path
- show `callsign.gif` with a live readout
- verify the user's callsign/voice before command capture
- launch installed apps through a visible Start menu flow
- expose stop, cancel, timeout, lockout, and runtime status states

The codebase also contains early surfaces for the broader Alpha v1 direction, including browser launch helpers, system control helpers, file search, visible-control routing, and richer command parsing. Those are part of the v1.x path, not a reason to weaken the v1.0 release gate.

## Open-source promise

Callsign is Free-first.

- The Free tier is MIT-licensed and useful on its own.
- The public repo contains the setup app, service runtime, visible overlay, profile/enrollment flow, command-routing contracts, docs, and tests for the open core.
- Alpha v1 features remain free until at least beta.
- The Free core must not depend on private code or paid entitlement.

This project should be understandable from source. If Callsign hears you, verifies you, or acts for you, the public core should make that behavior inspectable.

## Closed-source future

Callsign is also being designed for a future commercial layer.

Future Pro and Advanced features may ship as closed-source extension libraries: deeper Windows, WSL, Linux, browser, workflow, diagnostics, and specialized command catalogs that can evolve faster than the open core.

That boundary is intentional:

- Free stays the public trust layer.
- Pro and Advanced can expand the ceiling.
- Proprietary tier material belongs only in `/closed-source/`, which is ignored by git.
- Paid extensions must still pass the same identity, visibility, policy, and audit expectations.

## Alpha v1 release ladder

| Release | Scope |
|---|---|
| `v1.0 alpha` | Background service wake detection, callsign identity verification, `callsign.gif` wake overlay with live text readout, and visible Start menu app launch. |
| `v1.1 alpha` | Dictation with visible review before insertion, copy, paste, or other text actions. |
| `v1.2 alpha` | Browser control for visible open, search, navigation, and safe bounded browser tasks. |
| `v1.3 alpha` | System control for Windows, WSL, and Linux, including file search results shown or opened through Explorer. |
| `Beta or later` | Revisit Pro and Advanced packaging, entitlement, signed extension libraries, and continuously updated command catalogs. |

## Product structure

- `src/Callsign.UI` is setup, onboarding, monitoring, profile management, voice enrollment, overlay/readout UI, and user-visible controls.
- `src/Callsign.Service` is the background runtime for wake, identity, session orchestration, runtime status, and visible launch flow.
- `tests/Callsign.AlphaSmoke` contains the current alpha smoke coverage.
- `docs/reference` contains the canonical markdown used to generate the public website.
- `docs/index.html` is the generated public landing page.
- `CANON.md` is the root product canon, mirrored in `docs/reference/CANON.md`.

## Safety model

Callsign is not a hidden desktop automation framework.

- Wake audio alone cannot launch commands.
- Transcript text alone must not wake or authorize a session.
- Identity must pass before command capture.
- v1.0 actions stay visible and local.
- Arbitrary shell execution is out of scope.
- Passwords, 2FA codes, payments, account deletion, security settings, and silent external submissions are blocked.
- Screenshots, clipboard contents, file contents, and UI trees are sensitive and must not be sent to cloud models unless the user explicitly opts in.

## Build and verify

From a fresh checkout:

```powershell
.\buildcallsign.ps1
dotnet run --project tests/Callsign.AlphaSmoke/Callsign.AlphaSmoke.csproj
python scripts/build_site.py
```

Preview the public site locally:

```powershell
python -m http.server 8000 --directory docs
```

Then open `http://localhost:8000`.
