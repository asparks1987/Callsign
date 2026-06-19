# File Search

## Release

`v1.3 alpha`

## Promise

Search approved local scopes by file or folder name/path, show results visibly, and open or select the chosen result in Windows Explorer.

## Privacy boundary

Initial file search:

- Does not read file contents.
- Does not summarize files.
- Does not upload files or paths.
- Does not modify or delete files.
- Does not execute a result.
- Does not search unapproved roots.

Paths themselves are sensitive and must be redacted in logs.

## Search roots

Default roots must be explicit, reviewable, and configurable. Potential defaults:

- Desktop.
- Documents.
- Downloads.
- User-selected folders.

Exclude by default:

- Credential stores.
- Browser profiles.
- System directories.
- Other users' profiles.
- Application secrets.
- Hidden/system areas unless explicitly added.

## Ranking

- Prefer exact name and prefix matches.
- Use packaged `fzf` only through a bounded local invocation.
- Provide an in-process fallback.
- Limit result count and traversal time.
- Avoid following reparse points outside approved roots.
- Detect inaccessible directories without aborting the full search.
- Make case, Unicode, and extension behavior deterministic.

## Result UX

- Show name, type, parent location, and modified time.
- Do not show more path detail than needed.
- Require selection when ambiguity is material.
- For files, select in Explorer.
- For folders, open in Explorer.
- Never invoke the file's default application as the search action.

## Security

- Normalize and canonicalize roots and results.
- Prevent traversal and symlink/reparse escapes.
- Run as the current user.
- Do not elevate.
- Respect cancellation.
- Bound CPU, memory, results, and duration.
- Never interpolate speech into a shell command.

## Tests

- Temporary approved root fixture.
- Exact and fuzzy matches.
- No result.
- Multiple result picker.
- Reparse point escape.
- Permission denied.
- Long paths and Unicode.
- Removable/network drive behavior.
- `fzf` unavailable.
- Explorer failure.
- No content access assertion.
