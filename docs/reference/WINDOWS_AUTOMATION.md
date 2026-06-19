# Windows Automation Strategy

## Goal

Callsign automates the desktop only in ways the user can understand and stop.

The v1.0 alpha path is deliberately visible:

1. Service hears `Callsign`.
2. Service verifies the user's callsign.
3. User asks for an installed app.
4. Callsign opens Start search.
5. Callsign types or resolves the app name.
6. Callsign launches the matching app visibly.

All Alpha v1 features are free and remain free until at least beta.

## v1.0 strategy

- Use the Start menu search experience for app launching.
- Resolve installed app names and safe shell-backed destinations.
- Keep the app launch target visible in the UI and service status.
- Let the user cancel or reset the session.
- Reject paths, URLs, shell fragments, secrets, unsafe text, and non-launch automation.
- Avoid arbitrary shell execution.

## Automation priority order

Prefer action methods in this order:

1. Native app/API operation.
2. Windows UI Automation pattern.
3. Saved selector.
4. Vision/OCR-assisted target.
5. SendInput keyboard/mouse fallback.
6. Human handoff.

Raw coordinate clicking is a last resort and must include verification.

## v1.x direction

The repo already contains early services for broader control surfaces:

- browser launch/open/search helpers
- system control helpers
- file search and open/reveal helpers
- command routing for visible UI controls

These are the path toward:

- v1.1 dictation with visible review
- v1.2 browser control
- v1.3 Windows, WSL, Linux, and file search control

They should stay behind identity, policy, approval, visibility, and audit gates.

## Extension-library direction

Future Pro and Advanced tiers may add closed-source automation libraries.

Those libraries can expand command coverage, but they must not:

- bypass the policy engine,
- suppress audit logs,
- perform hidden actions by default,
- handle credentials or payment data,
- or execute arbitrary shell text as a shortcut.

## Safety rules

- Do not rely on hidden windows.
- Do not use background-only automation for the v1.0 launch path.
- Do not use raw coordinates when a visible semantic path is available.
- Do not bypass wake word, callsign identity, policy, approval, or audit.
- Do not send screenshots, UI trees, clipboard contents, or file contents to cloud models unless the user explicitly opts in.
