# Callsign Reference Docs

This folder contains the source markdown that defines the product, the current alpha shape, and the future desktop agent direction.

Start with `CANON.md` for the product book, then read `PRODUCT_SPEC.md` and `ROADMAP.md` for the alpha line and release plan.

The public landing page in `docs/index.html` is generated from these docs together with the site builder in `scripts/build_site.py`.

## What to read first

- `PRODUCT_SPEC.md` for the alpha product shape.
- `ARCHITECTURE.md` for the current implementation split and future service plan.
- `ROADMAP.md` for the next phases.
- `TEST_PLAN.md` for how alpha behavior should be verified.

## Site generation

Regenerate the rendered pages from the repository root:

```bash
python scripts/build_site.py
```
