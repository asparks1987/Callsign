param(
    [Parameter(Mandatory = $true)]
    [string]$WavPath,
    [string]$ModelPath = "$env:LOCALAPPDATA\Callsign\Models\callsign.onnx",
    [string]$PythonPath = "$env:LOCALAPPDATA\Callsign\Runtime\openwakeword\venv\Scripts\python.exe",
    [double]$Threshold = 0.55,
    [string]$PythonCommand,
    [string[]]$PythonArgs = @()
)

$ErrorActionPreference = "Stop"

function Find-BundledPythonOpenWakeWord {
    if (Test-Path -LiteralPath $PythonPath) {
        try {
            & $PythonPath -c "import pathlib, openwakeword, onnxruntime, numpy; root=pathlib.Path(openwakeword.__file__).parent/'resources'/'models'; missing=[name for name in ['melspectrogram.onnx','embedding_model.onnx'] if not (root/name).exists()]; raise SystemExit(1 if missing else 0)" *> $null
            if ($LASTEXITCODE -eq 0) {
                return @{ Command = $PythonPath; Args = @() }
            }
        }
        catch {
            # Fall through to the repair advice below.
        }
    }

    if ([string]::IsNullOrWhiteSpace($PythonCommand)) {
        return $null
    }

    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($PythonCommand)) {
        $candidates += @{ Command = $PythonCommand; Args = $PythonArgs }
    }

    $candidates += @{ Command = "python"; Args = @() }
    $candidates += @{ Command = "py"; Args = @("-3") }

    foreach ($candidate in $candidates) {
        try {
            & $candidate.Command @($candidate.Args + @("-c", "import pathlib, openwakeword, onnxruntime, numpy; root=pathlib.Path(openwakeword.__file__).parent/'resources'/'models'; missing=[name for name in ['melspectrogram.onnx','embedding_model.onnx'] if not (root/name).exists()]; raise SystemExit(1 if missing else 0)")) *> $null
            if ($LASTEXITCODE -eq 0) {
                return $candidate
            }
        }
        catch {
            # Try next candidate.
        }
    }

    return $null
}

if (-not (Test-Path -LiteralPath $WavPath)) {
    throw "WAV file was not found: $WavPath"
}

if (-not (Test-Path -LiteralPath $ModelPath)) {
    throw "The installed Callsign wake model was not found: $ModelPath"
}

$python = Find-BundledPythonOpenWakeWord
if ($null -eq $python) {
    throw "Bundled openWakeWord runtime or feature models are not ready. Use Repair Wakeword in Callsign to restore the installed runtime and model."
}

$script = @'
import json
import sys
import wave

import numpy as np
from openwakeword.model import Model

wav_path = sys.argv[1]
model_path = sys.argv[2]

model = Model(wakeword_models=[model_path], inference_framework="onnx")
hop_milliseconds = 20

predictions = []
with wave.open(wav_path, "rb") as wav_file:
    sample_rate = wav_file.getframerate()
    frame_size = max(1, int(sample_rate * hop_milliseconds / 1000.0))

    while True:
        raw_frame = wav_file.readframes(frame_size)
        if not raw_frame:
            break

        frame = np.frombuffer(raw_frame, dtype=np.int16)
        if frame.size == 0:
            continue

        predictions.append(model.predict(frame))


def iter_scores(value, label=None):
    if value is None:
        return

    if isinstance(value, dict):
        for label, nested in value.items():
            yield from iter_scores(nested, label)
        return

    if isinstance(value, (list, tuple)):
        if len(value) == 2 and isinstance(value[1], (int, float)):
            yield label, float(value[1])
            return

        for nested in value:
            yield from iter_scores(nested, label)
        return

    if hasattr(value, "tolist"):
        yield from iter_scores(value.tolist(), label)
        return

    try:
        yield label, float(value)
    except Exception:
        return


best_label = None
best_score = 0.0
for label, score in iter_scores(predictions):
    if score > best_score:
        best_score = score
        best_label = label

print(json.dumps({"Score": best_score, "Label": best_label}))
'@

$tempScript = Join-Path ([System.IO.Path]::GetTempPath()) "callsign-openwakeword-score.py"
Set-Content -LiteralPath $tempScript -Value $script -Encoding UTF8

try {
    $output = & $python.Command @($python.Args + @($tempScript, $WavPath, $ModelPath))
    if ($LASTEXITCODE -ne 0) {
        throw "openWakeWord scoring failed with exit code $LASTEXITCODE"
    }

    $result = $output | ConvertFrom-Json
    $detected = [double]$result.Score -ge $Threshold
    Write-Host "openWakeWord score: $($result.Score)"
    Write-Host "Label: $($result.Label)"
    Write-Host "Threshold: $Threshold"
    Write-Host "Detected: $detected"

    if (-not $detected) {
        exit 1
    }
}
finally {
    Remove-Item -LiteralPath $tempScript -Force -ErrorAction SilentlyContinue
}
