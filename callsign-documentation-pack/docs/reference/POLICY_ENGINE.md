# Policy Engine

## Purpose

The policy engine is the authorization boundary for current and future action adapters. It is independent of the model, prompt, and tool implementation.

## Inputs

A policy decision may use:

- Tool and version.
- Validated normalized arguments.
- Active release/capability set.
- Session and identity state.
- Active process/window context.
- Visibility state.
- Data/privacy classifications.
- Risk tier.
- Reversibility.
- Requested verification.
- User-configured allowlists.
- Prior approval scoped to the current task.
- Platform and integrity level.

It must not accept a free-form model claim as proof of user permission.

## Decisions

- `allow`
- `require_approval`
- `deny`
- `handoff`

See `schemas/policy-decision.schema.json`.

## Default posture

- Unknown tool: deny.
- Invalid or oversized input: deny.
- Missing identity/session state: deny.
- Blocked risk class: deny or handoff.
- External side effect: explicit specific approval every time until a later reviewed policy says otherwise.
- Local state change: approval by default.
- Local reversible action: allow only in verified visible context.
- Observation: minimize data and require an active permitted session where appropriate.

## Approval

An approval is:

- Specific to the target and consequence.
- Time-bounded.
- Single-use unless the user explicitly approves a bounded task scope.
- Invalidated by target/context change.
- Recorded without sensitive content.
- Never inferred from silence.
- Not reusable across sessions or profiles.

Example:

> Rename `Screenshot.png` to `router-settings.png` in Downloads?

Bad:

> Allow Callsign to manage files?

## Path policy

- Expand only trusted configuration variables.
- Canonicalize.
- Resolve reparse points where possible.
- Confirm the path remains inside an approved root.
- Reject traversal and alternate data stream tricks.
- Recheck before mutation.
- Never build a shell command.

## UI policy

- Verify active process/window.
- Require a semantic target.
- Escalate based on target text and app context.
- Block password, payment, 2FA, wallet, security, and unknown sensitive fields.
- Treat submit/send/delete/install/elevate controls as higher risk.

## Policy tests

Every rule needs:

- Positive allow case.
- Negative deny case.
- Approval case.
- Context-change case.
- Missing-data case.
- Normalization edge cases.
- Audit assertion.
- Regression ID tied to the threat model.

## Administration

Alpha policies are code/config shipped with the product. Users may tighten policy. Relaxing blocked boundaries requires an accepted design and cannot be hidden in an advanced settings toggle.
