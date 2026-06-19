# Deployment and Packaging

## Current target

Windows `v1.0 alpha` installable payload containing the configuration UI, background runtime, desktop entry point, overlay assets, wake/runtime dependencies, and diagnostics.

## Build outputs

Documented names include:

- `Callsign-Setup.exe`
- `Callsign-Run.exe`
- `Callsign-Service.exe`
- `build/alpha-installer-payload.json`
- Optional readiness report under `build/`

The current build script and installer project are authoritative for exact paths.

## Artifact requirements

Each release artifact has:

- Version and commit.
- Build timestamp.
- Target runtime/architecture.
- SHA-256.
- Included payload manifest.
- Third-party notices.
- Wake/model identifiers and hashes.
- Signing status.
- Test evidence reference.

## Installer behavior

- Runs visibly.
- Explains microphone/listener behavior.
- Installs at normal user scope unless an accepted service design needs otherwise.
- Stages UI, runtime, assets, and dependencies atomically.
- Creates the documented shortcut.
- Starts or configures the runtime explicitly.
- Handles reinstall over a running version.
- Writes sanitized logs.
- Rolls back partial failure.
- Offers data-preserving upgrade.
- Separates uninstalling binaries from deleting user data.

## Service or fallback

The installed UI shows:

- Active mode.
- Process/service health.
- Version compatibility.
- Startup setting.
- Stop/start/restart controls where safe.
- Repair path.

A fallback process must not be hidden from the user simply because service registration failed.

## Signing

Before Beta:

- Sign installer and executable artifacts.
- Protect signing keys outside the repo.
- Verify signatures in release checks.
- Publish key/certificate rotation procedure.
- Do not present unsigned artifacts as production trusted.

## Updates

Alpha may use manual reinstall. A future updater requires:

- Signed update metadata and artifacts.
- Channel selection.
- Rollback.
- Schema/model migration.
- User-visible release notes.
- No silent privilege escalation.
- No silent download of executable content.
- Failure recovery and offline behavior.

## GitHub Pages

Canonical Markdown under `docs/reference/` is rendered into `docs/pages/` by the repository site builder.

```powershell
python scripts/build_site.py
python -m http.server 8000 -d docs
```

CI should regenerate and fail if the worktree changes.

## Platform matrix

Every release records:

- Windows editions/versions.
- x64/Arm64 status.
- .NET/runtime model.
- Service/fallback mode.
- Supported microphone APIs/devices.
- Display/DPI matrix.
- WSL version status.
- Language/locale status.

Unsupported does not mean impossible; it means untested and not promised.
