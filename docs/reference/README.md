# Callsign Reference Docs

This folder contains the source markdown that defines the current alpha and the public site.

Start with `CANON.md` for the product book, then read `PRODUCT_SPEC.md` and `ROADMAP.md` for the v1.0 scope and release plan.

The public landing page in `docs/index.html` is generated from these docs together with the site builder in `scripts/build_site.py`.

## What to read first

- `PRODUCT_SPEC.md` for the alpha product shape.
- `ARCHITECTURE.md` for the current implementation split.
- `ROADMAP.md` for the current release line.
- `TEST_PLAN.md` for how alpha behavior should be verified.

## Site generation

Regenerate the rendered pages from the repository root:

```bash
python scripts/build_site.py
```
