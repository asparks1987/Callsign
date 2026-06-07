# Deployment

## Deployment modes

### Mode 1: Docs-only starter

This package is currently a docs-first starter.

Use it to:

- Start a GitHub repository.
- Publish the GitHub Pages site from `/docs`.
- Track implementation against the burndown list.
- Guide code agents with `AGENTS.md`.

### Mode 2: Local developer build

Future implementation target:

```text
DeskPilot.Host starts DeskPilot.McpServer over stdio.
User sends text command.
Host calls local or cloud model provider.
Model proposes tool call.
MCP server validates, evaluates policy, executes, and logs.
```

### Mode 3: Packaged local app

Future implementation target:

- Windows installer or zip package.
- Tray app.
- Config UI.
- Global hotkey.
- Voice provider settings.
- Local audit viewer.

## GitHub Pages deployment

This repository is arranged for GitHub Pages from `/docs`.

Steps:

1. Push repository to GitHub.
2. Go to repository **Settings**.
3. Open **Pages**.
4. Choose **Deploy from a branch**.
5. Select branch `main`.
6. Select folder `/docs`.
7. Save.

The site entry point is:

```text
docs/index.html
```

The included `.nojekyll` file tells GitHub Pages to serve the static site without Jekyll processing.

## Local site preview

From the repository root:

```bash
python -m http.server 8000 -d docs
```

Open:

```text
http://localhost:8000
```

## Regenerate site pages

After editing Markdown files in `docs/reference`, run:

```bash
python scripts/build_site.py
```

This regenerates HTML pages under:

```text
docs/pages/
```

## Suggested local app install layout

Future Windows package:

```text
%LOCALAPPDATA%/DeskPilot/
  DeskPilot.Host.exe
  DeskPilot.McpServer.exe
  config/
  audit/
  selectors/
  recipes/
```

## Configuration files

Suggested config locations:

```text
%LOCALAPPDATA%/DeskPilot/config/policy.yaml
%LOCALAPPDATA%/DeskPilot/config/providers.yaml
%LOCALAPPDATA%/DeskPilot/selectors/*.yaml
%LOCALAPPDATA%/DeskPilot/recipes/*.yaml
```

## MCP client configuration

Example stdio config shape:

```json
{
  "mcpServers": {
    "deskpilot-windows": {
      "command": "C:/Users/YOU/AppData/Local/DeskPilot/DeskPilot.McpServer.exe",
      "args": ["--stdio"],
      "env": {
        "DESKPILOT_POLICY": "C:/Users/YOU/AppData/Local/DeskPilot/config/policy.yaml"
      }
    }
  }
}
```

## Model provider deployment

### Local provider

Use an OpenAI-compatible local endpoint when possible.

Example provider config:

```yaml
model_providers:
  default: local-qwen
  providers:
    local-qwen:
      type: openai_compatible
      base_url: "http://localhost:11434/v1"
      model: "qwen3.5"
      send_screenshots: false
      send_clipboard: false
```

### Cloud provider

Cloud providers require explicit privacy settings.

```yaml
cloud_model_policy:
  send_ui_text: ask
  send_screenshots: false
  send_clipboard: false
  send_file_contents: false
```

## Windows permissions

MVP should run as a normal user process.

Do not require administrator privileges.

If an elevated window is encountered:

- Detect where possible.
- Do not attempt bypass.
- Handoff to user.

## Audit logs

Suggested default:

```text
%LOCALAPPDATA%/DeskPilot/audit/session_YYYYMMDD_NNN.jsonl
```

Audit logs should be local only.

## Upgrade strategy

Future packages should support:

- Config migration.
- Policy migration.
- Selector schema migration.
- Recipe schema migration.
- Audit format versioning.

## Uninstall strategy

Uninstall should remove binaries but ask whether to keep:

- Audit logs.
- Recipes.
- Selectors.
- Config.

## Deployment checklist

- [ ] MCP server starts over stdio.
- [ ] No network listener by default.
- [ ] Policy config is loaded.
- [ ] Audit directory is writable.
- [ ] Stop hotkey is registered.
- [ ] Dangerous tools are disabled.
- [ ] Site docs match shipped tool list.
