# Callsign

Callsign is a Windows + WSL-first voice assistant for your desktop.

Say `Callsign`, confirm your callsign, and ask it to open the app you want from the Start menu. The goal is simple: make desktop work feel magical without making it invisible.

## What Callsign is

- Open source at the core.
- Free version limited to launching apps from the Start menu.
- Built around a visible, consent-first workflow.
- Designed for voice identity, account setup, and safe app launching.

## What the alpha does now

- Create and manage user accounts.
- Enroll voice samples for a user profile.
- Confirm the user with wake word plus spoken callsign.
- Launch installed apps through a visible Start menu search flow.

## What makes it different

- It stays visible while it works.
- It asks for identity before it acts.
- It keeps the first release focused on the desktop tasks people actually use.
- It is being built as an open source product with future paid tiers called Home and Advanced to help cover costs and create profit, while the core experience stays free.

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

- Finish the alpha onboarding flow.
- Add a real always-on background service.
- Add richer voice capture and recognition.
- Expand desktop automation carefully.
- Keep Linux as an MVP target alongside Windows, with WSL as the development and runtime bridge.

## Safety

Callsign is not intended to be stealth software, a credential handler, a shell runner, or a hidden remote-control tool.
