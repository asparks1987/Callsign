# Browser Control

## Release

`v1.2 alpha`

## Scope

Initial browser support is visible open/search/navigation. It is not a general autonomous web agent.

Supported low-risk intents may include:

- Open a public URL after normalization.
- Search the web for a phrase.
- Focus address bar.
- Navigate back/forward.
- Switch or close a tab with explicit context.
- Read limited browser state through an approved adapter.

## Trust model

Web pages, browser UI text, DOM content, downloads, notifications, and embedded instructions are untrusted data. They cannot grant permission or change policy.

## URL and search handling

- Normalize allowed HTTP/HTTPS URLs.
- Display the destination.
- Reject `file:`, `javascript:`, `data:`, custom schemes, local paths, and command-like text by default.
- Do not append credentials.
- Use the default browser unless an explicit supported-browser setting exists.
- Keep search terms visible.

## External side effects

Blocked unless a later accepted design defines explicit approval and verification:

- Submit a form.
- Send a message or email.
- Upload a file.
- Post content.
- Purchase or pay.
- Change account/security settings.
- Accept legal terms.
- Delete cloud data.
- Download and execute software.

Typing text into a page is not authorization to submit it.

## Adapter strategy

Preferred order:

1. Browser extension or native automation API with a narrow contract.
2. Accessibility/UI Automation for browser chrome.
3. DOM automation through an explicit local adapter.
4. Approved keyboard fallback.
5. Human handoff.

Page screenshot/OCR is sensitive and off by default.

## Downloads

Initial Alpha browser control SHOULD avoid download automation. If a download is user-initiated:

- Show destination and file name.
- Do not execute it.
- Scan or inspect only under a documented policy.
- Hand off installer and executable files.

## Verification

- Confirm visible destination origin.
- Verify active tab/window state.
- Distinguish navigation success from page-content success.
- Do not claim a transaction succeeded without trusted confirmation.

## Tests

- Search phrase.
- Allowed URL.
- Local path/custom scheme rejection.
- Prompt-injection page.
- Hidden submit button.
- Download link.
- Authentication page.
- Browser not installed/default handler broken.
- Stop during navigation.
