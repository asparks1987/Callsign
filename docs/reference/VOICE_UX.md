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
- `Hearing your command...`
- `Command: open Notepad`
- `Launching Notepad...`

The overlay must not steal focus, block input, or prevent the user from stopping the session.

The visual direction is macOS Voice Control-level clarity rather than a dense dashboard: compact, high-contrast, translucent where appropriate, and readable at a glance. Listening state, identity state, live transcript, and stop/cancel affordance must remain visible without taking focus.
The wake overlay keeps a persistent accessible safety line in the message panel: `stop`, `cancel`, `stop listening`, and `reset session` are visible escape phrases, and commands remain blocked until identity is confirmed. This keeps the Callsign -> identity verification -> command -> visible action contract readable at the exact moment the microphone session begins.
The shared WinForms visual contract is `CallsignVisualStyle`: surfaces should identify the `macOS Voice Control` target and remain compact, high-contrast, translucent where appropriate, non-activating for overlays, accessible, and tied to visible status. The contract now carries concrete smoke-testable evidence tokens: text contrast at or above 4.5:1, overlay/surface opacity between 0.86 and 0.99, compact rounded geometry between 20 and 26 px, Segoe UI-family system typography, and a visible stop/cancel/status affordance.
The wake overlay, visible-controls HUD, mouse grid, keyboard overlay, command palette, correction chooser, update splash, and startup walkthrough expose this shared visual contract for smoke verification.

Users can ask `what did you hear`, `read status`, or `repeat status` to hear the current visible status, last heard transcript, and next action through local speech synthesis without executing an external command. `stop status readback` or `stop reading status` cancels that local speech. `clear recent speech` or `clear speech history` clears the visible recent speech list and asks the background runtime to clear its transcript-history snapshot. This keeps the visible readout recoverable for users who missed the overlay, while preserving Callsign's rule that status replay and transcript-history clearing are local, visible actions.

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

The setup app includes a visible Updates tab so users can see the update server, the 25-hour check cadence, the last check time, the next due time, and whether a manifest is pending. Callsign phones home on startup and while running, then downloads and installs updates in the background when a new manifest is approved for the channel. The manual `Check Now` action stays visible for users who want to force an immediate check.

## Visible control mode

The setup app, foreground Windows apps, and overlay should remain understandable to users who rely on visible UI.

Current and v1.x command routing can support visible-control concepts such as:

- `show numbers`
- `show numbers here`
- `show numbers everywhere`
- `show numbers on notepad`
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

When the foreground window is not Callsign, `show numbers` first inspects the active app with Windows UI Automation and numbers enabled, on-screen controls. `show numbers on notepad` and other `show numbers on <app name>` phrases scan visible top-level windows by spoken app or window name, then number the matched surface without hidden switching. `show numbers on taskbar` targets the visible Windows taskbar through the same UI Automation path so Start, pinned apps, and tray surfaces can be numbered without hidden actions. Saying `click 1`, `click one`, `tap second`, or `click save as` invokes, selects, toggles, expands, or focuses the matching UIA element when the app exposes a safe semantic pattern. `double click 1` and `right click 1` move to the visible center of the numbered control and perform the bounded mouse action, preserving visible targeting for controls that need mouse-style interaction.

The focused numbered-control list can also be traversed by voice without selecting anything. `next control`, `next field`, `move to next field`, and `tab forward` advance the visible focus cue; `previous control`, `previous field`, `move to previous field`, and `tab backward` move it back. `activate control` or `click selected control` then invokes only the currently focused visible control. These phrases are routed through Callsign's visible-control abstraction, not hidden coordinate clicks.

If UI Automation cannot inspect the foreground app, Callsign falls back to numbering its own visible setup controls. The mouse grid remains the visible fallback for targets that cannot be reached semantically.

The numbered-control overlay uses a compact HUD plus floating numbered badges over detected controls. The HUD shows how many controls are currently numbered, the latest voice cue, what Callsign heard, the focused target, a subtitle that teaches click, double-click, and right-click by focused number or label, a persistent safety strip that says numbers act only on visible targets and `hide` or `cancel` exits without clicking, a visible close button, and a visible list that marks the focused item. The overlay form and HUD elements expose accessible names and descriptions for the numbered-control surface, cue, transcript, focus target, safety strip, summary, close button, and numbered target list so the visible state is also inspectable by assistive technology. The rest of the overlay remains transparent and click-through so the foreground app stays visually dominant. Focused controls receive a stronger ring and badge, matching the Voice Control pattern of clear target numbers without covering the working surface. If a needed target is not numbered, the HUD points the user to the mouse grid fallback instead of implying hidden targeting.

## Mouse grid mode

The mouse grid is the fallback for visible targets that cannot be reached semantically. Its compact header includes a visible close button, and its persistent safety strip says grid commands are visible pointer actions only, that the user can refine or undo before a click or drag, and that `hide grid` or `cancel` exits without acting.

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
- `hold mouse`
- `release mouse`
- `drag mouse up`
- `drag mouse down`
- `drag mouse left`
- `drag mouse right`
- `drag mouse top left`
- `drag mouse bottom right`

`move mouse <direction>` starts visible continuous pointer motion in the requested direction, including diagonals such as `top left` and `bottom right`. `move mouse <direction> <distance>` performs a bounded fixed-distance move such as `move mouse left five`. `move faster` and `move slower` adjust the live motion speed, while `stop moving` ends continuous motion. Short `nudge` phrases remain available for bounded one-step motion when the user wants a smaller visible adjustment. `hold mouse` presses the visible left mouse button at the current pointer position; `release mouse` releases it. This supports simple visible drag workflows when the user combines a hold, continuous motion or nudges, and release. Direct drag commands press the left mouse button, move one bounded step in the requested direction, including diagonals, and release. Plain `stop` remains reserved for Callsign's session-safety cancel path, so pointer motion uses the explicit `stop moving` phrase. Callsign still prefers semantic UI Automation, numbered controls, and the visible grid before pointer fallback for important targets.

## Command discovery mode

`voice help`, `show commands`, `show all commands`, `show command list`, `open voice access help`, and `what can I say` open a command discovery surface. `hide commands`, `close commands`, `cancel commands`, and `dismiss command palette` close the palette without changing the active session. The palette also keeps a visible close button with the spoken label `Close command palette` so the dismissal path is obvious before voice input begins.
Escape dismisses the palette the same way the visible close button does, keeping the discovery surface consistent with the other transient HUDs.
`getting started`, `open voice access guide`, and `show voice access guide` open the clean-install walkthrough so new users can jump to Account, Voice, Session, Shortcuts, and Packs setup surfaces. The walkthrough exposes accessible names and descriptions for its form, surface, title, summary, safety and tier summary, status, steps, navigation buttons, visible close button, and dismissal buttons so first-run setup remains readable through assistive technology. The safety and tier summary names the Free alpha parity core, the visible `stop`, `cancel`, `stop listening`, and `reset session` escape path, and the fact that community, Pro, and Advanced packs remain reviewed, disableable, signed when distributed, and policy-gated before commands can run.
`open voice access settings` and `show voice access settings` open Callsign's visible Voice setup surface so microphone, wake, and identity settings stay local and inspectable.

Account, Voice, Session, and Voice Identity Training workflow buttons expose accessible spoken labels for account save/delete, data/log/app folder access, wake repair, voice identity training, sample recording/playback/reset, voice identity enrollment, microphone and wakeword calibration, identity-runtime repair, listening, wake, callsign verification, command capture, Start menu launch, ambiguous app confirmation, cancel, reset, and visible close dismissal. This keeps Callsign's identity-first setup flow discoverable through numbered controls and assistive technology before any command is allowed to run.

The Packs surface must make extension safety legible before enablement. Pack rows and details show tier, load status, community/import source, signature status, whether a signature is required, and any entitlement or signature gate that prevents commands from running. A dedicated visible drop zone accepts community command pack `.dll` files or folders of DLLs, routes them through the same import handler as the file/folder buttons, and explains that dropped packs are copied locally, imported disabled by default, and must be reviewed before enablement. A dedicated selected-pack summary line repeats those safety fields at a glance so the user does not have to reconstruct them from the command list. A dedicated enablement-readiness line says whether the selected pack is enabled, disabled for review, or blocked by signature, entitlement, invalid metadata, missing files, duplicate ids, or load failure, and reminds the user to review tier, signature, risk, privacy, approval, and visibility before enabling. The command palette can list disabled, unsigned, Pro, or Advanced extension commands as discovery metadata, but its availability text must say when a command is disabled, signature-gated, or entitlement-gated and must make clear that gated commands will not route until the relevant user-enable, signature, or paid-tier requirement is satisfied. Import, folder import, drag/drop, open-folder, enable, disable, and remove controls expose accessible spoken labels so community extension review and rollback remain discoverable through numbered controls and assistive technology. This keeps community, Pro, and Advanced packs reviewable without weakening Callsign's wake, identity, policy, visibility, and audit gates.

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
- expose visible quick filters for all commands, available commands, Free commands, app launch commands, navigation commands, profile commands, runtime commands, update commands, diagnostics commands, help commands, system commands, browser commands, file commands, keyboard commands, mouse commands, visible controls, settings commands, media commands, window commands, editing commands, safety commands, dictation, and extension commands so `what can I say?` starts with useful Apple-style browsing instead of a blank search box,
- let users search by category, phrase, example, source, tier, availability, load status, risk tier, and approval requirement, including structured `category:` searches for family browsing and `status:available` for open commands, so Free, community, Pro, Advanced, disabled, signature-gated, entitlement-gated, and approval-gated commands are discoverable before routing,
- show a dedicated tier column so Free, Pro, and Advanced commands are visible in the results list without selecting a row,
- include extension-pack commands when loaded,
- show a dedicated availability column so available, disabled, entitlement-required, and signature-required commands are visible in the results list without selecting a row,
- label extension-pack commands with pack tier and availability, including disabled, entitlement-required, and signature-required states,
- show risk/approval context where useful,
- remain searchable by phrase, alias, example, category, or source,
- show a compact result count while filtering,
- include command source metadata so the built-in Free core and extension commands are distinguishable,
- keep approval/fresh-identity requirements visible next to each command,
- and keep the existing Voice tab help text as an accessible fallback.

The visual target is the same compact macOS Voice Control-style surface used by the numbered-control HUD: light, searchable, readable, and focused on what the user can say next.

This is part of the Voice Access parity path because users need an always-available way to discover commands without leaving the visible Callsign model.

## Voice mode controls

Voice mode controls switch Callsign between commands-only, dictation-only, and the default command-plus-dictation behavior. These commands update the visible status surface and remain gated by the wake, identity, policy, visibility, and audit path.
The Session tab exposes a visible `Voice mode` chooser with accessible spoken labels for `Commands Only`, `Dictation Only`, and `Commands + Dictation`, while the visible listener buttons expose Voice Access-style wake/sleep and microphone aliases so the same mode and listener controls remain discoverable through numbered controls and assistive technology.

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
The Dictation tab keeps a persistent accessible safety line that states the same boundary in the UI: dictated text stays in Callsign's review buffer until copy or paste, paste into sensitive targets is blocked, readback is local, and stopping readback leaves the review buffer unchanged.

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

The visible Dictation review safety line and buttons expose accessible spoken labels for the review-buffer boundary, sensitive-target paste blocking, local readback, start/stop, read-aloud review, stop-readback, copy/paste, clear/cut, undo/redo, navigation, selection, deletion, line and paragraph boundaries, word/sentence/paragraph movement, replacement prompts, punctuation, parentheses, hyphen/dash, slash, and at-sign insertion. This keeps the review surface discoverable through numbered controls and assistive technology before text leaves Callsign.

## Dictation correction mode

Dictation corrections operate on Callsign's visible review surface before text is copied, pasted, inserted, or otherwise sent onward.

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

Browser commands use visible browser surfaces and standard browser shortcuts. Callsign does not inspect page contents or run hidden browser scripts in this parity slice. The Browser tab keeps a persistent accessible safety line that states the same boundary in the UI: browser targets are web-only, non-web schemes for files, scripts, settings, installers, and apps are blocked in browser mode, and browser commands use visible shortcuts without hidden page inspection.

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
- `start scrolling down`
- `browser start scrolling left`
- `stop scrolling`
- `browser scroll left`
- `browser scroll right`
- `scroll to top`
- `go to top of page`
- `scroll to bottom`
- `go to bottom of page`
- `browser zoom in`
- `browser zoom reset`

For address-bar text, Callsign focuses the visible browser address bar, resolves bare domains and ordinary search text through the same web-only validation as direct browser opens, types the resulting `http`/`https` target, and presses Enter. The visible Browser tab exposes a dedicated address-bar text field plus a button with spoken phrases such as `type in address bar example dot com` and `search address bar for callsign` so this route is discoverable without leaving the Browser surface. It does not execute scripts, local file paths, settings URIs, or app/installer schemes through the browser path.

For page search text, Callsign opens the visible browser Find field and types the requested search term. The visible Browser tab exposes a dedicated page-find text field plus a button with spoken phrases such as `search this page for privacy policy` and `find privacy policy on this page` so the text-search path is visible and bounded to the active browser window.

Browser scrolling stays in the visible active page. Bounded phrases such as `browser scroll down` and `page down in browser` step the page once, while `start scrolling down` and `browser start scrolling left` begin continuous page movement until the user says `stop scrolling`. Plain `stop` remains reserved for Callsign's session-safety cancel path, so browser scrolling uses the explicit stop-scrolling phrase rather than taking over the global stop command.

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

These commands are meant for visible desktop organization. Close-window phrases send the normal visible close request to the active app or window; they do not force-kill hidden processes. Window layout commands do not target hidden or minimized windows, and the System tab shows the requested action after execution. App-switching, show-desktop, Task View, and snap controls expose accessible names and descriptions with the matching spoken phrases so visible-control numbering and assistive technology can discover the same command surface.

## Task View mode

Task View commands make window switching visible instead of silently selecting a hidden target.

Supported phrases:

- `show open windows`
- `show all windows`
- `task view`
- `window switcher`
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

Keyboard commands are part of the safe Voice Access parity path for controlling the active visible window. The visible keyboard overlay includes a compact close button plus function-key, letter, number-row, modifier, held-modifier, release-all-modifiers, Enter, Backspace, Space, navigation, and arrow-key cues so discoverability matches the routed command surface and the user can see the safety release path. Its persistent safety strip says keypresses target the visible foreground app only and that `release all modifiers` clears held Shift, Control, or Alt. The visible System tab also exposes spoken labels for Task Manager, virtual desktop switching, Windows key, context-menu key, Caps Lock, Home, End, Page Up, and Page Down so those non-text controls remain discoverable through numbered controls and assistive technology.

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

The overlay/readout stays active while the command is routed, and the System tab records the requested keypress or shortcut. Visible System tab keypress controls expose accessible spoken labels for Enter, Tab, Escape, Backspace, Space, Delete, Insert, and arrow keys. Function keys are limited to F1 through F12 in this slice. Callsign also supports Voice Access-style repeated single-key presses such as `press tab five times` and `press down three times` for the same safe key set, keeping the action visible and bounded in the foreground app. `dismiss` maps to a visible Escape keypress for menus, flyouts, and transient surfaces. Held modifier commands are limited to Shift, Control, and Alt, and `release all modifiers` is the visible safety release command; Callsign does not support holding the Windows key or arbitrary keys. Editing, document, and zoom shortcuts are exact natural phrases so commands like `copy` do not swallow unrelated longer utterances. Visible System tab editing/document/zoom controls expose accessible spoken labels for copy, paste, cut, select all, save, undo, redo, formatting, find, new document/window, open file, print, zoom, and close-window commands. Active-app character commands target only the visible foreground app: previous/next character maps to left/right arrow, selection maps to Shift+left/right, and deletion maps to Backspace/Delete. Active-app line commands map to Home/End, Up/Down, Shift+Home/End, Shift+Up/Down, and bounded select-then-delete sequences for line deletion. Character and line controls are visible in the System tab with matching accessible spoken labels for movement, selection, and deletion. Active-app word, sentence, and paragraph commands route before generic visible-control label matching so phrases such as `select previous word` and `select previous paragraph` edit text instead of activating a numbered control by label; their visible System tab controls expose matching accessible spoken labels for movement, selection, and deletion. Paragraph boundary commands use bounded Alt+Up/Down or Alt+Shift+Up/Down style actions plus explicit delete where requested. `print` opens the visible print dialog with `Ctrl+P`; it does not silently confirm printing. Plain `zoom in`, `zoom out`, and `reset zoom` target the active visible app. Browser-specific zoom remains available through `browser zoom in`, `browser zoom out`, and `browser zoom reset`. 

The keyboard overlay form, header, close button, cue, and safety strip expose accessible names and descriptions for the spoken keyboard-command affordance and foreground-targeting boundary so the visible on-screen keyboard remains available to assistive technology. 

Modifier chords are an allowlisted safe subset and currently include tab/home/end, Shift+tab, Shift+letter chords, Shift+digit chords, Ctrl+tab, Ctrl+Shift+Tab, Alt+Shift+Tab, Control+Shift+letter chords, Control+Shift+digit chords, Alt+arrows, Alt+letter access-key chords, Alt+digit access-key chords, Control+Home/End, Control+Shift+Home/End, Control+letter chords, Control+digit chords, and common reversible Control-key shortcuts for copy, paste, cut, select all, save, undo, redo, find, formatting, document, open-file, print, navigation, refresh, close-tab/window, tab selection, and zoom control. The list is intentionally constrained to single keypress chords or a single explicitly held modifier, stays visible in command discovery, and does not become arbitrary multi-step macro execution.

## Update splash mode

The update splash is a visible macOS-style surface that appears when the latest manifest includes changes. It reads the manifest summary out loud, shows the added/changed/removed command counts, and lists the updated commands or extension packs in the details area before closing itself automatically. A persistent voice cue names `close update splash`, `dismiss update splash`, `hide update splash`, and `cancel update splash`, and reminds the user that reviewing update details does not enable gated commands because policy and entitlement still decide what can run. The splash surface, panel, title, published-time label, summary, voice cue, and details list expose accessible names and descriptions so newly added commands and extension-pack changes are discoverable through assistive technology. Enter and Escape dismiss the splash the same way the visible close button does, and dismissing the splash never changes installed commands.
The close control uses a compact icon-style glyph instead of a text label so the top-right corner stays visually quiet.

## v1.x voice modes

Alpha v1 voice modes now ship as the current free parity surface:

- v1.1: dictation with visible review before text actions
- v1.2: browser control with visible navigation and approval boundaries
- v1.3: system control, WSL/Linux workflows, and file search with visible results

## Response style

Keep active-session responses short.

Good:

- `Identity confirmed. Say the app name.`
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
