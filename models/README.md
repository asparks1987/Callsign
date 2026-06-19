# Callsign wake model

Place a clean custom openWakeWord model here only if it is safe to commit:

```text
models/callsign.onnx
```

For private or proprietary model work, use:

```text
closed-source/models/callsign.onnx
```

`buildcallsign.ps1` will package the first available model from those locations into the installer payload as `callsign.onnx`. The installed app extracts it to:

```text
%LOCALAPPDATA%\Callsign\Models\callsign.onnx
```

Do not commit openWakeWord pretrained model assets unless their license is reviewed and compatible with Callsign distribution.

Callsign now uses openWakeWord as the wake gate. If this model or the Python openWakeWord runtime is missing, wake events are disabled rather than falling back to transcript guessing.

You can also pass a model explicitly at build time:

```powershell
.\buildcallsign.ps1 -WakeModelPath C:\path\to\custom-callsign.onnx
```

Or build, install, and require openWakeWord through the verifier:

```powershell
.\verifycallsign-alpha.ps1 -WakeModelPath C:\path\to\custom-callsign.onnx -Install -RequireOpenWakeWord
```

## Local validation

After installing a clean custom model and Python dependencies, score a recorded wake sample with:

```powershell
.\testopenwakeword.ps1 -WavPath C:\path\to\callsign-sample.wav
```

The script exits successfully only when the model score is at or above the selected threshold.

If Python is installed but is not available as `python` or `py`, pass it explicitly:

```powershell
.\setupopenwakeword.ps1 -ModelPath C:\path\to\custom-callsign.onnx -PythonCommand C:\path\to\python.exe -InstallPythonPackages
.\testopenwakeword.ps1 -WavPath C:\path\to\callsign-sample.wav -PythonCommand C:\path\to\python.exe
```
