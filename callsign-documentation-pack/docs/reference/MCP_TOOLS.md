# MCP Tool Contract

## Status

Target architecture. The tool catalog is not evidence that these tools currently ship.

## Boundary

The local automation server exposes typed capabilities to an approved host over stdio. The model proposes calls; the server validates inputs, evaluates policy, executes through a bounded adapter, verifies the result, and records a redacted audit event.

## Naming

Use `domain.verb_object`.

Examples:

```text
server.info
server.ping
desktop.get_active_window
desktop.list_windows
desktop.inspect_window
desktop.find_element
desktop.invoke_element
desktop.set_text
desktop.send_hotkey
file.search_names
file.open_in_explorer
policy.evaluate
workflow.stop
workflow.handoff
```

## Required tool metadata

Every tool definition contains:

- Stable name and semantic version.
- Description that does not imply broader permission.
- JSON Schema input and output.
- Risk tier.
- Privacy classes observed or changed.
- Reversibility.
- Default approval behavior.
- Required session state.
- Timeout and cancellation behavior.
- Audit fields and redaction rule.
- Verification method.
- Platform and adapter support.
- Error codes.
- Examples and blocked examples.

## Invocation pipeline

```text
parse protocol
  -> authenticate/bind local client
  -> validate schema and bounds
  -> check session state
  -> normalize paths/identifiers
  -> classify data and risk
  -> evaluate policy
  -> request approval if required
  -> revalidate current context
  -> execute adapter
  -> verify postcondition
  -> emit result and audit event
```

No stage may be skipped because the caller claims urgency or user consent.

## Standard success envelope

See `schemas/tool-result.schema.json`.

```json
{
  "ok": true,
  "tool": "desktop.get_active_window",
  "tool_version": "0.1.0",
  "correlation_id": "session_01.step_01",
  "data": {},
  "warnings": [],
  "verification": {
    "performed": true,
    "method": "state_check",
    "summary": "Foreground handle matched."
  }
}
```

## Standard error envelope

```json
{
  "ok": false,
  "tool": "desktop.invoke_element",
  "tool_version": "0.1.0",
  "correlation_id": "session_01.step_04",
  "error": {
    "code": "ELEMENT_NOT_FOUND",
    "message": "No visible element matched the selector.",
    "recoverable": true,
    "suggested_next_tools": ["desktop.inspect_window", "workflow.handoff"]
  }
}
```

Errors do not leak raw UI text, full paths, or stack traces to the model by default.

## Initial catalog

### Observe

- `server.info`
- `server.ping`
- `desktop.get_active_window`
- `desktop.list_windows`
- `desktop.inspect_window`
- `desktop.find_element`
- `file.search_names`
- `policy.evaluate`

Observation is still privacy-sensitive and may require session permission.

### Local reversible

- `desktop.focus_window`
- `desktop.invoke_element` for low-risk controls.
- `desktop.set_text` in a verified non-sensitive local field.
- `desktop.send_hotkey` from a fixed allowlist.
- `file.open_in_explorer`
- `workflow.stop`

### Local state change

Candidate later tools:

- `file.rename`
- `file.move`
- application-specific save/update operations

These require explicit policy and verification.

### External side effect

Not part of initial Alpha. Each future tool requires explicit per-action approval and trusted verification.

### Dangerous or blocked

No tools for credentials, arbitrary shell, admin, payment, permanent deletion, security settings, or stealth behavior.

## Hotkeys

Allowed hotkeys are context-specific and centrally configured. `Win+R`, destructive shortcuts, send/submit shortcuts, and arbitrary key sequences are blocked by default.

## Resources and prompts

Resources exposing active desktop context must be bounded, redacted, and access-controlled. Prompts are templates, not policy. Prompt content cannot authorize a tool.

## Compatibility

- Tool schema changes use semantic versioning.
- Breaking changes require an ADR and migration note.
- Host and server negotiate capabilities.
- Unknown tools and fields fail closed.
- The server has a maximum message size and request rate.
- stdout is protocol-only; diagnostics go to stderr.

## New-tool checklist

Use [docs/checklists/NEW_TOOL.md](../checklists/NEW_TOOL.md) and [docs/guides/ADDING_A_TOOL.md](../guides/ADDING_A_TOOL.md).
