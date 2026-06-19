# Documentation Pack Validation

## Scope

This report validates the generated Callsign documentation overlay itself. It does **not** validate the Callsign source tree, Windows installer, service lifecycle, microphone behavior, wake model, speaker gate, UI, or release artifacts because the local repository was not mounted in the execution environment.

## Pack summary

- Files in the final pack: **99**
- Markdown files: **82**
- JSON files, including schemas and manifest: **12**
- YAML issue forms/config files: **4**
- JSONL examples: **1**
- Documentation volume: **more than 39,000 Markdown words**
- Canonical burndown tasks: **284** in **18** phases

## Checks completed

| Check | Result | Notes |
|---|---|---|
| UTF-8 readability | Pass | Every generated text file decoded as UTF-8. |
| Trailing whitespace | Pass | No generated line contains trailing spaces or tabs. |
| Markdown relative links | Pass | All repo-relative Markdown links resolve inside the overlay. External links and anchors were excluded from this filesystem check. |
| JSON parsing | Pass | All JSON documents parse successfully. |
| JSON Schema validity | Pass | All six schemas pass Draft 2020-12 schema validation. |
| Example conformance | Pass | Profile, runtime snapshot, policy decision, command intent, and audit-event examples validate against their applicable schemas. |
| JSONL parsing | Pass | Every audit-event example line parses as a standalone JSON object. |
| YAML parsing | Pass | All GitHub issue forms and configuration files parse as YAML. |
| Burndown task IDs | Pass | All 284 IDs are unique. |
| Burndown dependencies | Pass | Every dependency resolves to a known task; no task depends on itself. |
| Dependency cycles | Pass | No cycle was found in the generated task graph. |
| Manifest hashes | Pass | Every file listed in `PACK_MANIFEST.json` matches its SHA-256 digest. |
| ZIP integrity | Pass | The final archive opens successfully and every archive member passes CRC validation. |

## Status integrity

The pack uses `Documented done` and `Documented in progress` for claims imported from the existing repository documentation. It does not silently upgrade those claims to independently verified runtime status.

Current task snapshot:

| Status | Count |
|---|---:|
| Documented done | 39 |
| Documented in progress | 46 |
| Not started | 198 |
| Blocked | 1 |
| **Total** | **284** |

## Repository commands not run

The following commands remain mandatory after merging this overlay into a real Callsign checkout:

```powershell
python scripts/build_site.py
git diff --check
.\buildcallsign.ps1
dotnet run --project tests/Callsign.AlphaSmoke/Callsign.AlphaSmoke.csproj
.\verifycallsign-alpha.ps1 -Install -LiveActions -RequireAlphaReady `
  -ReportPath .\build\alpha-readiness.json
```

The installed manual voice walkthrough in [docs/guides/MANUAL_ALPHA_WALKTHROUGH.md](docs/guides/MANUAL_ALPHA_WALKTHROUGH.md) also remains required before any testing-ready release claim.

## Review entry points

- [README.md](README.md)
- [CANON.md](CANON.md)
- [AGENTS.md](AGENTS.md)
- [SOURCE_AUDIT.md](SOURCE_AUDIT.md)
- [MERGE_GUIDE.md](MERGE_GUIDE.md)
- [burndown.md](burndown.md)
- [docs/README.md](docs/README.md)
