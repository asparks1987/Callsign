# ADR-0005: Separate `v1.0 alpha` from the Alpha v1 family

## Status

Accepted by this documentation pack; maintainer ratification required.

## Context

Existing documents alternately define Alpha readiness as the first app-launch release and as the complete app-launch, dictation, browser, and file-search family.

## Decision

`v1.0 alpha` is the current release target. `Alpha v1` is the family `v1.0` through `v1.3`. Later partial implementations do not expand the `v1.0` exit gate.

## Consequences

- Release readiness becomes testable and narrow.
- Roadmap language remains intact.
- Burndown tasks carry a release field.
- Marketing must not call the complete Alpha family shipped when only `v1.0` is verified.
