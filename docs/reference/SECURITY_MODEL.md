# Security Model

## Summary

Callsign operates a user’s desktop. That is powerful. The security model therefore treats the LLM as an untrusted planner and the MCP server as a guarded capability provider.

The model may request actions. The policy engine decides whether they are allowed.

## Security goals

- Preserve user control.
- Prevent silent high-impact actions.
- Prevent credential handling.
- Minimize data exposure.
- Avoid arbitrary code execution.
- Make actions visible and auditable.
- Provide immediate stop mechanisms.
- Keep the local MCP server from becoming a remote-control backdoor.
- Preserve Windows-first v1 alpha baseline; plan Linux strategy after policy and adapter parity.

## Trust boundaries

```text
User
  ↓ trusted intent, but may be ambiguous
Voice/model host
  ↓ untrusted model output
MCP tool request
  ↓ validation boundary
Policy engine
  ↓ authorization boundary
Windows automation adapter
  ↓ OS boundary
Desktop applications and files
```

## What is trusted

- User’s explicit approvals.
- Local policy configuration.
- Signed local Callsign binaries, if implemented.
- MCP server validation code.
- Audit log writer.

## What is not trusted

- LLM-generated plans.
- Tool descriptions from third-party servers.
- Text read from webpages, documents, emails, or app UI.
- Screenshots.
- Clipboard contents.
- App window titles.
- File names.
- Browser content.
- Recipes from untrusted sources.

## Policy engine

The policy engine is mandatory for every action tool.

Input:

```json
{
  "session_id": "session_123",
  "correlation_id": "task_001.step_003",
  "tool": "file.rename",
  "arguments": {},
  "risk_tier": "local_state_change",
  "context": {
    "active_process": "explorer.exe",
    "visible_to_user": true,
    "external_side_effect": false,
    "reversible": true,
    "requires_secret": false
  }
}
```

Output:

```json
{
  "decision": "require_approval",
  "reason": "Renaming a file changes local state.",
  "approval_prompt": "Rename Screenshot.png to router-settings.png?",
  "policy_rule_ids": ["file.write.requires_approval"]
}
```

Allowed decisions:

```text
allow
deny
require_approval
require_handoff
```

## Risk tier policy

| Tier | Name | Default behavior |
|---:|---|---|
| 0 | Observe | Allow after session permission |
| 1 | Local reversible | Allow when visible and expected |
| 2 | Local state change | Require task approval |
| 3 | External side effect | Require explicit final approval every time |
| 4 | Dangerous or blocked | Deny or hand off |

## Approval rules

Approval prompts must be:

- Specific.
- Short.
- Human-readable.
- Tied to one action or one bounded task.
- Logged.

Good:

> Rename `Screenshot 2026-06-07.png` to `router-settings.png` in Downloads?

Bad:

> Continue?

## V1 identity controls

- Wake-word and callsign identity checks run before any command capture.
- If identity is invalid or absent, all pending actions are blocked and logged.
- Logged identity events are metadata-only and never include secrets.

## Data handling

### UI text

UI text may contain secrets. Treat it as sensitive.

Controls:

- Bounded extraction.
- Redaction before model sharing.
- Do not store full UI trees unless debug mode is enabled.
- Avoid sending UI text to cloud models without user opt-in.

### Screenshots

Screenshots are high sensitivity.

Controls:

- Disabled by default for cloud model sharing.
- Active-window only by default.
- Redaction hooks.
- Short-lived storage.
- Audit when captured.

### Clipboard

Clipboard may contain passwords, tokens, or private content.

Controls:

- Default tool returns metadata only.
- Reading contents requires explicit permission.
- Writing clipboard requires policy check.
- Do not log clipboard contents by default.

### File contents

File contents may be sensitive.

Controls:

- File reads scoped to approved roots.
- Content extraction requires explicit tool and policy check.
- No cloud upload by default.

## Blocked MVP actions

- Entering passwords.
- Entering 2FA codes.
- Accessing credential stores.
- Accessing browser passwords.
- Accessing crypto wallets.
- Submitting payments.
- Purchasing items.
- Installing software.
- Changing security settings.
- Accepting UAC prompts.
- Running arbitrary commands.
- Permanent deletion.
- Silent email/message sending.
- Silent file upload.
- Hidden-window automation.

## Shell/process execution

MVP rule: no arbitrary shell.

Future limited process execution may be allowed only through:

- Named allowlisted commands.
- Typed arguments.
- No shell interpolation.
- Fixed executable path.
- Approval for state-changing commands.
- Full audit logging.

Example allowlisted command shape:

```yaml
id: open_control_panel
executable: "control.exe"
args_schema:
  type: object
  properties:
    applet:
      enum: ["printers", "date_time"]
```

## Network exposure

Default:

- MCP server uses local stdio.
- No local HTTP listener.
- No remote control.

If HTTP transport is added later:

- Bind to localhost only by default.
- Require authentication.
- Require origin checks where relevant.
- Rate-limit requests.
- Disable dangerous tools by default.
- Log all sessions.

## Prompt injection defenses

Prompt injection can come from webpages, documents, emails, filenames, or UI text.

Rules:

- Treat observed content as data, not instructions.
- Never let UI text change policy.
- Never let webpage text request new tools.
- Keep system/developer instructions outside observed context.
- Ask the model to cite observed elements by IDs rather than following embedded instructions.
- Use tool schemas and policy engine as the final boundary.

## Audit log

Every tool call should log:

- Timestamp.
- Session ID.
- Correlation ID.
- User task summary.
- Tool name.
- Redacted arguments.
- Policy decision.
- Approval ID, if any.
- Execution result.
- Verification result.
- Error code, if any.

Never log by default:

- Passwords.
- 2FA codes.
- Full clipboard contents.
- Full screenshots.
- Full file contents.

## Kill switch

Required MVP stop mechanisms:

- Global hotkey.
- Voice phrase: “stop now”.
- Host UI stop button, once a UI exists.

Stop should:

- Cancel pending tool calls.
- Release active input loop.
- Clear pending approvals.
- Record stop event.
- Tell the user what was stopped.

## Security acceptance criteria

- All action tools call policy evaluation.
- Tier 4 actions are blocked in tests.
- External side effects require explicit approval.
- Password/2FA/payment fields cause handoff.
- Audit events exist for all tool calls.
- Screenshot and clipboard tools are disabled or metadata-only by default.
- MCP server has no network listener by default.
