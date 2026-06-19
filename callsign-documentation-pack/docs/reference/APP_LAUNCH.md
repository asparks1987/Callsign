# Visible Application Launch

## Release

`v1.0 alpha`

## User intent

Supported examples:

- `Open Notepad`
- `Launch Calculator`
- `Start Visual Studio Code`

The parser extracts a bounded installed-application target. It does not accept command-line arguments.

## Validation

Reject:

- Absolute or relative filesystem paths.
- URLs and URI schemes.
- Shell metacharacters and separators.
- PowerShell, `cmd`, WSL, script, or terminal commands.
- Redirection and piping.
- Environment-variable expansion supplied by speech.
- Installer, uninstall, elevation, or security-setting intents.
- Credentials or secret-looking text.
- Targets beyond the configured maximum length.

## Resolution

Candidate sources may include:

- User Start menu shortcuts.
- System Start menu shortcuts.
- Approved packaged-app registrations.
- An allowlisted alias table.

Resolution MUST:

- Normalize case and punctuation.
- Preserve the original user-visible label.
- Use bounded fuzzy matching.
- Prefer exact/alias matches.
- Return ranked candidates and confidence.
- Avoid executing a raw path from the transcript.
- Detect ambiguity.

## Confirmation

No confirmation is required for one high-confidence, low-risk installed-app match when the product has already shown the target.

Require confirmation when:

- Multiple candidates are close.
- The match is below the approved confidence threshold.
- The target has a risk-sensitive name or behavior.
- The resolver would use a fallback path.

Confirmation names the exact app. Silence or an unrelated utterance cancels.

## Execution

The current product goal is a visible Start menu path. Implementation may index shortcuts internally, but the user must be able to understand which app is being opened.

Execution rules:

- Revalidate the candidate immediately before action.
- Do not append arguments.
- Use normal-user context.
- Do not elevate.
- Respect cancellation.
- Report the chosen adapter.
- Time out safely.

## Verification

Verify using one or more:

- Process/window appears with expected identity.
- Start menu state transitions.
- Foreground window matches the target.
- An app-specific readiness signal.

Do not report success merely because an input event was sent.

## Audit

Record:

- Correlation ID.
- Normalized intent class.
- Redacted target label.
- Resolver result and candidate count.
- Policy decision.
- Execution adapter.
- Verification result.
- Duration and structured error.

## Tests

- Exact common app.
- Alias.
- App not installed.
- Ambiguous candidates.
- URL/path/shell rejection.
- Cancellation before and during execution.
- Start menu unavailable.
- Elevated target.
- Multi-language and punctuation normalization.
- Verification false positive prevention.
