# Testing Guide

## Start from the requirement

Every test names:

- Requirement or threat ID.
- Release target.
- Evidence level.
- Expected state transition.
- Expected action or denial.
- Data/redaction assertion.

## Deterministic first

Use abstractions for:

- Clock.
- Audio frames.
- Wake events.
- Identity results.
- Transcription.
- App resolver/launcher.
- Browser/Explorer.
- Policy decisions.
- Audit sink.
- IPC.

A live test supplements deterministic coverage; it does not replace it.

## State-machine testing

Generate or table-drive:

- Every allowed transition.
- Every rejected transition.
- Timeout from each bounded state.
- Cancel from each active state.
- Late callback after terminal state.
- Restart and profile switch.
- Duplicate wake and confirmation.

## Voice fixtures

Maintain:

- Positive wake.
- Negative ambient speech.
- Wake homophones that must not trigger.
- Silence/noise/clipping.
- Matching and nonmatching identity fixtures.
- Multiple accents/speaking rates where rights permit.
- Command fixtures and unsupported intents.

Store rights/provenance beside fixtures. Never use a contributor's private enrollment audio casually.

## Policy testing

For every action:

- Allow.
- Deny.
- Approval.
- Missing session.
- Target ambiguity.
- Sensitive context.
- Normalization attack.
- Context change.
- Cancellation.
- Audit result.

## Manual tests

Use the [manual walkthrough](MANUAL_ALPHA_WALKTHROUGH.md). Record exact build hashes and sanitize evidence.

## Test isolation

- Temporary `%LOCALAPPDATA%` root.
- Temporary filesystem roots.
- Mock browser.
- Test desktop app with known UIA tree.
- No real email, payments, credentials, or personal documents.
- Restore foreground/focus after tests.
- Kill helpers on teardown.

## Flake policy

A flaky safety test is a failed test.

- Assign owner.
- Capture seed/timing/environment.
- Quarantine only with an expiry and release-blocking assessment.
- Never rerun until green and hide the first failure.
