# Development Guide

## Repository orientation

Expected high-level areas:

```text
src/
  Callsign.UI/
  Callsign.Service/
  Callsign.Setup/
tests/
  Callsign.AlphaSmoke/
docs/
  reference/       canonical Markdown
  pages/           generated HTML
scripts/
models/
assets/
examples/
closed-source/     ignored private boundary
```

Confirm the current checkout; generated `bin/` and `obj/` folders are not architecture.

## Development loop

```powershell
git status --short
.\buildcallsign.ps1
dotnet run --project tests/Callsign.AlphaSmoke/Callsign.AlphaSmoke.csproj
python scripts/build_site.py
git diff --check
```

Run narrower tests during implementation and the complete relevant gate before review.

## Boundaries

- UI does not own the always-on microphone/session loop.
- Service does not become a general UI toolkit.
- Domain/session logic avoids WinForms and vendor SDK dependencies.
- Wake detector produces events; transcript parser cannot manufacture wake.
- Action adapters accept typed intents.
- Policy and audit are independent of model behavior.
- Storage code validates and migrates persisted data.
- Generated docs are not hand-edited.

## Adding a feature

1. Assign release.
2. Write feature spec and non-goals.
3. Identify data classes and threats.
4. Define state transitions.
5. Define typed contracts and errors.
6. Define policy and verification.
7. Add tests first where practical.
8. Implement narrow adapter.
9. Add telemetry/diagnostics without sensitive content.
10. Update burndown and evidence.
11. Regenerate docs.

## Local test data

Use synthetic:

- Callsigns.
- Audio fixtures with contributor consent and documented license.
- Temporary filesystem roots.
- Test windows/apps.
- Fake transcripts.
- Canary secrets to assert redaction.

Never commit personal audio or screenshots.

## Debugging

- Use correlation IDs.
- Keep stdout clean for stdio protocols.
- Prefer structured logs.
- Add temporary debug fields only if redacted.
- Reproduce with deterministic adapters before using a live microphone.
- Record exact environment and active profile schema.

## Dependencies

See [DEPENDENCIES.md](../reference/DEPENDENCIES.md). New model/runtime dependencies require license, provenance, hash, packaging, offline, update, and removal analysis.
