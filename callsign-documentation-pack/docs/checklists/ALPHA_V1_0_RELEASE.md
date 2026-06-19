# `v1.0 Alpha` Release Checklist

## Canon and scope

- [ ] Release is explicitly `v1.0 alpha`.
- [ ] Later Alpha features are not release blockers or overclaimed.
- [ ] Safety and identity language matches `CANON.md`.
- [ ] Known limitations are public.
- [ ] License decision is complete.

## Build and artifacts

- [ ] Clean checkout builds.
- [ ] UI, runtime, overlay, icon, wake model, and required helpers are packaged.
- [ ] Payload manifest and SHA-256 hashes exist.
- [ ] Third-party notices are complete.
- [ ] No private or personal assets.
- [ ] Signature status is disclosed.

## Install and lifecycle

- [ ] Clean install.
- [ ] Reinstall over running version.
- [ ] Shortcut.
- [ ] Runtime startup.
- [ ] UI/runtime version match.
- [ ] Restart recovery.
- [ ] Uninstall.
- [ ] User-data removal choice.

## Core flow

- [ ] Profile create/persist/delete.
- [ ] Enrollment capture/playback/reset.
- [ ] Wake detector event.
- [ ] Transcript cannot fake wake.
- [ ] Overlay lifecycle.
- [ ] Identity match/mismatch/timeout/lockout.
- [ ] Plain app target.
- [ ] Unsafe input rejection.
- [ ] Ambiguity handling.
- [ ] Visible launch.
- [ ] Verification.
- [ ] Stop/cancel in every active state.

## Quality

- [ ] Smoke suite.
- [ ] Installed verifier.
- [ ] Human-spoken walkthrough.
- [ ] Clean VM/user.
- [ ] Soak and device reconnect.
- [ ] Accessibility matrix.
- [ ] Redaction/secret scan.
- [ ] Docs generated with no drift.

## Approval

- [ ] Product.
- [ ] Runtime.
- [ ] Packaging.
- [ ] Security/privacy.
- [ ] Accessibility.
- [ ] Release evidence.
