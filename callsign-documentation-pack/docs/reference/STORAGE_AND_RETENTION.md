# Storage and Retention

## Storage roots

Callsign stores user data under `%LOCALAPPDATA%\Callsign\` by default. Installation binaries use the documented application install directory. Do not mix mutable user data with program files.

## Retention table

| Data | Default | User control |
|---|---|---|
| Profile settings | until profile deletion | view, edit, delete |
| Enrollment metadata | until reset/profile deletion | reset/delete |
| Raw enrollment samples | do not retain unless required/configured | inspect setting, delete |
| Derived speaker data | until reset/profile deletion/model migration | reset/delete |
| Command audio | memory only | none needed |
| Transcripts | memory/visible session only | clear/cancel |
| Runtime diagnostics | rotating local files | open, export, clear |
| Audit events | bounded local rotation | view/export/clear subject to safety |
| Crash dumps | off or local opt-in | preview/delete |
| Model cache | until update/uninstall/clear | inspect and clear |
| Installer manifest | keep with installed version | removed on uninstall |

## Protection

- Per-user ACLs.
- No secrets in filenames.
- OS-protected encryption considered for identity-derived data.
- Checksums for models and executable helpers.
- Atomic files.
- No silent network backup or sync.
- Warn when a user places profiles in a synced folder.
- Export is explicit and redacted.

## Rotation

Logs and audit files use:

- Maximum file size.
- Maximum file count or age.
- Atomic rollover.
- Retention enforcement at startup and periodically.
- A hard ceiling during repeated failure loops.
- Separate diagnostic and audit policies.

## Clear and uninstall

The UI should distinguish:

- Clear diagnostics.
- Clear action history.
- Reset enrollment.
- Delete profile.
- Remove cached models.
- Remove all Callsign user data.
- Uninstall binaries.

Deletion reports success/failure per data class. It must not claim secure erasure beyond what the filesystem can guarantee.

## Backups

Alpha does not promise profile sync or backup. If import/export is added:

- Version the package.
- Exclude secrets and unnecessary logs.
- Protect biometric-like enrollment data.
- Validate before import.
- Do not merge identity data across profiles implicitly.
