# ADR-0001: Local stdio automation server by default

## Status

Accepted for the target automation architecture.

## Context

Future desktop automation needs local OS access. A network listener increases discovery, authentication, exposure, and lifecycle risk.

## Decision

The Callsign automation server runs locally over stdio by default. The approved host launches and owns the process.

## Consequences

- stdout is protocol-only.
- Diagnostics go to stderr and local structured logs.
- The host manages lifecycle and capability negotiation.
- No unauthenticated local HTTP listener is introduced.
- Remote/mobile control requires a separate authenticated architecture and threat review.
- Tool authorization still requires session state and policy; local transport alone is not trust.
