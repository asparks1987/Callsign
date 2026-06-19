# Manual `v1.0 Alpha` Walkthrough

## Evidence header

Record:

- Commit:
- Build ID:
- Artifact hashes:
- Date/time:
- Tester:
- Windows version/edition/build:
- Architecture:
- Machine/VM:
- Microphone:
- Install mode:
- UI/runtime versions:
- Wake/identity/transcription models:

Use synthetic profile data.

## A. Clean install

- [ ] No previous Callsign processes.
- [ ] Choose whether prior test data is removed.
- [ ] Run setup visibly.
- [ ] Install completes without terminal.
- [ ] Shortcut exists.
- [ ] UI opens.
- [ ] Runtime mode/version/health are visible.
- [ ] Logs are created in documented local location.
- [ ] No unexpected network activity.

## B. Profile and enrollment

- [ ] Blank first run does not crash.
- [ ] Invalid callsigns are rejected.
- [ ] Valid test profile saves.
- [ ] Profile survives restart.
- [ ] Selected microphone is visible.
- [ ] Press/hold recording state is obvious.
- [ ] Silence/short/noisy sample gives useful error.
- [ ] Playback works.
- [ ] Reset works.
- [ ] Required samples enroll successfully.
- [ ] Storage/retention disclosure is visible.

## C. Wake and overlay

- [ ] Runtime reports wake ready.
- [ ] Ambient speech does not wake during observation window.
- [ ] Spoken `Callsign` wakes.
- [ ] Overlay appears promptly.
- [ ] Overlay does not steal focus.
- [ ] Text says identity is required.
- [ ] `stop` cancels and hides overlay.
- [ ] Transcript-only test cannot manufacture wake.

## D. Identity

- [ ] Correct test callsign passes.
- [ ] Identity utterance does not also execute a command.
- [ ] Wrong callsign fails.
- [ ] Repeated failures lock out according to policy.
- [ ] Timeout returns to idle.
- [ ] Switching profile cancels active session.

## E. App launch

- [ ] `Open Notepad` resolves and shows target.
- [ ] Launch occurs visibly.
- [ ] Result is verified.
- [ ] Missing app fails safely.
- [ ] Ambiguous target asks or fails safely.
- [ ] Path rejected.
- [ ] URL rejected.
- [ ] Shell/PowerShell/WSL request rejected.
- [ ] Cancel before execution does nothing.
- [ ] Cancel during adapter work prevents late action.

## F. Failure and recovery

- [ ] Disconnect microphone.
- [ ] Stop runtime.
- [ ] Remove/tamper with test wake model.
- [ ] Fill/deny log storage in controlled environment.
- [ ] Restart UI while runtime active.
- [ ] Restart runtime.
- [ ] Reinstall over running version.
- [ ] Repair controls explain changes.
- [ ] Uninstall behavior is clear.

## G. Accessibility

- [ ] Keyboard-only setup.
- [ ] Visible focus.
- [ ] Screen reader announces key states.
- [ ] High contrast.
- [ ] 200% text scaling.
- [ ] Reduced-motion/static overlay path.
- [ ] Stop path does not require precise pointer action.

## Result

- Pass:
- Fail:
- Blocked:
- Evidence paths:
- Sensitive-data review:
- Remaining uncertainty:
- Release recommendation:
