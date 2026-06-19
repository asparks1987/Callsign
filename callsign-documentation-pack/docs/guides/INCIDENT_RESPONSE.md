# Incident Response

## Examples

- Wake/identity bypass.
- Unauthorized action.
- Hidden action after overlay failure.
- Sensitive data in logs/export/network traffic.
- Malicious or tampered artifact/model.
- IPC abuse.
- Installer persistence or uninstall failure.
- Policy/audit bypass.
- Vulnerable bundled dependency.

## Immediate actions

1. Stop affected runtime/release distribution.
2. Preserve minimal relevant evidence securely.
3. Avoid collecting unrelated user data.
4. Revoke signing/update material if implicated.
5. Identify affected versions and capability.
6. Prepare a safe disable/rollback path.
7. Communicate through private security channels.

## Triage

Assess:

- User interaction.
- Privilege.
- Scope and persistence.
- Data classes.
- Remote/local exploitability.
- Safety-control bypass.
- Reproducibility.
- Existing mitigations.
- Whether current artifacts must be removed.

## Remediation

- Add failing regression test first.
- Fix at the correct boundary.
- Review adjacent capabilities.
- Update threat model and ADRs.
- Produce new signed artifact.
- Publish clear mitigation and upgrade guidance.
- Do not downplay uncertainty.

## Evidence privacy

Incident bundles may be especially sensitive. Use synthetic reproduction where possible, encrypt transfers, restrict access, and delete data according to the incident retention plan.
