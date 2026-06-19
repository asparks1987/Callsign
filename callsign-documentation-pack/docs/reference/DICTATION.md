# Dictation

## Release

`v1.1 alpha`

Partial implementation may exist before this release, but it remains experimental until this specification's exit criteria are met.

## Promise

Capture speech into a visible, editable review surface. The user decides whether to copy, paste, insert, save, or discard it.

## Modes

- UI-started dictation.
- Voice request that opens or focuses the visible dictation surface.
- Future hands-free handoff through authenticated local IPC.

The service must not silently type into the active application merely because the user said `dictate`.

## Capture

- Show active capture state.
- Support stop, cancel, silence timeout, and provider failure.
- Bound capture duration and transcript size.
- Show incremental or final transcript with clear status.
- Keep a local provider/cloud provider disclosure visible.
- Do not retain raw audio by default.

## Review

The review surface supports:

- Edit.
- Select all.
- Copy.
- Explicit paste/insert.
- Clear.
- Cancel/discard.
- Optional punctuation hints.

A failed paste leaves the text in the review surface.

## Target safety

Before inserting:

- Resolve the target element semantically when possible.
- Reject password, 2FA, payment, wallet, and credential fields.
- Treat browser pages, messages, forms, and external communication as higher risk.
- Require explicit approval for any external submission; insertion alone must not trigger submit.
- Verify the focused element immediately before transfer.
- Avoid clipboard use when direct semantic set-text is available.
- Clear sensitive temporary clipboard data only under a separately accepted design.

## Privacy

Transcripts may contain sensitive information. Logs store lengths, provider, duration, and error codes—not full text by default.

## Acceptance criteria

- Dictation starts and stops predictably.
- Text is visible before transfer.
- Cancel discards the session.
- Copy and explicit paste work.
- Unsupported/sensitive targets are blocked.
- No hidden submit or send occurs.
- Provider failure preserves user control.
- Screen-reader users can operate the complete flow.
