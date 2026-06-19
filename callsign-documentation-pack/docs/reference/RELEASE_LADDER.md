# Release Ladder

## Terminology

- **Alpha v1:** the family of releases from `v1.0 alpha` through `v1.3 alpha`.
- **Current release target:** the one release whose exit criteria define current readiness.
- **Partial implementation:** code may exist, but the capability remains outside the current release promise until its own gate is met.

## `v1.0 alpha` — wake, identity, and visible app launch

### Promise

A fresh Windows user can install Callsign, create and enroll a local profile, wake the background runtime, pass the active-profile voice/callsign gate, see the overlay/readout, and launch a common installed app through a visible path.

### Exit gate

- Clean installation.
- UI and runtime start without developer tooling.
- Enrollment succeeds and survives restart.
- openWakeWord produces the wake transition.
- Transcript text cannot fake wake.
- Identity mismatch and timeout block action.
- Overlay lifecycle is correct.
- Safe app-name validation and resolution work.
- Visible launch succeeds.
- Negative and cancellation paths are proven.
- Current verifier output and one human-spoken walkthrough are attached.

## `v1.1 alpha` — visible dictation

### Promise

The user can dictate text into a visible review surface and explicitly choose what happens to it.

### Exit gate

- Start/stop/cancel work.
- Text remains visible before transfer.
- Copy/paste/insertion are explicit.
- Sensitive-field and unknown-target boundaries are enforced.
- No hidden insertion occurs.

## `v1.2 alpha` — bounded browser control

### Promise

The user can visibly open a browser target, search, and navigate supported low-risk flows.

### Exit gate

- URLs and searches are normalized and visible.
- Local paths and command-like input are rejected.
- Page content cannot grant permission.
- External submissions remain blocked or explicitly approved by a later accepted design.
- Navigation can be stopped and recovered.

## `v1.3 alpha` — policy-gated system control and file search

### Promise

The user can perform an approved set of Windows, WSL, and Linux workflows, including visible name/path-based file search and Explorer result opening.

### Exit gate

- Policy, risk, approval, verification, and audit contracts are implemented.
- Supported roots and commands are explicit.
- File contents are not read by default.
- WSL/Linux boundaries are documented and tested.
- State-changing actions have rollback or human handoff.

## Beta or later

Potential goals:

- Signed artifacts.
- Update and rollback channels.
- Maintained support matrix.
- Privacy-preserving diagnostics.
- Formal security review.
- Hardened local automation server.
- Transparent Free/Pro/Advanced packaging, if adopted.
- Support and disclosure channels.
- Compatibility and migration guarantees.

## Scope rule

A later-release feature may be developed early. It must still be labeled experimental and must not expand the earlier release gate accidentally.
