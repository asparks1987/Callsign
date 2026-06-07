# Product Specification

## Product name

DeskPilot

## One-line description

DeskPilot is a local-first Windows 11 voice agent runtime that lets a user operate desktop applications through a voice/model host and a safety-gated Windows MCP automation server.

## Product thesis

A useful voice desktop agent should not pretend the screen is just pixels. It should expose the local machine as typed capabilities: inspect windows, find UI elements, invoke controls, set text, move files, request approval, and verify outcomes.

The product should feel like an assistant that can use the computer with you, not like a remote-control cursor guessing where to click.

## Target users

### 1. Accessibility-first users

Users who want hands-free or reduced-input control of Windows apps.

Needs:

- Dictation and command control.
- Reliable corrections.
- Clear confirmations.
- Ability to stop immediately.
- Workflows that reduce repetitive strain.

### 2. Power users

Users who want to automate repetitive desktop work without writing full scripts.

Needs:

- Natural-language task execution.
- Reusable recipes.
- Transparent logs.
- App-specific selectors.
- Local model options.

### 3. Developers and AI builders

Users who want an MCP-based Windows automation layer they can connect to different AI hosts.

Needs:

- Clean MCP tools.
- Strict schemas.
- Local stdio transport.
- Testable interfaces.
- Safety boundaries.

### 4. Small-business operators

Users who repeatedly process invoices, documents, downloads, forms, and email drafts.

Needs:

- File organization.
- Document extraction handoff.
- Approval before external side effects.
- Repeatable workflows.

## Core use cases

### Use case: operate a visible desktop app

User says:

> Click the Save button in this dialog.

DeskPilot should:

1. Inspect the active window.
2. Find a visible button named `Save`.
3. Check that invoking it is allowed.
4. Invoke it through UI Automation when possible.
5. Verify the dialog closed or file state changed.
6. Record the action.

### Use case: dictate and edit text

User says:

> Put this in the email body: Thanks for the update. I’ll review it this afternoon.

DeskPilot should:

1. Confirm the active target is an editable field.
2. Insert text through a semantic value/text pattern if available.
3. Fall back to typing only if needed.
4. Avoid sending the email unless explicitly approved.

### Use case: rename and move local files

User says:

> Rename the newest screenshot in Downloads to router-settings.png.

DeskPilot should:

1. List candidate files in the approved folder.
2. Identify the newest matching image.
3. Request approval because this is a local state change.
4. Rename the file.
5. Verify the target exists.

### Use case: teach a workflow

User says:

> Watch me process this invoice and make it repeatable.

DeskPilot should eventually:

1. Enter teach mode.
2. Observe user actions and UI selectors.
3. Capture semantic steps.
4. Ask the user to name the workflow.
5. Save a recipe with permissions and verification.

## MVP scope

The MVP should include:

- Text-command host before voice is required.
- Local Windows MCP server over stdio.
- Server info and health tools.
- Active window detection.
- Visible window listing.
- Bounded UI Automation tree inspection.
- Element finding by name, automation ID, control type, and selector hints.
- UIA invocation for buttons/menu items where available.
- Text setting for editable controls where available.
- Approved hotkey fallback.
- File listing, rename, move, and copy inside allowlisted roots.
- Policy engine v0.
- Approval prompt mechanism.
- JSONL audit log.
- Notepad demo.
- Calculator demo.
- File rename demo.

## Explicit MVP non-goals

The MVP should not include:

- Arbitrary shell execution.
- Autonomous browser purchasing.
- Password or 2FA handling.
- UAC/admin automation.
- Background remote control.
- Permanent deletion.
- Silent email or message sending.
- Cloud screenshot upload by default.
- Full browser DOM automation.
- Unattended long-running workflows.

## Product principles

### 1. The user remains in command

DeskPilot acts only in response to user intent. Risky actions require visible approval.

### 2. The model is not trusted with authorization

The LLM may propose a plan, but the policy engine makes allow/deny/approval decisions.

### 3. Use semantic automation before raw input

UI Automation and native APIs should be preferred over mouse coordinates and synthetic keystrokes.

### 4. Verify after acting

Every action should define a reasonable verification step.

### 5. Everything important is auditable

The system should log observations, proposed plans, policy decisions, approvals, tool calls, results, and verification status.

### 6. Privacy is local by default

Screenshots, clipboard contents, UI trees, file contents, and logs should remain local unless the user explicitly opts in to sending them to a cloud model.

## Success metrics

### Reliability

- 90%+ success rate for scripted Notepad, Calculator, and File Explorer demos.
- 95%+ correct policy classification on test fixtures.
- Less than 5% false invocation rate in visible UI tests.

### Safety

- 100% block rate for blocked Tier 4 actions in automated safety tests.
- 100% approval requirement for external side effects.
- Audit log coverage for every action tool.

### UX

- User can stop active automation with a hotkey or voice command.
- User can understand what the agent is about to do before approval.
- Errors include useful recovery suggestions.

## Personas and sample commands

### Accessibility user

> Open Notepad and write a grocery list.

> Read the buttons in this dialog.

> Click Cancel.

> Stop now.

### Power user

> Rename all selected files using the date in their filenames, but ask before changing them.

> Move the newest PDF from Downloads into my Invoices folder.

> Watch how I process this file and turn it into a recipe.

### Developer

> Inspect the active window and show me the UIA tree.

> Find all buttons in the Calculator window.

> Test whether the Save button supports InvokePattern.

## Future features

- Browser extension or Playwright adapter.
- Office app adapters.
- OCR and visual grounding.
- Workflow teach mode.
- Recipe marketplace or local recipe library.
- Tray app.
- Local wake word.
- Multimodal planner.
- Per-app permission profiles.
- Sandboxed testing environment.

## Open product questions

- Should voice be built into the host or pluggable through providers?
- Should screenshot sharing with cloud models be opt-in per session or per task?
- How much UI text should be sent to the planner by default?
- Should recipe replay be allowed unattended, or always visible?
- How should DeskPilot expose a human-readable “why I need this permission” explanation?
