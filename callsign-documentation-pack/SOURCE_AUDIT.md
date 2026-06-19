# Existing Documentation Audit

## Scope

This pack was derived from the documentation available in the synced Google Drive folder corresponding to `D:\GoogleDrive\dev\Callsign`. It did not execute the local repository, build scripts, service, microphone, installer, or tests.

## Strong existing foundations

The current documentation consistently establishes:

- A local-first, visible voice assistant.
- Windows + WSL-first direction.
- A service-driven `wake -> identity -> command` flow.
- openWakeWord as the canonical wake event.
- A local profile store.
- An animated overlay and live readout.
- Visible Start menu app launch as the first action.
- Free Alpha v1 capabilities through at least beta.
- A future local stdio MCP server.
- UI Automation before SendInput.
- Policy outside the model.
- Sensitive-by-default observation data.
- Explicit blocked credential, shell, payment, deletion, and stealth behavior.

## Reconciliation issues

### 1. Two root files named `README.md`

The Drive inventory contains:

- A newer README describing the implemented UI/service Alpha path.
- An older README describing a documentation-first starter kit whose `src` and `tests` were placeholders.

Recommendation: keep the newer current README. Move the older design history to `docs/archive/INITIAL_DESIGN_STARTER.md` only if its history is useful.

### 2. Missing canon file

The current README says `CANON.md` and `docs/reference/CANON.md` are canonical, but neither appeared in the documentation inventory.

Recommendation: add the canon files from this pack and make precedence explicit.

### 3. `v1.0 alpha` versus `Alpha v1`

Some documents define the full Alpha v1 testing target as app launch, dictation, browser control, and file search. Other documents correctly treat those as `v1.0`, `v1.1`, `v1.2`, and `v1.3`.

Recommendation: call `v1.0 alpha` the current release and `Alpha v1` the release family. A v1.0 readiness gate must not require later-release capability.

### 4. Current versus future architecture tense

Older docs describe the service and automation layers as future. Newer docs describe `Callsign.Service` as present.

Recommendation: split every architecture page into `Current`, `Target`, and `Deferred` sections.

### 5. Identity assurance

Documents sometimes use “identity verification” without stating the security strength of the voice/callsign mechanism.

Recommendation: describe it as an Alpha voice/callsign gate that reduces accidental activation. Do not imply high-assurance biometric authentication without a tested design.

### 6. Status evidence

The root burndown marks many tasks done or in progress, while several release gates still require installed, human-spoken evidence.

Recommendation: separate `Documented done` from `Verified`. Do not compute a release percentage from self-reported task labels alone.

### 7. Generated-site drift

Canonical Markdown and generated HTML coexist. The build script is the intended synchronizer, but the current docs still mention drift checks as in progress.

Recommendation: add CI that regenerates the site and fails on a dirty diff.

### 8. Licensing

The product is called open source, and an old layout lists `LICENSE`, but no license file was established during this documentation review.

Recommendation: select and publish a license before broad distribution or contribution intake.

### 9. Voice data lifecycle

The docs distinguish enrollment metadata from local state, but they do not fully specify raw sample format, encryption, retention, deletion, model cache behavior, or threshold calibration.

Recommendation: complete the privacy and storage specifications before claiming production-grade identity protection.

### 10. Platform matrix

Windows-first is clear, but supported Windows versions, CPU architectures, service privileges, audio devices, scaling, multi-monitor behavior, and WSL/Linux support levels are not formally tabulated.

Recommendation: adopt a tested support matrix and link every release claim to evidence.

## Pack strategy

This pack:

- Adds a single canon.
- Preserves existing Alpha decisions.
- Labels future MCP work as future.
- Separates v1.0 from the wider Alpha family.
- Adds privacy, operations, accessibility, schemas, evidence templates, and release gates.
- Imports current statuses without upgrading them to verified.
- Leaves generated HTML to the existing site builder.
