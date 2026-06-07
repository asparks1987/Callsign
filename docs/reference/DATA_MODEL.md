# Data Model

## Overview

DeskPilot should store durable state locally and minimally.

Main data types:

- Session.
- Task.
- Tool call.
- Policy decision.
- Approval.
- Audit event.
- Selector.
- Recipe.
- Provider config.

## Session record

```json
{
  "session_id": "session_20260607_001",
  "started_at": "2026-06-07T15:00:00Z",
  "ended_at": null,
  "host_version": "0.1.0",
  "server_version": "0.1.0",
  "model_provider": "ollama",
  "voice_provider": "none",
  "privacy_mode": "local_first"
}
```

## Task record

```json
{
  "task_id": "task_20260607_001",
  "session_id": "session_20260607_001",
  "user_utterance": "Rename the newest PNG in Downloads to router-settings.png",
  "normalized_intent": "rename_recent_file",
  "status": "completed",
  "risk_max": "local_state_change",
  "started_at": "2026-06-07T15:01:00Z",
  "ended_at": "2026-06-07T15:01:07Z"
}
```

## Audit event

Audit logs should be JSONL: one JSON object per line.

```json
{
  "event_id": "evt_001",
  "timestamp": "2026-06-07T15:01:01Z",
  "session_id": "session_20260607_001",
  "task_id": "task_20260607_001",
  "correlation_id": "task_20260607_001.step_001",
  "event_type": "tool_call_requested",
  "tool": "file.list_recent",
  "arguments_redacted": {
    "directory": "%USERPROFILE%/Downloads",
    "pattern": "*.png"
  }
}
```

Event types:

```text
session_started
session_ended
user_utterance
plan_created
tool_call_requested
policy_evaluated
approval_requested
approval_granted
approval_denied
tool_execution_started
tool_execution_finished
verification_finished
workflow_stopped
error
```

## Policy decision

```json
{
  "decision_id": "policy_001",
  "correlation_id": "task_001.step_002",
  "tool": "file.rename",
  "risk_tier": "local_state_change",
  "decision": "require_approval",
  "reason": "File rename changes local state.",
  "rule_ids": ["file.write.requires_approval"]
}
```

## Approval record

```json
{
  "approval_id": "approval_001",
  "task_id": "task_001",
  "requested_at": "2026-06-07T15:01:02Z",
  "resolved_at": "2026-06-07T15:01:05Z",
  "status": "approved",
  "prompt": "Rename Screenshot.png to router-settings.png?",
  "approved_by": "user_voice_confirmation",
  "expires_at": "2026-06-07T15:02:02Z"
}
```

## Selector record

Selectors identify UI elements across sessions.

```yaml
id: notepad.save_as.filename_box
version: 1
app:
  process_name: notepad.exe
  executable_path_hint: null
  window_title_pattern: "Save As*"
selector:
  control_type: Edit
  automation_id: "1001"
  name_any:
    - "File name:"
    - "File name"
  class_name: Edit
verification:
  supports_patterns:
    - ValuePattern
  visible: true
risk_notes:
  default_risk: local_reversible
created_at: "2026-06-07T00:00:00Z"
updated_at: "2026-06-07T00:00:00Z"
```

## Recipe record

Recipes define repeatable workflows.

```yaml
id: process_invoice
version: 1
name: Process invoice
summary: Rename a downloaded invoice and move it into the yearly invoice folder.
permissions:
  read:
    - "%USERPROFILE%/Downloads"
  write:
    - "%USERPROFILE%/Documents/Invoices"
inputs:
  - id: source_file
    type: file
    default_strategy: newest_matching
    pattern: "*.pdf"
steps:
  - id: find_invoice
    tool: file.list_recent
    args:
      directory: "%USERPROFILE%/Downloads"
      pattern: "*.pdf"
      max_results: 5
  - id: request_approval
    tool: policy.request_approval
    args:
      prompt: "Process the newest PDF invoice?"
  - id: move_file
    tool: file.move
    args:
      source_path: "${steps.find_invoice.files[0].path}"
      destination_path: "%USERPROFILE%/Documents/Invoices/${year}/${filename}"
verification:
  - type: file_exists
    path: "%USERPROFILE%/Documents/Invoices/${year}/${filename}"
```

## Provider config

```yaml
model_providers:
  default: ollama-qwen
  providers:
    ollama-qwen:
      type: openai_compatible
      base_url: "http://localhost:11434/v1"
      model: "qwen3.5"
      send_screenshots: false
      send_clipboard: false
    openai-realtime:
      type: openai_realtime
      model: "realtime-model-name"
      send_screenshots: false
      send_ui_text: ask
```

## Policy config

```yaml
version: 1
session:
  require_initial_permission: true
privacy:
  allow_cloud_screenshots: false
  allow_cloud_clipboard: false
  persist_screenshots: false
files:
  approved_roots:
    - "%USERPROFILE%/Downloads"
    - "%USERPROFILE%/Documents"
blocked:
  tools:
    - shell.run
    - credential.read
    - browser.read_passwords
approvals:
  local_state_change: required
  external_side_effect: always
  dangerous_or_blocked: deny
```

## Redaction

Redaction should happen before audit logging and before model sharing.

Common redaction markers:

```text
[REDACTED_EMAIL]
[REDACTED_PHONE]
[REDACTED_SECRET]
[REDACTED_PATH_SEGMENT]
[REDACTED_CLIPBOARD]
[REDACTED_SCREENSHOT]
```

## Storage layout

Suggested local storage:

```text
%LOCALAPPDATA%/DeskPilot/
  config/
    policy.yaml
    providers.yaml
  audit/
    session_20260607_001.jsonl
  selectors/
    notepad.yaml
    explorer.yaml
  recipes/
    process_invoice.yaml
  cache/
    ui_snapshots/
```

## Retention

Default retention:

- Audit logs: keep 30 days.
- UI snapshots: memory only or short TTL.
- Screenshots: do not persist.
- Clipboard contents: do not persist.
- Recipes/selectors: persist until user deletes.
