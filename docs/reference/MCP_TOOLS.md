# Automation Tool Design

This document describes the future automation surface for Callsign.

The current alpha focuses on the setup UI, voice enrollment, and visible app launch. The tool design below is the next-step automation contract for the background service and later desktop features.

## Naming convention

Use `domain.verb_object`.

Examples:

```text
server.info
desktop.get_active_window
desktop.inspect_window
desktop.find_element
desktop.invoke_element
desktop.set_text
file.rename
policy.evaluate
workflow.stop
```

## Tool metadata

Every tool must include:

- Name.
- Description.
- Input schema.
- Output schema.
- Risk tier.
- Privacy impact.
- Reversibility.
- Required approval behavior.
- Audit requirements.
- Verification behavior.

Command packs use the same metadata shape for voice-exposed actions. At minimum, every command definition must describe risk tier, visibility requirement, reversibility, privacy impact, approval requirement, help/examples, and verification strategy before it can be treated as part of the parity command surface.

## Risk tiers

| Tier | Name | Meaning |
|---:|---|---|
| 0 | `observe` | Reads state only |
| 1 | `local_reversible` | Performs visible local action that is usually reversible |
| 2 | `local_state_change` | Changes local files, documents, or settings |
| 3 | `external_side_effect` | Sends, uploads, submits, posts, or otherwise affects external systems |
| 4 | `dangerous_or_blocked` | Credentials, shell, admin, deletion, payment, security settings |

## Policy decisions

Policy evaluation returns one of:

| Decision | Meaning |
|---|---|
| `allow` | The verified session may run the command. |
| `deny` | The command is denied in the current context. |
| `require_approval` | The user must explicitly approve before execution. |
| `require_fresh_identity` | The user must repeat callsign identity verification. |
| `blocked_dangerous_action` | The command is blocked by Callsign safety policy. |

## Standard result envelope

```json
{
  "ok": true,
  "tool": "desktop.get_active_window",
  "correlation_id": "task_20260607_001.step_001",
  "data": {},
  "warnings": [],
  "verification": {
    "performed": true,
    "method": "state_check",
    "summary": "Verified active window handle."
  }
}
```

## Standard error envelope

```json
{
  "ok": false,
  "tool": "desktop.invoke_element",
  "correlation_id": "task_20260607_001.step_004",
  "error": {
    "code": "ELEMENT_NOT_FOUND",
    "message": "No visible element matched the provided selector.",
    "recoverable": true,
    "suggested_next_tools": ["desktop.inspect_window", "workflow.pause"]
  }
}
```

## Core tools v0.1

### `server.info`

Returns server metadata.

Risk: `observe`

Input:

```json
{}
```

Output data:

```json
{
  "name": "Callsign Automation Service",
  "version": "0.1.0",
  "transport": "stdio",
  "platform": "windows",
  "capabilities": ["desktop", "file", "policy", "audit"]
}
```

### `server.ping`

Health check.

Risk: `observe`

Input:

```json
{
  "message": "optional string"
}
```

Output data:

```json
{
  "message": "pong",
  "server_time": "2026-06-07T00:00:00Z"
}
```

### `desktop.get_active_window`

Returns the active foreground window.

Risk: `observe`

Privacy impact: window title and process name may be sensitive.

Input:

```json
{
  "include_bounds": true
}
```

Output data:

```json
{
  "window_id": "hwnd:0000000000123456",
  "title": "Untitled - Notepad",
  "process_name": "notepad.exe",
  "process_id": 1234,
  "bounds": { "x": 100, "y": 100, "width": 1200, "height": 800 },
  "is_elevated": false
}
```

### `desktop.list_windows`

Lists visible top-level windows.

Risk: `observe`

Input:

```json
{
  "visible_only": true,
  "include_minimized": false,
  "max_results": 50
}
```

Output data:

```json
{
  "windows": [
    {
      "window_id": "hwnd:0001",
      "title": "Calculator",
      "process_name": "CalculatorApp.exe",
      "is_active": true
    }
  ]
}
```

### `desktop.inspect_window`

Returns a bounded UI Automation tree summary for a window.

Risk: `observe`

Privacy impact: high. UI text may contain sensitive information.

Input:

```json
{
  "window_id": "active",
  "max_depth": 5,
  "max_nodes": 250,
  "include_text": true,
  "include_patterns": true,
  "redaction_mode": "standard"
}
```

Output data:

```json
{
  "window": {
    "window_id": "hwnd:0001",
    "title": "Calculator",
    "process_name": "CalculatorApp.exe"
  },
  "tree": {
    "element_id": "uia:root",
    "name": "Calculator",
    "control_type": "Window",
    "automation_id": "CalculatorWindow",
    "patterns": ["WindowPattern"],
    "children": []
  },
  "truncated": false
}
```

### `desktop.find_element`

Finds an element in the active or specified window.

Risk: `observe`

Input:

```json
{
  "window_id": "active",
  "name": "Save",
  "control_type": "Button",
  "automation_id": null,
  "selector": null,
  "visible_only": true,
  "max_results": 10
}
```

Output data:

```json
{
  "matches": [
    {
      "element_id": "uia:abc123",
      "name": "Save",
      "control_type": "Button",
      "automation_id": "saveButton",
      "patterns": ["InvokePattern"],
      "confidence": 0.94
    }
  ]
}
```

### `desktop.invoke_element`

Invokes a UI element through UI Automation when possible.

Risk: `local_reversible` or `local_state_change`, depending on target.

Input:

```json
{
  "element_id": "uia:abc123",
  "expected_name": "Save",
  "expected_control_type": "Button",
  "verification": {
    "type": "window_title_changes_or_dialog_closes",
    "timeout_ms": 5000
  }
}
```

Policy notes:

- Requires the element to still match expected identity.
- Requires visible target unless hidden mode is explicitly approved.
- If target text suggests external submission, deletion, purchase, or security action, escalate risk.

### `desktop.set_text`

Sets text in an editable element.

Risk: `local_reversible` or higher depending on target app/context.

Input:

```json
{
  "element_id": "uia:textbox123",
  "text": "Hello world",
  "mode": "replace",
  "allow_fallback_typing": true,
  "verify_readback": true
}
```

Policy notes:

- Do not set password fields.
- Do not set payment fields.
- Do not set 2FA fields.
- Large text insertion should be confirmed if target is external communication.

### `desktop.send_hotkey`

Sends an approved hotkey to the active window.

Risk: depends on hotkey and active app.

Input:

```json
{
  "keys": ["CTRL", "L"],
  "target_window_id": "active",
  "reason": "Focus address bar",
  "verification": {
    "type": "focus_changed",
    "timeout_ms": 1500
  }
}
```

Allowed MVP hotkeys:

```text
Escape
Enter only when low-risk context is verified
Tab
Shift+Tab
Ctrl+A
Ctrl+C
Ctrl+V with approval if clipboard contents are known
Ctrl+L
Ctrl+S with approval in document contexts
Ctrl+Z
Alt+Tab with approval or explicit request
```

Blocked hotkeys by default:

```text
Alt+F4 unless explicit
Shift+Delete
Win+R
Ctrl+Enter in email/message contexts
Any app-specific send/submit shortcut
```

### `desktop.type_text`

Types text into the currently focused element.

Risk: `local_reversible` or higher.

Input:

```json
{
  "text": "Text to type",
  "target_window_id": "active",
  "expected_focused_element": "uia:textbox123",
  "typing_delay_ms": 0,
  "verify_readback": true
}
```

Policy notes:

- Prefer `desktop.set_text` when possible.
- Block password/2FA/payment fields.
- Require confirmation for external communications.

### `file.list_recent`

Lists recent files inside an approved root.

Risk: `observe`

Input:

```json
{
  "directory": "%USERPROFILE%/Downloads",
  "pattern": "*.png",
  "max_results": 10
}
```

### `file.rename`

Renames a file inside an approved root.

Risk: `local_state_change`

Input:

```json
{
  "source_path": "%USERPROFILE%/Downloads/Screenshot.png",
  "new_name": "router-settings.png",
  "overwrite": false
}
```

Policy notes:

- Source must be inside an approved root.
- New name must not contain path traversal.
- Overwrite is false by default.
- Approval required by default.

### `file.move`

Moves a file between approved roots.

Risk: `local_state_change`

Input:

```json
{
  "source_path": "%USERPROFILE%/Downloads/invoice.pdf",
  "destination_path": "%USERPROFILE%/Documents/Invoices/invoice.pdf",
  "overwrite": false
}
```

### `policy.evaluate`

Evaluates whether a proposed tool call is allowed.

Risk: `observe`

Input:

```json
{
  "tool": "file.rename",
  "arguments": {},
  "task_intent": "Rename newest screenshot",
  "current_context": {
    "active_process": "explorer.exe",
    "visible_to_user": true
  }
}
```

Output data:

```json
{
  "decision": "require_approval",
  "risk_tier": "local_state_change",
  "reason": "Renaming a file changes local state.",
  "approval_prompt": "Rename Screenshot.png to router-settings.png?"
}
```

### `workflow.stop`

Stops the active workflow immediately.

Risk: `local_reversible`

Input:

```json
{
  "reason": "User requested stop"
}
```

### `workflow.handoff`

Tells the host to ask the user to perform a step manually.

Risk: `observe`

Input:

```json
{
  "reason": "Password field detected",
  "message_to_user": "Please enter your password manually. I will continue after you say resume."
}
```

## Tool safety checklist

Before merging a new tool:

- [ ] Has typed input schema.
- [ ] Has typed result schema.
- [ ] Has risk tier.
- [ ] Has privacy classification.
- [ ] Has policy tests.
- [ ] Has audit logging.
- [ ] Has verification behavior.
- [ ] Has examples.
- [ ] Has blocked-case tests.
