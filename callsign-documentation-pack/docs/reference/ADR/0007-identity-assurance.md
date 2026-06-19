# ADR-0007: Voice/callsign is a session gate, not OS authentication

## Status

Accepted by this documentation pack; maintainer ratification required.

## Context

Existing docs use “identity verification,” which can imply security guarantees beyond an Alpha voice/callsign comparison.

## Decision

Describe the feature as a voice/callsign identity gate scoped to one Callsign session. It reduces accidental or unauthorized activation but does not replace Windows authentication.

## Consequences

- UX and marketing avoid high-assurance biometric claims.
- Thresholds, false accepts/rejects, and replay remain explicit risks.
- No privilege elevation follows gate success.
- Sensitive actions remain blocked or policy-gated.
- Stronger assurance requires a separate security design and evaluation.
