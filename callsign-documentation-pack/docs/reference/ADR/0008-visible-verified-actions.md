# ADR-0008: Visible, verified actions before capability breadth

## Status

Accepted by this documentation pack; maintainer ratification required.

## Context

Desktop agents can appear powerful by adding many brittle actions. Callsign's trust depends on visibility, stoppability, and truthful results.

## Decision

A capability is not release-ready until it has a visible target, cancellation path, bounded execution, structured failure, and postcondition verification.

## Consequences

- `v1.0` remains narrowly focused on app launch.
- Sending input is not success.
- Hidden fallback cancels or hands off.
- Burndown exit criteria emphasize evidence, not feature count.
- Future tools must define verification before implementation.
