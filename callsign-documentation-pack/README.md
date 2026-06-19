# Callsign

**Callsign is a local-first, visible voice trigger for your computer.**

Say `Callsign`, complete the active profile's voice/callsign gate, and ask for a supported desktop action. Callsign keeps the session visible, shows what it heard, and gives the user an obvious way to stop.

> Current product focus: a Windows-first `v1.0 alpha` installed experience for profile setup, voice enrollment, service-driven wake detection, identity gating, the animated listening overlay, and visible Start menu application launch.

## The interaction contract

```text
wake event -> identity gate -> command capture -> policy check -> visible action -> result
```

For the current public alpha:

1. Create a local profile and choose a callsign.
2. Record and review voice samples.
3. Start the Callsign background runtime.
4. Say `Callsign`.
5. Confirm the animated overlay and live readout appear.
6. Say the enrolled callsign.
7. Ask to open an installed application.
8. Watch Callsign perform the launch through a visible Windows path.
9. Say `stop` or `cancel` at any point to end the session.

The callsign/voice gate reduces accidental activation. It is **not** a substitute for operating-system authentication, a password, or a high-assurance biometric.

## Product principles

- **Visible by default.** The user should see when a session is active and what action is being attempted.
- **Identity before command capture.** A wake event alone never authorizes an action.
- **Local first.** Profiles, enrollment metadata, status, and diagnostics stay local unless a later opt-in feature says otherwise.
- **Narrow before powerful.** The first release proves one safe action path before broader desktop control.
- **Easy to stop.** Cancel, stop, timeout, lockout, and failure states are first-class behavior.
- **Policy outside the model.** Future automation tools cannot authorize themselves.
- **No stealth.** Callsign is not a hidden remote-control, credential-entry, arbitrary-shell, or surveillance framework.

## Current documented status

The existing project documentation describes:

- A Windows setup/configuration UI in `src/Callsign.UI`.
- A background runtime in `src/Callsign.Service`.
- Local profile persistence under `%LOCALAPPDATA%\Callsign\Profiles\<callsign>\`.
- openWakeWord as the canonical wake-event source.
- `callsign.gif` as the visible listening overlay.
- Start menu app launch as the `v1.0 alpha` action surface.
- Partial follow-on work for dictation, browser opening/search, and Explorer-backed file search.
- A future local MCP automation boundary with policy, approval, verification, and audit contracts.

This documentation pack imports status labels from the existing burndown but does not claim the repository is release-ready. Release claims require current command output and human-installed evidence; see [the canonical burndown](burndown.md).

## Release ladder

| Release | Product promise |
|---|---|
| `v1.0 alpha` | Install, enroll, wake, verify, show the overlay, and visibly launch installed apps. |
| `v1.1 alpha` | Dictate into a visible review surface; insertion remains explicit. |
| `v1.2 alpha` | Open, search, and navigate the browser visibly within strict external-side-effect boundaries. |
| `v1.3 alpha` | Add approved Windows, WSL, and Linux workflows plus Explorer-backed file search. |
| Beta or later | Harden packaging, updates, support, policy-gated automation, and any transparent tier model. |

All Alpha v1 capabilities are documented as free through alpha and until at least beta. Any future Free, Pro, or Advanced packaging is directional, not a current entitlement or implementation claim.

## Architecture at a glance

```text
┌───────────────────────────────────────────────────────────────┐
│ Callsign.UI                                                   │
│ onboarding • profiles • enrollment • monitoring • diagnostics │
└──────────────────────────┬────────────────────────────────────┘
                           │ local settings/status
┌──────────────────────────▼────────────────────────────────────┐
│ Callsign.Service                                              │
│ microphone • openWakeWord • identity • session • transcription│
│ overlay state • command routing • visible action handoff       │
└──────────────────────────┬────────────────────────────────────┘
                           │ current narrow adapters
          ┌────────────────┼─────────────────┐
          ▼                ▼                 ▼
   Start menu launch   browser helper   Explorer file search

Future, after policy and audit gates are proven:

Voice/agent host -> local stdio MCP automation server -> UIA/native APIs
```

The future MCP server is a bounded capability layer, not permission to route arbitrary model output into the operating system.

## Build and smoke commands

From a Windows checkout:

```powershell
.\buildcallsign.ps1
dotnet run --project tests/Callsign.AlphaSmoke/Callsign.AlphaSmoke.csproj
```

Documented release-verification commands include:

```powershell
.\verifycallsign-alpha.ps1 -Install -LiveActions -RequireAlphaReady `
  -ReportPath .\build\alpha-readiness.json
```

Do not call an alpha testing-ready until the current checkout passes its automated checks and a human completes the installed voice walkthrough.

## Documentation map

Start with:

- [CANON.md](CANON.md) — precedence, product promises, terminology, and non-negotiable decisions.
- [AGENTS.md](AGENTS.md) — rules for coding agents and contributors.
- [docs/README.md](docs/README.md) — complete documentation index.
- [burndown.md](burndown.md) — dependency-linked execution plan and imported status snapshot.
- [docs/reference/PRODUCT_SPEC.md](docs/reference/PRODUCT_SPEC.md) — product requirements.
- [docs/reference/ARCHITECTURE.md](docs/reference/ARCHITECTURE.md) — current and target system boundaries.
- [docs/reference/SECURITY_MODEL.md](docs/reference/SECURITY_MODEL.md) — trust model and blocked behavior.
- [docs/reference/TEST_PLAN.md](docs/reference/TEST_PLAN.md) — automated and human evidence gates.
- [SOURCE_AUDIT.md](SOURCE_AUDIT.md) — how this pack reconciles the existing documentation.
- [MERGE_GUIDE.md](MERGE_GUIDE.md) — how to apply the overlay safely.

## Contribution and support

Read [CONTRIBUTING.md](CONTRIBUTING.md) before changing runtime behavior. Security-sensitive changes require the checklist in [docs/checklists/SECURITY_REVIEW.md](docs/checklists/SECURITY_REVIEW.md). Vulnerability reporting guidance is in [SECURITY.md](SECURITY.md).

## Closed-source boundary

Private business material, proprietary command packs, unreleased paid-tier code, private model caches, and internal credentials belong outside the public tracked source. The current repository convention uses `/closed-source/` for that boundary.

## License note

The existing documentation calls Callsign open source, but this documentation review did not establish a canonical license file. Select and publish the repository license before distributing releases or accepting substantial third-party contributions.
