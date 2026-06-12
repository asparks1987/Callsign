# Burndown List

## Legend

- P0: required for the current alpha.
- P1: required for a useful demo.
- P2: important after the alpha.
- P3: later enhancement.

## Current alpha work

| ID | Work item | Priority | Status | Acceptance criteria |
|---:|---|---:|---|---|
| 1 | Public landing page | P0 | Done | Home page sells Callsign clearly as open source with paid tiers |
| 2 | Alpha setup UI | P0 | Done | User can create and save an account |
| 3 | Voice enrollment state | P0 | Done | User can record samples and mark a profile as enrolled |
| 4 | Session state machine | P0 | Done | Wake, verify, capture, launch, cancel, timeout |
| 5 | Visible Start menu launch | P0 | Done | Installed app can be opened through visible search flow |
| 6 | Startup error handling | P0 | Done | Failures are visible and logged |
| 7 | Voice capture integration | P1 | Not started | Microphone input flows into real identity checks |
| 8 | Always-on background service | P1 | Not started | Assistant listens in the background and wakes on demand |
| 9 | Better app launch reliability | P1 | Not started | Common installed apps launch consistently |
| 10 | Manual docs cleanup | P0 | In progress | Docs match current alpha state and public story |
| 11 | WSL MVP planning | P1 | Not started | Linux MVP path is defined with WSL as the bridge |

## Next phase ideas

- Add live voice recognition.
- Improve identity matching.
- Add simple task automation beyond app launch.
- Keep Linux as an MVP item alongside Windows, with WSL support defined clearly.
- Add paid Home and Advanced features only after the core experience is solid.
