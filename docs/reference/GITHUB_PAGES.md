# GitHub Pages

The `/docs` folder is the public-facing Callsign site plus generated contributor reference pages.

## Public landing page

`docs/index.html` is the public page for open-source users.

It must:

- explain the canon in plain language,
- reinforce the `Callsign -> identity verification -> command` flow,
- show the overlay-first UX and live transcript concept,
- state that all Alpha v1 capabilities are free until at least beta,
- make clear that the Free/open-source core is the public Windows voice assistant,
- keep the public page focused on the current v1.0 release line,
- link to the tier architecture plan for the current Free-core boundary,
- and guide users to the release ladder.

## Reference docs

`docs/pages/` is generated from `docs/reference/`.

Reference content may include internal implementation details needed for contributors, while keeping `/docs/index.html` clean and user-facing.

`docs/pages/canon.html` is the canonical product book for the launch plan.

## Regeneration

Run:

```bash
python scripts/build_site.py
```

and confirm the site pages reflect the same current v1.0 canon as `README.md`, `CANON.md`, and `burndown.md`.
