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

Free must install cleanly from GitHub or the public website and work without a paid account.

## Pro direction

Pro is the planned paid tier for deeper everyday control.

Possible closed-source extension libraries:

- broader Windows control
- browser workflow control
- WSL and Linux control
- richer command packs
- workflow adapters
- signed automation recipes

Pro extensions must call into the same public runtime gates: identity, visibility, policy, approval, and audit.

## Advanced direction

Advanced is the planned paid tier for specialized and fast-moving capabilities.

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

The command registry defaults to a Free-only entitlement state. Pro and Advanced packs can be discovered and listed as metadata, but they remain `EntitlementRequired` and cannot resolve or execute voice commands until that tier is explicitly entitled. The command palette may list those commands for discovery, but the visible availability text must name the required tier and state that the command will not route until entitlement is satisfied. Command definitions are also gated by their own declared tier, so a Pro or Advanced command cannot route by being bundled inside a Free pack. Packs that declare `RequiresSignature` remain `SignatureRequired` unless their signature status is valid, signed, or trusted, and the command palette must mark signature-gated commands as listed for discovery only until a valid signature is present. Signature and entitlement checks only decide whether the pack or command may load or route; once a command is loadable, the normal wake, identity, policy, approval, visibility, and audit gates still decide whether it may run. The registry also evaluates policy at execution time, so direct pack execution fails closed without identity proof and approval-required commands do not run unless approval is explicitly supplied by the visible UI path. Those registry policy blocks carry structured `PolicyDecision`, `PolicyApprovalRequirement`, and `PolicyRiskTier` metadata so paid-pack hosts can audit the policy outcome without treating message text as an API. In particular, a command that declares `VisibleRequired` must produce a visible surface regardless of the legacy `VisibleAction` flag, and a command that declares `BackgroundAllowedWithApproval` must still stop at the approval gate before any background-capable workflow starts. Commands that declare `Clipboard`, `FileContents`, `ScreenshotOrOcr`, or `ExternalData` privacy impact are approval-gated by policy even when they come from an entitled paid pack.

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
