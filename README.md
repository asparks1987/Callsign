# Callsign

Callsign is the open-source Windows voice assistant for the current alpha.

It is meant to feel as approachable as Apple Voice Control and stay visibly safer because every voice session follows the same structure:

`Callsign -> identity verification -> command -> visible action`

## Why Callsign exists

Desktop voice tools are often either too shallow, too hidden, or too hard to trust.
Callsign tries to solve that by making the whole session visible:

- wake with `Callsign`
- show the wake overlay
- show what was heard
- verify the enrolled callsign
- then launch the visible action

## Alpha release line

All Alpha v1 features are free and remain free until at least beta.

The open-source Free tier is the part that most closely reaches parity with built-in Windows voice control:

- wake with `Callsign`
- verify the user's callsign and voice
- launch installed apps through visible Start menu flow
- show live feedback, transcripts, and stop controls
- stay local-first and inspectable

| Release | Scope |
|---|---|
| `v1.0 alpha` | Background service wake detection, identity verification, `callsign.gif` wake overlay, live text readout, and visible Start menu app launch. |

## v1.0 minimum bar

1. Create and save a local account and callsign.
2. Capture and review enrollment voice samples.
3. Activate the voice runtime and confirm listener health.
4. Say `Callsign` or `call sign` and see `callsign.gif` appear on screen.
5. Speak your callsign and confirm identity.
6. Speak the app launch command.
7. Watch the app open through Start menu flow.
8. Validate explicit stop, cancel, timeout, and lockout behavior.

## Product structure

- `src/Callsign.UI` is setup, monitoring, and configuration.
- `src/Callsign.Service` is the background runtime for wake, identity, and command flow.
- `docs/` contains the public site and generated reference docs.
- `CANON.md` is the canon book for the project, mirrored in `docs/reference/CANON.md`.

## Current scope

- Free is the open-source core and the public face of Callsign.
- Free is the part that should closely match and often beat Windows voice control on visible everyday tasks.
- The current release line stops at visible Start menu app launch.
- Later features such as dictation, browser control, system control, and future packaging are future work.

## Platform direction

Windows is the practical alpha launch platform first.

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
