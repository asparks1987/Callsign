# DeskPilot

**DeskPilot** is a design starter for a Windows 11 voice-operated desktop agent: a local-first automation runtime where a voice/model host talks to a Windows MCP server that safely observes and operates the user’s desktop.

The core idea is simple but powerful:

> The user speaks. A model plans. A local MCP server exposes safe Windows automation tools. A policy engine decides what is allowed. Every action is visible, reversible where possible, and auditable.

This repository is a documentation-first starter kit. It contains the project specification, architecture, security model, MCP tool design, implementation roadmap, burndown list, `AGENTS.md`, example configs, and a static GitHub Pages site that can publish from the `/docs` folder.

---

## Why this exists

Most desktop agents are either too brittle or too dangerous. They click coordinates, scrape screenshots, guess at user intent, and often treat the local machine as an unstructured visual surface. DeskPilot takes the opposite approach.

It treats the Windows desktop as a set of typed, inspectable, permissioned capabilities.

Instead of asking an LLM to “move the mouse over there,” DeskPilot asks the LLM to call tools like:

```text
desktop.inspect_window
desktop.find_element
desktop.invoke_element
desktop.set_text
file.rename
policy.request_approval
```

The Windows MCP server then chooses the safest available execution path:

1. Native app/API operation.
2. Windows UI Automation control pattern.
3. Saved selector.
4. Vision/OCR-assisted target.
5. Raw keyboard/mouse simulation as a fallback.
6. Human handoff.

This keeps the agent closer to assistive technology and test automation than to a haunted macro recorder.

---

## Project goals

DeskPilot is intended to become:

- A **local Windows MCP server** exposing desktop automation tools.
- A **voice/model host** that can use OpenAI Realtime, local STT/TTS, Ollama, LM Studio, llama.cpp, or other model providers.
- A **policy-gated execution layer** that requires approval for risky actions.
- An **audit-first automation environment** where every observation, plan, tool call, approval, and result can be reviewed.
- A **recipe system** for teachable workflows such as processing invoices, renaming files, updating spreadsheets, or filling repetitive forms.

---

## Non-goals

DeskPilot is not intended to be:

- A stealth automation framework.
- A bot for bypassing app protections.
- A credential-entry tool.
- A remote administration tool.
- A malware-like persistence, evasion, or exfiltration framework.
- A system that silently sends messages, purchases items, deletes data, or modifies security settings.

The MVP explicitly blocks or handoffs tasks involving passwords, 2FA, payments, credential stores, arbitrary shell execution, UAC/admin prompts, destructive deletion, and external submissions without approval.

---

## High-level architecture

```text
┌─────────────────────────────────────────────────────────────────────┐
│                         Voice / Agent Host                          │
│                                                                     │
│  Mic input → STT / realtime voice → LLM planner → Tool router        │
│                                  │                                  │
│                                  │ MCP client                       │
└──────────────────────────────────┼──────────────────────────────────┘
                                   │ stdio, local-first
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    Windows Automation MCP Server                     │
│                                                                     │
│  Tools: inspect_window, find_element, invoke, set_text, hotkey,      │
│         screenshot, open_app, read_selection, wait_for_state         │
│                                                                     │
│  Resources: active window tree, process list, screenshots, audit log │
│  Prompts: perform_task, teach_task, recover_from_failure             │
│                                                                     │
│  Policy Engine: risk tiers, confirmations, allowlists, deny rules    │
└──────────────────────────────────┬──────────────────────────────────┘
                                   │
            ┌──────────────────────┼──────────────────────┐
            ▼                      ▼                      ▼
   Windows UI Automation     Win32 / SendInput       App-specific APIs
   element tree, patterns    fallback input          browser, files,
   invoke/value/text/etc.    hotkeys/clicks          PowerShell allowlist
```

The MCP server is intentionally separate from the host. The host handles conversation, voice, model providers, planning, and session UX. The MCP server handles local Windows capabilities and safety enforcement.

---

## Repository layout

```text
.
├── README.md
├── AGENTS.md
├── SECURITY.md
├── CONTRIBUTING.md
├── LICENSE
├── docs/
│   ├── index.html
│   ├── .nojekyll
│   ├── assets/
│   │   ├── styles.css
│   │   └── site.js
│   ├── pages/
│   │   ├── product-spec.html
│   │   ├── architecture.html
│   │   ├── mcp-tools.html
│   │   └── ...
│   └── reference/
│       ├── PRODUCT_SPEC.md
│       ├── ARCHITECTURE.md
│       ├── MCP_TOOLS.md
│       ├── SECURITY_MODEL.md
│       ├── THREAT_MODEL.md
│       ├── WINDOWS_AUTOMATION.md
│       ├── VOICE_UX.md
│       ├── DATA_MODEL.md
│       ├── TEST_PLAN.md
│       ├── DEPLOYMENT.md
│       ├── ROADMAP.md
│       ├── BURNDOWN.md
│       └── ADR/
├── examples/
│   ├── policies/default.policy.yaml
│   ├── recipes/process_invoice.example.yaml
│   ├── mcp/claude_desktop_config.example.json
│   └── model-providers/ollama.example.yaml
├── scripts/
│   └── build_site.py
├── src/.gitkeep
└── tests/.gitkeep
```

---

## GitHub Pages setup

This package is already arranged so the website can publish from `/docs`.

1. Push this repository to GitHub.
2. Open the repository settings.
3. Go to **Pages**.
4. Under **Build and deployment**, select **Deploy from a branch**.
5. Select the branch, usually `main`.
6. Select the `/docs` folder.
7. Save.

GitHub’s current Pages docs state that a publishing source can use either the repository root `/` or a `/docs` folder on the selected branch. The included `/docs/index.html` is the site entry point.

The site is static HTML/CSS/JS and includes `.nojekyll`, so it does not require a Jekyll build.

---

## Development philosophy

DeskPilot should be built documentation-first, safety-first, and test-first.

The model is not the authority. The model proposes actions. The policy engine approves, denies, or requests user confirmation. The Windows automation layer executes only typed, validated tools. The audit log records what happened.

A good DeskPilot action has these properties:

- It is tied to an explicit user request.
- It operates on the visible desktop unless the user explicitly permits otherwise.
- It uses a semantic selector instead of raw coordinates when possible.
- It can explain what it is about to do.
- It can verify that it worked.
- It can stop immediately.
- It does not cross a risk boundary without approval.

---

## Risk tiers

| Tier | Name | Examples | Default MVP behavior |
|---:|---|---|---|
| 0 | Observe | list windows, inspect UI tree, read active title | Allowed after session permission |
| 1 | Local reversible | focus window, invoke button, type text, press Escape | Allowed with visible execution |
| 2 | Local state change | rename/move file, edit document, change local setting | Ask once per task |
| 3 | External side effect | send message, submit form, upload file, post content | Explicit approval every time |
| 4 | Dangerous or blocked | shell, admin, payment, credential entry, permanent deletion | Blocked or human handoff in MVP |

---

## Suggested implementation milestones

### Milestone 0: Documentation and skeleton

- Finalize this documentation set.
- Create the C#/.NET MCP server skeleton.
- Add stdio transport.
- Add `server.ping` and `server.info`.
- Add JSONL audit logging.
- Add policy engine stub.

### Milestone 1: Observe-only Windows server

- `desktop.get_active_window`
- `desktop.list_windows`
- `desktop.inspect_window`
- Bounded UI Automation tree extraction.
- Privacy redaction hooks.

### Milestone 2: Safe local actions

- `desktop.find_element`
- `desktop.invoke_element`
- `desktop.set_text`
- `desktop.focus_element`
- Approved hotkey fallback.
- Confirmation flow.

### Milestone 3: First demos

- Open Notepad and type a dictated note.
- Open Calculator and read a result.
- Rename the newest PNG in Downloads with approval.
- Navigate a browser search page with visible execution.

### Milestone 4: Voice host

- Text CLI first.
- Local Ollama provider.
- Optional OpenAI Realtime provider.
- Interrupt phrase and hotkey kill switch.
- Speech confirmation flow.

### Milestone 5: Recipes and teach mode

- Selector repository.
- Recipe schema.
- Record-and-replay workflow design.
- Verification conditions.

---

## Example task flow

User says:

> Rename the newest screenshot in Downloads to router-settings.png.

The host translates this into an action loop:

```text
1. file.list_recent("%USERPROFILE%/Downloads", pattern="*.png")
2. policy.evaluate(file.rename, target_path, risk_tier=local_state_change)
3. Ask: "I found Screenshot 2026-06-07 143210.png. Rename it?"
4. file.rename(old_path, new_name)
5. file.exists(new_path)
6. audit.record_result(success=true)
```

The model can plan, but it cannot override policy.

---

## Recommended tech stack

### Windows MCP server

- C#/.NET.
- Local stdio MCP transport.
- Windows UI Automation first.
- Win32/SendInput fallback.
- SQLite for durable local metadata if needed.
- JSONL for session audit logs.

### Voice/model host

- TypeScript, Python, or C#.
- Provider abstraction for OpenAI-compatible APIs, Ollama, LM Studio, llama.cpp, and realtime voice providers.
- Mock provider for deterministic tests.

### Docs/site

- Markdown source docs in `/docs/reference`.
- Static HTML site in `/docs`.
- No external CSS or JS dependencies.

---

## Key design decisions

| ADR | Decision |
|---|---|
| ADR-0001 | Use a local stdio MCP server by default. |
| ADR-0002 | Prefer Windows UI Automation before SendInput. |
| ADR-0003 | Require a policy engine for all action tools. |
| ADR-0004 | Treat screenshots, clipboard, and UI text as sensitive data. |

See `docs/reference/ADR/` for the full ADRs.

---

## Known hard parts

- UI Automation trees can be noisy or incomplete.
- Some apps expose poor accessibility metadata.
- Elevated/admin windows may reject input from non-elevated processes.
- Browser pages are better handled with DOM adapters later.
- LLMs may misread UI state unless tools return compact, structured observations.
- Voice UX needs careful interruption and confirmation design.
- Safety policy must stay outside the model.

---

## References

- GitHub Pages publishing sources: https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-your-github-pages-site
- GitHub Pages quickstart: https://docs.github.com/pages/quickstart
- Model Context Protocol docs: https://modelcontextprotocol.io/docs/getting-started/intro
- MCP server concepts: https://modelcontextprotocol.io/docs/learn/server-concepts
- MCP resources specification: https://modelcontextprotocol.io/specification/2025-06-18/server/resources
- MCP prompts specification: https://modelcontextprotocol.io/specification/2025-06-18/server/prompts
- MCP security best practices: https://modelcontextprotocol.io/docs/tutorials/security/security_best_practices
- Microsoft UI Automation overview: https://learn.microsoft.com/en-us/windows/win32/winauto/entry-uiauto-win32
- Microsoft UI Automation control patterns: https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-controlpatternsoverview
- Microsoft SendInput docs: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput
