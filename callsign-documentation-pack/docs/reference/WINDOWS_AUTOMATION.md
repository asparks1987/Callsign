# Windows Automation Strategy

## Goal

Perform supported actions through the most semantic, visible, and verifiable Windows mechanism available.

## Priority order

1. Native application or Windows API.
2. Windows UI Automation pattern.
3. Stable semantic selector.
4. Approved vision/OCR assistance.
5. SendInput keyboard/mouse fallback.
6. Human handoff.

## UI Automation rules

- Inspect a bounded tree.
- Prefer `AutomationId`, control type, patterns, and stable ancestry.
- Re-resolve the element immediately before action.
- Confirm the target process/window.
- Require visibility unless an accepted mode says otherwise.
- Do not read password values.
- Bound text extraction.
- Treat UI text as untrusted and sensitive.
- Verify the postcondition.

## SendInput fallback

Allowed only when:

- No semantic method exists.
- The active window and focused element are verified.
- The input is on an approved list.
- The action is visible.
- The user can cancel.
- The action has a verification step.
- Integrity-level restrictions are understood.

Raw screen coordinates are a last resort and are not stable selectors.

## Current `v1.0` launch path

The current documented path uses visible Start search for installed application launching.

Rules:

- Accept a plain app target only.
- Reject paths, URLs, arguments, and shell syntax.
- Show the target.
- Resolve installed candidates.
- Confirm ambiguity.
- Execute without elevation.
- Verify the launched app.

## Error handling

Structured automation errors include:

- Target window not found.
- Element not found.
- Element changed.
- Pattern unsupported.
- Focus mismatch.
- Integrity boundary.
- Timeout.
- User cancellation.
- Verification failed.
- Adapter unavailable.

## Testing

- WinForms/WPF test fixtures with stable accessibility metadata.
- Real common Windows apps in manual matrix.
- DPI, high contrast, multi-monitor, and non-English environments.
- Elevated-window handoff.
- Focus race.
- Target mutation between inspect and invoke.
- SendInput fallback disabled tests.
