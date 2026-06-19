# AGENTS.md

## Mission

Build Callsign as a local-first, visible, consent-first voice assistant for Windows, with WSL and Linux support added only through explicit release gates.

The current target is `v1.0 alpha`: install Callsign, create a local profile, enroll voice, run the background listener, wake with `Callsign`, pass the voice/callsign gate, see the overlay and live status, and launch an installed app through a visible Windows path.

Do not silently turn future MCP, browser, file, or system-control designs into current release claims.

## Read first

Before changing code or docs, read:

1. `CANON.md`
2. `docs/reference/PRODUCT_SPEC.md`
3. `docs/reference/ARCHITECTURE.md`
4. `docs/reference/SECURITY_MODEL.md`
5. `docs/reference/THREAT_MODEL.md`
6. `docs/reference/TEST_PLAN.md`
7. `burndown.md`
8. Relevant ADRs under `docs/reference/ADR/`

## Source-of-truth discipline

- Security/legal constraints outrank all project documents.
- `CANON.md` resolves product and terminology conflicts.
- Accepted ADRs resolve architectural decisions.
- Canonical Markdown outranks generated HTML.
- Root `burndown.md` owns execution status.
- Existing status labels are evidence claims, not decoration.
- Do not mark an item `Verified` without current command output or attached human evidence.
- Do not edit generated `docs/pages/*.html` by hand; update its Markdown source and regenerate.

## Current architecture

### Callsign.UI

Owns:

- First-run and profile UX.
- Callsign validation.
- Voice-sample capture, playback, reset, and enrollment controls.
- Service/session monitoring.
- Diagnostics and repair entry points.
- Visible dictation and review surfaces.
- User-facing configuration.

### Callsign.Service

Owns:

- Microphone lifecycle.
- openWakeWord integration.
- Wake-event emission.
- Identity and session state.
- Command capture and routing.
- Overlay/readout state.
- Timeouts and lockouts.
- Runtime snapshots and local diagnostics.
- Current narrow action adapters.

### Future automation server

The future local MCP server is a separate capability boundary. It must:

- Run over local stdio by default.
- Expose typed tools.
- Evaluate policy before every action.
- Prefer native APIs and UI Automation.
- Verify action results.
- Emit redacted audit records.
- Refuse arbitrary shell and blocked risk classes.

Do not make the host directly manipulate the desktop.

## Non-negotiable safety rules

Never add or normalize:

- Arbitrary shell, PowerShell, WSL, `cmd.exe`, script, or command execution.
- Password, 2FA, payment, wallet, token, or credential-store entry.
- Stealth, evasion, persistence, surveillance, or exfiltration behavior.
- Hidden external submissions.
- Purchases, money movement, account deletion, permanent deletion, admin/UAC, or security-setting changes without a newly accepted safety design.
- Model-controlled policy decisions.
- Action tools without validation, policy, verification, and audit behavior.
- Screenshot, clipboard, UI-tree, file-content, transcript, or voice-sample upload by default.
- Coordinate clicking when a semantic path exists.
- Release-readiness claims based only on source inspection.

When a task crosses a blocked boundary, hand it to the human.

## Identity rules

- openWakeWord detection is the wake event.
- Transcript text alone cannot wake the session.
- The identity utterance is identity-only.
- Wake, identity, and command are separate state transitions.
- A mismatch, timeout, low-confidence result, missing enrollment, or cancellation blocks action.
- Describe the Alpha gate accurately: it is not strong operating-system authentication.

## Automation priority

Use, in order:

1. Native application or operating-system API.
2. Windows UI Automation pattern.
3. Stable saved semantic selector.
4. Bounded vision/OCR assistance, only when privacy policy allows it.
5. Approved SendInput keyboard/mouse fallback.
6. Human handoff.

Re-resolve targets immediately before action. Verify state afterward.

## Tool contract

Every tool requires:

- `domain.verb_object` name.
- Typed, bounded input schema.
- Typed success and error envelopes.
- Risk tier.
- Privacy classification.
- Reversibility statement.
- Approval rule.
- Policy evaluation.
- Redacted audit event.
- Verification strategy.
- Timeout and cancellation handling.
- Positive, negative, and failure tests.

Tools must not infer permission from a model-generated rationale.

## Coding standards

- Keep UI, runtime, policy, storage, automation, and protocol concerns separable.
- Prefer deterministic, dependency-injected components.
- Keep global mutable state limited to explicit process/session registries.
- Normalize Windows paths before validation or policy checks.
- Bound audio buffers, UI trees, search results, logs, and retries.
- Use cancellation tokens for long-running operations.
- Use structured error codes, not string matching, across process boundaries.
- Keep stdio protocol output clean; diagnostics go to stderr.
- Include correlation IDs across session, policy, action, verification, and audit events.
- Treat process names, window titles, UI text, paths, transcripts, audio, screenshots, and clipboard data as sensitive.
- Do not log raw secrets or raw audio by default.

## Documentation requirements

A behavior-changing change updates, as applicable:

- `CANON.md`
- `docs/reference/PRODUCT_SPEC.md`
- `docs/reference/ARCHITECTURE.md`
- The feature spec under `docs/reference/`
- `docs/reference/SECURITY_MODEL.md`
- `docs/reference/THREAT_MODEL.md`
- `docs/reference/PRIVACY_MODEL.md`
- `docs/reference/TEST_PLAN.md`
- `burndown.md`
- Schemas and examples
- Generated site output

Create or update an ADR for a durable boundary, dependency, protocol, storage, safety, or release decision.

## Testing requirements

Each change needs the smallest applicable set of:

- Unit tests.
- Contract/schema tests.
- Session-state tests.
- Policy allow/deny/approval tests.
- Negative tests for blocked behavior.
- Integration tests with mocked audio/automation.
- Installed-payload smoke tests.
- Manual voice or accessibility evidence.
- Log-redaction assertions.
- Cancellation and timeout tests.

High-risk changes require explicit proof that denied paths remain denied.

## Expected commands

Documented baseline:

```powershell
.\buildcallsign.ps1
dotnet run --project tests/Callsign.AlphaSmoke/Callsign.AlphaSmoke.csproj
python scripts/build_site.py
git diff --check
```

Release evidence may additionally require:

```powershell
.\verifycallsign-alpha.ps1 -Install -LiveActions -RequireAlphaReady `
  -ReportPath .\build\alpha-readiness.json
```

Use commands that actually exist in the checkout. Record failures rather than rewriting history.

## Work protocol

For every task:

1. Identify the governing canon, requirement, ADR, and burndown item.
2. Inspect current behavior and tests.
3. State assumptions.
4. Implement the narrowest safe change.
5. Add or update evidence.
6. Update docs and schemas.
7. Regenerate derived docs.
8. Run relevant checks.
9. Report exactly what passed, failed, or remained unverified.

## Stop conditions

Stop and escalate when:

- The requested change conflicts with `CANON.md`.
- The task needs credentials, admin rights, destructive action, or undisclosed network data.
- A security boundary is unclear.
- Two canonical documents conflict.
- A dependency license is incompatible or unknown.
- A release claim cannot be supported by current evidence.
- A future paid/private feature would leak into public tracked source.
