# Windows Automation Strategy

## Goal

Callsign should perform Windows 11 desktop tasks through the safest, most semantic execution layer available.

The hierarchy is:

1. Native app/API operation.
2. Windows UI Automation pattern.
3. Saved selector.
4. Vision/OCR-assisted target.
5. Raw keyboard/mouse simulation with SendInput.
6. Human handoff.

## Why UI Automation first

Microsoft UI Automation exposes programmatic information about user interface elements and lets automation clients manipulate controls through control patterns. Control patterns represent capabilities such as invoking a button, setting a value, scrolling, toggling, selecting, or reading text.

That makes UIA far better than blind coordinate-clicking for an agent that needs to understand and operate UI safely.

## UI Automation adapter

### Responsibilities

- Enumerate top-level windows.
- Get active window metadata.
- Inspect bounded UIA trees.
- Extract names, automation IDs, control types, bounding rectangles, enabled state, focus state, and supported patterns.
- Find elements by semantic selectors.
- Invoke controls through `InvokePattern` where available.
- Set text through `ValuePattern` or text-focused alternatives.
- Read visible text through supported patterns.
- Detect password or protected fields where possible.

### UIA tree shape

```json
{
  "element_id": "uia:abc123",
  "name": "Save",
  "control_type": "Button",
  "automation_id": "saveButton",
  "class_name": "Button",
  "framework_id": "Win32",
  "is_enabled": true,
  "is_offscreen": false,
  "bounds": { "x": 10, "y": 20, "width": 80, "height": 32 },
  "patterns": ["InvokePattern"],
  "children": []
}
```

### Tree bounds

To avoid flooding the model and leaking too much data, `desktop.inspect_window` should enforce:

- `max_depth`
- `max_nodes`
- `include_text`
- `include_patterns`
- `redaction_mode`
- `visible_only`

### Element IDs

`element_id` should be stable enough for a short interaction but not treated as durable across app launches.

Suggested format:

```text
uia:{session_id}:{window_handle}:{runtime_id_hash}
```

Durable recipes should use selectors, not transient IDs.

## Selector strategy

Selectors are saved descriptions of how to locate elements.

Example:

```yaml
id: notepad.save_as.filename_box
app:
  process_name: notepad.exe
  window_title_pattern: "Save As*"
selector:
  control_type: Edit
  automation_id: "1001"
  name_any:
    - "File name:"
    - "File name"
verification:
  supports_patterns:
    - ValuePattern
```

Selector matching should return confidence and explain why.

```json
{
  "selector_id": "notepad.save_as.filename_box",
  "element_id": "uia:abc123",
  "confidence": 0.91,
  "matched_on": ["process_name", "control_type", "automation_id"],
  "warnings": []
}
```

## Win32 adapter

### Responsibilities

- Get foreground window.
- Activate a window.
- Send approved hotkeys.
- Type text into focused fields as fallback.
- Capture window bounds.
- Detect elevated windows where possible.

### SendInput limitations

`SendInput` can insert keyboard and mouse events into the input stream, but it is subject to Windows integrity boundaries. It may fail when attempting to inject into elevated/admin applications from a lower-integrity process.

Callsign should detect likely integrity mismatch and hand off instead of retrying blindly.

## Raw input rules

Raw input is allowed only when:

- The target is visible.
- The active window matches the expected process/title.
- The target element or intent is verified.
- The hotkey or input action is allowlisted.
- The risk tier allows it.
- The audit log records it.

Raw coordinate clicking additionally requires:

- A semantic or visual target explanation.
- A bounding box.
- Confidence score.
- Verification after the click.

## Screenshot and vision strategy

Screenshots are privacy-sensitive.

Default MVP behavior:

- No screenshots sent to cloud models.
- Active-window screenshot only, if enabled.
- Redaction hooks before storage or model sharing.
- No persistent screenshot storage unless explicitly configured.

Vision should be used for:

- Apps with poor UIA support.
- Icon-only controls.
- Legacy apps.
- Visual verification.

Vision should not be the default path for controls with good UIA metadata.

## App-specific adapters

App-specific adapters can provide more reliable operations than generic UI automation.

Potential adapters:

- File Explorer adapter.
- Browser adapter.
- Office adapter.
- Terminal adapter with strict limits.
- PDF reader adapter.

Adapters should still go through MCP tool schemas, policy, and audit logging.

## Browser strategy

MVP:

- Open browser.
- Navigate visibly.
- Use address bar/search.
- Fill simple visible fields with approval.
- Do not submit external forms without explicit approval.

Future:

- Browser extension.
- Playwright-style DOM adapter.
- Tab/resource inspection.
- Page-level prompt injection defenses.

## Known limitations

| Limitation | Impact | Mitigation |
|---|---|---|
| Poor accessibility metadata | Element finding may fail | Vision fallback or handoff |
| Large UI trees | Model context bloat | Bounded tree summaries |
| Elevated windows | Input may fail | Detect and hand off |
| Dynamic web apps | Selectors unstable | Browser adapter later |
| Multiple monitors/scaling | Coordinates unreliable | Avoid coordinates |
| Modal dialogs | State changes unexpectedly | Wait/verify active window |

## Implementation notes

- Keep automation adapter code separate from protocol code.
- Cache UI tree snapshots with short TTL only.
- Re-resolve elements before action.
- Never trust stale element IDs.
- Normalize and redact window titles where needed.
- Prefer allowlists over blocklists for high-risk input actions.
