# Windows Automation Strategy

## Goal

Callsign automates the desktop only in ways the user can understand and stop.

The v1.0 alpha path is deliberately visible:

1. Service hears `Callsign`.
2. Service verifies the user's callsign.
3. User asks for an installed app.
4. Callsign opens Start search.
5. Callsign types or resolves the app name.
6. Callsign launches the matching app visibly.

All Alpha v1 features are free and remain free until at least beta.

## v1.0 strategy

- Use the Start menu search experience for app launching.
- Resolve installed app names and safe shell-backed destinations.
- Keep the app launch target visible in the UI and service status.
- Let the user cancel or reset the session.
- Reject paths, URLs, shell fragments, secrets, unsafe text, and non-launch automation.
- Avoid arbitrary shell execution.

## Automation priority order

Prefer action methods in this order:

1. Native app/API operation.
2. Windows UI Automation pattern.
3. Saved selector.
4. Vision/OCR-assisted target.
5. SendInput keyboard/mouse fallback.
6. Human handoff.

Raw coordinate clicking is a last resort and must include verification.

The mouse grid is the visible coordinate fallback. It must be shown before any grid-based movement or click, and it should only be used when a semantic UI Automation path is not available.

Pointer nudges, continuous pointer motion, diagonal drag steps, and horizontal scrolling are also available as visible fallbacks. They use bounded relative movement or wheel input, not absolute coordinates.

Supported pointer fallback phrases:

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
- `nudge up`
- `nudge down`
- `scroll left`
- `scroll right`

`move mouse <direction>` starts visible continuous pointer motion, `move mouse <direction> <distance>` performs a bounded fixed-distance move, `move faster` / `move slower` adjust motion speed, and `stop moving` ends continuous motion. Callsign keeps plain `stop` reserved for immediate session cancel/reset safety, so pointer motion requires the more specific stop phrase.

Keyboard fallback also supports repeated single-key presses for the existing safe key set. Examples include `press tab five times`, `press down three times`, and `dismiss` for a visible Escape keypress. Repeated keypress commands remain bounded, foreground-window scoped, and limited to single safe keys rather than arbitrary repeated shortcuts.

## Visible control numbering

`show numbers` now prefers the foreground app's Windows UI Automation tree when the foreground window belongs to another process.

Callsign inspects enabled, on-screen UIA control elements, filters out tiny or unnamed targets, numbers up to 40 controls, and draws badges over their screen bounds. Numbered activation uses UIA patterns in this order:

1. `InvokePattern`
2. `SelectionItemPattern`
3. `TogglePattern`
4. `ExpandCollapsePattern`
5. `SetFocus`

Numbered mouse-style actions such as `double click 1`, `double click save`, `right click 1`, and `right click save` move to the visible center of the numbered or labeled control and perform a bounded double-click or right-click. These actions are visible fallback interactions for controls that need mouse semantics; semantic UIA activation remains preferred for ordinary `click` commands.

If the foreground window belongs to Callsign or UI Automation cannot inspect the app, Callsign falls back to numbering its own visible setup controls. The mouse grid remains available when no semantic UIA target is exposed.

Label matching normalizes common punctuation and symbol phrasing so spoken labels can match controls like `Save & Close`, `Read/Write`, `Don't Save`, or `Search_Box` using ordinary voice phrasing.

## v1.x direction

The repo already contains early services for broader control surfaces:

- browser launch/open/search helpers
- system control helpers
- file search and open/reveal helpers
- command routing for visible UI controls

These are the path toward:

- v1.1 dictation with visible review
- v1.2 browser control
- v1.3 Windows, WSL, Linux, and file search control

They should stay behind identity, policy, approval, visibility, and audit gates.

The Voice Access parity target is tracked in `VOICE_ACCESS_PARITY_MATRIX.md`. That matrix is the acceptance checklist for command families such as visible control numbers, mouse grid, dictation, browser navigation, file search, keyboard commands, app switching, and window management.

## Browser control

Browser control uses visible default-browser or Chrome launch plus standard browser shortcuts. Callsign does not inspect DOM contents or run hidden browser scripts in this parity slice.

Local voice shortcuts are stored as a built-in local pack and can compose existing visible Callsign commands plus bounded wait steps. A shortcut step does not execute raw automation directly. It re-enters the normal Callsign command pipeline, so browser, dictation, file, system, and extension-pack actions still go through identity verification, policy evaluation, visible execution, verification, and audit logging.

Supported browser actions include:

- open/search web target
- type or search a validated web target in the visible address bar
- back and forward
- refresh
- new tab and close tab
- bookmark page and open bookmarks
- save page and print page
- focus address bar
- find in page
- find in page with dictated text, such as `search this page for privacy policy`
- find next and previous
- scroll top/bottom/up/down
- start or stop continuous browser scrolling with explicit direction and stop phrases
- zoom in/out/reset

The address-bar text command focuses the visible browser address bar, validates the dictated target with the same web-only browser rules, types the resulting `http`/`https` URL or search URL, and presses Enter. The dictated page-find command opens the browser Find field visibly and types the search term. Browser page scrolling supports both bounded step commands and explicit continuous actions such as `start scrolling down` and `stop scrolling`, while keeping plain `stop` reserved for Callsign session safety. All of these stay behind wake, identity, policy, status readout, and audit boundaries.

## File search and Explorer reveal

File search remains visible and local. Callsign searches common user folders plus Callsign data, shows results in the Files tab, and lets the user select, open, or reveal numbered results by voice.

Supported visible result actions:

- `select result 1`
- `open file result 1`
- `reveal file result 1`
- `show result folder 1`

Reveal actions open Explorer on the result or containing folder. Direct file open is blocked for executable and script-like extensions; those results must be revealed in Explorer instead.

## Safe Settings surfaces

Safe system settings commands open visible Windows Settings pages through `ms-settings:` URIs. They do not toggle settings, change security posture, install software, or run a shell.

Supported pages:

- `windows settings`
- `open display settings`
- `open sound settings`
- `open bluetooth settings`
- `wifi settings`
- `open network settings`
- `accessibility settings`
- `magnifier settings`
- `narrator settings`
- `captions settings`
- `speech settings`
- `open magnifier`
- `magnifier zoom out`
- `close magnifier`
- `open mouse settings`
- `open keyboard settings`
- `open privacy settings`
- `default apps settings`
- `date and time settings`
- `notifications settings`
- `windows update settings`
- `personalization settings`

These commands remain local, reversible by closing Settings or the visible accessibility surface, and visible to the user. Accessibility subpage commands open Magnifier, Narrator, Captions, and Speech settings without toggling those assistive features. Magnifier uses standard Windows shortcuts to open, zoom out, or close the visible Magnifier surface. Any future command that changes settings must go through policy, risk classification, approval, verification, and audit logging.

## Safe media controls

Media commands use Windows media virtual keys through the same visible local system-control path as volume commands. They do not inspect media content, launch a player, or send external side effects.

Supported phrases:

- `play or pause`
- `play media`
- `pause media`
- `next track`
- `previous song`
- `stop playback`

These commands are local and reversible in normal media-player workflows. They remain behind wake, identity, policy, visible status, and audit.

## Window layout controls

Window layout commands use visible Windows shortcuts for the active foreground window. Callsign does not move hidden windows or target minimized apps.

Supported phrases:

- `snap window left`
- `snap right`
- `move window up`
- `dock window down`
- `show snap layouts`

Snap commands send Windows-key layout shortcuts, and `show snap layouts` opens the Windows 11 Snap Layouts surface. They are local, visible, and reversible through normal window controls.

## Task View and virtual desktops

Task View and virtual desktop commands use standard Windows shortcuts and keep the desktop state visible.

Supported phrases:

- `show open windows`
- `task view`
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
- `snipping toolbar`
- `screen snip`
- `new virtual desktop`
- `next desktop`
- `previous desktop`

These commands do not inspect hidden window contents or move hidden windows. Quick Settings, Notification Center, display projection/cast panels, the emoji/symbol picker, clipboard history, and the snipping toolbar open visible Windows shell surfaces only; Callsign does not toggle network, Bluetooth, focus, notification settings, select a projection mode, connect to a wireless display, capture screenshots, read screenshots, save screenshots, upload screenshots, or inspect clipboard/history contents from those surfaces. Clipboard history and the snipping toolbar are approval-gated because Windows may display or create private clipboard/screenshot content. Closing virtual desktops is intentionally not part of this safe slice because it can move windows unexpectedly.

## Keyboard keypress controls

Keyboard parity commands use `SendInput` only after the user has completed the wake and identity flow. They are intended for visible foreground-window interaction and use the same policy, visible status, and audit path as the rest of the system-control slice.

Supported phrases include:

- `press enter`
- `press tab`
- `press escape`
- `press backspace`
- `press space`
- `press delete`
- `press insert`
- `press home`
- `press end`
- `page up`
- `page down`
- `press f5`
- `function key twelve`
- `press shift a`
- `press shift z`
- `press shift 1`
- `press shift 9`
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
- `press alt shift tab`
- `press alt f`
- `press alt e`
- `press alt h`
- `press alt 1`
- `press alt 9`
- `hold shift`
- `press and hold control key`
- `hold alt`
- `release shift`
- `release control`
- `release alt`
- `release all modifiers`
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
- `go to previous word`
- `go to next word`
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
- `go to previous paragraph`
- `go to next paragraph`
- `select previous paragraph`
- `select next paragraph`
- `delete previous paragraph`
- `delete next paragraph`

Function-key routing supports F1 through F12. Modifier chords are an allowlisted safe subset for visible foreground-window work, including Shift+letter chords, Shift+digit chords, Control+Shift+letter chords, Control+Shift+digit chords, the explicit Alt+Shift+Tab backward app-switching chord, Alt+letter access-key chords, Alt+digit access-key chords, Control+letter chords, Control+digit chords, and common reversible Control-key shortcuts for copy, paste, cut, select all, save, undo, redo, find, formatting, document, open-file, print, navigation, refresh, close-tab/window, tab selection, and zoom control. Active-app character commands are bounded foreground-window actions: `previous character` and `next character` send Left/Right, `select previous character` and `select next character` send Shift+Left/Right, and `delete previous character` and `delete next character` send Backspace/Delete. Active-app line commands stay in the visible foreground app: line start/end send Home/End, previous/next line send Up/Down, line selection sends Shift+Home/End or Shift+Up/Down, and line deletion uses a bounded select-then-delete sequence. Active-app word, sentence, and paragraph commands route before generic visible-control label matching; paragraph boundary commands use bounded Alt+Up/Down or Alt+Shift+Up/Down style actions and explicit delete when requested. Held modifier commands are limited to Shift, Control, and Alt, with `release all modifiers` as the safety release path; holding the Windows key or arbitrary keys is not supported. These commands are single keypress chords or tightly bounded editor actions, not arbitrary macros; they do not type secrets, submit external forms by themselves, or bypass approval for high-risk actions.

## Extension-library direction

Future Pro and Advanced tiers may add closed-source automation libraries.

Those libraries can expand command coverage, but they must not:

- bypass the policy engine,
- suppress audit logs,
- perform hidden actions by default,
- handle credentials or payment data,
- or execute arbitrary shell text as a shortcut.

## Safety rules

- Do not rely on hidden windows.
- Do not use background-only automation for the v1.0 launch path.
- Do not use raw coordinates when a visible semantic path is available.
- Do not bypass wake word, callsign identity, policy, approval, or audit.
- Do not send screenshots, UI trees, clipboard contents, or file contents to cloud models unless the user explicitly opts in.
