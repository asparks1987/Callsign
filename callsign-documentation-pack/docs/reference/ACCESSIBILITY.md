# Accessibility

## Principle

Callsign is accessibility-oriented, so accessibility is part of the product contract, not a post-Alpha polish task.

## Setup UI

- Complete keyboard operation.
- Logical tab order.
- Visible focus.
- Programmatic labels and descriptions.
- No color-only state.
- Support Windows text scaling and high contrast.
- Screen-reader announcements for recording, errors, enrollment progress, and service state.
- Large, forgiving controls for record/stop.
- Avoid time-limited setup steps unless extendable.

## Voice session

- Overlay state always has text.
- Stop/cancel is available by voice, keyboard, and UI.
- Audio prompts do not replace text.
- Readout uses plain language.
- Timeouts are configurable within safe bounds.
- Repeated recognition failure offers a non-voice recovery path.
- No critical action depends on precise pointer control.

## Hearing and speech differences

- Visual indication of listening and audio level.
- Text-first feedback.
- Adjustable prompt sound/volume or silent mode.
- Enrollment retries without penalty.
- Do not assume one accent, cadence, or speech pattern.
- Publish known model limitations and tested languages.

## Cognitive accessibility

- One decision per step.
- Stable terminology.
- Exact target and consequence.
- Clear current phase.
- Reversible setup.
- Avoid anthropomorphic certainty.
- Avoid technical error codes without a plain explanation.

## Motion and visuals

- Respect reduced-motion preference where available.
- Provide a static or low-motion overlay mode.
- Avoid flashing.
- Maintain contrast.
- Support multi-monitor and magnification tools.
- Do not steal focus.

## Testing matrix

- Keyboard-only.
- Narrator and at least one additional screen reader where feasible.
- High contrast.
- 200% text scaling.
- Reduced motion.
- Multiple displays and DPI.
- No audio output.
- No microphone.
- Speech recognition failures.
- Switch/alternative input review with users when possible.

## Conformance

Adopt a documented target such as WCAG for web docs and relevant Microsoft accessibility guidance for the Windows UI. Do not claim conformance until audited.
