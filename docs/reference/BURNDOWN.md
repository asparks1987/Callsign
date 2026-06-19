# Callsign Alpha v1 Burndown

## Definition

Alpha v1 is a release line, not one oversized first drop.

The current public target is `v1.0 alpha`: install Callsign, create a profile, enroll voice, say the wake word, verify callsign identity, see `callsign.gif` plus live readout, and launch an installed app through the visible Start menu path.

All Alpha v1 features are free and remain free until at least beta.

Future Pro and Advanced features may ship as closed-source extension libraries, but the v1.0 Free core must work without them.

## Status legend

- `Done`: implemented and covered by smoke/manual evidence.
- `In progress`: partially implemented or recently changed but not fully proven.
- `Not started`: required and not implemented yet.
- `Deferred`: intentionally outside the named release.
- `Blocked`: cannot proceed without an external dependency or decision.

## Phase 0: Canon, build, and release hygiene

| ID | Release | Priority | Status | Work item | Acceptance criteria | Verification |
|---:|---|---|---|---|---|---|
| 0.1 | v1.0 | P0 | In progress | Keep README, canon, roadmap, site, and tests aligned around v1.0 as the wake/identity/app-launch MVP. | Docs distinguish current v1.0 release criteria from v1.x surfaces. | `rg -n "v1.0 alpha|v1.1 alpha|closed-source extension" README.md CANON.md docs/reference` |
| 0.2 | v1.0 | P0 | Done | Keep Alpha v1 free-through-alpha canon. | Docs say Alpha v1 features stay free until at least beta. | `rg -n "free until at least beta|remain free" README.md CANON.md docs/reference` |
| 0.3 | v1.0 | P0 | In progress | Generate public site from reference docs. | `/docs` renders canon, product spec, roadmap, tier architecture, burndown, and test plan. | `python scripts/build_site.py` |
| 0.4 | v1.0 | P0 | Done | Keep proprietary assets in `closed-source/`. | Private runtime/model/premium material stays ignored. | Inspect `.gitignore`. |

## Phase 1: v1.0 installer and runtime ownership

| ID | Release | Priority | Status | Work item | Acceptance criteria | Verification |
|---:|---|---|---|---|---|---|
| 1.1 | v1.0 | P0 | Done | Build a single setup executable. | `Callsign-Setup.exe` exists after build. | `.\buildcallsign.ps1` |
| 1.2 | v1.0 | P0 | Done | Package configuration manager and service runtime. | Installed app folder contains UI and service binaries. | Manual install inspect. |
| 1.3 | v1.0 | P0 | In progress | Ensure exactly one authoritative user runtime owns microphone capture. | Duplicate runtimes exit or mark themselves non-authoritative. | Troubleshooting report plus Session tab. |
| 1.4 | v1.0 | P0 | In progress | Prove runtime can hear audio. | Session tab shows active mic, packet age, and `CanHearAudio`. | Speak and inspect Session tab. |
| 1.5 | v1.0 | P0 | In progress | Cache build and deploy steps. | Unchanged builds reuse prior outputs and private runtime bundles. | `.\buildcallsign.ps1` twice. |

## Phase 2: v1.0 profile and enrollment

| ID | Release | Priority | Status | Work item | Acceptance criteria | Verification |
|---:|---|---|---|---|---|---|
| 2.1 | v1.0 | P0 | Done | Create, save, load, select, and delete local profiles. | Profile survives restart and blank first-run cannot crash. | Smoke test. |
| 2.2 | v1.0 | P0 | Done | Press-and-hold voice sample recording. | Recording is visibly live only while held. | Manual recording test. |
| 2.3 | v1.0 | P0 | Done | Play back processed voice samples. | User hears the sample Callsign will use. | Manual playback test. |
| 2.4 | v1.0 | P0 | In progress | Enroll at least three fresh biometric samples. | Enrollment metadata reports three distinct samples. | Train Voice Identity flow. |
| 2.5 | v1.0 | P0 | In progress | Explain mic/runtime/model failures clearly. | UI says whether failure is mic, wake runtime, identity runtime, model, or service. | Manual negative tests. |

## Phase 3: v1.0 wake, overlay, identity, and command flow

| ID | Release | Priority | Status | Work item | Acceptance criteria | Verification |
|---:|---|---|---|---|---|---|
| 3.1 | v1.0 | P0 | In progress | Use openWakeWord/audio detection as the wake event source. | Transcript text alone cannot wake the service. | Smoke and live wake tests. |
| 3.2 | v1.0 | P0 | In progress | Recognize `Callsign` and `call sign` reliably enough for public alpha. | Live wake score crosses threshold in normal room audio. | `testopenwakeword.ps1` against live segments. |
| 3.3 | v1.0 | P0 | In progress | Show `callsign.gif` immediately on wake. | Overlay appears above all windows and does not steal focus. | Manual wake test. |
| 3.4 | v1.0 | P0 | In progress | Show live text below the overlay animation. | Overlay updates during identity, command, and launch. | Manual service flow. |
| 3.5 | v1.0 | P0 | In progress | Treat post-wake utterance as identity-only. | `Callsign womprat open Notepad` cannot execute in one utterance. | Session tests. |
| 3.6 | v1.0 | P0 | In progress | Require callsign text and biometric identity before command capture. | Wrong, stale, missing, or weak identity cannot launch. | Manual negative tests. |

## Phase 4: v1.0 visible Start menu launch

| ID | Release | Priority | Status | Work item | Acceptance criteria | Verification |
|---:|---|---|---|---|---|---|
| 4.1 | v1.0 | P0 | Done | Restrict launch scope to installed app names and safe shell-backed destinations. | Paths, URLs, shells, WSL, separators, and unsafe text are rejected. | Smoke test. |
| 4.2 | v1.0 | P0 | Done | Resolve installed apps from Start menu entries. | Notepad, Calculator, browser, and VS Code resolve when installed. | Smoke/manual test. |
| 4.3 | v1.0 | P0 | In progress | Launch through visible Start menu flow. | User can see the desktop action. | Live launch Notepad. |
| 4.4 | v1.0 | P0 | Not started | Confirm ambiguous app matches. | Wrong app is not silently launched. | Ambiguous app manual test. |
| 4.5 | v1.0 | P0 | Not started | Complete clean-install release walkthrough. | Fresh user completes wake, identity, overlay, transcript, and launch. | Manual release checklist. |

## Phase 5: Alpha v1 extension line

| ID | Release | Priority | Status | Work item | Acceptance criteria | Verification |
|---:|---|---|---|---|---|---|
| 5.1 | v1.1 | P1 | Deferred | Dictation with visible review. | Text is reviewed before insertion/copy/paste. | Future dictation tests. |
| 5.2 | v1.2 | P1 | Deferred | Browser control. | Open/search/navigation are visible and external side effects require approval. | Future browser tests. |
| 5.3 | v1.3 | P1 | Deferred | System control, WSL/Linux, and file search. | Results/actions are visible, policy-gated, and audited. | Future system/file tests. |
| 5.4 | Beta+ | P1 | Deferred | Pro/Advanced closed-source extension libraries. | Free remains independent and extensions cannot bypass safety gates. | Future extension tests. |
