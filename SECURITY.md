# SECURITY.md

## Security posture

Callsign is a local desktop automation project. That makes safety and security part of the core product, not an afterthought.

The MVP should be treated as experimental software and should not be used on machines containing high-risk secrets, production credentials, financial accounts, sensitive medical/legal documents, or business-critical unattended workflows until the security model has been implemented and independently reviewed.

## Report a vulnerability

Until a formal disclosure channel exists, open a private security advisory in the repository or contact the maintainers through the repository owner profile.

Please include:

- Affected component.
- Steps to reproduce.
- Expected impact.
- Whether user approval is bypassed.
- Whether sensitive data can be read, written, sent, or deleted.
- Suggested mitigation, if known.

## MVP blocked areas

The project should block or hand off:

- Password entry.
- 2FA entry.
- Payment and purchase flows.
- Credential manager access.
- Browser password store access.
- Crypto wallet access.
- Arbitrary shell or PowerShell execution.
- UAC/admin prompts.
- Permanent file deletion.
- Silent network uploads.
- Silent email/message sending.
- Hidden or minimized-window action.

## Security design documents

See:

- `docs/reference/SECURITY_MODEL.md`
- `docs/reference/THREAT_MODEL.md`
- `docs/reference/MCP_TOOLS.md`
- `docs/reference/ADR/0003-policy-engine-required.md`
