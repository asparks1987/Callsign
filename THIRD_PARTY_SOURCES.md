# Callsign Third-Party Voice Runtime Sources

Callsign bundles or can package the following open-source runtime components for local voice wake detection, transcription, file search, and speaker identity. Keep proprietary models, private caches, and paid-tier assets under `closed-source/`.

- CPython 3.10 for Windows x64: https://www.python.org/
- openWakeWord: https://github.com/dscripka/openWakeWord
- openWakeWord feature models used by the ONNX preprocessing pipeline: `melspectrogram.onnx` and `embedding_model.onnx` from openWakeWord release assets.
- ONNX Runtime: https://onnxruntime.ai/
- pyannote.audio: https://github.com/pyannote/pyannote-audio/
- pyannote/embedding: https://huggingface.co/pyannote/embedding
- PyTorch and torchaudio: https://pytorch.org/
- NumPy: https://numpy.org/
- SciPy: https://scipy.org/
- SoundFile/libsndfile: https://python-soundfile.readthedocs.io/
- Hugging Face Hub: https://huggingface.co/docs/huggingface_hub
- fzf: https://github.com/junegunn/fzf

Release builders should package the runtime, wheelhouses, and model caches into prebuilt installer artifacts. Public installs should not build these components from source and should only fall back to online package resolution in explicitly marked developer repair flows.

The `pyannote/embedding` model is gated on Hugging Face. The release build must only package a local cache after the project has accepted the model terms and confirmed redistribution requirements.
