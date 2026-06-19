# Governance

## Decision model

Callsign currently uses maintainer-led governance. Decisions should be documented, reviewable, and reversible where possible.

## Decision classes

- **Editorial:** wording or organization with no behavioral effect.
- **Product:** release promise, scope, terminology, pricing, or user experience.
- **Architecture:** process boundary, dependency, protocol, schema, storage, or platform.
- **Safety/privacy:** trust boundary, data handling, risk classification, approval, audit, or blocked behavior.
- **Release:** readiness, supported platform, distribution, signing, update, or rollback.

Product, architecture, safety/privacy, and release decisions require an ADR or explicit canon update.

## Required reviewers

Until maintainers publish a formal ownership map:

- Runtime changes require a runtime maintainer.
- Installer/service changes require a Windows packaging reviewer.
- Voice or identity changes require a privacy/security reviewer.
- Automation/policy changes require a safety reviewer.
- Release claims require an evidence reviewer.
- Documentation-only changes still require the owner of the affected contract.

## Conflict resolution

Use the precedence order in [CANON.md](CANON.md). When two accepted decisions conflict, do not choose whichever is easier to implement; open an ADR that explicitly supersedes one.

## Transparency

Public release claims, feature gates, telemetry, cloud data flow, and pricing must be documented in plain language. Private business material can remain private, but public behavior cannot depend on undisclosed safety or data rules.
