# ADR-0006: Canonical Markdown; generated HTML is derived

## Status

Accepted by this documentation pack; maintainer ratification required.

## Context

The repo maintains Markdown reference docs and generated HTML pages. Manual edits or drift can create conflicting product claims.

## Decision

Canonical authoring occurs in Markdown under `docs/reference/`. `docs/pages/` and the public site are generated and cannot overrule source Markdown.

## Consequences

- The site generator must be deterministic.
- CI regenerates and fails on a dirty diff.
- Generated HTML is not edited by hand.
- Root canon and burndown remain separate explicit sources.
- Site-generation failures block documentation release.
