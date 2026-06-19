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

Every extension library, open or closed, must respect the core contract:

- no identity bypass
- no policy bypass
- no hidden action by default
- no suppressed audit logging
- no arbitrary shell execution as a shortcut
- no credential, payment, 2FA, or security-setting automation in MVP paths
- no cloud transfer of sensitive desktop data without explicit opt-in

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
