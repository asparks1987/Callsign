# System Control

## Release

`v1.3 alpha` and later

## Principle

System control is a curated set of typed capabilities, not a command interpreter.

## Candidate low-risk capabilities

Only after individual policy and test approval:

- Focus or switch a visible window.
- Invoke a semantic UI control.
- Set text in a verified non-sensitive field.
- Use an approved hotkey.
- Read limited system status.
- Open a settings page without changing it.
- Perform a reversible local action with explicit confirmation.

## Blocked Alpha capabilities

- Arbitrary shell, PowerShell, `cmd`, WSL command, or script execution.
- Registry editing.
- Service configuration beyond Callsign's own installer path.
- UAC/elevation.
- Security-setting changes.
- Firewall or antivirus changes.
- Driver/device modification.
- Account or permission changes.
- Permanent deletion.
- Credential operations.
- Software installation.
- Unattended background workflows.

## WSL and Linux boundary

“WSL support” does not imply passing arbitrary spoken text to a shell.

A future WSL/Linux adapter requires:

- Named, typed operations.
- Fixed executable/argument contracts.
- Working-directory allowlists.
- Path translation rules.
- No shell interpolation.
- Normalized output limits.
- Cancellation and timeout.
- Policy, audit, and verification.
- Separate support matrix.

## Capability lifecycle

1. Feature spec.
2. Threat/privacy review.
3. Tool schema.
4. Policy rule.
5. Adapter implementation.
6. Verification strategy.
7. Positive and blocked tests.
8. Manual evidence.
9. Release gate.
10. Deprecation and migration plan.

See [MCP_TOOLS.md](MCP_TOOLS.md) and [POLICY_ENGINE.md](POLICY_ENGINE.md).
