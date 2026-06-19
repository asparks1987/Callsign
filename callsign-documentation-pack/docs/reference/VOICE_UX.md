# Voice UX

## Experience goal

Callsign should feel calm, direct, premium, and impossible to mistake for a hidden process.

## `v1.0` happy path

1. User says `Callsign`.
2. Overlay appears without stealing focus.
3. Readout: `Callsign heard. Say your callsign.`
4. User says the active callsign.
5. Readout: `Identity confirmed. Say the app name.`
6. User says `Open Notepad`.
7. Readout: `Opening Notepad through Start.`
8. Visible launch occurs.
9. Readout briefly shows completion, then the overlay closes.

## Language rules

Prefer:

- `Listening for Callsign`
- `Say your callsign`
- `Identity did not match`
- `Say the app name`
- `I found more than one match`
- `Session cancelled`
- `Wake runtime needs repair`

Avoid:

- Implying certainty the system does not have.
- Calling the voice gate a password or secure biometric.
- Long paragraphs during an active session.
- Hiding the target or action.
- Blaming the user for microphone or model failures.
- Saying an action succeeded before verification.

## Turn boundaries

- Wake, identity, command, and confirmation are distinct turns.
- The identity phrase is never parsed as a command.
- A command uttered too early is ignored and the UI explains why.
- Confirmation repeats the exact target and consequence.

## Recovery copy

| Condition | Recommended message |
|---|---|
| Wrong or low-confidence identity | `I couldn't confirm this profile. Try again.` |
| Timeout | `The session timed out. Say Callsign to start over.` |
| Missing enrollment | `This profile needs voice enrollment before listening.` |
| Microphone missing | `No usable microphone is available. Open Audio settings.` |
| Ambiguous app | `I found multiple matches. Choose one in Callsign.` |
| Unsupported request | `That action is not supported in this release.` |
| Cancellation | `Session cancelled. Nothing was opened.` |

## Stop behavior

Voice phrases include `stop`, `stop now`, `cancel`, and `pause`. A keyboard or UI stop control remains available.

Stop must feel immediate. The product should not wait for a model response before cancelling local work.

## Privacy cues

- Show when the microphone is actively processing.
- Distinguish always-on wake detection from post-wake transcription.
- Explain whether a provider is local or cloud.
- Do not show raw confidence scores to ordinary users without context.
- Provide a clear link to enrollment data and deletion controls.

## Accessibility

See [ACCESSIBILITY.md](ACCESSIBILITY.md). Voice cannot be the only way to configure, stop, recover, or understand the product.
