# Contributing to Callsign

Thanks for helping build Callsign. This project is documentation-first, test-first, and safety-first because it listens continuously and can operate a real desktop.

## Before opening a change

Read:

- [CANON.md](CANON.md)
- [AGENTS.md](AGENTS.md)
- [Product specification](docs/reference/PRODUCT_SPEC.md)
- [Architecture](docs/reference/ARCHITECTURE.md)
- [Security model](docs/reference/SECURITY_MODEL.md)
- [Threat model](docs/reference/THREAT_MODEL.md)
- [Test plan](docs/reference/TEST_PLAN.md)
- [Burndown](burndown.md)

Pick or create a burndown item. A feature without requirements, safety impact, tests, and an owner is not ready to implement.

## Development flow

1. Describe the user-visible outcome and non-goals.
2. Identify affected trust boundaries and data classes.
3. Update or add an ADR for durable decisions.
4. Add tests for successful, denied, cancelled, timeout, and dependency-failure paths.
5. Implement the smallest safe slice.
6. Update canonical Markdown and schemas.
7. Regenerate the static site.
8. Run checks from a clean worktree.
9. Attach evidence to the pull request.

## Pull-request expectations

A pull request should state:

- Burndown task IDs.
- Release target.
- User-visible behavior.
- Security/privacy impact.
- Data stored, observed, transmitted, or deleted.
- Tests run and exact results.
- Manual evidence still required.
- Docs and schemas changed.
- Rollback strategy.

Use [.github/PULL_REQUEST_TEMPLATE.md](.github/PULL_REQUEST_TEMPLATE.md).

## Review triggers

Request security/privacy review for changes involving:

- Audio or voice samples.
- Identity or confidence thresholds.
- Filesystem access.
- Clipboard, screenshots, UI trees, or window metadata.
- Browser automation.
- Network communication.
- External submissions.
- Process launching.
- Service installation or persistence.
- Policy, approval, audit, or redaction logic.
- Model-provider integration.

## Documentation workflow

Canonical files live under `docs/reference/`. Rendered pages under `docs/pages/` are generated.

```powershell
python scripts/build_site.py
git diff --check
```

Do not fix a generated page without fixing its source.

## Testing

At minimum, run the tests affected by the change. The documented baseline is:

```powershell
dotnet run --project tests/Callsign.AlphaSmoke/Callsign.AlphaSmoke.csproj
```

Release-impacting changes also need an installed-payload walkthrough and current verifier output.

## Status integrity

Use `Verified` only when current evidence is attached. Source inspection, an old report, or a status copied from a previous burndown is not current verification.

## Contributor data

Do not commit:

- Voice samples from real people.
- Transcripts containing private information.
- Screenshots of personal desktops.
- Logs with usernames, paths, tokens, emails, or machine identifiers.
- Proprietary model files without redistributable rights.
- Private paid-tier materials.
