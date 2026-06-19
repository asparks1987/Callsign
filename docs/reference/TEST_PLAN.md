# Test Plan

## Purpose

`v1.0 alpha` is proven when the core service runtime works end-to-end from install through live launch.

All Alpha v1 features are free and remain free until at least beta.

## v1.0 automated coverage

Default smoke command:

```powershell
dotnet run --project tests/Callsign.AlphaSmoke/Callsign.AlphaSmoke.csproj
```

Required checks:

- fresh install and service start
- wake-only speech does not launch
- wake word detection and identity gate works for `Callsign` / `call sign`
- `callsign.gif` appears and session readout updates while active
- identity success and mismatch handling
- visible Start menu launch path
- safe stop/cancel/timeout/lockout handling
- runtime snapshot includes session state, transcript, overlay readout, and microphone telemetry

## Manual Alpha v1 smoke walkthrough

1. Build and install from clean state.
2. Create profile and train identity.
3. Speak wake phrase and confirm overlay + live text.
4. Speak callsign.
5. Speak launch command.
6. Confirm Start menu action is visible.
7. Validate wrong identity and no-mic cases do not launch.

## Follow-up alpha checks

### v1.1

- dictation review path
- clear stop/abort behavior

### v1.2

- visible browser control
- external action boundaries

### v1.3

- system control by explicit voice commands
- file search results open in Explorer

## Release gate

No `v1.0 alpha` release until:

- build site and installer are reproducible
- service/session fields are readable in runtime state
- overlay is visible at wake and hidden in terminal states
- identity failures remain safe and visible
