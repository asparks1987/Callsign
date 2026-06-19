# Architecture Decision Records

ADRs capture durable product, architecture, safety, privacy, storage, protocol, and release decisions.

## Status values

- `Proposed`
- `Accepted`
- `Superseded`
- `Deprecated`
- `Rejected`

## Index

- [0000 — Template](0000-template.md)
- [0001 — Local stdio automation server by default](0001-local-stdio-mcp.md)
- [0002 — UI Automation before SendInput](0002-uia-before-sendinput.md)
- [0003 — Policy engine required for every action](0003-policy-engine-required.md)
- [0004 — Observation data is sensitive](0004-sensitive-observation-data.md)
- [0005 — Separate `v1.0` from the Alpha v1 family](0005-release-ladder-canon.md)
- [0006 — Canonical Markdown; generated HTML is derived](0006-generated-site-derived.md)
- [0007 — Voice/callsign is a session gate, not OS authentication](0007-identity-assurance.md)
- [0008 — Visible action and verification before capability breadth](0008-visible-verified-actions.md)

## Process

1. Copy the template.
2. Give the decision the next number.
3. Describe alternatives and safety/privacy consequences.
4. Link affected burndown items.
5. Obtain required reviewers.
6. Update canon/specs in the same change.
7. Mark supersession explicitly; do not rewrite accepted history.
