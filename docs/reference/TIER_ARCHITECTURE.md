# Tier Architecture and Upgrade Plan

## Canon

Callsign is a Free-first open-source product in the current alpha.

The public release line must stay focused on the visible, identity-first control model:

```text
Callsign -> identity verification -> command -> visible action
```

## Current product boundary

The current repo and website should describe the Free core only:

- MIT-licensed open-source code
- local profile storage
- wake detection
- callsign identity verification
- `callsign.gif` wake overlay with live readout
- visible Start menu app launch
- stop, cancel, timeout, and lockout controls

The Free core must install cleanly from the public site or GitHub and must work without a paid account.

## What stays out of current scope

These are future ideas, not current alpha promises:

- paid tiers
- signed command packs
- browser control
- dictation
- system control
- WSL or Linux workflows

If later tiers are introduced, they must not weaken the Free core or make the open-source experience depend on private code.

## Repository boundary

The public repository should contain:

- the Free app
- the service runtime
- setup and monitoring UI
- open command interfaces
- policy engine contracts
- docs and website
- tests for the v1.0 capability set

The public repository should not contain:

- private paid-tier code
- proprietary command packs
- entitlement secrets
- commercial rollout configuration
- or private business experiments

Local private material belongs in `/closed-source/`, which remains git-ignored.

## Installer requirements

The installer should support:

- install Free without login
- launch Callsign after install
- repair wake runtime
- repair identity runtime
- preserve profiles on upgrade
- skip unchanged runtime extraction
- show progress for long operations
- and write readable logs under the Callsign local app data folder

The installer must not ask users to hunt for model files, command packs, or runtime dependencies.

## Non-negotiables

- Do not make Free dependent on private code.
- Do not remove Free features after monetization starts.
- Do not let paid entitlement bypass safety policy.
- Do not hide what Callsign heard or did.
- Do not put closed-source material in the public repo.

