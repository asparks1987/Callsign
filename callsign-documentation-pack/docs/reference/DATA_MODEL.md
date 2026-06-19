# Data Model

## Goals

- Small, local, versioned records.
- No secrets.
- Atomic writes.
- Explicit separation of profile, enrollment, runtime, and audit data.
- Migrations that fail safely.

## Directory model

Recommended:

```text
%LOCALAPPDATA%\Callsign\
  config\
    appsettings.json
  profiles\
    <stable-profile-id>\
      profile.json
      enrollment\
        manifest.json
        embedding.bin
        samples\
  runtime\
    snapshot.json
    service.pid
  logs\
    runtime-YYYYMMDD.jsonl
    audit-YYYYMMDD.jsonl
  cache\
    models\
  exports\
```

Use a stable validated profile ID for paths. The user-facing callsign can change.

## Profile

Canonical schema: `schemas/profile.schema.json`.

Fields include:

- Schema version.
- Stable ID.
- Callsign.
- Display name.
- Optional user-entered metadata.
- Active/enabled state.
- Voice enrollment summary.
- Created/updated timestamps.
- Preferences that are safe to sync only if a future design allows it.

Avoid storing email/department fields unless a real product requirement justifies them.

## Enrollment manifest

Suggested fields:

```json
{
  "schema_version": 1,
  "status": "enrolled",
  "sample_count": 3,
  "required_sample_count": 3,
  "model_id": "provider/model@version",
  "threshold_profile": "alpha-default-v1",
  "raw_samples_retained": false,
  "enrolled_utc": "2026-06-19T00:00:00Z"
}
```

Do not store embeddings or audio inline in JSON.

## Runtime snapshot

Canonical schema: `schemas/runtime-snapshot.schema.json`.

Snapshot data is observational. The service remains the authority for state transitions; editing the snapshot cannot authorize a session.

## Command intent

Canonical schema: `schemas/command-intent.schema.json`.

Intents are enumerated by release capability. Unknown intents fail closed.

## Policy decision

Canonical schema: `schemas/policy-decision.schema.json`.

Approvals are stored as scoped runtime records, not permanent broad grants.

## Audit event

Canonical schema: `schemas/audit-event.schema.json`.

Audit events are append-only, bounded, redacted, and do not include raw audio or full transcripts by default.

## Migration

- Every persisted object carries a schema version.
- Migrations are forward, tested, and backed up.
- Unsupported future versions open read-only recovery or fail visibly.
- Never overwrite the only copy before validation.
- Record migration outcome without copying sensitive content.
- A model/embedding incompatibility may require reenrollment rather than lossy conversion.

## Concurrency and atomicity

- Write temp file, flush, then atomically replace.
- Use one writer per record.
- Avoid holding file locks across UI interaction.
- Treat abrupt service termination as normal failure to recover from.
- Validate after read and before use.
