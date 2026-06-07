# Burndown List

## Legend

Priority:

- P0: required for MVP foundation.
- P1: required for useful demo.
- P2: important but can follow MVP.
- P3: later enhancement.

Status:

- Not started.
- In progress.
- Blocked.
- Done.

## Backlog

| ID | Work item | Priority | Status | Acceptance criteria |
|---:|---|---:|---|---|
| 1 | Product spec v0.1 | P0 | Done | Clear scope, users, MVP, non-goals |
| 2 | Architecture doc v0.1 | P0 | Done | Host/server boundary, transport, components |
| 3 | Threat model v0.1 | P0 | Done | Covers prompt injection, secrets, arbitrary execution, local compromise |
| 4 | Security model v0.1 | P0 | Done | Policy tiers, approvals, data handling, audit rules |
| 5 | MCP tools doc v0.1 | P0 | Done | Tool list, schemas, risk tiers, examples |
| 6 | Windows automation strategy | P0 | Done | UIA-first and SendInput fallback rules |
| 7 | Voice UX doc | P1 | Done | Confirmation, interruption, dictation, recovery patterns |
| 8 | Data model doc | P1 | Done | Audit, selectors, recipes, policy config |
| 9 | Test plan | P0 | Done | Unit, integration, safety, manual QA |
| 10 | Deployment doc | P1 | Done | GitHub Pages, local preview, future app deployment |
| 11 | Root README | P0 | Done | Fully details project and setup |
| 12 | AGENTS.md | P0 | Done | Code-agent rules and safety constraints |
| 13 | GitHub Pages site | P0 | Done | Static site served from `/docs` |
| 14 | C# solution skeleton | P0 | Not started | Builds empty solution with server/test projects |
| 15 | MCP stdio server skeleton | P0 | Not started | Starts over stdio and registers tools |
| 16 | `server.ping` tool | P0 | Not started | Returns pong and logs call |
| 17 | `server.info` tool | P0 | Not started | Returns version/capabilities |
| 18 | Standard result envelopes | P0 | Not started | All tools use typed success/error shape |
| 19 | JSONL audit writer | P0 | Not started | Writes session/tool/policy events |
| 20 | Policy engine stub | P0 | Not started | All action tools call policy |
| 21 | Policy config loader | P0 | Not started | Loads YAML/TOML policy file |
| 22 | Active window detection | P0 | Not started | Returns hwnd/title/process/bounds |
| 23 | Visible window listing | P0 | Not started | Lists visible top-level windows |
| 24 | UIA tree inspector | P0 | Not started | Bounded tree with controls/patterns |
| 25 | Redaction v0 | P0 | Not started | Redacts known sensitive strings in logs/context |
| 26 | Element finder | P0 | Not started | Finds by name/control type/automation ID |
| 27 | UIA invoke tool | P0 | Not started | Invokes supported visible button/menu item |
| 28 | UIA set text tool | P0 | Not started | Sets editable field or reports unsupported |
| 29 | Focus element tool | P1 | Not started | Focuses expected visible element |
| 30 | Approved hotkey fallback | P1 | Not started | Sends allowlisted hotkeys only |
| 31 | Type text fallback | P1 | Not started | Types into focused expected field with policy |
| 32 | Verification helpers | P1 | Not started | Supports window changed, readback, file exists |
| 33 | Approval request flow | P1 | Not started | Host prompts user and records approval |
| 34 | File approved roots | P1 | Not started | Enforces root allowlist |
| 35 | File list recent | P1 | Not started | Lists recent files in approved root |
| 36 | File rename | P1 | Not started | Renames with approval and verification |
| 37 | File move/copy | P1 | Not started | Moves/copies with approval and verification |
| 38 | Text CLI host | P1 | Not started | User can type command to host |
| 39 | MCP client in host | P1 | Not started | Host calls local server |
| 40 | Mock model provider | P1 | Not started | Deterministic tool-call tests |
| 41 | Ollama/OpenAI-compatible provider | P1 | Not started | Local model call works |
| 42 | Planner prompt v0 | P1 | Not started | Produces bounded tool plans |
| 43 | Notepad demo | P1 | Not started | Open/type/save-with-approval path |
| 44 | Calculator demo | P1 | Not started | Invoke buttons/read result |
| 45 | File rename demo | P1 | Not started | Rename newest PNG in temp folder |
| 46 | Stop workflow tool | P1 | Not started | Cancels active workflow |
| 47 | Global hotkey kill switch | P1 | Not started | User can stop execution |
| 48 | Voice provider interface | P2 | Not started | STT/TTS/realtime abstraction |
| 49 | OpenAI Realtime provider | P2 | Not started | Voice in/out and tool bridge |
| 50 | Local STT/TTS provider | P2 | Not started | Fully local voice option |
| 51 | Selector schema | P2 | Not started | YAML selector records |
| 52 | Selector matcher | P2 | Not started | Confidence scoring and explanations |
| 53 | Recipe schema | P2 | Not started | YAML repeatable workflows |
| 54 | Recipe runner | P2 | Not started | Executes approved recipe steps |
| 55 | Teach mode design | P3 | Not started | Recording and recipe generation design |
| 56 | Browser adapter design | P3 | Not started | DOM-based future plan |
| 57 | Tray app design | P3 | Not started | UI shell and status controls |
| 58 | Installer/packaging | P3 | Not started | Zip/MSIX/installer decision |

## MVP milestone target

MVP is complete when the system can:

1. Start a local MCP server over stdio.
2. Inspect the active Windows app.
3. Find a visible named UI element.
4. Invoke or set text safely.
5. Rename a local file with approval.
6. Stop immediately.
7. Log every action.
8. Block high-risk actions.

## Current next work

The first implementation sprint should focus on:

- C# solution skeleton.
- MCP stdio server.
- `server.ping` and `server.info`.
- JSONL audit writer.
- Policy engine stub.
- Active window detection.

## Risks

| Risk | Status | Mitigation |
|---|---|---|
| UIA support inconsistent across apps | Open | Test with Notepad, Calculator, Explorer, dialogs |
| Safety model underbuilt | Open | Keep all actions policy-gated from first implementation |
| Voice distracts from core execution | Open | Build text CLI first |
| Local model tool calling unreliable | Open | Add mock provider and deterministic planner tests |
| GitHub Pages docs drift from Markdown | Open | Use `scripts/build_site.py` after edits |
