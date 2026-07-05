# Callsign Reference Docs

This folder contains the source markdown used to generate the public website under `docs/`.

Start with `CANON.md` for the product book, then read `PRODUCT_SPEC.md`, `VOICE_ACCESS_PARITY_MATRIX.md`, `ROADMAP.md`, and `TIER_ARCHITECTURE.md` for the current release line, the Voice Access parity target, and the open-core/closed-extension plan.

Use `ROADMAP.md` for 0.0.x/1.0.0a alpha sequencing and `BURNDOWN.md` for what is still in progress.

## What to read first

- `CANON.md` for the product promise and boundaries.
- `PRODUCT_SPEC.md` for the current alpha shape.
- `VOICE_ACCESS_PARITY_MATRIX.md` for the 100% Windows Voice Access parity checklist.
- `ROADMAP.md` for v1.0 through v1.3.
- `TIER_ARCHITECTURE.md` for Free, Pro, Advanced, and closed-source extension boundaries.
- `ARCHITECTURE.md` for the service/UI split.
- `TEST_PLAN.md` for how alpha behavior should be verified.

## Site generation

Regenerate rendered pages from the repository root:

```powershell
python scripts/build_site.py
```

The generated homepage should sell both:

- the open-source Free core,
- and the future closed-source extension-library path for Advanced capabilities.
