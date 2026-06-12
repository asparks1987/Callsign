# Roadmap

## Phase 0: Public alpha shell

Status: current focus.

Deliverables:

- Public GitHub Pages landing page.
- Clear open-source and freemium product story.
- Windows setup and onboarding UI.
- Account creation and profile storage.
- Voice enrollment state.
- Visible app launch flow.

## Phase 1: Always-on voice service

Deliverables:

- Background service process.
- Wake word listener.
- Callsign identity gate.
- Session timeout and lockout.
- Visible stop and reset controls.

Exit criteria:

- The service can sit in the background and wait for the user.
- The service does not launch anything until identity is confirmed.

## Phase 2: Better desktop launch and control

Deliverables:

- More reliable Start menu launch handling.
- Visible launch feedback.
- App launch history.
- Better error recovery.

Exit criteria:

- Common installed apps launch reliably from the visible flow.

## Phase 3: Broader desktop automation

Deliverables:

- Safer UI automation.
- Text entry helpers.
- File workflow support.
- Approval prompts for state-changing tasks.

Exit criteria:

- The assistant can handle routine desktop work without becoming hidden or fragile.

## Phase 4: Product expansion

Potential additions:

- Freemium premium features.
- Better voice models.
- More identity options.
- Workflow memory.
- Recipe-style automation.

## Platform roadmap

- Windows first.
- Linux support later, after the Windows alpha is stable and understandable.

## Roadmap principles

- Ship visible behavior before broad automation.
- Keep the core experience usable without a subscription.
- Do not add hidden power before trust.
- Expand only after the current alpha is reliable.

