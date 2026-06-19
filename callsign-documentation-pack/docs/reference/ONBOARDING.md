# Onboarding and Profile Setup

## Goal

A first-time user should understand what Callsign listens for, what data is local, what the voice/callsign gate can and cannot guarantee, and how to stop or remove the product.

## First-run sequence

1. Welcome and product boundary.
2. Local-data and microphone disclosure.
3. Create display name and callsign.
4. Validate the callsign.
5. Select/test microphone.
6. Record the required enrollment samples.
7. Replay and accept or retry each sample.
8. Explain local sample and metadata storage.
9. Activate the profile.
10. Verify wake runtime health.
11. Rehearse the overlay without executing an action.
12. Run an optional guided `Open Notepad` test.
13. Show stop, reset, logs, deletion, and uninstall paths.

## Callsign requirements

A callsign:

- Is human-readable.
- Has a bounded length.
- Is normalized consistently.
- Cannot contain path separators or reserved filesystem names.
- Cannot be blank or whitespace-only.
- Should be distinguishable from the wake phrase and common command words.
- Is stored separately from display name.
- Can be changed only through an explicit profile operation.

Exact validation rules must match `schemas/profile.schema.json` and implementation tests.

## Enrollment UX

- Use press-and-hold or an equally obvious recording state.
- Show input level and clipping/silence feedback.
- Require fresh samples.
- Let the user replay before accepting.
- Explain how many samples are required.
- Never claim enrollment is secure authentication.
- Provide reset and deletion.
- Detect missing microphone, silence, excessive noise, too-short capture, and unsupported format.

## Permissions and disclosure

Before microphone use, explain:

- Wake detection runs while the runtime is enabled.
- Post-wake transcription begins only after an accepted wake event.
- Which components are local.
- Which components, if any, contact a network provider.
- What is stored.
- How to stop listening.
- How to delete profile and samples.

## Existing-profile flow

- List profiles without exposing sensitive metadata.
- Make the active profile obvious.
- Switching profile cancels any active session.
- Damaged profile data opens a recovery path, not an implicit reset.
- Profile deletion requires a specific confirmation and reports what was removed.

## Acceptance criteria

A clean user can complete onboarding without a terminal, can understand the data boundary, can retry every reversible step, and can exit without leaving a partially authorized runtime.
