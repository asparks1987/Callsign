# AGENTS.md

## Mission

Build DeskPilot: a local-first Windows 11 MCP server and companion host that lets a user control desktop applications through voice and/or text using an LLM, while preserving user consent, privacy, reversibility, and auditability.

The project is not a general-purpose malware-like automation framework. It is an accessibility-oriented, user-visible, consent-driven desktop assistant.

## Core architecture

DeskPilot is split into two primary layers.

### 1. Voice / Agent Host

The host handles:

- Microphone input.
- Speech-to-text.
- Text-to-speech.
- Realtime voice sessions.
- LLM provider routing.
- Task planning.
- MCP client connections.
- User-facing approval prompts.
- Session state.

The host does **not** directly manipulate the Windows desktop.

### 2. Windows Automation MCP Server

The MCP server handles:

- Local Windows automation tools.
- MCP resources for active desktop context.
- MCP prompts for repeatable workflows.
- UI Automation inspection and execution.
- Win32/SendInput fallback execution.
- Policy enforcement.
- Audit logging.
- Local configuration.

The MCP server runs locally by default over stdio.

## Non-negotiable safety rules

- Do not add arbitrary shell execution.
- Do not enter passwords, 2FA codes, payment details, crypto wallet data, or secrets.
- Do not perform purchases, money movement, account deletion, software installation, or security setting changes in MVP.
- Do not send emails, messages, uploads, or external submissions without explicit user approval.
- Do not execute hidden or minimized-window actions unless the user explicitly approved that mode.
- Do not rely on coordinates when a semantic UI Automation path is available.
- Do not bypass the policy engine.
- Do not suppress audit logging.
- Do not store screenshot contents or clipboard contents unless explicitly configured.
- Do not send screenshots, UI trees, clipboard contents, or file contents to cloud models unless the user explicitly opted in.
- Do not design stealth, persistence, evasion, credential theft, or exfiltration capabilities.

## Automation priority order

Prefer action methods in this order:

1. Native app/API operation.
2. Windows UI Automation pattern.
3. Saved selector.
4. Vision/OCR-assisted target.
5. SendInput keyboard/mouse fallback.
6. Human handoff.

Raw coordinate clicking is the last resort and must include verification.

## Tool design rules

Every MCP tool must have:

- A clear name using `domain.verb_object` style.
- A JSON-schema-compatible input shape.
- A typed result.
- A risk tier.
- A reversibility flag.
- A privacy-impact flag.
- Argument validation before execution.
- Structured errors.
- Audit logging.
- Policy evaluation.

Example risk tiers:

- `observe`
- `local_reversible`
- `local_state_change`
- `external_side_effect`
- `dangerous_or_blocked`

## Result format rules

Tool results should be compact, structured, and model-friendly.

Use this shape unless a tool has a strong reason not to:

```json
{
  "ok": true,
  "tool": "desktop.get_active_window",
  "correlation_id": "task_123.step_001",
  "data": {},
  "warnings": [],
  "verification": {
    "performed": true,
    "method": "state_check",
    "summary": "Active window title matched expected process."
  }
}
```

Errors should be explicit:

```json
{
  "ok": false,
  "tool": "desktop.invoke_element",
  "correlation_id": "task_123.step_004",
  "error": {
    "code": "ELEMENT_NOT_FOUND",
    "message": "No visible button named Save was found in the active window.",
    "recoverable": true,
    "suggested_next_tools": ["desktop.inspect_window", "workflow.pause"]
  }
}
```

## Coding standards

- Use typed contracts for all tool inputs and outputs.
- Keep UI Automation logic isolated from MCP protocol code.
- Keep policy logic independent from the LLM.
- Prefer deterministic functions.
- Avoid global mutable state except for session registries and explicitly managed caches.
- Log diagnostics to stderr for stdio MCP servers; do not write diagnostics to stdout.
- Write audit events to JSONL.
- Include correlation IDs for multi-step tasks.
- Use bounded UI tree extraction to avoid flooding the model with irrelevant nodes.
- Normalize Windows paths before policy checks.
- Treat process name, window title, UI text, screenshots, clipboard, and file paths as potentially sensitive.

## Testing requirements

Each new tool requires:

- Unit tests for input validation.
- Policy tests for allow/deny/approval behavior.
- At least one integration test or mocked automation test.
- Failure-mode tests.
- Audit-log tests.

High-risk tools require explicit negative tests proving blocked behavior.

## Documentation requirements

When adding or changing a tool, update:

- `docs/reference/MCP_TOOLS.md`
- `docs/reference/SECURITY_MODEL.md` if risk behavior changes
- `docs/reference/WINDOWS_AUTOMATION.md` if automation strategy changes
- `docs/reference/TEST_PLAN.md` if test coverage changes
- The GitHub Pages HTML by running `python scripts/build_site.py`

## Default development posture

Assume the model may misunderstand the UI.

Assume the UI may change.

Assume tool descriptions can be prompt-injected.

Assume screenshots and clipboard data may contain secrets.

Assume users need a visible, understandable way to stop the agent.

Build accordingly.
