# Release Process

## 1. Freeze scope

- Name exact release.
- Confirm canon and exit criteria.
- Defer unfinished later-release capability explicitly.
- Require owners for every blocker.

## 2. Clean source

- Clean worktree.
- No duplicate/stale canonical docs.
- No secrets, personal audio, private assets, or generated drift.
- Dependency/model manifest complete.
- Version set once.

## 3. Build

```powershell
.\buildcallsign.ps1
```

Capture logs, commit, environment, and artifact hashes.

## 4. Test

- Static/contract.
- Smoke.
- Packaged payload.
- Installed automated checks.
- Manual voice walkthrough.
- Negative/security.
- Accessibility.
- Clean VM/user.

## 5. Verify artifacts

- Payload contents.
- Model/runtime hashes.
- Third-party notices.
- Signature status.
- Version consistency.
- Offline behavior.
- Install/reinstall/uninstall.
- Rollback/recovery.

## 6. Documentation

- Canon.
- README.
- Product/release specs.
- Security/privacy.
- Known limitations.
- Support/version table.
- Release notes.
- Generated site.
- Burndown evidence links.

## 7. Approval

Required sign-offs:

- Product scope.
- Runtime.
- Packaging.
- Security/privacy.
- Accessibility.
- Evidence/release.

## 8. Publish

- Immutable artifacts.
- Checksums/signatures.
- Release notes.
- Evidence summary.
- Known issues.
- Supported matrix.
- Upgrade/uninstall instructions.

## 9. Post-release

- Monitor reports.
- Preserve build/evidence manifests.
- Triage security issues privately.
- Do not silently replace artifacts.
- Publish revocation/rollback if necessary.
