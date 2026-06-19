# ADR-0004: Treat observation data as sensitive

## Status

Accepted.

## Context

Window titles, UI text, screenshots, clipboard contents, file paths, transcripts, and audio can contain secrets and personal data.

## Decision

Observation data is sensitive by default and is minimized, bounded, redacted, and kept local unless the user explicitly opts into a disclosed flow.

## Consequences

- Screenshot and clipboard access are disabled/minimized by default.
- UI extraction is bounded.
- Logs use allowlisted fields.
- Cloud transmission is per-data-class opt-in.
- Diagnostic export has preview and redaction.
- New observation tools require privacy review.
