# Test Plan

## Goals

The DeskPilot test plan must prove three things:

1. The system can perform useful desktop tasks.
2. The system blocks unsafe actions.
3. The system produces auditable, explainable outcomes.

## Test layers

### Unit tests

Targets:

- Tool input validation.
- Path normalization.
- Policy decisions.
- Redaction.
- Selector matching.
- Result envelopes.
- Audit event formatting.

### Integration tests

Targets:

- MCP server startup over stdio.
- Tool registration.
- Active window detection.
- UIA tree extraction against test apps.
- File operations in temp approved roots.
- Policy + tool execution chain.

### Desktop automation tests

Targets:

- Notepad.
- Calculator.
- File Explorer or temp folder shell.
- Common dialogs.

### Safety tests

Targets:

- Blocked shell.
- Password fields.
- Payment fields.
- Hidden window action.
- External side effects.
- Screenshot sharing without opt-in.
- File operations outside approved roots.

### Golden tests

Use captured or mocked UI trees to test planner/tool behavior deterministically.

## MVP acceptance tests

### Test: server info

Input:

```json
{ "tool": "server.info", "arguments": {} }
```

Expected:

- Returns server name and version.
- Audit event is written.
- No policy approval required.

### Test: active window

Expected:

- Returns foreground window metadata.
- Does not include screenshot.
- Redacts sensitive title if configured.

### Test: inspect Calculator

Steps:

1. Open Calculator manually or through approved app launch.
2. Call `desktop.inspect_window`.

Expected:

- Returns bounded UIA tree.
- Includes buttons with control types.
- Does not exceed max node count.

### Test: invoke Calculator button

Steps:

1. Open Calculator.
2. Find button `7`.
3. Invoke it.

Expected:

- UIA invoke succeeds or fallback is logged.
- Result display updates.
- Verification passes.

### Test: Notepad typing

Steps:

1. Open Notepad.
2. Set or type text.

Expected:

- Text appears in editor.
- No send/save occurs without approval.
- Audit event records redacted text length or content depending config.

### Test: file rename approval

Steps:

1. Create temp approved root.
2. Create file `Screenshot.png`.
3. Request rename.

Expected:

- Policy requires approval.
- Without approval, no rename occurs.
- With approval, target exists.
- Audit links approval to tool call.

## Safety acceptance tests

| Test | Expected |
|---|---|
| `shell.run` requested | Denied |
| File path outside approved root | Denied |
| Rename with `../` in new name | Denied |
| Type into password field | Handoff |
| Submit email without approval | Denied or approval required |
| SendInput to hidden window | Denied |
| Screenshot cloud share while disabled | Denied |
| Stop hotkey during workflow | Workflow stops |

## Policy test fixtures

Example fixture:

```yaml
name: deny_file_outside_root
input:
  tool: file.rename
  arguments:
    source_path: "C:/Users/Alice/Secrets/token.txt"
    new_name: "token-old.txt"
  context:
    visible_to_user: true
expected:
  decision: deny
  reason_contains: approved root
```

## Mock UI tree fixture

```json
{
  "window": {
    "title": "Save As",
    "process_name": "notepad.exe"
  },
  "tree": {
    "name": "Save As",
    "control_type": "Window",
    "children": [
      {
        "name": "File name:",
        "control_type": "Edit",
        "automation_id": "1001",
        "patterns": ["ValuePattern"]
      },
      {
        "name": "Save",
        "control_type": "Button",
        "patterns": ["InvokePattern"]
      }
    ]
  }
}
```

## Manual QA scripts

### Script: first-run safety

1. Start host.
2. Ask it to inspect active window.
3. Ask it to type into Notepad.
4. Ask it to send an email.
5. Ask it to run PowerShell.

Expected:

- Inspection works.
- Typing works in Notepad.
- Email send requires approval.
- PowerShell is denied.

### Script: stop behavior

1. Start a multi-step file workflow.
2. Say “stop now” during execution.

Expected:

- Workflow halts.
- No further actions occur.
- Audit records stop.

## CI considerations

Windows desktop automation tests may require:

- Windows runner.
- Interactive desktop session.
- Test app fixtures.
- Skipping UI tests in headless CI.
- Separate manual test lane for real desktop automation.

## Coverage goals

- 90%+ unit coverage for policy engine.
- 90%+ unit coverage for path normalization.
- 100% audit coverage for action tools.
- 100% negative coverage for blocked MVP tool classes.

## Release gate

A release candidate cannot ship unless:

- All P0 tests pass.
- Safety tests pass.
- Audit logging is verified.
- Docs reflect current tool behavior.
- Threat model is updated for any new tool category.
