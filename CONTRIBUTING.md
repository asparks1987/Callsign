# Contributing

Thanks for helping build Callsign.

The project is documentation-first and safety-first. Before adding new automation capabilities, read:

- `AGENTS.md`
- `docs/reference/SECURITY_MODEL.md`
- `docs/reference/THREAT_MODEL.md`
- `docs/reference/MCP_TOOLS.md`

## Contribution flow

1. Open or pick an issue from the burndown list.
2. Write or update the design notes before implementation.
3. Add tests for expected and blocked behavior.
4. Update the docs.
5. Regenerate the static site with:

```bash
python scripts/build_site.py
```

## Pull request checklist

- [ ] Tool inputs are typed and validated.
- [ ] Policy engine cannot be bypassed.
- [ ] Audit logging is present.
- [ ] Risk tier is documented.
- [ ] Negative tests cover unsafe use.
- [ ] Docs are updated.
- [ ] Site is regenerated.

## Safety review trigger

Request a safety review for any change that touches:

- Filesystem writes.
- Shell/process execution.
- Clipboard contents.
- Screenshots.
- Browser automation.
- External network side effects.
- Credential-adjacent flows.
- Policy engine logic.
