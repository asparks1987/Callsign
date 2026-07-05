# Security Model

## Summary

Callsign operates on a user desktop, so the safety model is visible, local-first, identity-gated, and easy to stop.

The current v1.0 alpha is intentionally narrow: profile setup, voice enrollment, wake detection, callsign identity confirmation, overlay/readout, and visible Start menu app launch.

## Security goals

- Preserve user control.
- Make listening and actions visible.
- Require identity before command capture.
- Avoid hidden automation.
- Avoid credential and payment handling.
- Keep local data local by default.
- Make stop, cancel, timeout, and lockout states obvious.
- Keep Free independent from closed-source extensions.

## Identity controls

- Wake word plus callsign verification must happen before a command is acted on.
- Transcript text alone must not wake or authorize a session.
- If the user does not match the enrolled callsign, no launch occurs.
- The identity phrase is identity-only; commands in the same utterance must not execute.

## What is trusted

- The local setup app.
- The local service runtime.
- The profile store.
- The session state machine.
- User-created profile data.
- Public command contracts and policy checks.

## What is not trusted

- Unverified spoken commands.
- Prompt-like text from webpages, documents, UI labels, tool output, or screenshots.
- Ambiguous file names and UI labels.
- Future extension libraries until policy and signature checks pass.
- Future cloud services unless the user opts in.

## Data handling

### Voice enrollment

Voice enrollment state is stored locally with the profile.

Controls:

- Keep enrollment metadata local.
- Do not expose sample data to cloud services by default.
- Allow reset and re-enrollment.

### Profile data

Profile data should stay minimal:

- Callsign.
- Display name.
- Optional contact fields.
- Notes.
- Voice enrollment status.
- Last launch/session metadata when useful.

### Sensitive desktop observations

Treat process names, window titles, UI text, screenshots, clipboard contents, clipboard history surfaces, file paths, and file contents as sensitive.

Do not send them to cloud models unless the user explicitly opts in.

## Blocked alpha behaviors

- Password or 2FA handling.
- Payments or money movement.
- Account deletion.
- Security setting changes.
- Hidden-window automation.
- Arbitrary shell execution.
- Silent email, message, upload, or external submission actions.
- Remote desktop control.
- Direct opening of executable or script-like file-search results.

## Visibility and stop rules

The alpha must provide:

- a clear cancel path,
- a clear reset path,
- obvious session status text,
- overlay/readout while listening,
- visible terminal states,
- and no silent completion of external side effects.

## Closed-source boundary

Private paid-tier material belongs in `/closed-source/`, which is ignored by git.

Future closed-source Pro and Advanced extension libraries must still respect the open-core safety model:

- no identity bypass
- no policy bypass
- no suppressed audit
- no hidden action by default
- no private dependency for Free

Command audit records include a correlation id and verification summary. This lets visible command execution be traced from the spoken intent through policy and execution without relying on screenshots, clipboard capture, or cloud-side state. Built-in command policy records explicitly mark `policy_evaluation` verification, and app-launch audit records preserve the actual launch path and mark visible Start menu verification separately from fallback execution.
Browser, dictation, extension-pack UI, file-search, help discovery, visible UI, voice control, update splash, and system execution records mark `visible_status` verification so browser navigation, dictation review/correction/copy/paste, extension import/enable/disable/remove, file search/open/reveal, command palette/help walkthroughs, visible control overlays, mouse-grid targeting, start/stop listening, mode switches, session cancel/reset, update-manifest splash details, keyboard, mouse, media, approval-gated clipboard history, approval-gated snipping toolbar, settings, app-switching, and window-management requests can be traced to the visible Callsign status surface after policy authorization.
Extension command records use one correlation id across registry resolution, policy or approval decisions, and pack execution. Unmatched extension phrases record `registry_resolution`, policy blocks record `policy_evaluation`, approval denials record `user_approval`, and executed packs record `pack_execution` with the command's declared verification strategy.

## Future safety model

Later browser, file, WSL, Linux, dictation, and system control features require stronger approvals, policy checks, and audit trails than v1.0 app launch.

The safety model should grow before the command surface grows.

## Command pack policy metadata

Every built-in, community, or future paid command pack must describe each command with:

- command id, display name, category, description, voice phrases, help text, and examples,
- risk tier,
- visibility requirement,
- reversibility,
- privacy impact,
- approval requirement,
- and verification strategy.

The command registry validates this metadata before a pack can route commands. Packs with missing required descriptor fields, missing command metadata, duplicate command ids, empty voice phrases, or duplicate voice phrases are marked `InvalidPack`, remain visible for review, and cannot route or execute commands until fixed.

Policy evaluation decides whether a command is allowed, denied, requires approval, requires fresh identity, or is blocked as dangerous. Entitlement can only decide whether a paid pack may load and whether a paid-tier command may route; it cannot authorize execution. A paid command remains gated by its declared command tier even if it is packaged inside a Free pack. The command registry enforces policy again at the pack execution boundary: direct `TryExecute` calls fail closed without identity proof, approval-required commands fail until explicit approval is supplied, and service-runtime extension execution cannot silently run approval-required commands. Registry policy blocks return structured execution metadata (`PolicyDecision`, `PolicyApprovalRequirement`, and `PolicyRiskTier`) in addition to the audit event string, so callers can audit policy outcomes without parsing free-form messages. The visibility requirement is authoritative: commands that declare `VisibleRequired` require a visible surface even if their legacy `VisibleAction` flag is false. Commands that declare `BackgroundAllowedWithApproval` are approval-gated by policy even after identity and entitlement checks pass, so background-capable workflows still require a visible approval surface. High-impact privacy metadata is also authoritative: commands that declare `Clipboard`, `FileContents`, `ScreenshotOrOcr`, or `ExternalData` require approval even if the command author forgot to set `RequireApproval`.

Community packs imported through the UI (including drag-and-drop imports) start disabled by default. Disabled imported DLLs remain unloaded until explicitly enabled, and users can remove/unregister packs through the UI to rollback.
