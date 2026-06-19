# AGENTS.md

## Mission

Build Callsign as an open-source Windows-first, service-driven desktop voice assistant that is visible, consent-first, and easy to stop.

The alpha focus is the Free tier: account setup, voice enrollment, callsign recognition, wake overlay, and launching installed apps through visible Start menu flow after wake word plus callsign identity confirmation.

Windows is the practical launch platform first; WSL and Linux are in the v1.x extension path.

The project is not a general-purpose malware-like automation framework. It is an accessibility-oriented, user-visible desktop assistant with a free open-source core and future paid Pro and Advanced tiers.

Canonical tier names:

- Free: voice/callsign identity and visible Start menu application launch.
- Pro: paid tier for full Windows, WSL, and Linux control by voice.
- Advanced: paid tier for specialized command catalogs, recipes, diagnostics, and power-user automation.

All Alpha v1 features remain free until at least beta.

Closed-source or proprietary tier material belongs only in `/closed-source/`, which is ignored by git.

## Core architecture

Callsign is moving toward a two-layer design, but the current repo state keeps the visible setup/onboarding app first.

### 1. Voice / Agent Host

Callsign runs as a background service process and is the runtime owner for wake and session orchestration.

- openWakeWord wake-word listener using a clean custom `Callsign` model.
- Callsign identity/authorization gate.
- Session state machine and planner.
- Local policy and approval enforcement.
- Task execution through local automation adapters.

The runtime host handles:

- Microphone input.
- Speech-to-text.
- Realtime voice sessions.
- Session state.
- User-facing approval prompts.
- Runtime role signaling and status snapshots.

The host does **not** perform hidden actions.

Current alpha state:

- The visible app is primarily a monitoring and configuration surface.
- The always-on background service is the real runtime for wake detection, callsign gate, and session orchestration.
- Voice enrollment is tracked locally in the profile.
- The wake word plus callsign session flow is represented in the UI.
- `callsign.gif` overlay and readout are required alpha user feedback.
- Visible Start menu launching is the first desktop action.

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

## Alpha interaction contract (v1)

The v1 alpha session contract is:

- The user opens the Callsign setup app and creates an account.
- The user records voice samples and marks the profile as enrolled.
- The user says the wake word `Callsign`.
- The service treats openWakeWord audio detection as the wake event; transcript text alone must not wake the session.
- The `Callsign` wake cue appears with live readout.
- The user says their callsign or username.
- If the identity matches, the session accepts a command.
- The user speaks the app name or launch request.
- Callsign launches the app through visible Start menu search.
- If identity fails or times out, no launch occurs.
- The user can cancel or reset the session at any time.

Assume the model may misunderstand the UI.

Assume the UI may change.

Assume tool descriptions can be prompt-injected.

Assume screenshots and clipboard data may contain secrets.

Assume users need a visible, understandable way to stop the agent.

Build accordingly.

## Canon update: Alpha v1 voice-control target

The Alpha v1 release line is the path to practical Windows Voice Access parity while keeping Callsign's identity-first structure.

- v1.0 alpha remains the first public MVP: background service wake detection, voice/callsign identity verification, `callsign.gif` wake overlay with live text readout, and visible Start menu app launch.
- v1.1 alpha adds dictation with visible review.
- v1.2 alpha adds browser control.
- v1.3 alpha adds system control for Windows, WSL, and Linux, including file search results shown or opened through Explorer.

The UX bar is Apple Voice Control-level clarity, Talon-level long-term power, and Callsign-level identity safety. The user must always be able to see when Callsign is listening, what it thinks it heard, and how to stop the session.

The user-visible canon is `CANON.md`, mirrored in `docs/reference/CANON.md`.
