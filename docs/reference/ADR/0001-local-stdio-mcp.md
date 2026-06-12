# ADR-0001: Local stdio automation service by default

## Status

Accepted for MVP.

## Context

Callsign will need to expose local Windows automation capabilities to a future voice or task host. Exposing those capabilities through a network listener would create a larger attack surface.

## Decision

The Windows automation component should run locally over stdio by default when that service layer exists.

## Rationale

- Desktop automation requires local OS access.
- Stdio avoids exposing an unauthenticated local HTTP server.
- The host can manage server process lifetime.
- Logs can be kept local.
- Remote control can be added later through a separate authenticated bridge.

## Consequences

- Server diagnostics must go to stderr, not stdout.
- The host launches the server process.
- Remote/mobile scenarios are out of scope for the alpha.
