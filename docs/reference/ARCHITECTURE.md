# Architecture

## Overview

Callsign is moving toward a split architecture:

- A visible Windows setup and onboarding app exists now.
- A background voice service is the next major step.

The current repo state is not a full always-on agent yet. The code today focuses on account setup, voice enrollment state, session gating, and a visible Start menu launch flow.

## Current implementation shape

### Windows setup app

The current app is a WinForms onboarding experience that lets the user:

- Create and select an account.
- Save a callsign profile.
- Record and reset voice samples.
- Start the wake word plus identity session flow.
- Launch an installed app through Start search.

### Local profile store

Profiles are stored locally under `%LOCALAPPDATA%\Callsign\Profiles\<callsign>\settings.json`.

The storage model is simple on purpose:

- One folder per callsign.
- One settings file per profile.
- Local-only by default.

### Session state machine

The alpha session flow is:

1. Idle.
2. Wake word detected.
3. Identity verification.
4. Command capture.
5. Launch or cancel.
6. Timeout or lockout on failed identity.

This is the first step toward a real service process.

## Target architecture

### 1. Background service

The future service will handle:

- Wake word listening.
- Identity confirmation.
- Command capture.
- Session timing and lockout.
- Task handoff to the desktop launcher or future automation layers.

### 2. Windows desktop interaction layer

The launcher and later automation logic should stay visible, safe, and easy to stop.

For alpha, the visible app launch path is deliberately simple:

- Open Start search.
- Type the app name.
- Launch the matching app.

### 3. Future automation layer

Later phases can add richer desktop automation, but only after the alpha account and identity flow is reliable.

## Platform direction

### Windows and Linux MVP

Windows and Linux are both MVP targets.

### WSL as the bridge

WSL is the bridge for Linux development and runtime workflows from Windows.

The current alpha should keep the Windows desktop path visible while also keeping the Linux story realistic through WSL-first tooling and parity planning.

## Design rules

- Keep the user visible to the system.
- Keep identity before action.
- Keep local state simple and inspectable.
- Keep the product easy to explain.
