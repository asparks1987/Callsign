# Tier Architecture and Upgrade Plan

## Canon

Callsign is built as two products that share one visible, identity-first control model.

The Free core is the MIT-licensed open-source product. It should install from GitHub or the public website with minimal effort, work without a paid account, and reach practical parity with built-in Windows voice controls while using Callsign's stricter session flow:

```text
Callsign -> identity verification -> command -> visible action
```

The Pro product is the paid expansion layer. It unlocks deeper automation and a continuously updated advanced command library without making the Free core dependent on closed-source code.

## Product goals

- Free must be useful on its own, not a demo shell.
- Free must remain buildable from the public repository.
- Free must be installable from GitHub Releases and the Callsign website with a simple installer.
- Pro must be upgradeable from inside the app after the Free version is already working.
- Pro must support frequent command-library updates without requiring a full application rebuild every time.
- Pro and Advanced functionality must remain policy-gated, signed, auditable, and user-visible.
- Closed-source implementation material must stay outside the MIT repository boundary.

## Free distribution path

The public Free release should provide:

- a signed Windows installer,
- a portable developer build when practical,
- bundled wake/runtime assets that are redistributable,
- first-run profile and callsign setup,
- voice enrollment and listener health checks,
- the `callsign.gif` wake overlay with live readout,
- local profile storage,
- clear repair buttons for voice runtime assets,
- and no login requirement for Free features.

The website download and GitHub Release download should point to the same signed installer artifact. A new user should be able to download, install, create a profile, train voice identity, and try basic commands without reading developer setup notes.

## Free capability boundary

Free is the Windows Voice Access parity layer.

The Free command set should cover the everyday voice-control surface:

- wake word detection,
- callsign and voice identity verification,
- visible overlay and transcript feedback,
- app launching,
- dictation with review,
- basic browser open/search/navigation,
- visible system control that matches built-in voice-control expectations,
- Explorer-backed file and folder search when file search lands in the Alpha v1 line,
- stop, cancel, timeout, and lockout controls,
- and accessibility-oriented status feedback.

Free should not include arbitrary hidden automation, secret handling, purchases, money movement, account deletion, or silent external submissions.

## Pro upgrade path

The app should expose a clear upgrade path only after the Free experience is functional:

1. User installs Free.
2. User creates a profile and confirms Callsign can hear them.
3. User opens `Upgrade to Pro` from settings or the website.
4. App validates entitlement through a license file, signed token, or account-based activation.
5. App downloads or unlocks signed Pro command packs.
6. App registers those packs in the local command library.
7. Commands remain disabled until policy, capability, and signature checks pass.

The upgrade should not replace the Free executable with an unrelated application. It should add private command packs, private adapters, private recipes, and entitlement metadata around the same visible Callsign runtime.

## Entitlement and license model

Entitlement should answer only one question: whether a paid command pack may be loaded.

Policy should answer the separate question of whether a command may run right now.

That separation matters because a paid feature can still be unsafe in a particular context. A Pro license must never bypass:

- identity verification,
- risk-tier policy,
- explicit approval prompts,
- audit logging,
- visibility requirements,
- or blocked-action rules.

Recommended entitlement behavior:

- signed local entitlement cache,
- online activation and refresh when available,
- offline grace period for already activated users,
- readable expiration or repair status,
- no secret tokens committed to the repo,
- no closed-source entitlement code required to build Free.

## Advanced command library

The Pro and Advanced business engine should be a command-library system.

A command pack should include:

- pack id,
- display name,
- version,
- publisher,
- signature,
- changelog,
- risk tier,
- required capabilities,
- phrase aliases,
- command schemas,
- approval requirements,
- rollback metadata,
- implementation module reference,
- and compatibility constraints.

The local command registry should merge:

- Free built-in commands from the MIT core,
- enabled Pro packs,
- enabled Advanced packs,
- disabled packs kept for rollback,
- and user-created local recipes when that feature exists.

The command registry should be inspectable in the UI. Users should be able to see which pack owns a command, when it was updated, what risk tier it has, and how to disable it.

## Command pack update channel

Pro should support continuous expansion through a signed update channel:

1. App checks the command-pack feed.
2. Feed returns signed metadata only.
3. App compares versions, compatibility, and rollout flags.
4. App downloads changed packs.
5. App verifies signatures and hashes.
6. App stages the update.
7. App restarts only the command registry when possible.
8. App keeps rollback data for the last known-good pack.

Updates should be small and frequent. The main app should not need a full reinstall just because a new browser recipe, Windows workflow, WSL command, or diagnostic pack was added.

## Repository boundary

The public repository should contain:

- the Free app,
- the service runtime,
- setup and monitoring UI,
- open command interfaces,
- policy engine contracts,
- command registry contracts,
- docs and website,
- tests for the Free capability set,
- and sample command packs that are safe to publish.

The public repository should not contain:

- private Pro command implementations,
- proprietary command packs,
- private model assets that cannot be redistributed,
- paid license keys,
- entitlement secrets,
- commercial rollout configuration,
- or private business experiments.

Local private material belongs in `/closed-source/`, which remains git-ignored. Production private distribution can live in a separate private repository or release system.

## Installer and repair requirements

The installer should support:

- install Free without login,
- launch Callsign after install,
- repair wake runtime,
- repair identity runtime,
- preserve profiles on upgrade,
- install Pro command packs after entitlement,
- skip unchanged runtime extraction,
- show progress for long operations,
- and write readable logs under the Callsign local app data folder.

The installer must not ask users to hunt for model files, wheelhouses, command packs, or runtime dependencies.

## Security requirements

Every Free and paid command path must keep the same safety posture:

- wake word alone authorizes nothing,
- identity must pass before command capture,
- high-risk actions require explicit approval,
- signed command packs are required for paid features,
- command execution remains auditable,
- users can disable command packs,
- hidden execution is blocked unless explicitly configured,
- and sensitive data is not uploaded without opt-in.

## Implementation phases

| Phase | Goal | Acceptance |
|---|---|---|
| 1 | Free installer polish | New user installs from GitHub or website, creates profile, enrolls voice, and tests Free commands without developer setup. |
| 2 | Free parity command set | Free reaches practical parity with built-in Windows voice controls through visible, identity-gated commands. |
| 3 | Command registry contract | Free commands and future paid packs use one local registry, one policy path, and one audit path. |
| 4 | Pro entitlement shell | App can activate, cache, refresh, and revoke Pro entitlement without changing Free behavior. |
| 5 | Signed command packs | App loads, verifies, updates, disables, and rolls back signed paid command packs. |
| 6 | Pro command library | Paid library receives frequent new commands, recipes, diagnostics, and workflow packs. |
| 7 | Advanced expansion | Advanced adds specialized high-skill workflows, admin/dev packs, and deep diagnostics behind stronger approval policy. |

## Non-negotiables

- Do not make Free dependent on private code.
- Do not remove Free features after monetization starts.
- Do not let paid entitlement bypass safety policy.
- Do not hide what Callsign heard or did.
- Do not ship unsigned paid command packs.
- Do not put closed-source material in the public repo.
