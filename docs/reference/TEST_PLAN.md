# Test Plan

## Purpose

`v1.0 alpha` is proven when the Free open-source core works end-to-end from install through visible launch.

All Alpha v1 features are free and remain free until at least beta.

## v1.0 automated coverage

Default smoke command:

```powershell
dotnet run --project tests/Callsign.AlphaSmoke/Callsign.AlphaSmoke.csproj
```

Required checks:

- fresh install/service startup assumptions
- profile creation and local settings persistence
- wake-only speech does not launch
- wake word detection and identity gate for `Callsign` / `call sign`
- `callsign.gif` appears and session readout updates while active, including visible wake-overlay safety text for stop/cancel and identity-before-command behavior
- identity success and mismatch handling
- visible Start menu launch path
- safe stop, cancel, timeout, and lockout handling
- runtime snapshot includes session state, transcript, overlay readout, and microphone telemetry
- unsafe launch text, paths, URLs, shell fragments, and secret-like inputs are rejected

## Manual v1.0 walkthrough

1. Build and install from clean state.
2. Create profile and callsign.
3. Record and review voice samples.
4. Train or mark voice identity enrolled.
5. Confirm service/runtime health in the UI.
6. Speak wake phrase and confirm overlay + live text, including visible stop/cancel/reset safety text and the identity-before-command reminder.
7. Speak callsign and confirm identity acceptance.
8. Speak launch command.
9. Confirm Start menu action is visible.
10. Validate wrong identity and no-mic cases do not launch.
11. Confirm Getting Started reopens the walkthrough from the Account tab, exposes accessible names/descriptions for its guidance, safety and tier summary, visible close button, and navigation controls, and can jump to Shortcuts and Packs for local shortcut and extension management. Confirm the summary names the Free alpha parity core, the visible stop/cancel path, and community/Pro/Advanced pack gates before enablement.
12. Validate stop, cancel, timeout, and lockout behavior.

## Voice Access parity coverage

The v1.x parity line is proven against `VOICE_ACCESS_PARITY_MATRIX.md`.

Automated coverage must include:

- command parser and alias tests for every parity command family
- Start menu resolver tests proving exact/alias matches resolve and ambiguous app names require confirmation
- policy allow/deny/approval/fresh-identity tests by risk tier
- command pack load/import/drag-drop/disable/enable/remove tests
- local voice-shortcut store/save/delete/enable/disable tests plus follow-up-step execution metadata tests
- command discovery tests for built-in and extension-pack commands
- update manifest parsing tests for splash-screen command deltas
- dictation review/edit/correction tests, including previous/next word, sentence, and paragraph movement, previous/next word and sentence selection/deletion, spoken target-text and range-based select/delete/replace inside the visible review buffer, delete-all, tab, sentence/line/paragraph break commands, and undo/redo recovery snapshots in the visible review buffer
- dictation punctuation and symbol command tests for quotes, parentheses, brackets, braces, dash, slash, backslash, pipe, backtick, tilde, underscore, plus, equals, number sign, dollar sign, ampersand, percent, caret, asterisk, and at sign
- dictation case-formatting parser, casing-mode command, and review-buffer transformation tests
- correction-alternative parser and review-buffer replacement tests
- visible controls, foreground-app UI Automation label normalization, compact numbered-overlay layout, visible numbered-control safety text, numbered click/double-click/right-click routing, and browser page-find/private-window/bookmark/print/save/scroll/start-scroll/stop-scroll/system/file command routing tests
- Task View and virtual desktop routing tests
- visible projection/cast panel routing tests that open Windows panels without selecting projection mode or connecting to a wireless display
- approval-gated clipboard-history routing, clipboard privacy metadata, and explicit approval tests with no clipboard-content inspection
- approval-gated snipping-toolbar routing, screenshot privacy metadata, and explicit approval tests with no screenshot capture/read/save/upload
- safe Windows Settings page routing tests that preserve plain `open Settings` as Start menu launch behavior and cover display, sound, Bluetooth, Wi-Fi, network, accessibility, mouse, keyboard, privacy, power, installed apps, default apps, date/time, notifications, Windows Update, and personalization pages
- accessibility subpage routing tests for Magnifier, Narrator, Captions, and Speech settings that open visible Settings pages without toggling assistive features
- visible Magnifier accessibility-surface routing tests for open, zoom out, and close commands
- safe media-key routing tests for play/pause, next, previous, and stop playback
- keyboard keypress and natural editing/formatting/document/zoom shortcut routing tests for Space, letter keys, number-row keys, symbol keys, Delete, Insert, Windows key, context menu key, Caps Lock, F1-F12, repeated single-key phrases such as `press tab five times` and `press down three times`, `dismiss` as a visible Escape alias, modifier chords (including tab/home/end and allowed Control/Alt/Shift combinations), held Shift/Control/Alt modifier commands with release-all safety, active-app character, line, word, sentence, and paragraph movement/selection/deletion, copy, paste, cut, select all, save, undo, redo, bold, italic, underline, new document, open file, print, zoom in, zoom out, and reset zoom phrases
- visible window snap and Snap Layouts routing tests
- continuous pointer-motion routing tests for cardinal/diagonal directions, speed changes, explicit stop-moving behavior, and fixed-distance `move mouse <direction> <distance>` phrases
- pointer nudge and horizontal scroll routing tests
- mouse button hold/release, triple-click, and direct mouse drag routing tests for visible drag workflows
- file result select/open/reveal routing tests plus blocked executable/script direct-open tests
- mouse grid routing, visible mouse-grid safety text, drag routing, and cell geometry tests

Manual parity coverage must include:

- clean install from the public website installer
- microphone setup and voice enrollment
- ambiguous app launch confirmation, including Voice Access-style bare-number/result phrases, next/previous choice movement, confirm-app confirmation, and cancel/reset clearing choices
- wrong identity, stale identity, timeout, and cancel flows
- show numbers/grid on common Windows apps, including compact UI Automation-numbered controls in the foreground app, visible numbered-control safety text, and click, double-click, and right-click by visible number or label
- dictation into Notepad with review, visible review-buffer safety text, spoken target-text/range select/delete/replace, and correction
- dictation case-formatting in the review buffer before copy/paste
- dictation copy/paste review safety, including paste blocking for sensitive-looking foreground targets and local readback stop without review-buffer mutation
- correction alternatives for previous word/sentence/paragraph, including visible replacement-safety text, visible close button, cancel without mutation, and numbered choice
- browser navigation in Edge or Chrome, including visible browser safety text, home page navigation, fullscreen, private window launch, bookmark/favorites, print, and save-page actions, tab/window controls, visible page find with dictated search text, vertical/horizontal scrolling, and continuous browser scrolling with explicit start/stop phrases
- app switching by next/previous window and by named open app/window with numbered multi-match confirmation, Task View, virtual desktop navigation, window management, snap left/right/up/down, and Snap Layouts
- visible Project and Cast panel opening without selecting a target display
- approval-gated clipboard history panel opening, verifying that Callsign does not read or store clipboard history contents
- approval-gated snipping toolbar opening, verifying that Callsign does not capture, read, save, or upload screenshots
- continuous pointer motion, visible mouse-grid safety text, speed changes, explicit stop-moving behavior, pointer nudges, mouse hold/release drag, direct mouse drag, and horizontal scrolling on a visible scrollable surface
- visible System safety text plus safe Windows Settings surfaces for display, sound, Bluetooth, Wi-Fi, network, accessibility, mouse, keyboard, privacy, power, installed apps, default apps, date/time, notifications, Windows Update, and personalization pages
- accessibility subpage walkthrough for Magnifier, Narrator, Captions, and Speech settings
- visible Magnifier open, zoom-out, and close flow
- safe media controls for play/pause, next track, previous track, and stop playback
- keyboard keypress and natural editing/formatting/document/zoom shortcut commands for Space, letter keys, number-row keys, symbol keys, Delete, Insert, Windows key, context menu key, Caps Lock, a function key, visible keyboard-overlay safety text, repeated single-key commands, `dismiss` as an Escape alias, safe modifier chords (including allowlisted tab/home/end and Ctrl/Alt/Shift combinations), held Shift/Control/Alt with release-all safety, active-app previous/next character movement, character selection/deletion, line start/end movement, previous/next line movement, line selection/deletion, word/sentence/paragraph movement and selection, paragraph boundary selection/deletion, copy, paste, cut, select all, save, undo, redo, bold, italic, underline, new document, open file, visible print dialog, zoom in, zoom out, and reset zoom in a visible local app
- Explorer-backed file search, visible file-search safety text, numbered result selection, safe open, blocked executable direct-open, and reveal
- `what can I say` command discovery, including quick filters for all/Free/safety/dictation/visible-controls/extensions, search, selected-command details, risk/examples, built-in commands, extension commands, visible close-button dismissal, and voice dismissal
- community extension import through the Packs UI, including visible drop-zone import for DLLs or folders, disabled-by-default state, enablement-readiness review for signature/entitlement/metadata/file blockers, metadata/signature/entitlement review, enable, disable, remove, and reimport/rollback behavior
- local voice shortcuts create/manage/execute walkthrough, including visible safety text, command steps, wait steps, enable/disable, paid-entitlement/policy gate inheritance, and visible execution through existing Callsign surfaces
- update splash from a manifest that includes added, changed, and removed commands or extension-pack changes, including spoken summary, visible details, accessible voice-dismissal cue, policy/entitlement reminder, and voice dismissal

## Release gate

No `v1.0 alpha` release until:

- build site and installer are reproducible,
- service/session fields are readable in runtime state,
- overlay is visible at wake and hidden in terminal states,
- identity failures remain safe and visible,
- common app launches work through visible flow,
- and docs/website accurately describe open-source Free plus future closed-source extensions.

### Parity gate

`1.0.0a` is the first release that claims practical Windows 11 Voice Access parity.
No release can claim parity until every row in `VOICE_ACCESS_PARITY_MATRIX.md` is `Done` and automation + manual evidence is attached to each row.
Run `.\scripts\voice_access_parity_evidence.ps1` before any parity claim, or use `.\scripts\prepare-release-packet.ps1` to run the release-readiness build/site steps plus the parity smoke/template flow together. The generated JSON separates local evidence success from release proof: `passed` means the automated/documentation checks completed, while `release_ready` is true only when manual/live parity evidence is supplied and no evidence checks fail. It reports `canonical_manual_evidence.categories_missing` to prove the generated manual walkthrough template covers every parity category, and `manual_evidence.categories_covered` / `manual_evidence.categories_missing` to show which categories have actual submitted live evidence. Use `-RequireManualEvidence` when validating a release candidate so missing live walkthrough evidence fails the gate instead of being treated as implied completion. Manual evidence must contain passed named checks for clean install from the public installer, enrollment, identity failure/cancel/reset, show numbers/grid, dictation, browser, window/settings, keyboard/mouse/media/file workflows, command discovery, local voice shortcuts create/manage/execute, community extension import/manage, update splash, Apple Voice Control-style visual polish across the core visible surfaces, and public website installer hash comparison.
To prepare that file, run `.\scripts\voice_access_parity_evidence.ps1 -WriteManualEvidenceTemplate`. The generated canonical template is written to `build/voice-access-parity-manual-evidence.template.json` and includes every required check id, a description, a concrete `evidence_command` walkthrough prompt, an `expected_result` proof target, `parity_categories`, the local installer hash and byte size, and placeholders for public website download hash, public website byte size, machine details, operator, timestamps, observed result, artifact references, and notes. Strict validation requires the supported manual-evidence schema, unique manual check ids, valid evidence timestamps, a public `Callsign-Setup.exe` download URL, test machine, Windows version, Callsign version, a timestamp, operator, environment, and notes for every manual check. It also requires the manual local and website installer SHA-256 values and byte sizes to match the current local `Callsign-Setup.exe`. Each manual check's `description`, `evidence_command`, `expected_result`, and `parity_categories` must also match the generated canonical template, so stale copied evidence cannot silently satisfy a newer parity walkthrough. Any check marked `passed` must include an `observed_result` and at least one `artifacts` reference such as a screenshot, video, transcript, audit log, hash output, or manual test note file. Artifact references must be HTTP(S) URLs or local paths inside the Callsign workspace, using either absolute paths under the repo or repo-relative paths.

