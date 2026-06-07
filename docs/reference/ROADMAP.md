# Roadmap

## Phase 0: Documentation starter

Status: included in this package.

Deliverables:

- README.
- Product spec.
- Architecture.
- MCP tool design.
- Security model.
- Threat model.
- Windows automation strategy.
- Voice UX.
- Data model.
- Test plan.
- Deployment guide.
- Burndown list.
- AGENTS.md.
- GitHub Pages site under `/docs`.

## Phase 1: MCP server skeleton

Deliverables:

- C#/.NET solution.
- Stdio MCP server.
- `server.info`.
- `server.ping`.
- Structured result envelopes.
- JSONL audit writer.
- Policy engine stub.
- Unit test project.

Exit criteria:

- A host/client can start the server and call `server.ping`.
- Every tool call writes an audit event.
- Policy is invoked for all action tools, even if only stubbed.

## Phase 2: Observe-only Windows capabilities

Deliverables:

- Active window metadata.
- Visible window list.
- Bounded UIA tree inspection.
- Basic redaction.
- Resource URIs for active window and UI tree.

Exit criteria:

- Calculator and Notepad UI trees can be inspected.
- Tree extraction respects max depth and node count.
- No screenshots required.

## Phase 3: Safe local UI actions

Deliverables:

- Element finder.
- UIA invoke.
- UIA set text.
- Focus element.
- Approved hotkeys.
- Verification helpers.
- Approval flow.

Exit criteria:

- Notepad typing demo works.
- Calculator button demo works.
- Policy blocks unsafe hotkeys.

## Phase 4: File tools

Deliverables:

- Approved root config.
- File list recent.
- Rename.
- Move.
- Copy.
- Path traversal protection.
- Approval for writes.

Exit criteria:

- Newest PNG rename demo works.
- File operation outside approved root is denied.
- No overwrite by default.

## Phase 5: Text host and local model provider

Deliverables:

- CLI host.
- MCP client.
- Model provider abstraction.
- Mock provider.
- Ollama/OpenAI-compatible provider.
- Tool-call planning prompt.

Exit criteria:

- User can type a command.
- Model can plan a bounded tool call.
- Host can route call through MCP server.
- Policy/approval is enforced.

## Phase 6: Voice host

Deliverables:

- STT/TTS or realtime voice provider.
- Push-to-talk or wake mode.
- Interrupt phrase.
- Voice confirmations.
- Transcript.

Exit criteria:

- User can perform Notepad demo by voice.
- “Stop now” interrupts workflow.
- External side effects are still gated.

## Phase 7: Selector repository

Deliverables:

- Selector schema.
- Selector matcher.
- Confidence scoring.
- App selector packs.
- Selector debugging output.

Exit criteria:

- Notepad Save As selector works across launches.
- Calculator selectors work across sessions.

## Phase 8: Recipe system

Deliverables:

- Recipe schema.
- Recipe runner.
- Recipe permission model.
- Recipe verification.
- Manual recipe authoring.

Exit criteria:

- Process-invoice example recipe can be dry-run.
- Recipe writes are policy-gated.

## Phase 9: Teach mode

Deliverables:

- Observation recording.
- Step inference.
- Selector capture.
- User review UI.
- Recipe generation.

Exit criteria:

- User can demonstrate a simple file workflow and save it as a recipe.

## Phase 10: App adapters

Potential adapters:

- Browser extension / DOM adapter.
- Office adapter.
- PDF adapter.
- File Explorer adapter.

Exit criteria:

- Adapters expose semantic tools while still using policy and audit.

## Long-term ideas

- Tray app.
- Local audit viewer.
- Per-app profiles.
- Screen-region privacy masks.
- Signed recipe packs.
- Multi-monitor visual overlays.
- Local-only voice pipeline.
- Enterprise policy templates.
- Accessibility-focused onboarding.

## Roadmap principles

- Do not add power faster than safety.
- Do not add hidden automation before visible automation is reliable.
- Do not add external side effects until approvals are excellent.
- Do not add arbitrary shell.
- Keep local-first behavior as the default.
