# Session State Machine

## Goal

Make authorization and cancellation explicit. No action adapter may infer session permission from a transcript alone.

## States

| State | Meaning |
|---|---|
| `Unconfigured` | No usable active profile/enrollment. |
| `Idle` | Listener ready; no authorized session. |
| `WakeDetected` | Wake event accepted; overlay starting. |
| `AwaitingIdentity` | Capturing a separate identity utterance. |
| `EvaluatingIdentity` | Comparing active-profile signals. |
| `AwaitingCommand` | Identity passed; bounded command window open. |
| `ParsingCommand` | Transcript is normalized into a supported intent. |
| `AwaitingConfirmation` | Ambiguity or risk requires user confirmation. |
| `Executing` | A validated adapter is performing a visible action. |
| `Verifying` | Result is checked. |
| `Completed` | Terminal success; authorization is cleared. |
| `Cancelled` | User or system cancelled; authorization is cleared. |
| `TimedOut` | A bounded phase expired. |
| `LockedOut` | Repeated identity failures prevent immediate retry. |
| `Faulted` | Dependency or invariant failed safely. |

## Core transitions

```text
Unconfigured -> Idle                 when an active enrolled profile becomes valid
Idle -> WakeDetected                 only on a wake detector event
WakeDetected -> AwaitingIdentity     after overlay/session initialization
AwaitingIdentity -> EvaluatingIdentity
EvaluatingIdentity -> AwaitingCommand    on match
EvaluatingIdentity -> LockedOut/Cancelled on mismatch policy
AwaitingCommand -> ParsingCommand
ParsingCommand -> AwaitingConfirmation   on ambiguity/risk
ParsingCommand -> Executing              on supported unambiguous intent
Executing -> Verifying
Verifying -> Completed/Faulted
any active state -> Cancelled             on stop/cancel
bounded state -> TimedOut                 on deadline
terminal -> Idle                          after cleanup
```

## Invariants

- `Idle` carries no action authorization.
- A `WakeDetected` event has a detector correlation ID.
- Identity success is scoped to one session and expires.
- Command capture starts after identity success and not in the same audio turn.
- Every active session has one cancellation source.
- Every transition is validated centrally.
- Terminal cleanup clears captured text, authorization, pending confirmations, and adapter handles.
- A restart never restores an authorized session.
- A profile switch cancels the active session.
- An action cannot continue after terminal transition.

## Snapshot contract

The runtime exposes a redacted snapshot containing:

- State.
- Session and correlation IDs.
- Active-profile identifier, redacted or stable local ID.
- Wake readiness and last score.
- Microphone readiness.
- Identity status.
- Last safe readout.
- Current deadline.
- Last structured error.
- Overlay visibility.
- Pending action summary.
- Version and process health.

See `schemas/runtime-snapshot.schema.json`.

## Concurrency

- One active voice session per interactive user by default.
- New wake events are ignored or recorded while a session is active.
- UI commands use the same transition API as voice events.
- Adapter completion after cancellation is discarded and audited.
- State changes are serialized.
- Deadlines use monotonic time where possible.

## Tests

Use table-driven transition tests for every allowed and denied edge. Include process restart, profile switch, duplicate wake, late dependency callback, repeated cancellation, and lockout expiry.
