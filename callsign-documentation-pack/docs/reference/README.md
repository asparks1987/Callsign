# Callsign Reference Documentation

These Markdown files are canonical inputs for the public and contributor documentation.

## Precedence

Read [the mirrored canon](CANON.md). The root `CANON.md` remains authoritative.

## Core sequence

1. [PRODUCT_SPEC.md](PRODUCT_SPEC.md)
2. [RELEASE_LADDER.md](RELEASE_LADDER.md)
3. [ARCHITECTURE.md](ARCHITECTURE.md)
4. [VOICE_PIPELINE.md](VOICE_PIPELINE.md)
5. [SESSION_STATE_MACHINE.md](SESSION_STATE_MACHINE.md)
6. [SECURITY_MODEL.md](SECURITY_MODEL.md)
7. [THREAT_MODEL.md](THREAT_MODEL.md)
8. [TEST_PLAN.md](TEST_PLAN.md)
9. Root [burndown.md](../../burndown.md)

## Authoring rules

- State whether behavior is `Current`, `Target`, or `Deferred`.
- Use requirement words consistently: `MUST`, `SHOULD`, and `MAY`.
- Do not present examples as implementation evidence.
- Keep risk and privacy impact adjacent to behavior.
- Update the relevant ADR when a durable decision changes.
- Regenerate `docs/pages/` after canonical edits.
