# Callsign Documentation

This directory is the documentation home for Callsign.

## Start here

| Audience | Start with |
|---|---|
| New user or tester | [Getting started](guides/GETTING_STARTED.md) |
| Contributor | [Development guide](guides/DEVELOPMENT.md) |
| Product reviewer | [Product specification](reference/PRODUCT_SPEC.md) |
| Runtime engineer | [Architecture](reference/ARCHITECTURE.md) |
| Voice engineer | [Voice pipeline](reference/VOICE_PIPELINE.md) |
| Security reviewer | [Security model](reference/SECURITY_MODEL.md) |
| Release manager | [Release process](guides/RELEASE_PROCESS.md) and [burndown](../burndown.md) |
| Coding agent | [AGENTS.md](../AGENTS.md) |

## Canonical sources

- Root [CANON.md](../CANON.md) governs product promises and terminology.
- `docs/reference/` contains canonical product and engineering specifications.
- Root [burndown.md](../burndown.md) is the canonical execution checklist.
- `docs/pages/` is generated static HTML and must not be edited directly.

## Current release family

- `v1.0 alpha`: wake, voice/callsign gate, overlay/readout, visible installed-app launch.
- `v1.1 alpha`: visible dictation review.
- `v1.2 alpha`: bounded browser control.
- `v1.3 alpha`: policy-gated system workflows and Explorer-backed file search.
- Beta or later: hardened distribution, support, policy-gated automation, and any transparent tier packaging.

## Documentation sections

### Product and UX

- [Product specification](reference/PRODUCT_SPEC.md)
- [Release ladder](reference/RELEASE_LADDER.md)
- [Voice UX](reference/VOICE_UX.md)
- [Overlay UX](reference/OVERLAY_UX.md)
- [Onboarding](reference/ONBOARDING.md)
- [Accessibility](reference/ACCESSIBILITY.md)

### Features

- [Voice enrollment](reference/VOICE_ENROLLMENT.md)
- [App launch](reference/APP_LAUNCH.md)
- [Dictation](reference/DICTATION.md)
- [Browser control](reference/BROWSER_CONTROL.md)
- [File search](reference/FILE_SEARCH.md)
- [System control](reference/SYSTEM_CONTROL.md)

### Architecture and contracts

- [Architecture](reference/ARCHITECTURE.md)
- [Voice pipeline](reference/VOICE_PIPELINE.md)
- [Session state machine](reference/SESSION_STATE_MACHINE.md)
- [Windows automation](reference/WINDOWS_AUTOMATION.md)
- [MCP tools](reference/MCP_TOOLS.md)
- [Data model](reference/DATA_MODEL.md)
- [Configuration](reference/CONFIGURATION.md)
- [Schemas](../schemas/)

### Safety and privacy

- [Security model](reference/SECURITY_MODEL.md)
- [Threat model](reference/THREAT_MODEL.md)
- [Privacy model](reference/PRIVACY_MODEL.md)
- [Policy engine](reference/POLICY_ENGINE.md)
- [Risk tiers](reference/RISK_TIERS.md)
- [Audit logging](reference/AUDIT_LOGGING.md)

### Build, test, and operations

- [Test plan](reference/TEST_PLAN.md)
- [Deployment](reference/DEPLOYMENT.md)
- [Observability](reference/OBSERVABILITY.md)
- [Troubleshooting](guides/TROUBLESHOOTING.md)
- [Incident response](guides/INCIDENT_RESPONSE.md)
- [Release process](guides/RELEASE_PROCESS.md)

### Planning and decisions

- [Roadmap](reference/ROADMAP.md)
- [Open questions](reference/OPEN_QUESTIONS.md)
- [ADRs](reference/ADR/README.md)
- [Burndown](../burndown.md)

## Editing

See [Docs workflow](guides/DOCS_WORKFLOW.md). Update canonical Markdown first, regenerate the static site, and fail the change if generated output drifts.
