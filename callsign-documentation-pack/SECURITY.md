# Security Policy

## Security posture

Callsign is experimental desktop-control software. It combines continuous microphone access, local identity state, process execution, and future automation capabilities. Treat every new capability as a security boundary.

Do not use an unreviewed build on a machine containing high-risk credentials, financial workflows, sensitive legal or medical records, production secrets, or unattended business-critical automation.

## Supported versions

No version is currently declared production-supported by this documentation pack. Publish a supported-version table when releases have signed artifacts, update policy, and maintained security branches.

## Reporting a vulnerability

Use the repository's private security-advisory mechanism or the maintainer's published private contact channel. Do not file a public issue for an unpatched vulnerability.

Include:

- Affected version, commit, and component.
- Environment and installation mode.
- Reproduction steps.
- Whether wake, identity, policy, approval, visibility, or audit controls are bypassed.
- Data classes exposed.
- User interaction required.
- Persistence and cross-user impact.
- Suggested mitigation, if known.

Do not include real credentials, raw voice samples, or unrelated personal data.

## Blocked classes

The Alpha design blocks or hands off:

- Password and 2FA entry.
- Payment, purchase, wallet, or money movement.
- Credential-store access.
- Arbitrary shell, PowerShell, WSL, command, or script execution.
- UAC/admin elevation.
- Permanent deletion.
- Security-setting changes.
- Hidden external submissions.
- Silent email, message, upload, or posting.
- Stealth, evasion, surveillance, persistence abuse, or remote administration.

## Safe testing

- Use test profiles and disposable files.
- Use a VM or non-sensitive Windows user for installed automation tests.
- Redact paths, names, transcripts, and identifiers from evidence.
- Never upload a real user's raw enrollment audio to an issue.
- Test denied behavior explicitly.
- Stop immediately if an action escapes the intended visible boundary.

## Security documentation

- [Security model](docs/reference/SECURITY_MODEL.md)
- [Threat model](docs/reference/THREAT_MODEL.md)
- [Privacy model](docs/reference/PRIVACY_MODEL.md)
- [Policy engine](docs/reference/POLICY_ENGINE.md)
- [Risk tiers](docs/reference/RISK_TIERS.md)
- [Security review checklist](docs/checklists/SECURITY_REVIEW.md)
