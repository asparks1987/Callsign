# Test Plan

## Objective

A release claim is valid only when the current checkout, built artifacts, installed behavior, and human-visible workflow provide evidence.

## Evidence levels

| Level | Evidence |
|---|---|
| E0 | Requirement/design only |
| E1 | Unit/schema/static test |
| E2 | Component integration test |
| E3 | Packaged artifact smoke test |
| E4 | Installed automated test |
| E5 | Human-spoken/manual walkthrough |
| E6 | Clean-machine or VM release proof |

`Documented done` is not an evidence level. `Verified` references current E-level artifacts.

## Baseline commands

Documented project commands:

```powershell
.\buildcallsign.ps1
dotnet run --project tests/Callsign.AlphaSmoke/Callsign.AlphaSmoke.csproj
python scripts/build_site.py
git diff --check
```

Documented release verifier:

```powershell
.\verifycallsign-alpha.ps1 -Install -LiveActions -RequireAlphaReady `
  -ReportPath .\build\alpha-readiness.json
```

Confirm command names in the current checkout before treating them as executable canon.

## Test layers

### Static and contract

- Build warnings/errors.
- Markdown link and generated-site drift.
- JSON Schema validation.
- Dependency/license manifest.
- Secret and sensitive-data scanning.
- Configuration bounds.
- State-transition tables.
- Policy rule coverage.

### Unit

- Callsign normalization.
- Profile paths and schema migration.
- Audio frame buffering.
- Wake-event adapter.
- Identity threshold/result mapping.
- Intent parsing.
- Unsafe target rejection.
- Candidate ranking and ambiguity.
- Error mapping.
- Redaction.

### Integration

- UI/profile store.
- UI/service status.
- Mock wake to session state.
- Mock identity pass/fail.
- Overlay lifecycle.
- Transcription failure.
- Start-menu resolver/launcher abstraction.
- Explorer and browser helpers.
- Policy/tool pipeline.
- Audit correlation.

### Packaged artifact

Validate that artifacts include:

- UI binary.
- Service/runtime binary.
- Overlay asset.
- Icon.
- Wake model.
- Voice helper/runtime.
- `fzf` when required.
- Dependency/license notices.
- Payload manifest and hashes.

### Installed automated

- Shortcut and install layout.
- Service or fallback startup.
- Clean profile.
- Repair controls.
- Restart persistence.
- Uninstall/cleanup.
- Version mismatch.
- Log locations and ACLs.

### Manual voice

Use a non-sensitive Windows user/VM:

1. Install.
2. Create profile.
3. Record/replay/reset/enroll.
4. Confirm service and microphone.
5. Say `Callsign`.
6. Confirm overlay.
7. Say active callsign.
8. Confirm identity state.
9. Say `Open Notepad`.
10. Observe visible launch.
11. Test wrong callsign.
12. Test stop/cancel at every phase.
13. Test timeout, missing target, missing mic, and runtime repair.
14. Sanitize and attach evidence.

## `v1.0` release matrix

| Area | Required proof |
|---|---|
| Build | clean checkout build and artifact manifest |
| Installer | E4 install/reinstall/uninstall |
| Profile | E2 persistence + E5 clean setup |
| Enrollment | E2 failures + E5 real microphone |
| Wake | E2 detector fixture + E5 spoken wake |
| Identity | E2 match/mismatch/lockout + E5 active profile |
| Overlay | E2 state lifecycle + E5 topmost/focus behavior |
| App parsing | E1 unsafe rejection |
| App launch | E2 mocked + E5 visible common-app launch |
| Cancel | E2 every state + E5 active operation |
| Diagnostics | E4 repair/log access + redaction review |
| Accessibility | keyboard/screen-reader/manual checklist |
| Docs | generated output clean and current |
| Security | blocked behavior regression suite |

## Negative tests

Required:

- Transcript `Callsign` without detector event.
- Wake and command in one event.
- Identity and command in one utterance.
- Wrong profile.
- No enrollment.
- Repeated mismatch and lockout.
- Path, URL, shell, script, separator, and secret-like app input.
- Ambiguous target.
- Elevated target.
- Late adapter completion after cancel.
- Overlay failure.
- Audit/log failure.
- Corrupt profile.
- Missing/tampered model.
- Service/UI version mismatch.
- Disk full and permission denied.
- Dependency repair offline.

## Performance and soak

- 8-hour listener with bounded memory.
- Device disconnect/reconnect.
- Repeated wake/cancel cycles.
- High CPU/load behavior.
- p50/p95 state latency.
- Log rotation.
- Service restart recovery.
- No unauthorized state survives restart.

## Evidence format

Use [docs/templates/TEST_EVIDENCE.md](../templates/TEST_EVIDENCE.md). Reports include commit, artifact hashes, environment, commands, results, sanitized attachments, and remaining uncertainty.
