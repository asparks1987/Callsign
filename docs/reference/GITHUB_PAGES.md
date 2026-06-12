# GitHub Pages

The `/docs` folder contains the public Callsign site.

The site has two audiences:

- New users who want to understand what Callsign does.
- Contributors who want the source docs and roadmap.

## Public landing page

The top-level `docs/index.html` is the marketing page.

It should:

- Explain the product in plain language.
- Sell the open-source core and paid-tier story clearly.
- Show the alpha workflow.
- Avoid technical protocol details in the hero section.

## Reference pages

The rendered docs under `docs/pages/` are generated from `docs/reference/`.

Those pages are for contributors and future implementation work.

## Regeneration

Run:

```bash
python scripts/build_site.py
```
