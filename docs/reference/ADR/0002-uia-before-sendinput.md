# ADR-0002: UI Automation before SendInput

## Status

Accepted for MVP.

## Context

Windows tasks can be automated through semantic accessibility APIs or through raw simulated input. Raw input is less reliable and less inspectable.

## Decision

Callsign must attempt Windows UI Automation or native APIs before using SendInput.

## Rationale

- UI Automation exposes control names, types, automation IDs, and supported patterns.
- Semantic actions are easier to validate and audit.
- Coordinate clicking is fragile across monitors, scaling, layouts, and themes.
- SendInput can fail across integrity boundaries.

## Consequences

- Every action should re-resolve its target before execution.
- Coordinate-based input is fallback only.
- Tests should prefer UIA fixtures and semantic selectors.
