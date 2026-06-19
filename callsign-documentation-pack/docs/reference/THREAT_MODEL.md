# Threat Model

## Scope

Current Alpha profile, microphone, wake, identity, overlay, app-launch, dictation, browser, file-search, installer, and service behavior; plus the target local automation server.

## Assets

- User control of the desktop.
- Local profile and settings.
- Voice samples and derived identity data.
- Audio and transcripts.
- Session authorization state.
- Window, UI, path, clipboard, screenshot, and file metadata.
- Audit and diagnostic records.
- Release artifacts and update channel.
- Future policy and recipe data.

## Adversaries and failure actors

- Nearby person or replayed audio.
- Confused or ambiguous user.
- Malicious local process.
- Other local user.
- Prompt-injecting webpage/document/file name.
- Compromised model/provider.
- Tampered dependency/model/update artifact.
- Malicious or buggy tool adapter.
- Stale generated documentation.
- Contributor accidentally committing sensitive/private assets.

## Trust boundaries

1. Microphone to wake detector.
2. Wake detector to session state.
3. Identity input to active profile.
4. Transcript/model to bounded intent.
5. Host to automation server.
6. Policy to adapter.
7. Adapter to Windows/UIA/filesystem/browser.
8. Runtime to local storage.
9. Installer/update artifact to machine.
10. Local data to any network provider.

## Threat catalog

| ID | Threat | Primary mitigation |
|---|---|---|
| T-01 | Transcript text fakes wake | Detector event is the only wake transition |
| T-02 | Wrong speaker passes gate | Calibrated match, callsign phrase, attempts/lockout, clear assurance limits |
| T-03 | Replay audio | Treat as residual Alpha risk; research liveness before stronger claims |
| T-04 | Wake alone executes | State-machine invariant and tests |
| T-05 | Identity and command combined | Separate capture turns |
| T-06 | Prompt injection authorizes tool | Policy outside model; observed text is data |
| T-07 | Shell injection through app/path | Typed parser; no shell interpolation |
| T-08 | Path escapes approved root | Canonicalization, reparse checks, revalidation |
| T-09 | UI target changes after inspection | Re-resolve and verify immediately before action |
| T-10 | Hidden action after overlay failure | Cancel or visible fallback; fail closed |
| T-11 | Sensitive data in logs | Structured redaction, allowlisted fields, secret scanning |
| T-12 | Clipboard/screenshot leaks | Disabled/minimized by default; explicit opt-in |
| T-13 | Malicious local IPC client | Per-user authenticated IPC and session binding |
| T-14 | Elevated target bypass | No elevation; handoff |
| T-15 | Bundled model/runtime tampering | Hash manifest, signing, provenance |
| T-16 | Silent dependency repair | Visible pinned repair flow |
| T-17 | Stale approval reused | Scoped, one-time, expiring approvals |
| T-18 | Late callback acts after cancel | Cancellation token and state/correlation check |
| T-19 | Other profile becomes active | Profile switch cancels session |
| T-20 | Generated docs overclaim | Canonical source precedence and CI drift check |
| T-21 | Paid/private material leaks | ignored boundary, secret scan, release review |
| T-22 | Denial of service via audio/UI tree | Bounded buffers, nodes, messages, timeouts |
| T-23 | Network provider retains data | explicit provider disclosure and opt-in |
| T-24 | Installer persists unsafely | normal-user design, service review, uninstall/rollback |
| T-25 | Update channel compromise | signed metadata/artifacts and rollback |

## Residual risks

Alpha may remain vulnerable to:

- Skilled voice replay or mimicry.
- False accepts/rejects.
- Malicious software already running as the same user.
- Incomplete accessibility metadata.
- Human approval mistakes.
- Provider outages or policy changes.
- Unsupported Windows configurations.

These risks must be stated, not hidden behind “local-first” language.

## Review cadence

Update this model when:

- A new data class is observed.
- A new action class is added.
- IPC or process privilege changes.
- A cloud provider is introduced.
- Installer/update behavior changes.
- Identity technology or thresholds change.
- A security report reveals a new abuse path.
