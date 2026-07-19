# Voice UX

## Goal

Callsign should feel calm, visible, and easy to stop.

The user should never wonder:

- whether Callsign is listening,
- what Callsign heard,
- whether identity passed,
- what command is pending,
- or how to cancel.

All Alpha v1 features are free and remain free until at least beta.

## v1.0 voice flow

1. Say `Callsign` or `call sign`.
2. See `callsign.gif` appear above the desktop.
3. Read the live hearing cue or transcript below the animation.
4. Say your enrolled callsign.
5. Wait for identity confirmation.
6. Speak the installed app you want to launch.
7. Watch the app open through visible Start menu flow.

## Wake mode

The background service waits for the wake word through openWakeWord/audio detection.

Examples:

- `Callsign`
- `Call sign`

Transcription mistakes can be shown as diagnostics after audio capture, but transcript text alone must not wake the service.
Voice Access-style phrases such as `wake up`, `voice access wake up`, `unmute microphone`, `go to sleep`, `voice access sleep`, `turn off microphone`, `turn off voice access`, `stop voice access`, `close voice access`, `exit voice access`, and `quit voice access` are accepted only as visible listener-control commands after the normal Callsign wake/identity path or inside the setup UI. They do not replace the audio wake detector and cannot promote transcript text into a runtime wake event.

## Overlay mode

The overlay is a visible state cue.

It should show:

- wake phase
- identity phase
- command phase
- current transcript or hearing cue
- current launch/stop result
- authority/status information when useful

Required readout examples:

- `Callsign heard. Say your callsign.`
- `Hearing your callsign...`
- `Heard: womprat`
- `Identity confirmed. Say the app name.`
- `Hearing your command...`
- `Command: open Notepad`
- `Launching Notepad...`

The overlay must not steal focus, block input, or prevent the user from stopping the session.

The visual direction is macOS Voice Control-level clarity rather than a dense dashboard: compact, high-contrast, translucent where appropriate, and readable at a glance. Listening state, identity state, live transcript, and stop/cancel affordance must remain visible without taking focus.
The wake overlay keeps a persistent accessible safety line in the message panel: `stop`, `cancel`, `stop listening`, and `reset session` are visible escape phrases, and commands remain blocked until identity is confirmed. A compact visible `STOP` badge now sits beside the live wake badge so the escape boundary is obvious at a glance, and the visible-controls and mouse-grid targeting HUDs use the same compact stop badge pattern while numbered targets are on screen. This keeps the Callsign -> identity verification -> command -> visible action contract readable at the exact moment the microphone session begins.
The shared WinForms visual contract is `CallsignVisualStyle`: the main shell visual target and its supporting surfaces should identify the `macOS Voice Control` target and remain compact, high-contrast, translucent where appropriate, non-activating for overlays, accessible, high-contrast-aware, text-scaling-aware, reduced-motion-aware, and tied to visible status. The contract now carries concrete smoke-testable evidence tokens: text contrast at or above 4.5:1, overlay/surface opacity between 0.86 and 0.99, compact rounded geometry between 20 and 26 px, Segoe UI-family system typography, high-contrast readiness, text-scaling readiness, reduced-motion-safe surfaces, and visible stop/cancel/browser-helper affordances. `CallsignVisualStyle.GetPalette` exposes a default palette plus a Windows system-color high-contrast palette, `ClampTextScale` bounds scaled text between 1.0 and 1.6 so HUDs stay readable without breaking layout, and `DescribeAccessibilityMode` reports the active palette, clamped text scale, reduced-motion state, and shared evidence tokens for local readback. The main shell overview banner now also names browser helper discovery alongside the Free boundary and visual target so the top-level copy stays aligned with the visible badge row below it. The visible `Read Visual Status` action and voice phrases such as `read visual status`, `read visual polish status`, `read visual contract`, `read accessibility mode`, `read high contrast status`, `read text scale status`, and `read reduced motion status` read those tokens aloud locally with the current shell badges plus the active accessibility mode, including palette mode, bounded text scale, and reduced-motion-safe state, keeping the Apple-style visual proof inspectable without changing installed commands.
The wake overlay, visible-controls HUD, mouse grid, keyboard overlay, command palette, correction chooser, update splash, and startup walkthrough expose this shared visual contract for smoke verification.
The Voice identity training surface also makes wake calibration provenance visible: it shows whether the current wake threshold came from the trusted sample set, how many wake samples informed it, which source sample informed the calibration when available, and when the threshold was last calibrated.

Voice enrollment sample review now uses the same quality analyzer in the visible training surface and the service-side enrollment proof. Each accepted sample records quality state, peak/RMS, duration, clipping ratio, and zero-crossing rate in `voice-identity/enrollment-samples.json`; clipped, silent/too-quiet, too-short, and excessive broadband-noise samples are rejected before biometric enrollment so the user gets a local, visible retry reason instead of a vague identity failure.
The startup walkthrough also keeps release proof visible by reminding contributors to verify the public installer and website download before calling a run complete, and it now exposes direct `Read Summary Again`, `Read Updates Status`, `Read Restart Proof`, `Open Installer`, `Open Release Evidence`, `Open Manual Evidence`, `Open Checklist`, and `Open Release Proof` jump buttons alongside the normal setup surfaces so the release check and update evidence stay visible instead of buried in text. The release-proof step explicitly names the accessibility visual audit inside the manual evidence checklist, so Apple-style visual proof covers keyboard-only use, focus, screen-reader labels, high contrast, 200% text scaling, reduced motion, multi-monitor/DPI behavior, no-audio fallback, visible stop/cancel, and non-focus-stealing overlays before a release parity claim. The Updates step in the walkthrough now names the same `Read Summary Again` replay path so the update narration is recoverable from first-run setup as well as from the Updates tab itself, the walkthrough voice cue says `read summary again` alongside the update-status, installer-open, release-evidence, manual-evidence, checklist, and accessibility-audit cues, and the walkthrough status strip now includes compact visible `STOP` and `Browser: helpers visible` badges so the stop state and browser overlay discovery stay obvious during the first-run flow. The shared walkthrough replay hint also tells users that feature-only review splashes still appear, so a release with only new capabilities still has a visible review surface from the first step onward.

Users can ask `what did you hear`, `read status`, or `repeat status` to hear the current visible status, last heard transcript, and next action through local speech synthesis without executing an external command. `stop status readback` or `stop reading status` cancels that local speech. `clear recent speech` or `clear speech history` clears the visible recent speech list and asks the background runtime to clear its transcript-history snapshot. This keeps the visible readout recoverable for users who missed the overlay, while preserving Callsign's rule that status replay and transcript-history clearing are local, visible actions.

The runtime treats speech segments and wake-window WAVs as temporary processing artifacts. It deletes segment files after transcription and wake scoring when wake diagnostics are off, deletes abandoned or queued segments during shutdown, and prunes stale temporary audio at listener startup. This keeps the Apple-style visible transcript/readout useful without turning local audio processing into hidden raw-audio retention.

## Identity mode

The post-wake identity utterance is identity-only.

Examples:

- `Alpha`
- `Jordan`
- `womprat`

Commands in the same utterance must not execute. If identity fails or times out, no launch occurs.

## v1.0 command mode

After identity is confirmed, v1.0 accepts installed app launch requests.

Examples:

- `launch Notepad`
- `open Calculator`
- `start Visual Studio Code`
- `open Settings`
- `open File Explorer`
- `open Downloads`
- `open Documents`

The launch path should stay visible and understandable.

If Callsign hears an app name that matches more than one installed app, it must not choose silently. The Session tab shows visible numbered app choices, the launch stays paused, and the user can click `Confirm App` or say `1`, `click 1`, `choose result 1`, `confirm app`, `next app choice`, or `previous app choice`. Cancel, `clear app choices`, or Reset clears the pending choices.

The preferred launch path stays the visible Start menu search. If Windows will not open the Start menu in a given session, Callsign falls back to a visible shell-backed launch for trusted installed apps so the user still gets a clear desktop action instead of a silent failure.

## Updates

The setup app includes a visible Updates tab so users can see the update server, the 25-hour check cadence, the last check time, the last successful phone-home check-in, the next due time, whether a manifest is pending, whether the current installer download is ready to open, the privacy-preserving update id, the last downloaded installer path when one exists, the evidence status, and the public website download target that the release proof compares against. Callsign phones home on startup and while running, but the check-in sends hashed account/device identifiers rather than the raw profile callsign or local update device id. It then downloads and installs updates in the background when a new manifest is approved for the channel. The visible `Check Now` action stays available for users who want to force an immediate check, the visible `Read Summary Again` action repeats the latest update narration, the visible `Read Updates Status` action reads the current update status, release proof, restart proof, evidence status, manual-evidence progress, release-gate progress, and next proof instructions aloud, the visible `Read Check-In Status` action reads the latest phone-home timestamp and privacy-id explanation aloud, the visible `Read Evidence Status` action reads whether the release evidence folder, release packet summary, parity evidence JSON, manual evidence template, and manual checklist are present, including the parity evidence `release_ready` flag, blocker count, manual-check count, missing-category count, failed-automation count, the first remaining manual proof items, and next release-evidence action when available. A compact `Manual evidence progress strip` sits beside the evidence line and exposes `Manual`, `Remaining`, `Categories`, `Proof`, `Category`, and `Next` badges, so the user can see at a glance whether manual/live proof was supplied, how many walkthrough checks remain, how many Voice Access parity categories still lack live evidence, the first remaining proof item, the first missing category, and the next checklist action. A compact `Release gates strip` exposes `Installed gate`, `Spoken gate`, `Failure gate`, and `Clean VM gate` badges for the four documentation-pack release gates: installed end-to-end automation, human-spoken core walkthrough, failure-state walkthrough, and clean Windows user or VM proof. A visible `Proof steps` line reads the generated manual evidence template and shows the concrete `evidence_command` and `expected_result` for the first remaining proof item, so the release walkthrough can move from blocker to action without searching the JSON. The visible `Create Proof Note` action creates and opens `build/manual-proof-notes/<check>.md` with observed-result, artifact-reference, privacy-review, remaining-uncertainty, and release-recommendation fields already laid out for the next proof item. The visible `Read Blockers` action reads the release-ready state, blocker count, manual-check count, missing-category count, automated-failure count, first missing proof, first missing category, and next release action aloud. The visible `Read Next Proof` action reads that same first remaining proof item, first missing category, and checklist action aloud as a focused next-step prompt. The visible `Read Proof Steps` action reads the matched manual evidence command and expected result aloud, and the visible `Read Gates` action reads the four release gates aloud as a focused release-candidate checklist. The visible `Read Release Proof` action reads the installer hash comparison and public download target aloud, the visible `Read Restart Proof` action focuses the state-reload proof and installer-download state, the visible `Open Installer` action opens the downloaded installer when one is already on disk and otherwise falls back to the staged release download, the visible `Open Release Evidence` action opens the local release-artifacts folder so the generated parity evidence stays easy to inspect, the visible `Open Manual Evidence` action opens the canonical manual-evidence template for the public clean-install proof, the visible `Open Checklist` action opens the human-readable checklist that mirrors the release-proof template, and the visible `Open Release Proof` action jumps straight to the release-proof step in the walkthrough so the installer/site comparison stays adjacent to update checking. The walkthrough's own `Open Release Evidence` button now opens the release evidence folder directly when the action is available, which keeps the proof path visible from the first-run flow as well. The Updates tab also exposes a visible downloaded-installer-path line: it repeats the persisted `updates-state.json` download path after restart, making the restart-safe installer state easy to inspect alongside the release-proof and restart-proof lines. A separate website-target line keeps the public `/downloads/Callsign-Setup.exe` download target visible in the same surface so the site comparison stays explicit. Those release-proof lines compare the local installer SHA-256 against the public download target and state that the update service reloads last known version, pending manifest, and next-due timing from `updates-state.json` after restart. The startup walkthrough's release-proof summary now mirrors that same download-path state so the first-run proof route can surface the persisted installer path too. Voice commands such as `check for updates now`, `read update summary again`, `read updates status`, `read check-in status`, `read evidence status`, `read release blockers`, `why is release blocked`, `read next proof`, `what proof is next`, `read proof steps`, `how do I prove the next check`, `create proof note`, `create next proof note`, `read release gates`, `what gates remain`, `read release proof`, `read restart proof`, `open installer`, `show installer`, `open release evidence`, `open manual evidence`, `open checklist`, and `open release proof` all open the same Updates/release-proof flow. The update splash repeats the same boundary language with a compact `Boundary: Free open` badge, a compact `STOP` badge, and a compact `Browser: helpers visible` badge so the user can see that the Free core stays open-source while also seeing the stop state and browser overlay discovery while reviewing updates. It adds compact scope and kind badges so the manifest reads as commands, packs, features, mixed, or feature-only at a glance. The summary now says `Feature-only update` when the payload is all features so that state is obvious without reading the counts row. The import splash follows the same visible pattern for community extension packs: it lists the imported pack names, the first few imported command ids and display names, and the disabled-by-default review state before the user dismisses it. When an update manifest includes feature-only highlights, the splash still appears and adds a visible feature count, feature-highlight details, and spoken feature recap so new capabilities stay visible even when no commands changed.

  The evidence readback also names whether manual evidence was supplied, then previews the first missing manual categories from `manual_evidence.categories_missing`, bounded to three items, so release proof points to the next walkthrough area without flooding the overlay.

  The Updates tab also exposes a visible `Proof notes` status line, a `Read Proof Notes` action, and an `Open Proof Notes` action. The readback says how many markdown notes exist, names the first note file when available, and repeats the privacy/artifact handoff reminder. The open action opens the `build/manual-proof-notes` folder created by `Create Proof Note`, keeping observed-result notes, artifact references, privacy-review notes, and release recommendations inspectable beside the generated parity evidence.

  The visible `Create All Notes` action prepares markdown notes for every unchecked manual parity proof item. It uses the same `build/manual-proof-notes` folder and does not mark any check passed; it only lays out evidence-command, expected-result, observed-result, artifact-reference, privacy-review, uncertainty, and release-recommendation fields so the live walkthrough can be captured check by check.

  The visible `Read Evidence Header` action reads the generated manual evidence header aloud before a parity claim. It names missing header fields, the artifact-hash requirement, the local installer SHA-256 and size, and whether the public website download proof has a URL, hash, and size.

  The visible `Create Evidence Draft` action creates `build/voice-access-parity-manual-evidence.draft.json` from the generated template. Callsign pre-fills local-only facts it can verify safely, including the current timestamp, app version, architecture, machine name, local installer SHA-256 and size, and artifact hashes, while leaving tester, microphone, public website proof, and pass/fail walkthrough results for the human release operator.

  The visible `Open Evidence Draft` action opens the current local draft and creates it first from the generated parity template if it is missing. This keeps the release operator inside the visible Updates workflow when continuing manual clean-install and live voice walkthrough proof.

  The visible `Read Evidence Draft` action reads the local draft status aloud. It reports how many checks are in the draft, how many are already marked passed, whether artifact hashes and local installer proof are present, whether public website proof is still missing, which human fields still need completion, and the next unchecked proof item with its `evidence_command` and `expected_result`.

  ## Help

The command palette keeps the same compact `STOP` cue in its status strip so the visible stop boundary stays obvious while browsing commands and filters, and it now carries a compact `Browser: helpers visible` badge so browser overlay discovery stays at a glance beside the search and filter chips. Its selected, filtered, and Updates quick filter and readback text also call out feature-only review splashes so the update surface reads the same way in the palette that it does in the walkthrough and splash. The palette exposes a `Free Parity` quick filter backed by `free parity`, `voice access parity`, and `open core parity` searches so Windows Voice Access parity commands remain visibly separated as Free open-core capabilities before any paid-pack discovery. It also exposes a `Visible Choice` approval filter backed by `approval:visible choice` so ambiguous commands are discoverable as commands that stop for an explicit visible choice before execution, and plain help output includes approval metadata beside source, tier, and availability. The Help tab mirrors that same browser-helper badge beside its discovery badges so the path to browser commands remains obvious from the central help surface as well.

## Plans

The setup app includes a visible Plans tab so users can see the Free core boundary and the paid tiers in one place. The tab states that the public Free core remains open-source and useful on its own, that the full Windows Voice Access parity baseline stays Free/open-core with no paid account required, that Pro is the paid tier for beyond-parity Windows, WSL, Linux, browser, and workflow control, and that Advanced is the paid tier for beyond-parity recipes, diagnostics, admin/dev workflows, and power-user automation. The tab also states the rule that entitlement may decide whether a paid pack may load, but policy still decides whether any command may run. The active profile now carries its own entitlement summary, and the Plans and Packs surfaces reflect that profile-scoped boundary so the app can show when the current account is Free-only or entitlement-enabled. The Plans tab also exposes a visible entitlement preset chooser and a Read Plans Status button so the boundary can be adjusted or spoken back from the same surface that explains it, including the current entitlement summary and the statement that paid packs start beyond parity. The visible Help tab, command palette quick filters, and first-run walkthrough also expose Read Plans Status so the boundary stays discoverable from the entry surfaces, not just from the Plans tab itself. A roadmap strip now makes the alpha cadence visible inside the app itself: start at `0.0.3a`, use `0.0.01a` micro revisions for fixes and polish, use `0.0.1a` major revisions for new alpha capability clusters, ship `v1.0.0a` as the first public alpha, and keep alpha features free through beta before introducing the paid layers. The first-run walkthrough and command palette repeat that same boundary with a compact `Boundary: Free open` badge so the paid line stays legible from first launch onward.

## Visible control mode

The setup app, foreground Windows apps, and overlay should remain understandable to users who rely on visible UI.

Current and v1.x command routing can support visible-control concepts such as:

- `show numbers`
- `show numbers here`
- `show numbers everywhere`
- `show numbers on notepad`
- `show labels on notepad`
- `show labels on taskbar`
- `show numbers on taskbar`
- `show numbers for this window`
- `show visible controls`
- `number controls`
- `number clickable controls`
- `show what i can click`
- `show clickable controls`
- `hide visible controls`
- `hide control numbers`
- `clear numbers`
- `remove numbers`
- `close visible controls`
- `close control numbers`
- `cancel visible controls`
- `cancel control numbers`
- `next control`
- `previous control`
- `activate control`
- `click 1`
- `click 2`
- `click one`
- `tap second`
- `double click 1`
- `double click save`
- `right click 1`
- `right click save`
- `move slider right two times`
- `move volume slider up 3 times`
- `adjust slider down one step`
- `click display name`
- `click train voice identity`
- `next control`
- `next field`
- `move to next field`
- `tab forward`
- `previous control`
- `previous field`
- `move to previous field`
- `tab backward`
- `activate control`
- `click selected control`

When the foreground window is not Callsign, `show numbers`, `show labels`, and `show names` first inspect the active app with Windows UI Automation and number enabled, on-screen controls. `show numbers on notepad`, `show labels on notepad`, and other numbered label/name phrases scan visible top-level windows by spoken app or window name, then number the matched surface without hidden switching. `show numbers on taskbar` and `show labels on taskbar` target the visible Windows taskbar through the same UI Automation path so Start, pinned apps, and tray surfaces can be numbered without hidden actions. Saying `click 1`, `click one`, `tap second`, or `click save as` invokes, selects, toggles, expands, or focuses the matching UIA element when the app exposes a safe semantic pattern. Explicit Voice Access-style commands such as `toggle number one`, `toggle wifi`, `flip dark mode`, `expand settings`, `open dropdown font`, `collapse settings`, and `close menu account` route to UIA toggle or expand/collapse patterns when available, or to Callsign's own visible checkbox, radio, and combo-box controls when the numbered surface is local. `double click 1`, `triple click 1`, and `right click 1` move to the visible center of the numbered control and perform the bounded mouse action, preserving visible targeting for controls that need mouse-style interaction. Slider phrases such as `move slider right two times`, `move volume slider up 3 times`, and `adjust slider down one step` use UI Automation range patterns on visible sliders when available and fail visibly instead of guessing with hidden coordinates.

The focused numbered-control list can also be traversed by voice without selecting anything. `next control`, `next field`, `move to next field`, and `tab forward` advance the visible focus cue; `previous control`, `previous field`, `move to previous field`, and `tab backward` move it back. `activate control` or `click selected control` then invokes only the currently focused visible control. These phrases are routed through Callsign's visible-control abstraction, not hidden coordinate clicks.

If UI Automation cannot inspect the foreground app, Callsign falls back to numbering its own visible setup controls. The mouse grid remains the visible fallback for targets that cannot be reached semantically.

The numbered-control overlay uses a compact HUD plus floating numbered badges over detected controls. The HUD shows how many controls are currently numbered, the latest voice cue, what Callsign heard, the focused target, a subtitle that teaches click, toggle, expand, collapse, double-click, triple-click, and right-click by focused number or label, a persistent safety strip that says numbers act only on visible targets and `hide` or `cancel` exits without clicking, a visible close button, and a visible list that marks the focused item. The overlay form and HUD elements expose accessible names and descriptions for the numbered-control surface, cue, transcript, focus target, safety strip, summary, close button, and numbered target list so the visible state is also inspectable by assistive technology. The rest of the overlay remains transparent and click-through so the foreground app stays visually dominant. Focused controls receive a stronger ring and badge, matching the Voice Control pattern of clear target numbers without covering the working surface. If a needed target is not numbered, the HUD points the user to the mouse grid fallback instead of implying hidden targeting.

## Mouse grid mode

The mouse grid is the fallback for visible targets that cannot be reached semantically. Its compact header includes a visible close button, and its status strip includes a compact `STOP` badge so the same stop/cancel boundary remains visible while the user is targeting by grid. Its persistent safety strip says grid commands are visible pointer actions only, that the user can refine or undo before a click or drag, and that `hide grid` or `cancel` exits without acting.

Supported phrases:

- `show grid`
- `show grid everywhere`
- `show window grid`
- `show grid here`
- `show mouse grid`
- `show mousegrid`
- `show mouse grid here`
- `show numbered grid`
- `mouse grid`
- `mousegrid`
- `numbered grid`
- `open grid`
- `grid bravo`
- `mouse grid alpha`
- `mouse grid a 114`
- `mouse grid 114`
- `mouse grid 1 1 4`
- `mark`
- `mark four`
- `undo`
- `undo that`
- `drag`
- `grid 1` through `grid 9`
- `click grid 1` through `click grid 9`
- `click mouse grid 1` through `click mouse grid 9`
- `drag grid 1 to grid 9`
- `drag mouse grid 1 to grid 9`
- `hide grid`
- `hide mouse grid`
- `close mouse grid`
- `cancel grid`
- `cancel mouse grid`

The grid must be visible before it moves, clicks, or drags. `show grid`, `show grid everywhere`, and `show window grid` open the desktop-wide targeting surface; `show grid here` and `show mouse grid here` scope the grid to the current visible foreground window. On multi-display desktops, Callsign can focus a specific display with phrases such as `grid bravo` or `mouse grid alpha`, then jump straight to a refined region with a shortcut such as `mouse grid a 114`. Callsign also supports the current Voice Access-style multi-step shortcut path on the current scope, so `mouse grid 114` and `mouse grid 1 1 4` refine multiple grid levels at once without needing a display identifier. Each selected number refines the visible target area. `undo` and `undo that` return the overlay to the previous grid state, matching the current Voice Access grid behavior. A click command moves the cursor to the center of the chosen cell before any refinement changes the active bounds, clicks visibly, and hides the grid. Direct drag phrases such as `drag grid 1 to grid 9` move from the center of the first cell to the center of the second cell and then hide the grid. The overlay form, voice cue, and safety strip expose accessible names and descriptions so mouse-grid targeting remains discoverable through assistive technology before pointer input is sent.

Callsign also supports the Voice Access-style marked drag flow. After drilling into a source location, the user can say `mark` or `mark four` to set a visible drag start and highlight that point. Callsign then redraws the grid at the root scope so the user can navigate to the destination and say `drag` to drop the marked item at the current mouse-grid location. This keeps drag-and-drop visible, reversible up to the point of release, and consistent with Callsign's no-hidden-actions rule.

The grid overlay uses the same compact macOS Voice Control-style visual target as numbered controls: a clear cue strip, translucent working surface, large centered numbers, an updated cue after each refinement so the user can see the current targeting state, and a visible marked-point highlight when a drag start is set. The overlay form and cue strip expose accessible names and descriptions so assistive technology can identify the current mouse-grid targeting options; after refinement, the cue description names the focused cell and keeps the available click/drag/hide commands discoverable.

Semantic UI Automation remains preferred. The grid is for coarse-to-fine visible targeting when no better semantic action is available.

## Pointer fallback mode

Pointer movement commands are a bounded fallback for visible targets that are not reachable through UI Automation or the grid.

Supported phrases:

- `click`
- `tap`
- `left click`
- `double click`
- `triple click`
- `right click`
- `move mouse up`
- `move mouse down`
- `move mouse left`
- `move mouse right`
- `move mouse top left`
- `move mouse bottom right`
- `move mouse left five`
- `move faster`
- `move slower`
- `stop moving`
- `mouse scroll up`
- `mouse scroll down`
- `scroll left`
- `scroll right`
- `start scrolling up`
- `start scrolling down`
- `start scrolling left`
- `start scrolling right`
- `stop scrolling`
- `scroll to top`
- `scroll to bottom`
- `scroll to left edge`
- `scroll to right edge`
- `hold mouse`
- `release mouse`
- `drag mouse up`
- `drag mouse down`
- `drag mouse left`
- `drag mouse right`
- `drag mouse top left`
- `drag mouse bottom right`

`move mouse <direction>` starts visible continuous pointer motion in the requested direction, including diagonals such as `top left` and `bottom right`. `move mouse <direction> <distance>` performs a bounded fixed-distance move such as `move mouse left five`. `move faster` and `move slower` adjust the live motion speed, while `stop moving` ends continuous motion. Short `nudge` phrases remain available for bounded one-step motion when the user wants a smaller visible adjustment. `start scrolling up`, `start scrolling down`, `start scrolling left`, and `start scrolling right` begin active-window scrolling in the current visible pointer/focus region until `stop scrolling` is heard. `scroll to top`, `scroll to bottom`, `scroll to left edge`, and `scroll to right edge` move the active visible app to a scroll edge through normal keyboard navigation. `hold mouse` presses the visible left mouse button at the current pointer position; `release mouse` releases it. This supports simple visible drag workflows when the user combines a hold, continuous motion or nudges, and release. Direct drag commands press the left mouse button, move one bounded step in the requested direction, including diagonals, and release. Plain `stop` remains reserved for Callsign's session-safety cancel path, so pointer motion uses the explicit `stop moving` phrase and continuous active-window scrolling uses `stop scrolling`. Callsign still prefers semantic UI Automation, numbered controls, and the visible grid before pointer fallback for important targets.

## Command discovery mode

`voice help`, `show commands`, `show all commands`, `show command list`, `open voice access help`, and `what can I say` open a command discovery surface. `hide commands`, `close commands`, `cancel commands`, and `dismiss command palette` close the palette without changing the active session. The palette also keeps a visible close button with the spoken label `Close command palette` so the dismissal path is obvious before voice input begins. Its quick filters include dedicated `Windowing`, `Task View`, `Snap Layouts`, `Task Manager`, `Show Desktop`, `Close Window`, `Getting Started`, `Open Voice Access Guide`, `Release Proof`, `Read Voice Mode`, `Restart Proof`, `Open Installer`, `Voice Mode`, and `Desktop` chips for Task View, Snap Layouts, Task Manager, show-desktop, close-window, the clean-install walkthrough, the Voice Access guide route, the release-proof walkthrough step, the voice-mode readback action, the installer-open action, voice mode switching, and virtual-desktop commands so the visible discovery surface mirrors the broader desktop/workspace workflow.
Escape dismisses the palette the same way the visible close button does, keeping the discovery surface consistent with the other transient HUDs.
`getting started`, `open voice access guide`, and `show voice access guide` open the clean-install walkthrough so new users can jump to Account, Voice, Session, Shortcuts, Plans, Updates, and Packs setup surfaces. The walkthrough also exposes a direct `Open Voice Access Guide` button so the user can reopen the guide from the current visible step, and the Account and Help tabs now surface the same guide route as a visible button beside `Getting Started`. Its voice cue now mentions the same guide route alongside the other setup discovery paths. It still points straight at the Voice tab's wake-repair and voice-identity recovery controls so a fresh install has a visible path back to calibration if wake feels weak. It exposes accessible names and descriptions for its form, surface, title, summary, safety and tier summary, status, steps, navigation buttons, visible close button, and dismissal buttons so first-run setup remains readable through assistive technology. The Safety and Help surfaces also expose direct visible routes for `start listening` and `stop listening`, plus the common windowing commands `task view`, `snap layouts`, `show desktop`, `close window`, `switch to app`, `next window`, `previous window`, `new desktop`, `next desktop`, and `previous desktop`, the browser navigation verbs `browser back`, `browser forward`, `browser new tab`, `browser find`, and `browser close tab`, the accessibility settings routes for `magnifier settings`, `narrator settings`, `captions settings`, `speech settings`, `open settings`, `display settings`, `sound settings`, `accessibility`, `mouse settings`, `keyboard settings`, `privacy settings`, `power settings`, `installed apps`, `default apps`, `date/time`, `notifications`, `windows update`, and `personalize`, the voice-control routes for `volume up`, `volume down`, `mute volume`, `play or pause`, `next track`, `previous track`, `stop media`, `quick settings`, `notification center`, `clipboard history`, `snipping toolbar`, `task manager`, `minimize window`, `maximize window`, and `restore window`, the keyboard routes for `press enter`, `press tab`, `press escape`, `press backspace`, `press space`, `press delete`, `press insert`, `press windows key`, `press context menu`, and `press caps lock`, the navigation-key routes for `up`, `down`, `left`, `right`, `home`, `end`, `page up`, and `page down`, the chord shortcuts for `shift tab`, `ctrl tab`, `ctrl shift tab`, `alt left`, `alt right`, `alt up`, `alt down`, `ctrl home`, `ctrl end`, `ctrl shift home`, and `ctrl shift end`, the symbol shortcuts for `comma`, `period`, `slash`, `question`, `semicolon`, `colon`, `quote`, `at sign`, and `plus`, the dictation review routes `read dictation`, `stop reading`, `show correction alternatives`, and dictation formatting, and the common editing and document controls `copy`, `paste`, `select all`, `undo`, `redo`, `new document`, `open file`, `print`, `zoom in`, `zoom out`, and `reset zoom`, so the user can reach the live listener, desktop-management, browser-navigation, accessibility-settings, visible dictation-recovery, media, shell-surface, keyboard, navigation-key, chord, symbol, and everyday editing controls from the discovery page instead of hunting through tabs. The safety and tier summary names the Free alpha parity core, the visible `stop`, `cancel`, `stop listening`, and `reset session` escape path, and the fact that community, Pro, and Advanced packs remain reviewed, disableable, signed when distributed, and policy-gated before commands can run.
`open voice access settings` and `show voice access settings` open Callsign's visible Voice setup surface so microphone, wake, and identity settings stay local and inspectable.

Account, Voice, Session, and Voice Identity Training workflow buttons expose accessible spoken labels for account save/delete, data/log/app folder access, wake repair, voice identity training, sample recording/playback/reset, voice identity enrollment, microphone and wakeword calibration, identity-runtime repair, listening, wake, callsign verification, command capture, Start menu launch, ambiguous app confirmation, cancel, reset, release proof, and visible close dismissal. This keeps Callsign's identity-first setup flow discoverable through numbered controls and assistive technology before any command is allowed to run.

The Packs surface must make extension safety legible before enablement. Pack rows and details show tier, load status, community/import source, signature status, whether a signature is required, and any entitlement or signature gate that prevents commands from running. A dedicated visible filter field and matching quick chips let users narrow the installed list by community, trusted, disabled, entitlement, signature, pack name, or other review terms without losing the visible source and gate cues. A dedicated visible drop zone accepts community command pack `.dll` files or folders of DLLs, routes them through the same import handler as the file/folder buttons, and explains that dropped packs are copied locally, imported disabled by default, watched for live discovery, and must be reviewed before enablement. The full Packs tab now accepts the same drag-and-drop path so the import gesture stays visible no matter where the user drops the files. When the watched folder discovers a newly added pack, Callsign reuses the visible import splash so the new commands or features are announced before enablement. A visible last-import summary line now repeats the most recent import result, pack source, pack tier, and the next review action so the user can tell at a glance whether they should review, enable, update, rollback, or troubleshoot before leaving the Packs surface. The imported-pack splash mirrors the update splash with a visible `Read Import Again` action so the user can replay the narration if they miss the first pass, and the Packs surface now adds its own `Read Import Again` button and voice cue so the replay path stays visible even after the splash closes and can reopen the last import summary after dismissal. Its spoken and visible pack-change details now call out community versus trusted source alongside tier and signature state. Voice routes such as `import extension pack`, `import extension folder`, `update extension pack`, `rollback extension pack`, `refresh packs`, `open packs folder`, `enable selected pack`, `disable selected pack`, and `remove selected pack` map to the same visible Packs workflow so the import, update, and rollback path stays discoverable without hiding the controls behind a mouse-only path. A dedicated selected-pack summary line repeats those safety fields at a glance so the user does not have to reconstruct them from the command list. A dedicated enablement-readiness line says whether the selected pack is enabled, disabled for review, or blocked by signature, entitlement, invalid metadata, missing files, duplicate ids, or load failure, and reminds the user to review tier, signature, risk, privacy, approval, and visibility before enabling. The command palette can list disabled, unsigned, Pro, or Advanced extension commands as discovery metadata, but its availability text, selected-command details, and voice cue must say when a command is disabled, signature-gated, or entitlement-gated and must make clear that gated commands are listed for discovery only and will not route until the relevant user-enable, signature, or paid-tier requirement is satisfied. The selected-command details include a dedicated routing gate line so users do not have to infer routeability from the table column. Import, folder import, update, rollback, drag/drop, open-folder, enable, disable, and remove controls expose accessible spoken labels so community extension review and rollback remain discoverable through numbered controls and assistive technology. This keeps community, Pro, and Advanced packs reviewable without weakening Callsign's wake, identity, policy, visibility, and audit gates.

The Shortcuts surface is the local Voice Access-style shortcut builder. `open voice shortcuts`, `show voice shortcuts`, and `open shortcuts` switch to a visible surface where the user can save a spoken phrase plus one to eight steps. Each step is either an existing Callsign-visible command such as `browser focus address bar`, `press control l`, `open notepad`, or `type hello world`, or a bounded wait step measured in milliseconds. A persistent safety line states that shortcuts compose existing visible Callsign commands and still require wake, identity, policy, visibility, audit, and any paid entitlement gates; bounded waits do not add new privileges. Save, delete, enable, disable, add-command, add-wait, and remove-action controls expose spoken labels so a user can manage shortcuts through visible controls, numbered controls, and dictation without leaving the Callsign app. Enabled shortcuts appear in command discovery under `Voice shortcuts` and behave like a built-in pack, but they remain local-first and do not grant any new privilege beyond the commands they compose.

The command palette should:

- group commands by purpose,
- name app-launch commands as app launch actions rather than generic app lists,
- surface the app-choice follow-up commands for ambiguous launches, such as `choose app 1` and `confirm app`,
- surface safety commands such as `cancel`, `stop`, `stop now`, `pause`, `stop listening`, and `reset session` as first-class visible commands,
- show the exact phrase to say,
- list known alternate spoken phrases alongside the primary phrase,
- list current parity examples such as `correct all text`, `close active app`, and safe key phrases as the command set grows,
- keep representative examples visible in selected-command details, including natural late-list aliases such as dictation fixes, browser find text, pointer nudges, and extension commands,
- keep a visible safety line for `cancel`, `stop listening`, and `reset session`, with selected-command details listing the urgent stop aliases,
- expose accessible names and descriptions for the command-palette surface, search field, command results list, title, verified-session instructions, result count, safety line, and selected-command details,
- show a compact boundary badge that names the open Free core and the paid entitlement/policy gate,
- expose visible quick filters for all commands, available commands, Free commands, app launch commands, navigation commands, Plans, Updates, Community, profile commands, runtime commands, update commands, diagnostics commands, help commands, system commands, browser commands, file commands, keyboard commands, mouse commands, visible controls, show numbers, show grid, show keyboard, settings commands, media commands, window commands, editing commands, safety commands, dictation, and extension commands so `what can I say?` starts with useful Apple-style browsing instead of a blank search box,
- let users search by category, phrase, example, source, tier, availability, load status, risk tier, and approval requirement, including structured `category:` searches, `status:available` for open commands, and approval filters such as `approval:visible choice`, `approval:fresh`, and `approval:none`, so Free, community, Pro, Advanced, disabled, signature-gated, entitlement-gated, visible-choice, fresh-identity, and approval-gated commands are discoverable before routing,
- show a dedicated tier column so Free, Pro, and Advanced commands are visible in the results list without selecting a row,
- include extension-pack commands when loaded,
- show a dedicated availability column so available, disabled, entitlement-required, and signature-required commands are visible in the results list without selecting a row,
- label extension-pack commands with pack tier and availability, including disabled, entitlement-required, and signature-required states,
- show risk/approval context where useful,
- remain searchable by phrase, alias, example, category, or source,
- show a compact result count while filtering,
- include command source metadata so open-core commands and extension commands are distinguishable,
- keep approval/fresh-identity requirements visible next to each command,
- and keep the existing Voice tab help text as an accessible fallback.

The visual target is the same compact macOS Voice Control-style surface used by the numbered-control HUD: light, searchable, readable, and focused on what the user can say next.

This is part of the Voice Access parity path because users need an always-available way to discover commands without leaving the visible Callsign model.

## Voice mode controls

Voice mode controls switch Callsign between commands-only, dictation-only, and the default command-plus-dictation behavior. These commands update the visible status surface and remain gated by the wake, identity, policy, visibility, and audit path.
The Session tab exposes a visible `Voice mode` chooser with accessible spoken labels for `Commands Only`, `Dictation Only`, and `Commands + Dictation`, a compact wake status strip for detector, summary, score, and margin, and visible listener buttons with Voice Access-style wake/sleep and microphone aliases so the same mode and listener controls remain discoverable through numbered controls and assistive technology. The compact runtime authority badge treats stale runtime snapshots as unknown current service health instead of continuing to claim that the background service is hearing audio, so dropped or delayed runtime state does not mislead the user. The detailed Runtime proof line also labels `snapshot=fresh` or `snapshot=stale`; when the snapshot is stale, the microphone-level text and proof line tell the user to restart or reconnect the Callsign service before trusting microphone state. The Voice tab also shows the current wake threshold, sensitivity, calibration timestamp, and calibration sample count so the user can see how wake tuning is set before starting a session. The startup walkthrough now includes a visible step badge and current-surface badge so the clean-install path is easier to scan at a glance.

Supported phrases:

- `commands only mode`
- `start command mode`
- `turn off dictation mode`
- `dictation mode`
- `start dictation mode`
- `typing mode`
- `default mode`
- `commands and dictation mode`
- `commands plus dictation mode`

## Dictation editing mode

Dictation edit commands operate on the visible review surface before text is read aloud, copied, pasted, inserted, or otherwise sent onward.
Direct text-entry phrases also land in this review surface first. `type hello world`, `write hello world`, `insert text hello comma world`, and `dictate alpha new line bravo` add reviewed text inside Callsign; they do not type directly into another app until the user explicitly copies or pastes from the review surface. The user can say `read dictation`, `read that back`, or `speak text` to hear the selected text or whole review buffer through local speech synthesis before taking an external text action. The user can say `stop reading`, `stop readback`, or `stop speaking` to cancel that local readback without changing the reviewed text.
The Dictation tab keeps a persistent accessible safety line, a live preview line, and a compact status strip that state the same boundary in the UI: dictated text stays in Callsign's review buffer until copy or paste, paste into sensitive targets is blocked, readback is local, stopping readback leaves the review buffer unchanged, and the review strip shows the current mode, review size, and local readback state at a glance. Local and service-fed dictation are bounded to a 10-minute capture window, 12,000 reviewed characters, and 128 service segments; when a bound is reached Callsign stops capture visibly and preserves the review buffer instead of silently continuing to collect transcript text.

Supported phrases:

- `type hello world`
- `start typing`
- `start taking dictation`
- `resume typing`
- `pause typing`
- `pause voice typing`
- `stop taking dictation`
- `stop voice typing`
- `write hello world`
- `insert text hello comma world`
- `dictate alpha new line bravo`
- `spell it out`
- `spell alpha bravo charlie`
- `spell capital alpha bravo cap letter charlie`
- `spell capital alpha lowercase bravo lower case letter charlie digit five number six`
- `type letter`
- `insert alpha underscore one`
- `spell support at sign example dot com`
- `insert open bracket alpha close bracket plus sign one`
- `spell womprat`
- `add womprat to vocabulary`
- `add to vocabulary womprat`
- `add project zephyr to dictation vocabulary`
- `turn on fluid dictation`
- `turn off fluid dictation`
- `turn on automatic punctuation`
- `turn off automatic punctuation`
- `turn on profanity filter`
- `do not filter profanity`
- `revert`
- `undo that`
- `select privacy policy`
- `go before privacy policy`
- `move after the phrase privacy policy`
- `delete privacy policy`
- `replace privacy policy with safety notes`
- `select from privacy to section`
- `delete from privacy to section`
- `replace from privacy to section with safety notes`
- `select previous word`
- `select next word`
- `go to previous word`
- `go to next word`
- `delete previous word`
- `delete next word`
- `go to previous sentence`
- `go to next sentence`
- `select previous sentence`
- `select next sentence`
- `delete previous sentence`
- `delete next sentence`
- `go to beginning of text`
- `select to beginning of text`
- `delete to beginning of text`
- `go to end of text`
- `select to end of dictation`
- `delete to end of dictation`
- `go to previous paragraph`
- `go to next paragraph`
- `go to line start`
- `go to line end`
- `delete to line start`
- `delete to line end`
- `go to paragraph start`
- `go to paragraph end`
- `delete to paragraph start`
- `delete to paragraph end`
- `comma`
- `period`
- `question mark`
- `exclamation`
- `exclamation point`
- `semi colon`
- `colon`
- `quote`
- `open parentheses`
- `close parentheses`
- `open square bracket`
- `close square bracket`

Spoken target-text edit commands match words in the visible review buffer, tolerate punctuation between words, and affect only the first visible match. Range commands select, delete, or replace from the first start phrase through the next end phrase. Cursor movement commands such as `go before privacy policy` and `move after the phrase privacy policy` move the caret inside the visible review buffer without changing text. They do not reach into another app directly; the user still reviews the result before copy, paste, or insertion.

Voice Access-style vocabulary commands such as `add womprat to vocabulary`, `add to vocabulary womprat`, and `add project zephyr to dictation vocabulary` store short words or phrases in the active local profile as dictation bias metadata. Vocabulary entries stay on the PC unless the user explicitly exports or syncs the profile; they do not send custom words to a cloud model.

Voice Access-style dictation option commands such as `turn on fluid dictation`, `turn off fluid dictation`, `turn on automatic punctuation`, `turn off automatic punctuation`, `turn on profanity filter`, and `do not filter profanity` update local profile settings and show the change in the visible status surface. New dictated phrases and service-fed review updates respect those settings locally. Callsign's open-source fluid dictation path performs deterministic local cleanup for obvious filler words, spacing, casing, and sentence punctuation in the visible review surface; `revert` or `undo that` restores the prior visible review text through the normal local undo path. These options are free local dictation preferences; they are not paid features and do not bypass review before text leaves Callsign.

The visible Dictation review safety line, compact status strip, correction launcher, formatting launcher, and buttons expose accessible spoken labels for the review-buffer boundary, sensitive-target paste blocking, local readback, start/stop, read-aloud review, stop-readback, copy/paste, clear/cut, undo/redo, navigation, selection, deletion, line and paragraph boundaries, word/sentence/paragraph movement, replacement prompts, punctuation, parentheses, hyphen/dash, slash, and at-sign insertion. This keeps the review surface discoverable through numbered controls and assistive technology before text leaves Callsign.

The Browser tab includes visible overlay launchers for `show numbers`, `show grid`, and `hide overlays` so browser-page targeting stays one click away from the same macOS-style visible-control surfaces used elsewhere in Callsign.

## Dictation correction mode

Dictation corrections operate on Callsign's visible review surface before text is copied, pasted, inserted, or otherwise sent onward.

The Dictation tab includes a visible correction launcher so users can choose a scope and open the correction HUD without relying on voice alone.

The Dictation tab also includes a visible formatting launcher so users can choose a scope and apply sentence case, title case, uppercase, or lowercase to the reviewed text before copying or pasting it onward.

Dictation paste is blocked when the foreground target looks like a password, passcode, payment, wallet, recovery phrase, or password-manager surface. Callsign keeps the reviewed text visible and tells the user to paste manually only if that destination is intentional.

Supported phrases:

- `correct previous word`
- `correct previous sentence`
- `correct previous paragraph`
- `correct all text`
- `show correction alternatives`
- `next correction`
- `previous correction`
- `accept correction`
- `choose correction 1` through `choose correction 6`
- `cancel correction`
- `close correction`
- `hide correction`
- `dismiss correction`

The correction chooser presents numbered alternatives derived from the reviewed text, such as case changes, joined words, hyphenated words, and punctuation cleanup. The compact macOS-style correction HUD shows the active scope, the current numbered choice, and spoken cues for `next correction`, `previous correction`, `accept correction`, natural aliases such as `accept that` or `use that`, numbered choices such as `choose correction 1`, shorter choices such as `choose one`, `pick option two`, or `use alternative 3`, and `close correction`. The correction form, surface, title, visible close button, scope, summary, voice cue, persistent safety line, and numbered alternatives list expose accessible names and descriptions so correction choices are discoverable through assistive technology before any replacement is applied. Saying `accept correction` or `accept that` applies the currently highlighted alternative after voice navigation. The safety line states that choosing or accepting replaces reviewed text, while closing, hiding, dismissing, or cancelling the correction HUD leaves the reviewed text unchanged. Choosing an option replaces only the highlighted review text or the explicitly requested whole review buffer, and keeps focus in the dictation surface.

Dictation formatting commands operate on the same visible review buffer before anything is copied, pasted, or inserted elsewhere.

Supported phrases:

- `capitalize previous word`
- `uppercase previous sentence`
- `lowercase all text`
- `title case previous paragraph`
- `make that uppercase`
- `make previous sentence lower case`
- `make all text title case`

After a formatting command, Callsign highlights the changed span in the review surface so the user can inspect it before taking any external text action.

Dictation punctuation and symbol commands also insert into the visible review buffer first. Supported phrases include:

- `full stop`
- `dot`
- `comma`
- `period`
- `question mark`
- `exclamation`
- `exclamation mark`
- `exclamation point`
- `quote`
- `open parenthesis`
- `open parentheses`
- `left paren`
- `close parenthesis`
- `close parentheses`
- `right paren`
- `open bracket`
- `open square bracket`
- `close bracket`
- `close square bracket`
- `open brace`
- `close brace`
- `semicolon`
- `semi colon`
- `colon`
- `hyphen`
- `minus`
- `dash`
- `slash`
- `backslash`
- `back slash`
- `pipe`
- `vertical bar`
- `backtick`
- `back tick`
- `tilde`
- `underscore`
- `under score`
- `plus sign`
- `equal sign`
- `equals sign`
- `number sign`
- `pound`
- `dollar sign`
- `ampersand`
- `and sign`
- `percent sign`
- `caret`
- `asterisk`
- `star`
- `at sign`
- `at symbol`
- `single quote`
- `apostrophe`

## File search mode

File search is a visible Explorer-backed workflow. Search results appear in the Files tab before any result is opened or revealed. Callsign searches user/profile-style roots such as Desktop, Documents, Downloads, media folders, Callsign app data, and temporary working areas; system and program roots are blocked by policy and reported as warnings instead of being scanned. The Files tab keeps a persistent accessible safety line that states the same boundary in the UI: search stays in common user folders and Callsign data, results are shown before action, and executable or script-like files are blocked from direct open and should be revealed in Explorer instead.

Supported phrases:

- `open files tab`
- `show files tab`
- `go to files`
- `switch to files`
- `files tab`
- `search my files for budget`
- `search my files for invoice`
- `search my documents for budget`
- `look in files for alpha notes`
- `find file budget`
- `find folder named invoices`
- `find a folder called receipts`
- `look for folder invoices`
- `search my folders for invoices`
- `look in folders for receipts`
- `search folders called invoices`
- `search my pc for invoice`
- `select result 1`
- `select first result`
- `choose twentieth result`
- `open file result 1`
- `open folder result 2`
- `open result eleventh`
- `open second result`
- `reveal file result 1`
- `reveal search result 3`
- `show search result 4`
- `show result folder 2`
- `open containing folder for result 1`
- `show containing folder for result 2`

Opening a folder result uses Explorer. Opening a file result uses the normal Windows shell only for non-executable targets. Executable or script-like results are blocked from direct open and should be revealed in Explorer instead.

The visible Files tab exposes accessible spoken labels for the file-search safety boundary, search, a numbered result picker, explicit select/open/reveal result-number controls, plus open selected result and reveal selected result buttons so file workflows remain discoverable through numbered controls and assistive technology.

## Browser mode

Browser commands use visible browser surfaces and standard browser shortcuts. Callsign does not inspect page contents or run hidden browser scripts in this parity slice. The Browser tab keeps a persistent accessible safety line that states the same boundary in the UI: browser targets are web-only, non-web schemes for files, scripts, settings, installers, and apps are blocked in browser mode, and browser commands use visible shortcuts without hidden page inspection. Service-side browser commands now route `browser-*` action ids through the same visible `BrowserLaunchService` action path as the Browser tab before any URL or search fallback, so spoken tab, find, scroll, zoom, private-window, save, print, downloads, history, address-bar, and page-action commands do not become accidental web searches.

Direct browser opens are web-only: Callsign accepts `http` and `https` URLs, bare domains, and ordinary search text. Non-web URI schemes such as `file:`, `javascript:`, `data:`, `vbscript:`, `ms-settings:`, and installer/app schemes are blocked in browser mode so they cannot bypass the visible command surface intended for files, settings, or app actions.

Supported phrases include:

- `browser open example.com`
- `browser open callsign`
- `browser search callsign`
- `launch browser example.com`
- `browser go to example.com`
- `open website example dot com`
- `search the web for Callsign setup`
- `type in address bar example dot com`
- `search address bar for Callsign setup`
- `address bar`
- `url bar`
- `browser back`
- `browser forward`
- `browser refresh`
- `browser new tab`
- `open new tab`
- `browser new window`
- `browser private window`
- `new private window`
- `browser incognito`
- `open incognito window`
- `browser bookmark page`
- `bookmark this page`
- `add bookmark`
- `browser bookmarks`
- `browser save page`
- `browser print page`
- `browser next tab`
- `browser previous tab`
- `browser full screen`
- `browser close tab`
- `close browser tab`
- `reopen closed tab`
- `undo close tab`
- `browser focus address bar`
- `browser home`
- `browser downloads`
- `show downloads`
- `browser history`
- `show history`
- `browser find`
- `open find box`
- `show find box`
- `search this page for privacy policy`
- `find next`
- `find previous`
- `browser scroll up`
- `browser page up`
- `page up in browser`
- `browser scroll down`
- `browser page down`
- `page down in browser`
- `browser start scrolling down`
- `browser start scrolling left`
- `browser stop scrolling`
- `browser scroll left`
- `browser scroll right`
- `browser scroll top`
- `go to top of page`
- `browser scroll bottom`
- `go to bottom of page`
- `browser zoom in`
- `browser zoom reset`

For address-bar text, Callsign focuses the visible browser address bar, resolves bare domains and ordinary search text through the same web-only validation as direct browser opens, types the resulting `http`/`https` target, and presses Enter. The visible Browser tab exposes a dedicated address-bar text field plus a button with spoken phrases such as `type in address bar example dot com` and `search address bar for callsign` so this route is discoverable without leaving the Browser surface. It does not execute scripts, local file paths, settings URIs, or app/installer schemes through the browser path.

For page search text, Callsign opens the visible browser Find field and types the requested search term. The visible Browser tab exposes a dedicated page-find text field plus a button with spoken phrases such as `search this page for privacy policy` and `find privacy policy on this page` so the text-search path is visible and bounded to the active browser window.

Browser scrolling stays in the visible active page. Bounded phrases such as `browser scroll down` and `page down in browser` step the page once, while browser-prefixed phrases such as `browser start scrolling down` and `browser start scrolling left` begin continuous page movement until the user says `browser stop scrolling`. Plain `start scrolling` and `stop scrolling` are reserved for active-window Voice Access-style scrolling, and plain `stop` remains Callsign's session-safety cancel path.

The visible Browser tab exposes accessible spoken labels for the browser safety boundary, open/search, navigation, tab and window actions, closed-tab restore, home, bookmarks, downloads, history, save/print, address bar focus, page find, bounded horizontal and vertical scrolling, continuous scroll start/stop controls, fullscreen, and zoom controls. These labels keep browser parity commands discoverable through numbered controls and assistive technology without inspecting page contents.

## Safe settings mode

Settings commands open visible Windows Settings pages without changing values on the user's behalf.

Supported phrases:

- `windows settings`
- `open display settings`
- `open sound settings`
- `open bluetooth settings`
- `wifi settings`
- `open network settings`
- `accessibility settings`
- `magnifier settings`
- `zoom settings`
- `narrator settings`
- `screen reader settings`
- `captions settings`
- `live captions settings`
- `speech settings`
- `voice access settings`
- `voice typing settings`
- `dictation settings`
- `open magnifier`
- `magnifier zoom out`
- `close magnifier`
- `open mouse settings`
- `open keyboard settings`
- `open privacy settings`
- `power and battery settings`
- `installed apps settings`
- `default apps settings`
- `date and time settings`
- `notifications settings`
- `windows update settings`
- `personalization settings`

Plain `open Settings` remains part of the visible Start menu app-launch flow. The page-specific settings commands are the safer Voice Access parity path for getting the user to a system surface without hidden changes. Visible System tab buttons for Windows, display, sound, Bluetooth, network/Wi-Fi, accessibility, mouse, keyboard, privacy, power/battery, installed apps, default apps, date/time, notifications, Windows Update, and personalization settings expose accessible spoken labels so numbered controls and assistive technology can discover the same safe routing surface.

The System tab keeps a persistent accessible safety line that states the same boundary in the UI: system commands stay visible and reversible where possible, settings and shell surfaces open visibly, keyboard/mouse/window actions use bounded input, and Callsign does not toggle settings, read clipboard contents, capture screenshots, force-kill apps, or act in hidden windows from this surface.

Accessibility subpage commands open visible Windows Settings pages for Magnifier, Narrator, Captions, and Speech without toggling those assistive features. The visible System tab exposes accessible spoken labels for Magnifier, Narrator, Captions, and Speech settings so those accessibility routes remain discoverable through numbered controls and assistive technology. Magnifier commands use the visible Windows Magnifier accessibility surface. `open magnifier` and `magnifier zoom in` open or increase Magnifier, `magnifier zoom out` decreases it, and `close magnifier` sends the normal Windows close shortcut for Magnifier.

## Volume and media mode

Volume and media commands use safe local actions that remain visible in the System tab.

Supported phrases:

- `volume up`
- `volume down`
- `mute volume`
- `mute audio`
- `play or pause`
- `play media`
- `pause media`
- `next track`
- `previous song`
- `stop playback`

The System tab shows the requested action after execution so the user can see what Callsign sent. Volume and media buttons expose accessible names and descriptions that include the matching spoken phrases, keeping the visible controls discoverable through assistive technology and numbered-control label matching.

## Media control mode

Media controls use safe local media-key commands. Callsign does not inspect media content or open a player.

## Window layout mode

Window layout commands operate on the active visible window using Windows snap shortcuts.

Supported phrases:

- `close this window`
- `close active app`
- `snap window left`
- `snap right`
- `move window up`
- `dock window down`
- `show snap layouts`

These commands are meant for visible desktop organization. Close-window phrases require explicit approval, then send the normal visible close request to the active app or window; they do not force-kill hidden processes. Window layout commands do not target hidden or minimized windows, and the System tab shows the requested action after execution. App-switching, show-desktop, Task View, virtual-desktop, and snap controls expose accessible names and descriptions with the matching spoken phrases so visible-control numbering and assistive technology can discover the same command surface.

## Task View mode

Task View commands make window switching visible instead of silently selecting a hidden target.

Supported phrases:

- `show open windows`
- `show windows`
- `show all windows`
- `task view`
- `open task view`
- `show task view`
- `window switcher`
- `next window`
- `previous window`
- `switch to Edge`
- `go to Notepad`
- `choose window 1`
- `confirm window`
- `switch application`
- `next application`
- `last app`
- `new virtual desktop`
- `next desktop`
- `previous desktop`
- `quick settings`
- `notification center`
- `project display`
- `display switch`
- `cast display`
- `wireless display`
- `emoji panel`
- `emoji picker`
- `symbol picker`
- `clipboard history`
- `open clipboard`
- `show clipboard picker`
- `snipping toolbar`
- `screen snip`
- `show screenshot toolbar`
- `open screenshot tools`

The active Windows surface remains visible, and the System tab records the requested action. `switch to Edge` and other named-app switching phrases resolve open windows by visible title and process name. If more than one visible window matches, Callsign shows a numbered choice list in the System tab and waits for `1`, `click 1`, `choose window 1`, `confirm window`, `next window choice`, `previous window choice`, or `cancel` before moving focus. Quick Settings, Notification Center, display projection/cast panels, the emoji/symbol picker, clipboard history, and the snipping toolbar open visible Windows shell surfaces only. The visible System tab exposes accessible spoken labels for these shell surfaces so the same actions remain discoverable through numbered controls and assistive technology. Callsign does not toggle network, Bluetooth, focus, notification settings, select a projection mode, connect to a wireless display, capture screenshots, read screenshots, save screenshots, upload screenshots, or inspect clipboard/history contents from those surfaces. Clipboard history and the snipping toolbar require explicit approval because Windows may display or create private clipboard/screenshot content. Callsign avoids close-desktop commands in the safe parity slice.

## Keyboard command mode

Keyboard commands are part of the safe Voice Access parity path for controlling the active visible window. The visible keyboard overlay includes a compact close button plus function-key, letter, number-row, modifier, held-modifier, release-all-modifiers, Enter, Backspace, Space, navigation, current/previous/next word, line, and paragraph selection, deletion, copy, cut, and formatting commands, selection-boundary movement, and arrow-key cues so discoverability matches the routed command surface and the user can see the safety release path. Selection-boundary aliases such as `move to beginning of selection` and `go to end of selection` collapse the selected range through visible keyboard semantics in the foreground control, `unselect that` / `clear selection` clear the active selection through the same visible foreground-keyboard path, `go to beginning of word` and `move to end of word` move to the current word boundary, `select word`, `select line`, and `select paragraph` select the current text unit at the cursor, and `delete word`, `delete line`, `delete paragraph`, `copy word`, `copy line`, `copy paragraph`, `cut word`, `cut line`, `cut paragraph`, `last character`, `select last character`, `move backward five characters`, `move left five characters`, `last six letters`, `forward six letters`, `right six letters`, `move left three words`, `move right three words`, `go up three lines`, `go down three lines`, `go back three lines`, `go up two paragraphs`, `go back two paragraphs`, `select down three paragraphs`, `select backward two lines`, `highlight forward five lines`, `copy previous word`, `copy next line`, `copy previous three characters`, `copy last four characters`, `copy backward four characters`, `copy forward six letters`, `copy backward three lines`, `copy previous three words`, `copy next five lines`, `copy next three paragraphs`, `cut previous paragraph`, `cut forward six letters`, `cut forward four lines`, `cut next five words`, `cut next five lines`, `cut next three paragraphs`, `bold previous word`, `italicize next line`, and `underline paragraph` act on text units through the visible foreground-keyboard path without reading clipboard contents. Its persistent safety strip says keypresses target the visible foreground app only and that `release all modifiers` clears held Shift, Control, or Alt. The visible System tab also exposes spoken labels for Task Manager, virtual desktop switching, Windows key, context-menu key, Caps Lock, Home, End, Page Up, and Page Down so those non-text controls remain discoverable through numbered controls and assistive technology.

Supported phrases include:

- `show keyboard`
- `show on screen keyboard`
- `keyboard overlay`
- `hide keyboard`
- `close keyboard`
- `cancel keyboard`
- `dismiss keyboard`
- `press enter`
- `press tab`
- `press escape`
- `press backspace`
- `press space`
- `press delete`
- `press insert`
- `press Windows key`
- `context menu key`
- `press caps lock`
- `press A`
- `letter key z`
- `press 5`
- `number key zero`
- `press comma`
- `press question mark`
- `symbol key at sign`
- `press shift tab`
- `press shift a`
- `press shift z`
- `press shift 1`
- `press shift 9`
- `press control tab`
- `press control shift tab`
- `press control shift t`
- `press control shift n`
- `press control shift 1`
- `press control shift 9`
- `press control c`
- `press control v`
- `press control a`
- `press control s`
- `press control f`
- `press control r`
- `press control l`
- `press control w`
- `press control 1`
- `press control 9`
- `press control plus`
- `press control minus`
- `press control zero`
- `press alt left`
- `press alt right`
- `press alt up`
- `press alt down`
- `press alt f`
- `press alt e`
- `press alt h`
- `press alt 1`
- `press alt 9`
- `press control home`
- `press control end`
- `press control shift home`
- `press control shift end`
- `press alt shift tab`
- `hold shift`
- `press and hold control key`
- `hold alt`
- `dismiss`
- `press tab five times`
- `release`
- `release shift`
- `release control`
- `release alt`
- `release all modifiers`
- `press home`
- `press end`
- `page up`
- `page down`
- `press f5`
- `function key twelve`
- `copy`
- `paste`
- `cut`
- `select all`
- `save`
- `undo`
- `redo`
- `bold`
- `bold that`
- `make that bold`
- `italic`
- `italicize that`
- `make that italic`
- `underline`
- `underline that`
- `make that underlined`
- `previous character`
- `next character`
- `select previous character`
- `select next character`
- `delete previous character`
- `delete next character`
- `go to line start`
- `go to line end`
- `go to previous line`
- `go to next line`
- `select to line start`
- `select to line end`
- `select previous line`
- `select next line`
- `delete to line start`
- `delete to line end`
- `delete previous line`
- `delete next line`
- `new document`
- `open file`
- `print`
- `go to next word`
- `go to previous word`
- `select previous word`
- `select next word`
- `delete previous word`
- `delete next word`
- `go to previous sentence`
- `go to next sentence`
- `select previous sentence`
- `select next sentence`
- `delete previous sentence`
- `delete next sentence`
- `go to paragraph start`
- `go to paragraph end`
- `select to paragraph start`
- `select to paragraph end`
- `delete to paragraph start`
- `delete to paragraph end`
- `go to next paragraph`
- `go to previous paragraph`
- `select previous paragraph`
- `select next paragraph`
- `delete previous paragraph`
- `delete next paragraph`
- `zoom in`
- `zoom out`
- `reset zoom`

The overlay/readout stays active while the command is routed, and the System tab records the requested keypress or shortcut. Visible System tab keypress controls expose accessible spoken labels for Enter, Tab, Escape, Backspace, Space, Delete, Insert, and arrow keys. Function keys are limited to F1 through F12 in this slice. Callsign also supports Voice Access-style repeated single-key presses such as `press tab five times` and `press down three times`, plus repeated directional movement such as `move down two times` and `go right four times`, for the same safe key set, keeping the action visible and bounded in the foreground app. `dismiss` maps to a visible Escape keypress for menus, flyouts, and transient surfaces. Held modifier commands are limited to Shift, Control, and Alt, and `release all modifiers` is the visible safety release command; Callsign does not support holding the Windows key or arbitrary keys. Editing, document, and zoom shortcuts are exact natural phrases so commands like `copy` do not swallow unrelated longer utterances. Visible System tab editing/document/zoom controls expose accessible spoken labels for copy, paste, cut, select all, save, undo, redo, formatting, find, new document/window, open file, print, zoom, and close-window commands. Active-app character commands target only the visible foreground app: previous/next character maps to left/right arrow, selection maps to Shift+left/right, and deletion maps to Backspace/Delete. Active-app line commands map to Home/End, Up/Down, Shift+Home/End, Shift+Up/Down, and bounded select-then-delete sequences for line deletion. Character and line controls are visible in the System tab with matching accessible spoken labels for movement, selection, and deletion. Active-app word, sentence, and paragraph commands route before generic visible-control label matching so phrases such as `select previous word` and `select previous paragraph` edit text instead of activating a numbered control by label; their visible System tab controls expose matching accessible spoken labels for movement, selection, and deletion. Paragraph boundary commands use bounded Alt+Up/Down or Alt+Shift+Up/Down style actions plus explicit delete where requested. `print` opens the visible print dialog with `Ctrl+P`; it does not silently confirm printing. Plain `zoom in`, `zoom out`, and `reset zoom` target the active visible app. Browser-specific zoom remains available through `browser zoom in`, `browser zoom out`, and `browser zoom reset`. 

The keyboard overlay form, header, close button, cue, and safety strip expose accessible names and descriptions for the spoken keyboard-command affordance and foreground-targeting boundary so the visible on-screen keyboard remains available to assistive technology. 

Modifier chords are an allowlisted safe subset and currently include tab/home/end, Shift+tab, Shift+letter chords, Shift+digit chords, Ctrl+tab, Ctrl+Shift+Tab, Alt+Shift+Tab, Control+Shift+letter chords, Control+Shift+digit chords, Alt+arrows, Alt+letter access-key chords, Alt+digit access-key chords, Control+Home/End, Control+Shift+Home/End, Control+letter chords, Control+digit chords, and common reversible Control-key shortcuts for copy, paste, cut, select all, save, undo, redo, find, formatting, document, open-file, print, navigation, refresh, close-tab/window, tab selection, and zoom control. The list is intentionally constrained to single keypress chords or a single explicitly held modifier, stays visible in command discovery, and does not become arbitrary multi-step macro execution.

## Update splash mode

The update splash is a visible macOS-style surface that appears when the latest manifest includes changes. It reads the manifest summary out loud, shows the added/changed/removed command counts, and lists the updated commands, features, or extension packs in the details area before closing itself automatically. A visible `Read Summary Again` action lets the user replay the narration if they miss it the first time. A persistent voice cue names `close update splash`, `dismiss update splash`, `hide update splash`, and `cancel update splash`, and reminds the user that reviewing update details does not enable gated commands because policy and entitlement still decide what can run. The splash surface, panel, title, published-time label, summary, voice cue, and details list expose accessible names and descriptions so newly added commands, feature highlights, and extension-pack changes are discoverable through assistive technology. Enter and Escape dismiss the splash the same way the visible close button does, and dismissing the splash never changes installed commands.
The close control uses a compact icon-style glyph instead of a text label so the top-right corner stays visually quiet.
The Voice tab also exposes a visible `Read Voice Mode` action, and the command palette includes `read voice mode status`, so the current commands-only, dictation-only, or default mode can be read back locally without changing the active session.

## v1.x voice modes

Alpha v1 voice modes now ship as the current free parity surface:

- v1.1: dictation with visible review before text actions
- v1.2: browser control with visible navigation and approval boundaries
- v1.3: system control, WSL/Linux workflows, and file search with visible results

## Response style

Keep active-session responses short.

Good:

- `Identity confirmed. Say the app name.`
- `Identity did not match. Try again.`
- `Launching Notepad.`
- `Session cancelled.`

Avoid long explanations while the user is in the middle of a voice flow.

## Stop and recovery

Required stop phrases:

- `Stop`
- `Stop now`
- `Cancel`
- `Pause`
- `Go to sleep`
- `Voice access sleep`
- `Microphone off`

Required behavior:

1. Stop pending actions.
2. Clear the session; plain `Stop`, `Stop now`, and `Pause` map to the visible session-cancel path, while `Go to sleep`, `Voice access sleep`, and `Microphone off` stop the listener.
3. Hide the overlay.
4. Show a visible status update.
5. Return to idle.

## Accessibility considerations

- Support keyboard interaction for setup screens.
- Keep session state visible.
- Show live text while the user is speaking.
- Show a recent speech history in the Session tab, with a visible and voice-accessible way to clear it.
- Allow voice enrollment reset and retry.
- Keep failures readable: mic, wake runtime, identity runtime, model, service, or launch failure.
