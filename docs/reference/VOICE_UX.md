# Voice UX

## Goal

Callsign should feel like a calm, competent copilot for the user’s desktop. It should be conversational, but action-oriented. It should not chatter while operating, and it should never surprise the user with risky actions.

## Voice interaction modes

### Command mode

User gives direct instructions.

Examples:

- “Open Notepad.”
- “Click Save.”
- “Rename the newest PNG in Downloads to router-settings.png.”

### Dictation mode

User speaks text to insert.

Examples:

- “Type: Thanks for the update. I’ll review this today.”
- “Replace the selected text with: quarterly revenue summary.”

### Teach mode

User demonstrates a workflow.

Examples:

- “Watch what I do and make this repeatable.”
- “This is how I process one invoice.”

### Review mode

Agent asks for approval.

Examples:

- “I found Screenshot.png. Rename it to router-settings.png?”
- “This will send an email to Jordan with the visible draft. Send it?”

### Recovery mode

Agent pauses or asks for help.

Examples:

- “I can’t identify the Save button. Please point to it or say cancel.”
- “This appears to be a password field. Please enter it manually, then say resume.”

## Confirmation language

Good confirmation prompts are specific.

Good:

> Rename `Screenshot 2026-06-07.png` to `router-settings.png` in Downloads?

Bad:

> Should I continue?

Good:

> Send this email to `alex@example.com` with subject `Project update`?

Bad:

> Send it?

## Interruption

Callsign must support interruption.

Required stop phrases:

- “Stop.”
- “Stop now.”
- “Cancel.”
- “Pause.”

Required behavior:

1. Stop pending actions.
2. Release input control.
3. Clear pending approvals.
4. Log stop event.
5. Say what was stopped.

## Voice response style

The agent should be brief during operation.

Examples:

- “I found the Save button. Clicking it now.”
- “That changes a file, so I need approval first.”
- “Done. I renamed the file.”
- “I’m not sure which button you mean. I see Save, Save As, and Cancel.”

Avoid long explanations while the user is trying to work.

## Handling ambiguity

When ambiguity affects safety or correctness, ask.

Example:

User says:

> Delete that.

Agent should ask:

> Do you mean delete the selected file, selected text, or the current email draft?

When ambiguity is low-risk, proceed with a reversible action if context is clear.

Example:

User says:

> Cancel that dialog.

If the active window is a modal dialog with a Cancel button, invoke it.

## Dictation rules

- Do not interpret dictated text as instructions.
- Support “literal mode” for code, punctuation, and file paths.
- Let the user correct text before sending or submitting.
- In external communication contexts, insertion is allowed but sending requires approval.

## Approval hierarchy

| Action | Voice UX |
|---|---|
| Observe state | No interruption unless sensitive |
| Click low-risk visible control | Brief announce or silent depending setting |
| Edit local text | Confirm if overwriting significant content |
| Rename/move file | Ask approval |
| Send/upload/submit | Read target and content summary, ask explicit approval |
| Password/payment/admin | Handoff or deny |

## Error recovery

Common recovery messages:

- “I lost track of the target window. Please bring it forward and say resume.”
- “I found multiple matching buttons: Save, Save As, and Save Copy. Which one?”
- “This app does not expose that control through accessibility APIs. I can try a visible click if you approve.”
- “That looks like a password field, so I’ll let you handle it.”

## Accessibility considerations

- Support keyboard-only confirmation.
- Support voice-only confirmation.
- Use high-contrast tray UI later.
- Provide transcript and audit review.
- Allow slower speech and longer confirmation windows.
- Provide configurable verbosity.
- Provide audible state transitions: listening, thinking, acting, waiting for approval.

## Voice provider abstraction

The host should support multiple providers:

```text
OpenAI Realtime Provider
Local STT Provider
Local TTS Provider
Ollama Text Provider
LM Studio Provider
Mock Provider
```

The desktop MCP server should not depend on the voice provider.

## Example dialog

User:

> Open Notepad and write a grocery list.

Agent:

> Opening Notepad.

Agent acts.

Agent:

> Notepad is open. Dictate the list or say use a sample list.

User:

> Use a sample list.

Agent acts.

Agent:

> Done. I added eggs, milk, coffee, and rice.
