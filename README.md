# Callsign

Callsign is the open-source Windows voice-control killer.

It is meant to feel as approachable as Apple Voice Control, build toward more power than Talon, and stay visibly safer because every voice session follows the same structure:

`Callsign -> identity verification -> command -> visible action`

## Why Callsign exists

Desktop voice tools are often either too shallow, too hidden, or too hard to trust.
Callsign tries to solve that by making the whole session visible:

- wake with `Callsign`,
- show the wake overlay,
- show what was heard,
- verify the enrolled callsign,
- then launch the visible action.

## Alpha release ladder

All Alpha v1 features are free and remain free until at least beta.

The open-source Free tier is the part that most closely reaches parity with, and often surpasses, built-in Windows voice control:

- wake with `Callsign`
- verify the user's callsign and voice
- launch installed apps through visible Start menu flow
- show live feedback, transcripts, and stop controls
- stay local-first and inspectable

Paid tiers are for continuously expanding the ceiling:

- Pro adds deeper computer control, broader Windows/WSL/Linux workflows, and stronger automation surfaces
- Advanced adds the fastest-moving command catalog, specialized recipes, diagnostics, and power-user automation that can grow as often as we ship it

| Release | Scope |
|---|---|
| `v1.0 alpha` | Background service wake detection, identity verification, `callsign.gif` wake overlay, live text readout, and visible Start menu app launch. |
| `v1.1 alpha` | Dictation with visible review and explicit insertion/copy controls. |
| `v1.2 alpha` | Browser control with visible open/search/navigation. |
| `v1.3 alpha` | System control for Windows, WSL, and Linux, including Explorer-backed file search. |
| `Beta or later` | Revisit Pro / Advanced packaging and continuously extended command layers. |

## v1.0 minimum bar

1. Create and save a local account and callsign.
2. Capture and review enrollment voice samples.
3. Activate the voice runtime and confirm listener health.
4. Say `Callsign` or `call sign` and see `callsign.gif` appear on screen.
5. Speak your callsign and confirm identity.
6. Speak the app launch command.
7. Watch the app open through Start menu flow.
8. Validate explicit stop/cancel/timeout/lockout behavior.

## Product structure

- `src/Callsign.UI` is setup, monitoring, and configuration.
- `src/Callsign.Service` is the background runtime for wake, identity, and command flow.
- `docs/` contains the public site and generated reference docs.
- `CANON.md` is the canon book for the project, mirrored in `docs/reference/CANON.md`.

## Tier promise

- Free is the open-source core and the public face of Callsign.
- Free is the part that should closely match and often beat Windows voice control on visible everyday tasks.
- Pro and Advanced are the paid expansion tiers layered on top of the same visible runtime.
- Pro is for deeper Windows, WSL, Linux, browser, workflow, and system control.
- Advanced is for the fastest-moving command library: new commands, recipes, diagnostics, and specialized workflows that can keep growing without rebuilding the open-source core.
- See `docs/reference/TIER_ARCHITECTURE.md` for the installer, upgrade, entitlement, and command-pack plan.

## Platform direction

Windows is the practical alpha launch platform first, with WSL and Linux support as the v1.x extension path.

## Build and verify

From a fresh checkout:

```powershell
.\buildcallsign.ps1
dotnet run --project tests/Callsign.AlphaSmoke/Callsign.AlphaSmoke.csproj
python scripts/build_site.py
```

## Safety

Callsign is designed for visible control:

- wake audio alone cannot launch commands,
- identity and policy checks gate execution,
- v1.0 remains local-first and user-visible from wake to action,
- and the overlay/readout should always show what Callsign is hearing.
