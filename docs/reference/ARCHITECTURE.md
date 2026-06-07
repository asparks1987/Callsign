# Architecture

## Overview

DeskPilot is split into a voice/model host and a Windows Automation MCP server.

The split matters because MCP is a tool/resource/prompt interface, not the entire agent. The host owns conversation, voice, model provider choice, task planning, and user-facing approval UX. The MCP server owns Windows-specific capabilities, tool validation, policy checks, execution, and audit logging.

## Component diagram

```text
┌─────────────────────────────────────────────────────────────────────┐
│                         Voice / Agent Host                          │
│                                                                     │
│  ┌──────────────┐   ┌───────────────┐   ┌───────────────────────┐   │
│  │ Voice Input  │ → │ Transcription │ → │ Conversation Manager  │   │
│  └──────────────┘   └───────────────┘   └──────────┬────────────┘   │
│                                                     │                │
│  ┌──────────────┐   ┌───────────────┐   ┌──────────▼────────────┐   │
│  │ Voice Output │ ← │ Response Synth│ ← │ Planner / Tool Router │   │
│  └──────────────┘   └───────────────┘   └──────────┬────────────┘   │
│                                                     │                │
│                                             ┌───────▼────────┐       │
│                                             │ MCP Client     │       │
│                                             └───────┬────────┘       │
└─────────────────────────────────────────────────────┼────────────────┘
                                                      │ stdio
                                                      ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    Windows Automation MCP Server                     │
│                                                                     │
│  ┌──────────────┐   ┌──────────────┐   ┌────────────────────────┐   │
│  │ MCP Protocol │ → │ Tool Router  │ → │ Policy Engine          │   │
│  └──────────────┘   └──────────────┘   └───────────┬────────────┘   │
│                                                     │                │
│                                             ┌───────▼────────┐       │
│                                             │ Execution Core │       │
│                                             └───────┬────────┘       │
│                                                     │                │
│           ┌─────────────────────────┬───────────────┼──────────────┐ │
│           ▼                         ▼               ▼              ▼ │
│  UI Automation Adapter      Win32 Adapter     File Adapter   Audit Log│
└─────────────────────────────────────────────────────────────────────┘
```

## Runtime loop

DeskPilot uses an observe-plan-act-verify loop.

```text
1. User gives a voice or text instruction.
2. Host transcribes or receives text.
3. Host asks the model to produce a bounded plan.
4. Host routes read-only observations to the MCP server.
5. MCP server returns structured desktop context.
6. Model proposes typed tool calls.
7. MCP server validates arguments.
8. Policy engine allows, denies, or requires approval.
9. If needed, host asks the user for approval.
10. MCP server executes the approved action.
11. MCP server verifies the outcome where possible.
12. Audit log records the full chain.
13. Host summarizes the result.
```

## Process boundaries

### Host process

Responsibilities:

- Voice session.
- Model provider abstraction.
- Conversation state.
- Approval UX.
- MCP client connection.
- Task-level planning.
- User-facing explanations.

The host should not contain direct Windows automation code.

### MCP server process

Responsibilities:

- Tool registration.
- Resource registration.
- Prompt registration.
- Argument validation.
- Policy enforcement.
- Windows automation adapters.
- File allowlist enforcement.
- Audit log.
- Safe error handling.

The server should not choose high-level task intent. It should execute validated, policy-approved capabilities.

## Transport

Default transport: local stdio.

Reasons:

- No open local port by default.
- Natural fit for local MCP clients.
- Simple process lifetime management.
- Lower attack surface than an unauthenticated local HTTP server.

Future transport options:

- Authenticated local HTTP for tray app integration.
- Named pipe transport.
- Remote bridge only through a separate authenticated component.

## Tool categories

### Observe tools

Read local desktop state.

Examples:

- `server.info`
- `desktop.get_active_window`
- `desktop.list_windows`
- `desktop.inspect_window`
- `desktop.find_element`

### Act tools

Change local state or interact with UI.

Examples:

- `desktop.invoke_element`
- `desktop.set_text`
- `desktop.focus_element`
- `desktop.send_hotkey`
- `file.rename`

### Recovery tools

Stop, undo, cancel, or hand off.

Examples:

- `workflow.pause`
- `workflow.stop`
- `desktop.escape`
- `desktop.undo_last_input`
- `workflow.handoff`

### Policy tools

Expose approval and policy outcomes.

Examples:

- `policy.evaluate`
- `policy.request_approval`

## Resources

Resources provide passive context to hosts and clients.

Recommended resource URIs:

```text
windows://active
windows://windows
windows://window/{hwnd}/tree
windows://window/{hwnd}/text
windows://processes
audit://session/{session_id}
config://policy
config://selectors
```

## Prompts

Prompts provide reusable task templates.

Recommended prompts:

```text
perform_desktop_task
inspect_before_acting
teach_new_task
recover_from_failed_action
explain_last_action
create_selector_recipe
```

## Automation adapters

### UI Automation adapter

Primary adapter for semantic desktop interaction.

Responsibilities:

- Enumerate top-level windows.
- Inspect UIA tree.
- Extract properties.
- Identify supported patterns.
- Invoke controls.
- Set values.
- Read text.
- Scroll when safe.

### Win32 adapter

Fallback adapter.

Responsibilities:

- Get foreground window.
- Activate windows.
- Send approved hotkeys.
- Type text when semantic text setting fails.
- Click coordinates only after selector/vision resolution and approval.

### File adapter

Native deterministic file operations.

Responsibilities:

- List files inside approved roots.
- Rename files.
- Move files.
- Copy files.
- Verify existence.
- Prevent path traversal.

### Vision adapter

Future adapter for screenshots, OCR, and visual target finding.

This adapter should be disabled by default for cloud providers because screenshots may contain sensitive information.

## State model

The MCP server should maintain minimal in-memory state:

- Current session ID.
- Correlation IDs.
- Recent tool call history.
- Pending approvals.
- Cached UI tree snapshots with short TTL.
- Selector cache.

Durable state:

- Audit JSONL.
- Selector repository.
- Policy config.
- Recipes.

## Error handling

Errors should be structured and recoverable when possible.

Example:

```json
{
  "ok": false,
  "error": {
    "code": "POLICY_APPROVAL_REQUIRED",
    "message": "Renaming a file requires user approval.",
    "recoverable": true,
    "approval_request_id": "approval_01"
  }
}
```

## Verification strategy

Every action tool should declare a verification strategy.

Examples:

| Action | Verification |
|---|---|
| `desktop.invoke_element` | State changed, dialog closed, focused element changed, or user-visible confirmation requested |
| `desktop.set_text` | Read back value when possible |
| `file.rename` | Target exists and source no longer exists |
| `desktop.send_hotkey` | Active window or UI tree changed as expected |
| `desktop.open_app` | Process/window appears within timeout |

## Extension points

- Model providers.
- Voice providers.
- App-specific adapters.
- Browser DOM adapters.
- Policy packs.
- Selector packs.
- Recipes.
- Test fixtures.

## Architectural risks

| Risk | Mitigation |
|---|---|
| UIA tree too large | Bounded extraction and summarization |
| Model proposes unsafe tool call | Policy engine outside model |
| App exposes poor accessibility data | Fallback adapter and human handoff |
| Screenshots leak secrets | Disabled by default, redaction hooks |
| SendInput fails on elevated windows | Detect integrity issues and hand off |
| Prompt injection through UI text | Treat UI text as untrusted data |
