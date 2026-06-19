# Privacy Review Guide

For every new feature, answer:

## Data

- What is collected, generated, inferred, stored, displayed, logged, exported, or transmitted?
- Is it audio, transcript, biometric-like data, path/UI content, identifier, or telemetry?
- Is collection necessary?

## Flow

- Source.
- In-memory processing.
- Local storage.
- Process/IPC boundaries.
- Network/provider.
- Retention.
- Deletion.
- Export/support.

## User control

- Notice.
- Consent/opt-in.
- Disable.
- Inspect.
- Delete.
- Provider choice.
- Non-cloud fallback.

## Security

- ACL/encryption.
- Schema validation.
- Redaction.
- Access logging.
- Abuse scenarios.
- Incident plan.

## Documentation

Update privacy model, data model, storage/retention, threat model, configuration, UI copy, tests, and burndown.

A feature is not privacy-reviewed because it says “local-first”; actual data flow decides.
