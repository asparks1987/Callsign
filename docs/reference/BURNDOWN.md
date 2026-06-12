# Callsign v1 Alpha Burndown

The canonical checklist lives at the repository root in [burndown.md](/burndown.md).

v1 alpha means a fresh checkout can be built, installed, launched, and smoke-tested, and the app can complete the four documented MVP flows:

- Start menu launching.
- Text dictation.
- Web browsing.
- File search.

The alpha also requires:

- Profile and callsign creation.
- Voice enrollment and re-training.
- Wake word plus spoken callsign identity confirmation.
- Visible states for missing microphone, silence, transcription failure, cancel, and stop.
- Human-readable action feedback before and after execution.

## Phase map

| Phase | Area | Goal |
|---:|---|---|
| 0 | Canon and release hygiene | Keep docs, site, and release claims aligned with the alpha contract. |
| 1 | Profile foundation | Create, save, select, and delete user profiles with a personal callsign. |
| 2 | Voice reliability | Record, replay, enroll, and recognize the user's voice and callsign reliably. |
| 3 | Session control | Run the wake/identity/command flow with visible stop, cancel, timeout, and lockout behavior. |
| 4 | Start menu launch | Launch installed apps reliably through the visible Start menu path. |
| 5 | Dictation | Capture speech, transcribe it, and expose or insert the text per the documented UX. |
| 6 | Web browsing | Open and navigate browsing targets with useful feedback and failure recovery. |
| 7 | File search | Search the intended file scope and act on results safely. |
| 8 | Build and install | Produce the executable installer and documented launch entry from a clean checkout. |
| 9 | Alpha verification | Prove the full alpha flow through smoke tests, manual checks, and release gate criteria. |

## Required gates

- Root `burndown.md` is the source of truth.
- Free remains the visible open-source core.
- The alpha does not launch when identity fails or times out.
- Unsafe launch text, secrets, hidden actions, and silent failures stay blocked.
- Docs and generated pages must match the same alpha contract.

See [burndown.md](/burndown.md) for the detailed checklist.
