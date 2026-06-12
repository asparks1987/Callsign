# Roadmap

## Phase 0: Canon and repo hygiene

Status: current focus.

Deliverables:

- Root `burndown.md` as the canonical multiphase checklist.
- Free, Pro, and Advanced tier language across all docs and the website.
- `/closed-source/` ignored by git for private paid-tier material.
- Generated GitHub Pages output aligned with source docs.

Exit criteria:

- No deprecated middle-tier wording remains in user-facing docs.
- Public docs explain what exists now versus what is planned.

## Phase 1: Free tier completion

Deliverables:

- Account creation and profile storage.
- Voice sample recording and playback.
- Callsign identity and wake-word flow.
- Reliable Start menu application launch.
- Local recognition quality checks and ambiguous transcript confirmation.
- Dictation, browser, and file-search tools with readable errors and visible output.
- Build/install path that emits the installer and launchable executable from a fresh checkout.

Exit criteria:

- A user can create a callsign profile and launch common installed apps by voice.
- Free rejects paths, URLs, shells, command text, and non-launch automation.
- A user can dictate text, open a website or search query, and search local files with visible results or clear empty/error states.
- The documented build/install path works from a clean checkout.

## Phase 2: Always-on background service

Deliverables:

- Background service process.
- Wake-word listener.
- Callsign identity gate.
- Tray/status controls.
- Local launch audit log.

Exit criteria:

- Callsign can wait in the background and act only after identity is confirmed.
- Stop, cancel, timeout, and lockout paths work without opening hidden actions.

## Phase 3: Pro tier

Deliverables:

- Full Windows desktop control by voice.
- WSL workflow bridge.
- Linux desktop MVP support.
- Policy engine, approvals, and audit dashboard.
- Pro command catalog for routine desktop workflows.

Exit criteria:

- Pro can safely control Windows, WSL, and Linux workflows with visible approvals.
- Risky actions require policy approval before execution.

## Phase 4: Advanced tier

Deliverables:

- Hundreds of specialized commands.
- Recipe workflow system.
- Policy packs.
- Developer, admin, data, and document workflow packs.
- Diagnostics center, workflow memory, and advanced voice modes.

Exit criteria:

- Advanced supports power-user workflows without weakening the safety model.
- Dangerous, external, secret, and destructive actions are blocked unless explicitly designed and approved.

## Phase 5: Productization

Deliverables:

- Licensing and tier gates.
- Installer, updater, release notes, and rollback strategy.
- Opt-in telemetry and privacy-preserving diagnostics.
- Support and feedback path.
- Signed release packages when distribution begins.

Exit criteria:

- Free remains useful without a subscription.
- Pro and Advanced are monetizable without hiding the open-source core.
- Public docs, website, and release artifacts match the tier model.
