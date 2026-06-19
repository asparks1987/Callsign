# Callsign Canon

This file is the product and engineering source of truth. It exists to prevent a README, generated site page, roadmap, old design starter, or aspirational architecture note from silently changing the product promise.

## 1. Precedence

When documents conflict, use this order:

1. Security and legal constraints.
2. This `CANON.md`.
3. Accepted architecture decision records in `docs/reference/ADR/`.
4. The current release specification and acceptance criteria.
5. The root `burndown.md`.
6. Canonical Markdown under `docs/reference/`.
7. Root `README.md`, contributor guides, and operational guides.
8. Generated HTML under `docs/pages/`.
9. Examples, screenshots, historical drafts, and archived plans.

Generated pages are derived output. They do not overrule their Markdown source.

## 2. Product identity

Callsign is a Windows-first, local-first, voice-driven desktop assistant designed around visible user control.

Canonical one-line description:

> Say `Callsign`, complete the active profile's voice/callsign gate, and control supported desktop workflows through visible, stoppable actions.

The product is accessibility-oriented and user-visible. It is not a stealth agent, a remote administration product, a credential handler, an unrestricted model-to-shell bridge, or an unattended automation daemon.

## 3. Current release

The current public target is **`v1.0 alpha`**, not the entire Alpha v1 feature ladder.

`v1.0 alpha` promises:

- A Windows installer or installable payload.
- A configuration and monitoring UI.
- Local profile and callsign creation.
- Voice-sample recording, review, reset, and enrollment state.
- A background runtime or documented per-user fallback.
- openWakeWord-driven wake detection.
- An animated overlay and readable session transcript/status.
- A callsign/voice identity gate before command capture.
- Visible Start menu launch of an installed application.
- Safe stop, cancel, timeout, lockout, startup, microphone, and target-not-found behavior.
- Build, smoke, install, and human-walkthrough evidence.

Dictation, browser control, and file/system workflows belong to later Alpha releases even when partial implementation already exists.

## 4. Release ladder

- `v1.0 alpha`: wake, identity, overlay, and visible installed-app launch.
- `v1.1 alpha`: dictation with visible review and explicit transfer.
- `v1.2 alpha`: browser open/search/navigation with external-action boundaries.
- `v1.3 alpha`: approved Windows/WSL/Linux control and Explorer-backed file search.
- Beta or later: signed distribution, updates, support, hardened policy-gated automation, and any tier packaging.

A partial implementation does not move a capability into an earlier release promise.

## 5. Identity language

The Alpha gate combines a wake event, active-profile state, and a spoken callsign/voice match.

Use these phrases:

- `voice/callsign gate`
- `identity gate`
- `active-profile verification`
- `reduces accidental or unauthorized activation`

Do not describe Alpha as providing strong biometric authentication, account security, speaker-proof authorization, or protection against a determined nearby attacker unless a tested security design later supports those claims.

The operating system remains the security boundary for access to the machine.

## 6. Safety invariants

Every release preserves these rules:

- Wake alone cannot execute an action.
- The identity phrase is not also a command.
- Model output never bypasses policy.
- Arbitrary shell, PowerShell, WSL, or command execution is blocked by default.
- Passwords, 2FA codes, payment details, wallet secrets, and credential stores are human-only.
- External submissions require explicit, specific approval when they become supported.
- Permanent deletion, admin/UAC, security-setting changes, purchasing, and money movement are blocked until an accepted design says otherwise.
- Visible semantic automation is preferred over raw input.
- The user can stop the active workflow.
- Observation data is sensitive by default.
- Audit and diagnostic data is minimized and redacted.
- No hidden persistence, evasion, surveillance, or exfiltration behavior is permitted.

## 7. Architecture invariants

Current runtime:

- `Callsign.UI` owns onboarding, profile configuration, enrollment UX, monitoring, diagnostics, and visible review surfaces.
- `Callsign.Service` owns the microphone, wake event, identity/session state, command capture, status snapshots, and action routing.
- Profiles and status are local by default.
- The `v1.0` action adapter is intentionally narrow.

Target automation architecture:

- A host handles voice, planning, session UX, and user approvals.
- A local automation server exposes typed capabilities.
- The server uses stdio by default.
- Policy, verification, and audit enforcement live outside the model.
- Native APIs and UI Automation precede SendInput.
- Future network control requires a separate authenticated design and is not inherited from the local architecture.

## 8. Data canon

- Profile data lives locally under `%LOCALAPPDATA%\Callsign\`.
- File names, paths, window titles, UI trees, screenshots, clipboard contents, transcripts, voice samples, and audit arguments may be sensitive.
- Secrets are not stored.
- Cloud processing is opt-in and disclosed per data class.
- Retention and deletion controls must exist before a data class is called production-ready.
- Raw voice-sample retention must be documented separately from enrollment metadata.

## 9. Status and evidence

Permitted labels:

- `Documented done`: existing project docs say the item is done; current evidence was not independently rerun for this pack.
- `Documented in progress`: existing project docs say work is partial.
- `Needs repo proof`: implementation may exist, but current automated or manual evidence is missing.
- `Not started`: no implementation evidence is claimed.
- `Blocked`: an external decision or dependency prevents completion.
- `Verified`: current commands and/or human evidence are attached to the task.

Only current evidence can promote a task to `Verified`.

## 10. Pricing and source boundary

All Alpha v1 features remain free until at least beta.

Free, Pro, and Advanced are possible future package names, not a current guarantee. Any paid model must:

- Preserve the proven Alpha core.
- Be described plainly.
- Keep security controls across every tier.
- Avoid covert data collection.
- Keep proprietary work out of public tracked source.

## 11. Documentation rules

- Canonical authoring format is Markdown.
- `docs/reference/` contains product and engineering source documents.
- `docs/pages/` is generated.
- The root burndown is canonical; reference copies link to it.
- Every behavior-changing pull request updates requirements, safety impact, tests, and release status together.
- Examples must be labeled as current, proposed, or illustrative.
- A future design must never be written in the present tense as shipped behavior.

## 12. Decisions still required

The project must explicitly decide:

- Repository license.
- Supported Windows versions and architectures.
- Exact speaker-verification technology, thresholds, and threat claims.
- Raw voice-sample format, encryption, retention, and deletion.
- Service account and IPC design.
- Signing and update channel.
- Telemetry policy.
- Cloud-provider boundary, if any.
- Ownership and support policy.
- Beta packaging and monetization.
