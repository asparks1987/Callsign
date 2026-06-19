# Merge Guide

This pack is a documentation overlay for the existing Callsign repository. It intentionally does not contain application source, compiled binaries, model files, or generated HTML.

## Recommended merge sequence

1. Create a documentation branch from a clean worktree.
2. Back up or tag the current documentation state.
3. Copy this pack into the repository root, preserving paths.
4. Review `SOURCE_AUDIT.md`.
5. Resolve the duplicate `README.md` objects in the synced Drive folder; keep one current root README and archive the old design-starter copy outside the repo.
6. Add the missing root `CANON.md` and mirrored `docs/reference/CANON.md`.
7. Treat root `burndown.md` as canonical.
8. Keep `docs/reference/BURNDOWN.md` as a summary/link, not a second checklist.
9. Extend `scripts/build_site.py` to render any newly added canonical reference pages.
10. Regenerate `docs/pages/`.
11. Run link, schema, docs-generation, smoke, and diff checks.
12. Review all imported status labels before publishing.

## Files that replace existing documents

These are proposed complete replacements and require maintainer review:

- `README.md`
- `AGENTS.md`
- `CONTRIBUTING.md`
- `SECURITY.md`
- `THIRD_PARTY_SOURCES.md`
- `burndown.md`
- Existing matching files under `docs/reference/`

## Files that are additive

- `CANON.md`
- `SUPPORT.md`
- `GOVERNANCE.md`
- `SOURCE_AUDIT.md`
- New reference specifications, guides, checklists, templates, schemas, and examples.
- GitHub issue and pull-request templates.

## Generated files

Do not copy generated HTML from this pack; none is included. The repository's site generator should produce `docs/pages/*.html` from canonical Markdown.

## Status caution

Existing task statuses were imported as `Documented done` or `Documented in progress`. Promote them to `Verified` only after running current checks and attaching evidence.

## Suggested review commands

```powershell
git status --short
git diff --check
python scripts/build_site.py
dotnet run --project tests/Callsign.AlphaSmoke/Callsign.AlphaSmoke.csproj
.\verifycallsign-alpha.ps1 -Install -LiveActions -RequireAlphaReady `
  -ReportPath .\build\alpha-readiness.json
```
