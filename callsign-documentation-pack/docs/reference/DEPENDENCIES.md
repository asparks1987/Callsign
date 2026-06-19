# Dependency and Model Management

## Classes

- .NET/runtime libraries.
- Native Windows helpers.
- Python runtime and wheels.
- Wake and feature-extraction models.
- Speaker-verification models.
- Transcription models.
- `fzf`.
- Installer tooling.
- Documentation tooling.

## Required manifest fields

For each dependency/model:

- Name.
- Version/revision.
- Source.
- SHA-256.
- License and notice path.
- Redistribution status.
- Target platform/architecture.
- Direct/transitive classification.
- Purpose.
- Update owner.
- Vulnerability/model review date.
- Network requirement.
- Removal/replacement procedure.

## Rules

- Pin exact versions for release artifacts.
- Hash every bundled executable and model.
- Do not download unpinned code or weights during ordinary install.
- Separate developer restore from user repair.
- Verify model license, not only library license.
- Keep proprietary/private assets outside public tracked source.
- Generate an SBOM before Beta.
- Review native and Python transitive dependencies.
- Fail the build when required notices or manifest entries are missing.
- Test offline install and repair behavior.

## Model changes

A model update can change behavior and privacy just like code.

Require:

- Versioned evaluation fixtures.
- Threshold recalibration.
- Compatibility decision for existing enrollment.
- Rollback.
- Artifact hash.
- License review.
- Release notes.
- Reenrollment path when necessary.
