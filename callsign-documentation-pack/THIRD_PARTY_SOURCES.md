# Callsign Third-Party Runtime Sources

This inventory records the documented source families used or contemplated for local wake detection, transcription, speaker identity, file ranking, and the Windows application runtime.

## Documented components

- CPython runtime for packaged voice helpers.
- openWakeWord and its preprocessing feature models.
- ONNX Runtime.
- pyannote.audio and a speaker-embedding model.
- PyTorch and torchaudio.
- NumPy and SciPy.
- SoundFile/libsndfile.
- Hugging Face Hub tooling.
- `fzf` for ranked file-name matching.
- .NET Windows desktop/runtime dependencies.

## Requirements before distribution

For every bundled component or model, record:

- Exact name and version.
- Source URL and immutable checksum.
- License and notices.
- Redistribution rights for code and model weights.
- Target OS/architecture.
- Build or download provenance.
- Vulnerability-review date.
- Whether network access is required after install.
- Update and rollback procedure.
- Removal procedure.

Public installs should use prebuilt, inspected artifacts. Online dependency repair must be explicit, visible, and documented; it must not silently download executable or model content.

## Private assets

Proprietary models, private caches, paid-tier assets, tokens, and internal mirrors belong outside public tracked source.
