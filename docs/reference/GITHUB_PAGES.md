# GitHub Pages

The `/docs` folder is the public-facing Callsign site plus generated contributor reference pages.

## Public landing page

`docs/index.html` is the public page for open-source users and future extension customers.

It must:

- explain the canon in plain language,
- reinforce the `Callsign -> identity verification -> command -> visible action` flow,
- sell the Free/open-source core as the public trust layer,
- show the overlay-first UX and live transcript concept,
- state that all Alpha v1 capabilities are free until at least beta,
- explain that v1.0 starts with wake, identity, overlay, and visible Start menu launch,
- show the Alpha v1 path through dictation, browser control, and system control,
- mention the future closed-source Pro/Advanced extension-library plan,
- and link to the tier architecture plan for the exact boundary.

## Reference docs

`docs/pages/` is generated from `docs/reference/`.

Reference content may include implementation details needed for contributors, while keeping `/docs/index.html` focused on the user story.

`docs/pages/canon.html` is the canonical product book for the launch plan.

## Regeneration

Run:

```powershell
python scripts/build_site.py
```

Then confirm the site pages reflect the same canon as `README.md`, `CANON.md`, and `docs/reference`.
