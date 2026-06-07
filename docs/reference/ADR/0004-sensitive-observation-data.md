# ADR-0004: Treat observation data as sensitive

## Status

Accepted for MVP.

## Context

Window titles, UI trees, screenshots, clipboard contents, and file paths may contain private or secret data.

## Decision

Observation data is sensitive by default.

## Rationale

- Screenshots may contain emails, documents, account pages, or private messages.
- Clipboard contents may contain passwords or tokens.
- UI trees may include form values.
- File paths may reveal personal information.

## Consequences

- Screenshot sharing with cloud models is off by default.
- Clipboard content reading is disabled by default.
- UI tree extraction is bounded.
- Audit logs use redacted arguments by default.
