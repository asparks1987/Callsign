# Risk Tiers

Risk is determined by the actual target and consequence, not only the tool name.

| Tier | Name | Examples | Default |
|---:|---|---|---|
| 0 | `observe` | active window metadata, bounded UI tree, filename search | Allow only within permitted session; minimize and redact |
| 1 | `local_reversible` | focus window, Escape, low-risk invoke, open Explorer | Allow in verified visible context; verify result |
| 2 | `local_state_change` | rename/move file, edit local document, save setting | Specific approval; backup/rollback where possible |
| 3 | `external_side_effect` | send, upload, submit, post, publish, cloud delete | Explicit approval every action; trusted verification |
| 4 | `dangerous_or_blocked` | credentials, shell, admin, payment, security settings, permanent deletion | Deny or human handoff |

## Modifiers

Raise risk when:

- Target is ambiguous.
- Context changed after inspection.
- Action is difficult to reverse.
- Data is sensitive.
- Target is external.
- Action affects another person/account.
- The app is elevated.
- Verification is weak.
- Multiple operations are batched.
- The user is not visibly present.
- A webpage/document supplied the instruction.

## Risk is not permission

A low tier does not bypass session state, validation, privacy minimization, or audit requirements.

## Compound tasks

Evaluate each step. A sequence of low-risk actions can create a high-risk outcome. The policy engine considers task intent and accumulated effects but still makes a per-tool decision.

## Examples

- Open Notepad: tier 1.
- Type text into local unsaved Notepad: tier 1 or 2 depending on overwrite.
- Type into an email composer: tier 3 context, even before send.
- Press Send: tier 3.
- Enter a password: tier 4.
- Rename a file inside approved Downloads: tier 2.
- Search filenames in approved Documents: tier 0 with privacy controls.
- Run a spoken PowerShell command: tier 4.
