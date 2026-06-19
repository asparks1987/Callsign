# Test Plan

## Purpose

`v1.0 alpha` is proven when the Free open-source core works end-to-end from install through visible launch.

All Alpha v1 features are free and remain free until at least beta.

## v1.0 automated coverage

Default smoke command:

```powershell
dotnet run --project tests/Callsign.AlphaSmoke/Callsign.AlphaSmoke.csproj
```

Required checks:

- fresh install/service startup assumptions
- profile creation and local settings persistence
- wake-only speech does not launch
- wake word detection and identity gate for `Callsign` / `call sign`
- `callsign.gif` appears and session readout updates while active
- identity success and mismatch handling
- visible Start menu launch path
- safe stop, cancel, timeout, and lockout handling
- runtime snapshot includes session state, transcript, overlay readout, and microphone telemetry
- unsafe launch text, paths, URLs, shell fragments, and secret-like inputs are rejected

## Manual v1.0 walkthrough

1. Build and install from clean state.
2. Create profile and callsign.
3. Record and review voice samples.
4. Train or mark voice identity enrolled.
5. Confirm service/runtime health in the UI.
6. Speak wake phrase and confirm overlay + live text.
7. Speak callsign and confirm identity acceptance.
8. Speak launch command.
9. Confirm Start menu action is visible.
10. Validate wrong identity and no-mic cases do not launch.
11. Validate stop, cancel, timeout, and lockout behavior.

## v1.x test direction

Future Alpha v1 releases need additional suites:

- v1.1 dictation review and no-silent-insert tests
- v1.2 browser navigation and external-side-effect approval tests
- v1.3 system control, WSL/Linux, file search, and audit tests
- extension-library signature, entitlement, and policy tests before Pro/Advanced packaging

## Release gate

No `v1.0 alpha` release until:

- build site and installer are reproducible,
- service/session fields are readable in runtime state,
- overlay is visible at wake and hidden in terminal states,
- identity failures remain safe and visible,
- common app launches work through visible flow,
- and docs/website accurately describe open-source Free plus future closed-source extensions.
