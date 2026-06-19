# Open Questions and Decision Gates

These questions block strong product, security, or release claims.

## Product and governance

- What license governs the public repository?
- Who owns product, security, release, and documentation decisions?
- What is the support commitment during Alpha?
- Are Free, Pro, and Advanced still intended names?
- Which Alpha capabilities are permanently free?
- What user research validates the wake/identity interaction?

## Platform

- Which Windows versions and editions are supported?
- x64 only, or Arm64?
- Windows service, scheduled task, startup app, or hybrid?
- What is the authenticated UI/service IPC mechanism?
- How do Fast User Switching and multiple interactive sessions behave?
- What exact WSL/Linux capabilities are intended without shell exposure?

## Voice and identity

- Which microphone API and sample format are canonical?
- Which openWakeWord model version and threshold?
- Which speaker-verification model and license?
- Does the gate require transcript match, speaker match, or both?
- What false-accept/false-reject targets are acceptable?
- Is replay/liveness detection in scope?
- Are raw enrollment samples retained?
- How are embeddings protected and migrated?
- Which languages, accents, and speech differences are supported?

## Privacy

- Is all command transcription local in Alpha?
- Which cloud providers may be optional?
- What data can leave the device?
- What is the retention schedule?
- Is telemetry off by default?
- What does uninstall remove?
- How does a user export or delete all personal data?

## Architecture

- What are the stable assembly boundaries?
- Is the future MCP server a separate process/repository/package?
- How are capabilities negotiated?
- How are tool/version/schema compatibility handled?
- What audit guarantees are required when storage fails?
- How are browser adapters isolated?
- How are model/runtime dependencies updated?

## Release

- What are the canonical build and verifier scripts after repository cleanup?
- What CI provider/matrix is required?
- How are artifacts signed?
- What is the update/rollback strategy?
- What clean-machine environments are required?
- Where are evidence reports published?
- What is the Alpha crash/reporting channel?

Each answer should become canon, an ADR, a schema, or a release requirement—not remain buried in an issue thread.
