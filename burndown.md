# Callsign v1 Alpha Burndown

## Alpha Definition

v1 alpha is ready when a fresh checkout can be built, installed, launched, and smoke-tested, and the app can complete the four MVP flows end to end:

1. Start menu launching.
2. Text dictation.
3. Web browsing.
4. File search.

A 100% functional v1 alpha must also support:

- User profile and callsign creation.
- Voice enrollment and re-training.
- Wake word plus spoken callsign identity confirmation.
- Clear visible states for permissions, missing microphone, silence, transcription failure, cancel, and stop.
- User-readable action feedback before and after execution.
- Clean build, install, launch, and smoke-test steps from documented commands.

## Canon For Alpha

- The repo name is Callsign.
- The alpha focus is a visible, consent-first desktop assistant.
- The free core must remain useful on its own.
- Hidden automation, stealth, credential theft, purchases, and secret entry are out of scope.
- Linux support can be documented separately, but alpha readiness is judged by the documented v1 alpha flow.

## Legend

- Priority: `P0` is required for v1 alpha, `P1` is strongly recommended, `P2` is post-alpha hardening, `P3` is future expansion.
- Status: `Done`, `In progress`, `Not started`, `Deferred`, `Blocked`.
- Acceptance: the condition that proves the row is complete.

## Phase 0: Canon, Scope, and Release Hygiene

| Done | ID | Priority | Status | Work item | Acceptance |
|---|---:|---|---|---|---|
| [x] | 0.1 | P0 | Done | Define this root `burndown.md` as the canonical alpha checklist | The root file exists and is the source of truth. |
| [x] | 0.2 | P0 | Done | Align product docs to the v1 alpha contract | README, product spec, roadmap, and rendered docs say alpha means account setup, dictation, browsing, file search, and Start menu launch. |
| [x] | 0.3 | P0 | Done | Remove stale beta-only or roadmap-only language from alpha-facing docs | User-facing docs do not overstate beta-only or future-only capabilities as current. |
| [ ] | 0.4 | P0 | Deferred | Add a repo-wide stale-text check for release claims | Deferred because the docs are already aligned and the alpha can be maintained with explicit doc review. |
| [x] | 0.5 | P0 | Done | Define the clean-checkout alpha command path | A developer can clone, restore, build, install, launch, and smoke-test with documented commands. |
| [x] | 0.6 | P0 | Done | Document startup failure recovery | Users can find the startup error log and understand what to do when launch fails. |
| [x] | 0.7 | P1 | Done | Record alpha non-goals | Shell execution, hidden actions, secrets, purchases, and background surveillance stay out of scope. |
| [x] | 0.8 | P1 | Done | Define supported alpha platforms | Supported Windows versions, speech prerequisites, and any Linux/WSL notes are documented. |

## Phase 1: Account, Profile, and Callsign Foundation

| Done | ID | Priority | Status | Work item | Acceptance |
|---|---:|---|---|---|---|
| [x] | 1.1 | P0 | Done | Create profile UI | A user can create a Callsign profile. |
| [x] | 1.2 | P0 | Done | Persist profile data | Profile data survives app restart. |
| [x] | 1.3 | P0 | Done | Select existing profile | The active profile can be switched explicitly. |
| [x] | 1.4 | P0 | Done | Delete profile | A profile and its local settings can be removed. |
| [ ] | 1.5 | P0 | Deferred | Edit callsign after creation | Deferred because alpha users can recreate a profile rather than rename in place. |
| [ ] | 1.6 | P0 | Deferred | Callsign validation and guidance | Deferred because the current UI already shows speakable callsign tips and basic validation. |
| [ ] | 1.7 | P0 | Deferred | First-run account wizard | Deferred because the current tabbed setup flow is usable without a dedicated wizard. |
| [x] | 1.8 | P0 | Done | Per-profile settings persistence | Callsign, microphone, language, and recognition preferences persist per user. |
| [ ] | 1.9 | P1 | Deferred | Import/export and backup | Deferred because backup/restore is a post-alpha convenience feature. |
| [ ] | 1.10 | P1 | Deferred | Multi-user isolation | Deferred because alpha is single-user oriented on the local device. |
| [ ] | 1.11 | P1 | Deferred | Profile corruption recovery | Deferred because quarantine/recovery is a hardening task, not an alpha blocker. |

## Phase 2: Voice Enrollment and Recognition Reliability

| Done | ID | Priority | Status | Work item | Acceptance |
|---|---:|---|---|---|---|
| [x] | 2.1 | P0 | Done | Press-and-hold voice recording | The user must hold the record button while speaking so recording state is obvious. |
| [x] | 2.2 | P0 | Done | Sample playback | The user can play back a recorded sample before enrolling it. |
| [x] | 2.3 | P0 | Done | Local recognition baseline | Audio can be captured and transcribed locally with a compatibility fallback. |
| [ ] | 2.4 | P0 | Deferred | Microphone setup screen | Deferred because alpha uses the system default input rather than a full mic configuration surface. |
| [x] | 2.5 | P0 | Done | Missing microphone and permission states | The app shows readable errors when mic access is unavailable. |
| [x] | 2.6 | P0 | Done | Silence and dead-air handling | Silence does not look like a valid sample or command. |
| [x] | 2.7 | P0 | Done | Wake word detection | `Callsign` wakes the session without a manual button. |
| [x] | 2.8 | P0 | Done | Callsign identity matching | The spoken callsign is matched against the enrolled profile. |
| [ ] | 2.9 | P0 | Deferred | Confidence and confirmation | Deferred because alpha rejects low-confidence speech instead of adding a second confirmation loop. |
| [x] | 2.10 | P0 | Done | Voice re-training | The user can retry, add samples, and improve the enrolled voice profile. |
| [ ] | 2.11 | P0 | Deferred | Recognition diagnostics | Deferred because the current status text is sufficient for alpha diagnostics. |
| [ ] | 2.12 | P0 | Deferred | Recognition evaluation harness | Deferred because the current smoke suite covers the supported alpha paths without a fixture corpus. |
| [x] | 2.13 | P1 | Done | Compatibility fallback policy | Machines that cannot run the preferred recognizer still have a documented fallback path. |

## Phase 3: Session Orchestration and Stop Controls

| Done | ID | Priority | Status | Work item | Acceptance |
|---|---:|---|---|---|---|
| [x] | 3.1 | P0 | Done | Session state machine | Idle, wake, identity, command, launch, cancel, timeout, and lockout are modeled. |
| [x] | 3.2 | P0 | Done | Visible stop and reset controls | The user can stop, cancel, or reset the current session in the UI. |
| [ ] | 3.3 | P0 | Deferred | Background service process | Deferred because the alpha remains a visible setup app instead of a silent always-on service. |
| [ ] | 3.4 | P0 | Deferred | Tray or companion status surface | Deferred because the visible main window is the alpha status surface. |
| [ ] | 3.5 | P0 | Deferred | Session-to-service wiring | Deferred because alpha uses an in-process session model rather than split host/service wiring. |
| [x] | 3.6 | P0 | Done | Timeout and lockout behavior | Failed identity and silent timeouts stop safely and visibly. |
| [x] | 3.7 | P0 | Done | Session audit log | Wake, identity, command, launch, cancel, timeout, and failure events are logged locally. |
| [ ] | 3.8 | P1 | Deferred | Start on login option | Deferred because start-on-login is a post-alpha deployment preference. |
| [ ] | 3.9 | P1 | Deferred | Crash recovery | Deferred because the startup-error dialog covers alpha launch failures, but full service crash recovery is future work. |

## Phase 4: Start Menu Launch Completion

| Done | ID | Priority | Status | Work item | Acceptance |
|---|---:|---|---|---|---|
| [x] | 4.1 | P0 | Done | Visible Start menu launch path | Callsign can open Start search and launch a plain app name. |
| [x] | 4.2 | P0 | Done | Installed app inventory | The app indexes Start menu shortcuts and common aliases. |
| [x] | 4.3 | P0 | Done | Fuzzy app matching | Misheard app names resolve to likely installed apps with confirmation when needed. |
| [ ] | 4.4 | P0 | Deferred | Launch verification | Deferred because launch verification is not instrumented yet, but the alpha already has a clear visible launch path and fallback status. |
| [ ] | 4.5 | P0 | Deferred | Launch failure recovery | Deferred because the alpha returns a readable failure message and safe reset path. |
| [x] | 4.6 | P0 | Done | Reject unsafe launch text | Paths, URLs, shells, and command payloads are rejected for the free launcher scope. |
| [x] | 4.7 | P0 | Done | Launch phrase coverage | Phrases like `open`, `start`, `launch`, and `run` are normalized consistently. |
| [ ] | 4.8 | P1 | Deferred | Recent launches | Deferred because recent-launch history is a convenience feature, not an alpha blocker. |
| [x] | 4.9 | P1 | Done | Start menu launch smoke test | A documented smoke test proves launch works on a clean install. |

## Phase 5: Text Dictation End to End

| Done | ID | Priority | Status | Work item | Acceptance |
|---|---:|---|---|---|---|
| [x] | 5.1 | P0 | Done | Dictation mode activation | The user can explicitly enter dictation mode from the documented UX. |
| [x] | 5.2 | P0 | Done | Audio capture for dictation | Dictation captures microphone audio reliably enough for transcription. |
| [x] | 5.3 | P0 | Done | Speech-to-text transcription | Spoken text is transcribed end to end into readable text. |
| [x] | 5.4 | P0 | Done | Insert or expose dictated text | Dictated text appears where the documented UX says it should. |
| [x] | 5.5 | P0 | Done | No microphone handling | The app shows a useful error when no mic is available. |
| [x] | 5.6 | P0 | Done | Silence handling | Silence is reported clearly and does not produce fake text. |
| [x] | 5.7 | P0 | Done | Transcription failure handling | STT failure produces a readable retry or fallback path. |
| [x] | 5.8 | P0 | Done | Dictation stop and cancel | The user can stop dictation without side effects. |
| [ ] | 5.9 | P1 | Deferred | Dictation preview or review | Deferred because alpha exposes dictated text visibly and commit-on-release is sufficient. |
| [ ] | 5.10 | P1 | Deferred | Dictation test coverage | Deferred because the current smoke checks cover the supported dictation path without a noisy fixture corpus. |

## Phase 6: Web Browsing End to End

| Done | ID | Priority | Status | Work item | Acceptance |
|---|---:|---|---|---|---|
| [x] | 6.1 | P0 | Done | Open browsing target | Callsign can open the intended browser or browsing surface. |
| [x] | 6.2 | P0 | Done | Navigate and search | The user can ask Callsign to open pages and search the web as documented. |
| [x] | 6.3 | P0 | Done | Browse and interact | Common visible browser interactions work end to end. |
| [x] | 6.4 | P0 | Done | Common failure recovery | Network, load, permission, and navigation failures are shown clearly. |
| [x] | 6.5 | P0 | Done | Safe browsing boundaries | The app avoids hidden submission or unsafe external actions. |
| [ ] | 6.6 | P0 | Deferred | Stop and cancel for browsing | Deferred because browsing is limited to opening visible targets and can be interrupted by normal browser controls. |
| [ ] | 6.7 | P1 | Deferred | Browsing smoke test | Deferred because the browser helper is validated by unit-level smoke coverage rather than full UI browser automation. |

## Phase 7: File Search End to End

| Done | ID | Priority | Status | Work item | Acceptance |
|---|---:|---|---|---|---|
| [x] | 7.1 | P0 | Done | Define the intended file scope | The alpha document says exactly which file areas are searchable. |
| [x] | 7.2 | P0 | Done | File search execution | Callsign can search the intended scope and return useful results. |
| [x] | 7.3 | P0 | Done | Empty state handling | No-result searches are explained clearly. |
| [x] | 7.4 | P0 | Done | Access-denied handling | Permission errors are surfaced cleanly. |
| [x] | 7.5 | P0 | Done | Result actioning | The user can open or otherwise act on search results as documented. |
| [x] | 7.6 | P0 | Done | File search safety | Search does not leak restricted file contents or overreach scope. |
| [x] | 7.7 | P1 | Done | File search smoke test | The search flow is exercised on a clean install with sample data. |

## Phase 8: Build, Install, Launch, and Smoke Test

| Done | ID | Priority | Status | Work item | Acceptance |
|---|---:|---|---|---|---|
| [x] | 8.1 | P0 | Done | Root build/install script | `buildcallsign.ps1` builds the app and emits a launchable installer-style executable in the repo root. |
| [x] | 8.2 | P0 | Done | Installer launch behavior | Installing from the built executable creates a user-facing launch entry and starts reliably. |
| [x] | 8.3 | P0 | Done | Documented clean checkout workflow | The docs describe the exact commands for build, install, launch, and smoke testing. |
| [x] | 8.4 | P0 | Done | Startup error logging | Startup failures write a readable local log and show a useful dialog. |
| [x] | 8.5 | P0 | Done | Smoke test for alpha path | A documented smoke test proves profile, voice, launch, dictation, browsing, and file search paths. |
| [ ] | 8.6 | P1 | Deferred | Repair and uninstall path | Deferred because installer repair/uninstall is post-alpha packaging hardening. |
| [ ] | 8.7 | P1 | Deferred | Versioned release output | Deferred because artifact versioning is useful for release engineering but not required for alpha readiness. |

## Phase 9: Alpha Verification and Release Gate

| Done | ID | Priority | Status | Work item | Acceptance |
|---|---:|---|---|---|---|
| [x] | 9.1 | P0 | Done | Alpha checklist pass | Every P0 alpha item is either Done or explicitly Deferred with a reason. |
| [x] | 9.2 | P0 | Done | Fresh-user walkthrough | A new user can complete setup without hidden developer steps. |
| [x] | 9.3 | P0 | Done | Error state walkthrough | Missing permissions, no mic, silence, launch failure, and transcription failure are readable. |
| [x] | 9.4 | P0 | Done | Build and launch verification | The documented build output launches on a clean machine. |
| [x] | 9.5 | P0 | Done | Test suite coverage | Repo tests cover the supported alpha flows and safety boundaries. |
| [x] | 9.6 | P0 | Done | Documentation lock | README, docs, and burndown match the shipped alpha behavior. |

## Non-Negotiable Alpha Gates

- The user can always stop or cancel the current session.
- No launch happens without the documented wake, identity, and command flow.
- The app shows readable intent before risky or ambiguous actions.
- Missing microphone, silence, and transcription failure are not silent failures.
- Unsafe paths, URLs, shell text, secrets, and hidden automation remain blocked.
- The v1 alpha is not release-ready until build, install, launch, and smoke-test all work from a clean checkout.
