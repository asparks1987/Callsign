# Architecture

## Overview

Callsign uses a service-first Windows architecture for the current Alpha and reserves a separate local automation boundary for later capability expansion.

## Current architecture

```text
                       local user
                           │
             ┌─────────────▼─────────────┐
             │ Callsign.UI               │
             │ onboarding/configuration  │
             │ enrollment/monitoring     │
             │ visible review/repair     │
             └─────────────┬─────────────┘
                           │ local profile + status
             ┌─────────────▼─────────────┐
microphone ->│ Callsign.Service          │-> overlay/readout
             │ wake • identity • session │
             │ transcription • routing   │
             └──────┬─────────┬──────────┘
                    │         │
          ┌─────────▼───┐ ┌───▼────────────┐
          │ launch       │ │ later Alpha    │
          │ Start menu   │ │ browser/files  │
          └──────────────┘ └────────────────┘
```

### `Callsign.UI`

Responsibilities:

- First-run experience.
- Profile create/select/update/delete.
- Voice-sample recording, playback, reset, and enrollment.
- Runtime status and health.
- Diagnostics and repair entry points.
- Visible dictation review.
- Configuration that requires human understanding.

The UI is not the always-on listener.

### `Callsign.Service`

Responsibilities:

- Audio-device selection and microphone lifecycle.
- Wake-detector lifecycle.
- Wake-event emission.
- Active-profile loading.
- Voice/callsign gate.
- Session timing, cancellation, timeout, and lockout.
- Post-wake transcription.
- Command-intent parsing.
- Overlay/readout state.
- Bounded action routing.
- Runtime snapshots and sanitized diagnostics.

The service MUST not become an arbitrary command executor.

### Local storage

Profiles and runtime metadata live under `%LOCALAPPDATA%\Callsign\`. Exact schemas are defined in [DATA_MODEL.md](DATA_MODEL.md) and `/schemas`.

Storage adapters SHOULD be versioned and atomic. Corrupt or incompatible files MUST fail visibly and preserve recoverable data where possible.

## Target automation architecture

After the policy and audit foundations are implemented:

```text
Voice/Agent Host
  conversation • provider routing • planning • approvals
                        │ MCP over local stdio
                        ▼
Callsign Automation Server
  typed tools • policy • privacy • verification • audit
                        │
      native APIs -> UI Automation -> bounded fallback
```

### Why separate host and automation server

- The model cannot authorize itself.
- Automation dependencies remain local and testable.
- Protocol contracts can be fuzzed and versioned.
- stdout can remain protocol-only.
- Policy and audit can cover every action consistently.
- Future hosts or providers do not inherit unrestricted OS access.

## Process and privilege model

Target rules:

- Run at normal user integrity.
- Do not self-elevate.
- Detect elevated targets and hand off.
- Bind local IPC to the active user.
- Authenticate any IPC that can cross a process/user boundary.
- Keep service, UI, and helper ownership explicit.
- Avoid a network listener for local automation.
- Make startup mode and process health visible in the UI.

The exact Windows service account and IPC mechanism remain decisions; see [OPEN_QUESTIONS.md](OPEN_QUESTIONS.md).

## Dependency direction

```text
UI -> application contracts -> storage/runtime clients
Service -> application contracts -> audio/wake/identity/action adapters
Automation protocol -> policy/tool interfaces
Infrastructure -> implements interfaces
Domain/session state -> depends on no UI, protocol, or vendor SDK
```

Avoid circular dependencies between UI and service assemblies.

## Reliability boundaries

- Audio and wake loops use bounded buffers and cancellation.
- A failed transcription provider does not bypass state transitions.
- Overlay failure is visible but never authorizes an action.
- Action adapter failure returns a structured terminal result.
- Profile corruption does not start an unconfigured session.
- Logs do not become an unbounded data store.
- External helpers have timeouts, version checks, and kill behavior.

## Current versus future rules

Every architecture document and diagram MUST label components as:

- `Current`
- `Target`
- `Deferred`

Do not use a future MCP tool catalog as evidence that the tools ship.
