# ADR-0002: UI Automation before SendInput

## Status

Accepted.

## Context

Semantic Windows accessibility APIs expose identity and supported patterns. Simulated input is focus-sensitive, fragile across display conditions, and difficult to verify.

## Decision

Use native application/Windows APIs and UI Automation before SendInput. Coordinates are last-resort fallback.

## Consequences

- Re-resolve semantic targets immediately before action.
- Verify process, window, element, and pattern.
- Use bounded tree extraction.
- Elevated/inaccessible targets hand off.
- Fallback keys/clicks require context, cancellation, and postcondition verification.
- Test fixtures prefer semantic selectors.
