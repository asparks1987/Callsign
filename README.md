# Callsign

Callsign is a Windows + WSL-first voice assistant for your desktop.

Say `Callsign`, confirm your callsign, and ask it to open the app you want from the Start menu. The goal is simple: make desktop work feel magical without making it invisible.

## What Callsign is

- Open source at the core.
- Free version limited to voice/callsign identity and launching apps from the Start menu.
- Pro tier planned for full Windows, WSL, and Linux control by voice.
- Advanced tier planned for large command libraries, recipes, diagnostics, and power-user workflows.
- Built around a visible, consent-first workflow.
- Designed for voice identity, account setup, and safe app launching.

## What the alpha does now

- Create and manage user accounts.
- Enroll voice samples for a user profile.
- Confirm the user with wake word plus spoken callsign.
- Launch installed apps through a visible Start menu search flow.
- Dictate text into a visible editor and copy or paste the result.
- Open websites or search the web through the default browser.
- Search the intended local file scope and open matching results.

## What makes it different

- It stays visible while it works.
- It asks for identity before it acts.
- It keeps the first release focused on the desktop tasks people actually use.
- It is being built as an open source product with future paid tiers called Pro and Advanced to help cover costs and create profit, while the core experience stays free.

## Current app

The current implementation is a Windows setup and onboarding app in `src/Callsign.UI`.

It lets you:

- Create a profile.
- Save your callsign.
- Record voice samples.
- Train the enrolled voice state.
- Try the session flow that leads to app launch.

## Why this exists

Most desktop assistants either feel too hidden or too fragile. Callsign is meant to feel like a helpful system service with a human face: responsive, understandable, and easy to stop.

## Public site

The GitHub Pages site lives in `docs/` and is generated from the markdown reference docs. The landing page is written to sell the product to the public while the reference pages remain available for contributors.

## Roadmap

- Finish the Free tier: voice/callsign recognition, reliable Start menu app launching, dictation, browsing, and file search.
- Add a real always-on background service.
- Expand into Pro: full Windows, WSL, and Linux control by voice with policy and approvals.
- Expand into Advanced: hundreds of commands, recipes, diagnostics, and specialized workflows.
- Keep Linux as an MVP target alongside Windows, with WSL as the development and runtime bridge.

## Build and Smoke Test

From a fresh checkout:

```powershell
.\buildcallsign.ps1
dotnet run --project tests/Callsign.AlphaSmoke/Callsign.AlphaSmoke.csproj
```

The build script emits `Callsign-Setup.exe` and `Callsign-Run.exe` in the repo root. The smoke test exercises profile persistence, voice/session gating, app launch validation, browser helper validation, and file-search helper validation.

The canonical step-by-step product burndown lives in `burndown.md`.

## Closed-source boundary

Private paid-tier material, licensing experiments, proprietary command packs, and internal business work belong in `closed-source/`. That directory is intentionally ignored by git.

## Safety

Callsign is not intended to be stealth software, a credential handler, a shell runner, or a hidden remote-control tool.
