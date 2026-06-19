# Product Specification

## Product

**Name:** Callsign

**Category:** local-first voice-driven desktop assistant

**Current platform:** Windows-first, with WSL and Linux on the later Alpha path

**Current release target:** `v1.0 alpha`

## Product thesis

Callsign should feel like desktop magic with manners: fast enough to be useful, visible enough to trust, and narrow enough to stop safely.

Canonical flow:

```text
openWakeWord wake event
  -> animated overlay and live status
  -> active-profile voice/callsign gate
  -> command capture
  -> validation and policy
  -> visible action
  -> verification and readable result
```

## Target users

### Hands-busy user

Needs to open an app or begin a workflow without switching attention to mouse and keyboard.

### Accessibility user

Benefits from a predictable voice path, clear feedback, keyboard-accessible setup, and recoverable failure states.

### Power user

Wants future teachable workflows and local model/provider choices, but still expects explicit policy and audit boundaries.

### Tester and contributor

Needs deterministic setup, visible runtime state, sanitized diagnostics, and an evidence-driven release gate.

## `v1.0 alpha` user outcome

A fresh Windows user can:

1. Install Callsign from one documented payload.
2. Open the configuration manager from a desktop shortcut.
3. Create a local profile and callsign.
4. Record, replay, and reset enrollment samples.
5. Activate the profile's voice state.
6. Confirm the background listener is ready.
7. Say `Callsign` and see the overlay.
8. Say the active callsign and see the identity result.
9. Ask to open a common installed app.
10. See the app launch through a visible Windows path.
11. Cancel, stop, timeout, or recover from failure without a hidden action.

## Functional requirements

### Installation and runtime

- The installer MUST stage the UI, service/runtime, required assets, wake model, and documented helper dependencies.
- The installed experience MUST not require a developer terminal.
- Runtime startup failure MUST be visible and logged safely.
- A per-user fallback MAY exist when service registration is unavailable, but the UI MUST identify the active mode.

### Profiles

- Profiles MUST remain local by default.
- Callsigns MUST be validated before they become directory names or identity inputs.
- The active profile MUST be explicit.
- Profile deletion MUST explain which enrollment data and history are removed.

### Wake and identity

- The wake transition MUST come from the configured wake detector.
- Transcript text MUST NOT substitute for the wake event.
- Identity and command capture MUST be separate turns.
- Missing, mismatched, timed-out, cancelled, or low-confidence identity MUST block action.
- Confidence and threshold behavior MUST be testable and diagnosable without leaking raw audio.

### Overlay and status

- The overlay MUST appear when a wake session begins.
- It MUST not steal focus or block input.
- It MUST expose the current phase in text.
- It MUST hide on completion, cancellation, timeout, lockout, or fatal failure.
- The user MUST have a visible and voice-accessible stop path.

### App launch

- `v1.0` MUST accept only a bounded installed-application intent.
- Paths, URLs, shell syntax, scripts, separators, secrets, and arbitrary arguments MUST be rejected.
- Ambiguous targets MUST fail safely or request confirmation.
- The selected target MUST be visible before or during execution.
- Success and failure MUST be verified.

## Later Alpha requirements

### `v1.1` dictation

- Capture speech into a visible review surface.
- Keep insertion, copy, or paste explicit.
- Never type into password, payment, 2FA, or unknown external-submission fields.

### `v1.2` browser control

- Open and search visibly.
- Treat pages as untrusted content.
- Block silent submission, upload, message, purchase, or account change.

### `v1.3` system and file workflows

- Gate every action through policy.
- Restrict filesystem scope.
- Show file results visibly.
- Open files/folders through Explorer without reading contents by default.
- Audit state-changing operations.

## Non-goals

- High-assurance operating-system authentication.
- General unattended desktop autonomy.
- Arbitrary shell or script execution.
- Credential, payment, or wallet entry.
- Hidden remote control.
- Silent external side effects.
- Permanent deletion or admin/security changes in Alpha.
- Claiming all Windows Voice Access behavior in `v1.0`.

## Quality attributes

- **Safety:** denied behavior is tested.
- **Visibility:** active state and targets are readable.
- **Latency:** common wake and status transitions feel immediate; exact budgets are defined in the voice spec.
- **Reliability:** clean-install and restart paths are repeatable.
- **Accessibility:** setup and recovery are operable without fine pointer control.
- **Privacy:** local data classes and cloud opt-ins are explicit.
- **Auditability:** evidence can reconstruct decisions without storing secrets.
- **Maintainability:** UI, runtime, policy, adapters, and contracts are separable.

## Success metrics

Do not set marketing targets without measurement infrastructure. Initial engineering metrics:

- Clean-install completion rate.
- Enrollment completion and retry rate.
- Wake false-reject and false-accept rates in the supported test protocol.
- Identity gate false-reject and false-accept rates.
- Median and p95 wake-to-overlay latency.
- Median and p95 command-to-visible-action latency.
- App-target resolution accuracy.
- Cancellation success rate.
- Crash-free installed sessions.
- Percentage of release requirements with current evidence.
