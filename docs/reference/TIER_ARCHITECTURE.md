# Tier Architecture and Extension Plan

## Canon

Callsign is Free-first and open-core.

The Free tier is the MIT-licensed public foundation. Future paid features may arrive as closed-source extension libraries, but the open core must remain useful, understandable, and safe on its own.

```text
Free open core -> optional Pro/Advanced extension libraries
```

## Free tier

Free is the public Callsign product.

It includes:

- setup and onboarding
- local profiles and callsigns
- voice enrollment state
- background service runtime
- wake detection and session state
- callsign identity verification
- `callsign.gif` overlay with live readout
- visible Start menu app launch
- stop, cancel, timeout, and lockout behavior
- docs, public contracts, and alpha smoke tests

Free must install cleanly from GitHub or the public website and work without a paid account. The full Windows Voice Access parity baseline belongs in this Free open core; Pro and Advanced begin with beyond-parity command packs and workflows.

## Pro direction

Pro is the paid tier for beyond-parity everyday control.

Possible closed-source extension libraries:

- broader Windows control
- browser workflow control
- WSL and Linux control
- richer command packs
- workflow adapters
- signed automation recipes

Pro extensions must call into the same public runtime gates: identity, visibility, policy, approval, and audit.

## Advanced direction

Advanced is the paid tier for specialized and fast-moving beyond-parity capabilities.

Possible closed-source extension libraries:

- specialized command catalogs
- diagnostics
- power-user recipes
- domain-specific workflows
- richer system-control packs
- continuously updated automation libraries

Advanced should expand what expert users can do without turning Callsign into hidden malware-like automation.

## Repository boundary

The public repository should contain:

- the Free app and service runtime
- setup and monitoring UI
- open command interfaces
- policy and audit contracts
- docs and generated website
- tests for the open-core capability set

The public repository must not contain:

- private paid-tier code
- proprietary command packs
- entitlement secrets
- commercial rollout configuration
- private business experiments

Local private material belongs in `/closed-source/`, which remains git-ignored.

## Extension rules

Every extension library, open or closed, must provide valid pack and command metadata and respect the core contract:

- no identity bypass
- no policy bypass
- no hidden action by default
- no suppressed audit logging
- no arbitrary shell execution as a shortcut
- no credential, payment, 2FA, or security-setting automation in MVP paths
- no cloud transfer of sensitive desktop data without explicit opt-in

The registry treats invalid command metadata as a load-blocking safety issue. Missing pack descriptor fields, missing command ids/display names/categories/help/examples, empty voice phrases, duplicate command ids, or duplicate voice phrases mark the pack `InvalidPack`; the pack remains visible for review but cannot route or execute commands.

Community extension imports must also use normal Windows `.dll` filenames before they enter Callsign's managed pack folder. Reserved device names, non-DLL filenames, control-character filenames, and trailing-dot/trailing-space filenames are rejected during direct DLL import and folder expansion.

When a managed pack DLL is removed from the active registry but kept on disk for rollback or operator review, the registry persists it as disabled. A refresh must not silently reactivate that kept DLL; explicit reimport or rollback has to pass through normal signature, entitlement, policy, and enablement gates again.

The command registry defaults to a Free-only entitlement state. Pro and Advanced packs can be discovered and listed as metadata, but they remain `EntitlementRequired` and cannot resolve or execute voice commands until that tier is explicitly entitled. The command palette may list those commands for discovery, but the visible availability text must name the required tier and state that the command will not route until entitlement is satisfied. Command definitions are also gated by their own declared tier, so a Pro or Advanced command cannot route by being bundled inside a Free pack. Packs that declare `RequiresSignature` remain `SignatureRequired` unless their signature status is valid, signed, or trusted, and the command palette must mark signature-gated commands as listed for discovery only until a valid signature is present. Signature and entitlement checks only decide whether the pack or command may load or route; once a command is loadable, the normal wake, identity, policy, approval, visibility, and audit gates still decide whether it may run. The registry also evaluates policy at execution time, so direct pack execution fails closed without identity proof, approval-required commands do not run unless approval is explicitly supplied by the visible UI path, and commands that declare `AskWhenAmbiguous` stop at the approval gate until the user has made a visible choice. Those registry policy blocks carry structured `PolicyDecision`, `PolicyApprovalRequirement`, `PolicyRiskTier`, and `PolicyVisibleActionRequired` metadata so paid-pack hosts can audit the policy outcome and visible-surface requirement without treating message text as an API. In particular, a command that declares `VisibleRequired` must produce a visible surface regardless of the legacy `VisibleAction` flag; commands that declare `AskWhenAmbiguous`, `RequireApproval`, or `RequireFreshIdentity` must report a visible approval or identity surface even if pack metadata marks visibility as preferred; and a command that declares `BackgroundAllowedWithApproval` must still stop at the approval gate before any background-capable workflow starts. Commands that declare `Clipboard`, `FileContents`, `ScreenshotOrOcr`, or `ExternalData` privacy impact are approval-gated by policy and require a visible approval surface even when they come from an entitled paid pack or misdeclare visibility as preferred.

Command discovery keeps the Free parity boundary visible. The command palette exposes a `Free Parity` quick filter, and plain help accepts `free parity`, `voice access parity`, `open core parity`, and `windows voice access parity` as filters for Free, open-core parity commands. Paid Pro or Advanced packs may appear for discovery, but they are excluded from that Free Parity view and remain gated by entitlement, signature, policy, approval, visibility, and audit.

Entitlement is profile-scoped in the current app design: the selected account persists its allowed tiers locally, and the visible Plans surface reflects the active entitlement summary. The Plans surface now also exposes an entitlement preset picker and a voice readback button so the active profile can switch between Free-only and the supported paid-boundary presets without leaving the visible UI, and the current boundary and entitlement summary can be spoken back without leaving the Plans tab. The visible Help surface and startup walkthrough also expose the same readback path so the paywall boundary remains discoverable from the entry surfaces. The command registry can be updated when the active profile changes so registered packs are replayed against the new entitlement state: paid packs unlock only for the currently selected account state, and a later downgrade back to Free-only immediately returns those packs to `EntitlementRequired` so their commands stop routing.

## Installer and upgrade direction

The installer should support:

- install Free without login
- launch Callsign after install
- repair wake runtime
- repair identity runtime
- preserve profiles on upgrade
- skip unchanged runtime extraction
- show progress for long operations
- write readable logs under the Callsign local app data folder

Future upgrade flows can add:

- extension discovery
- signed extension validation
- entitlement checks
- extension updates
- clear labeling of Free, Pro, and Advanced capabilities

## Non-negotiables

- Do not make Free dependent on private code.
- Do not remove Alpha v1 features from Free.
- Do not let paid entitlement bypass safety policy.
- Do not hide what Callsign heard or did.
- Do not put closed-source material in the public repo.
