# Callsign Alpha v1 Burndown

## Definition

Alpha v1 is a release line, not one oversized first drop.

The current public target is `v1.0 alpha`: install Callsign, create a profile, enroll voice, say the wake word, verify callsign identity, see `callsign.gif` plus live readout, and launch an installed app through the visible Start menu path.

All Alpha v1 features are free and remain free until at least beta.

Future Pro and Advanced features may ship as closed-source extension libraries, but the v1.0 Free core must work without them.

The Free-core parity target is practical stable Windows 11 Voice Access usefulness while preserving Callsign wake, identity, visible action, policy, and audit rules. The canonical checklist is `VOICE_ACCESS_PARITY_MATRIX.md`.

## Status legend

- `Done`: implemented and covered by smoke/manual evidence.
- `In progress`: partially implemented or recently changed but not fully proven.
- `Not started`: required and not implemented yet.
- `Deferred`: intentionally outside the named release.
- `Blocked`: cannot proceed without an external dependency or decision.

## Phase 0: Canon, build, and release hygiene

| ID | Release | Priority | Status | Work item | Acceptance criteria | Verification |
|---:|---|---|---|---|---|---|
| 0.1 | v1.0 | P0 | Done | Keep README, canon, roadmap, site, and tests aligned around v1.0 as the wake/identity/app-launch MVP. | Docs distinguish current v1.0 release criteria from v1.x surfaces. | `rg -n "v1.0 alpha|v1.1 alpha|closed-source extension" README.md CANON.md docs/reference` |
| 0.2 | v1.0 | P0 | Done | Keep Alpha v1 free-through-alpha canon. | Docs say Alpha v1 features stay free until at least beta. | `rg -n "free until at least beta|remain free" README.md CANON.md docs/reference` |
| 0.3 | v1.0 | P0 | Done | Generate public site from reference docs. | `/docs` renders canon, product spec, roadmap, tier architecture, burndown, and test plan. | `python scripts/build_site.py` |
| 0.4 | v1.0 | P0 | Done | Keep proprietary assets in `closed-source/`. | Private runtime/model/premium material stays ignored. | Inspect `.gitignore`. |

## Phase 0.1: Alpha release numbering

| ID | Release | Priority | Status | Work item | Acceptance criteria | Verification |
|---:|---|---|---|---|---|---|
| 0.6 | 0.0.3a | P0 | Done | Reach the extension-library baseline milestone with folder-import packs and policy-safe defaults. | Registry supports `.dll` drop/folder import and community packs are loaded disabled by default. | Build + smoke manual pack flow (`.dll` import, refresh, enable, command use). |
| 0.7 | 0.0.4a | P0 | Done | Expand core parity command families into explicit command-family surfaces. | `App launch`, `browser`, `system`, `dictation`, and `file` routes all route through policy + audit. | Smoke matrix command-family execution and policy tests, including verified session routing for built-in parity families. |
| 0.8 | 0.0.5a | P0 | In progress | Close reliability gaps in wake/session and install/update flows before public packaging. | Wake/restart behavior and manifest-triggered update splash are deterministic in smoke coverage, update cadence/status survives restart, pending update state reloads after a service restart, and the staged installer path survives restart for evidence reuse. | Manual session stability plus update check/reinstall pass; release validation still needs a clean reboot/manual proof. |
| 0.9 | 0.0.01a | P1 | Not started | Urgent hotfix rail with visible release-note updates. | Micro fixes patch open regressions without changing roadmap scope. | `git` + smoke verification log. |
| 0.10 | 0.1.0a | P0 | Not started | Major pre-`v1.0` consolidation milestone before public alpha. | All `v1.0` release criteria are in evidence and installer is reproducible. | Release checklist + installer/site validation. |
| 0.11 | 1.0.0a | P0 | Not started | First public alpha parity milestone with `0`-to-`100` planning evidence in place. | All parity categories in `VOICE_ACCESS_PARITY_MATRIX.md` are complete, with automated evidence and smoke walkthroughs. | Full matrix evidence review + final installer/site comparison.

## Phase 1: v1.0 installer and runtime ownership

| ID | Release | Priority | Status | Work item | Acceptance criteria | Verification |
|---:|---|---|---|---|---|---|
| 1.1 | v1.0 | P0 | Done | Build a single setup executable. | `Callsign-Setup.exe` exists after build. | `.\buildcallsign.ps1` |
| 1.2 | v1.0 | P0 | Done | Package configuration manager and service runtime. | Installed app folder contains UI and service binaries. | Manual install inspect. |
| 1.3 | v1.0 | P0 | Done | Ensure exactly one authoritative user runtime owns microphone capture. | Duplicate `--user-runtime` launches exit through `Local\Callsign.UserRuntime`, and the Session tab shows runtime owner, role, PID, process count, freshness, and authority. | Runtime ownership smoke test plus Session tab owner proof. |
| 1.4 | v1.0 | P0 | Done | Prove runtime can hear audio. | Session tab has a dedicated Runtime proof line showing active mic, packet age, packet freshness, runtime authority, and `CanHearAudio`; alpha smoke covers hearing and silent packet states. | `dotnet run --project .\tests\Callsign.AlphaSmoke\Callsign.AlphaSmoke.csproj -c Release --no-build` |
| 1.5 | v1.0 | P0 | Done | Cache build and deploy steps. | Unchanged builds reuse prior outputs and private runtime bundles. | `.\buildcallsign.ps1` twice. |

## Phase 2: v1.0 profile and enrollment

| ID | Release | Priority | Status | Work item | Acceptance criteria | Verification |
|---:|---|---|---|---|---|---|
| 2.1 | v1.0 | P0 | Done | Create, save, load, select, and delete local profiles. | Profile survives restart and blank first-run cannot crash. | Smoke test. |
| 2.2 | v1.0 | P0 | Done | Press-and-hold voice sample recording. | Recording is visibly live only while held. | Manual recording test. |
| 2.3 | v1.0 | P0 | Done | Play back processed voice samples. | User hears the sample Callsign will use. | Manual playback test. |
| 2.4 | v1.0 | P0 | Done | Enroll at least three fresh biometric samples. | Enrollment writes `voice-identity/enrollment-samples.json` with sample count, distinct hash count, byte lengths, timestamps, freshness, SHA-256 hashes, quality state, peak/RMS, duration, clipping ratio, and zero-crossing rate; duplicate paths, duplicate audio content, clipped samples, silent/too-quiet samples, and excessive broadband noise are rejected before biometric enrollment. | Multi-sample enrollment and sample-quality smoke tests. |
| 2.5 | v1.0 | P0 | Done | Explain mic/runtime/model/sample failures clearly. | Voice tab and Train Voice Identity classify microphone, identity runtime, model cache, duplicate/invalid sample proof, timeout, and service failures with targeted next actions. | Voice tab and identity-training smoke tests. |

## Phase 3: v1.0 wake, overlay, identity, and command flow

| ID | Release | Priority | Status | Work item | Acceptance criteria | Verification |
|---:|---|---|---|---|---|---|
| 3.1 | v1.0 | P0 | Done | Use openWakeWord/audio detection as the wake event source. | Runtime snapshots expose `LastWakeTransitionSource`; live wake events use `audio-wake-detector`, scripted/dev control is labeled separately, and transcript text alone cannot wake the service. | Wake transition source smoke test and service-worker transcript guard. |
| 3.2 | v1.0 | P0 | Done | Recognize `Callsign` and `call sign` reliably enough for public alpha. | Wake calibration crosses the app-matched effective threshold on recorded Callsign wake samples, with score margin reported. Wake calibration records provenance, including the trusted sample set and source sample when available, so the Voice tab and wake overlay explain why the threshold changed. | `testopenwakeword.ps1` against a recorded local wake sample; helper reports sensitivity, effective threshold, score, margin, detection result, and trusted-sample calibration coverage. |
| 3.3 | v1.0 | P0 | Done | Show `callsign.gif` immediately on wake. | Overlay loads the bundled `callsign.gif`, is topmost, and uses no-activate/click-through tool-window styles so it appears without stealing focus. | Wake overlay smoke test plus wake-event source guard. |
| 3.4 | v1.0 | P0 | Done | Show live text below the overlay animation. | Overlay readout/caption/history/activity surfaces update through identity, command, ready, launching, transcript, authority, and mic activity states. | Overlay readout formatter and WakeOverlay smoke tests. |
| 3.5 | v1.0 | P0 | Done | Treat post-wake utterance as identity-only. | `Callsign womprat open Notepad` is rejected during identity, even with accepted biometric proof; the runtime identity handler cannot execute commands and command capture requires a separate turn. | Identity-turn smoke test and service-worker source guard. |
| 3.6 | v1.0 | P0 | Done | Require callsign text and biometric identity before command capture. | The service advances to command capture only through a result-aware identity gate; missing, rejected, stale, and weak near-match biometric proof stays in identity phase, while accepted biometric proof opens command capture. | Session biometric identity smoke test. |

## Phase 4: v1.0 visible Start menu launch

| ID | Release | Priority | Status | Work item | Acceptance criteria | Verification |
|---:|---|---|---|---|---|---|
| 4.1 | v1.0 | P0 | Done | Restrict launch scope to installed app names and safe shell-backed destinations. | Paths, URLs, shells, WSL, separators, and unsafe text are rejected. | Smoke test. |
| 4.2 | v1.0 | P0 | Done | Resolve installed apps from Start menu entries. | Notepad, Calculator, browser, and VS Code resolve when installed. | Smoke/manual test. |
| 4.3 | v1.0 | P0 | Done | Launch through visible Start menu flow. | User can see the desktop action; live probe reports `LaunchPath=start-menu-search`, `StartMenuOpened=True`, and `ShellFallbackUsed=False` for common installed apps. | `--live-launch Notepad` and `--live-launch Calculator`; launcher telemetry shows Start/Search opened, search text typed, Enter pressed, and no shell fallback used. |
| 4.4 | v1.0 | P0 | Done | Confirm ambiguous app matches. | Wrong app is not silently launched; ambiguous candidates are shown in the Session tab and require explicit selection before launch; spoken choices such as `choose app 1`, `select option five`, and `use result three` are parsed only for pending choices. | Smoke resolver, candidate-selection parser, and command-discovery tests. |
| 4.5 | v1.0 | P0 | In progress | Complete clean-install release walkthrough. | Fresh user sees the first-run walkthrough, completes wake, identity, overlay, transcript, and launch, can jump between the Account, Voice, Session, Shortcuts, Files, Plans, Updates, and Packs setup surfaces, can reach the visible release-proof step to compare the local installer, release evidence folder, and public website download before release, can open the release evidence folder from the Updates tab or the walkthrough's direct Open Release Evidence button, can open the manual evidence template and checklist from the Account, Help, Updates, or walkthrough surfaces, can read the visible update check-in status, evidence status, and visual status aloud, can see that the update privacy id is hashed before phone-home check-ins, can reach the Packs import entrypoints from the walkthrough, including the visible Drop DLL Folder route for folder-based community pack import, can discover browser overlay helpers such as Browser Show Numbers, Browser Show Grid, and Browser Hide Overlays from the walkthrough and command palette, and the release packet carries documentation-pack gates for installed end-to-end automation, human-spoken core walkthrough, failure-state walkthrough, and clean Windows user or VM proof. | Walkthrough UI smoke test plus update check-in payload privacy smoke test, evidence-status readback smoke test, and visual-status readback smoke test, `.\scripts\alpha_v1_checklist.ps1 -Verify`, `.\scripts\verify-release-readiness.ps1 -RequireWebsiteVerification`, and manual evidence checks `installed_end_to_end_automated_checks`, `human_spoken_core_walkthrough`, `failure_state_walkthrough`, and `clean_windows_user_or_vm_test`; public clean-install/manual voice pass still required. |

## Phase 5: Alpha v1 Voice Access parity line

| ID | Release | Priority | Status | Work item | Acceptance criteria | Verification |
|---:|---|---|---|---|---|---|
| 5.1 | v1.1 | P0 | Done | Dictation with visible review, text editing, formatting, symbols, and correction alternatives. | Text is reviewed before insertion/copy/paste; select/delete/replace/case-format/punctuation/symbol commands, a live preview line, and numbered correction alternatives are covered. | Dictation smoke tests and Notepad manual walkthrough. |
| 5.2 | v1.2 | P0 | Done | Visible controls, command discovery, and browser control. | `show numbers` can inspect foreground-app UIA controls or Callsign controls; visible activation, browser open/search/navigation, and `what can I say` are usable. | UIA label tests, UI overlay tests, browser tests, and manual walkthrough. |
| 5.3 | v1.3 | P0 | Done | Desktop/system/file parity commands. | Window management including snap layouts, app switching with Task View and virtual desktop navigation, safe keyboard/mouse/media commands including Space, letter keys, number-row keys, symbol keys, modifier chords (allowlisted), Delete, Insert, Windows key, context menu key, Caps Lock, F1-F12, natural editing/formatting/document/zoom shortcuts, pointer nudges, mouse hold/release drag, direct mouse drag, safe Windows Settings page surfaces, numbered file result select/open/reveal, and Explorer-backed file search are policy-gated and audited. | System/file/settings/media/window-layout/keyboard tests, matrix review, and manual walkthrough evidence. |
| 5.4 | v1.4 | P0 | Done | Close the Voice Access parity matrix. | Every category in `VOICE_ACCESS_PARITY_MATRIX.md` is `Done`, with automated and manual evidence. | Matrix review plus `.\scripts\voice_access_parity_evidence.ps1`; release candidates use `-RequireManualEvidence`. |
| 5.5 | v1.4 | P0 | Done | Community extension import, import splash replay, and update splash. | Community DLLs import disabled by default; manifests list new commands/features for the splash screen, and the import splash can replay the narration and survive restart. | Pack import tests, import-splash replay tests, import-splash persistence tests, and manifest parsing tests. |
| 5.6 | Beta+ | P1 | Deferred | Pro/Advanced closed-source extension libraries. | Free parity remains independent and paid extensions cannot bypass safety gates. | Future entitlement, signature, rollback, and policy tests. |
