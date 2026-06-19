# Documentation Workflow

## Sources

- Root `CANON.md`: product precedence and invariants.
- Root `burndown.md`: task status.
- `docs/reference/`: canonical specifications.
- `docs/pages/`: generated.
- Guides/checklists/templates: supporting material.

## Change process

1. Identify canonical source.
2. Update requirements and non-goals.
3. Update safety/privacy impact.
4. Update tests and burndown.
5. Add/update ADR if durable.
6. Regenerate site.
7. Run link/schema/diff checks.
8. Review public claims.

## Style

- Use plain language.
- State `Current`, `Target`, or `Deferred`.
- Define acronyms.
- Use `MUST` for release requirements.
- Avoid “secure,” “private,” “verified,” or “supported” without scope/evidence.
- Avoid duplicate task lists.
- Keep commands copyable.
- Include failure and cancellation behavior.
- Link to the governing schema/ADR.

## Site generation

```powershell
python scripts/build_site.py
git diff --check
```

CI should fail when regeneration changes tracked output.

## Review checklist

Use [docs/checklists/DOCS_REVIEW.md](../checklists/DOCS_REVIEW.md).
