param(
    [Parameter(Mandatory = $true)]
    [string]$WavPath,
    [string]$ModelPath = "$env:LOCALAPPDATA\Callsign\Models\callsign.onnx",
    [string]$PythonPath = "$env:LOCALAPPDATA\Callsign\Runtime\openwakeword\venv\Scripts\python.exe",
    [double]$Threshold = 0.12,
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
import wave
import sys

import numpy as np
from openwakeword.model import Model

wav_path = sys.argv[1]
model_path = sys.argv[2]
threshold = float(sys.argv[3])

model = Model(
    wakeword_models=[model_path],
    inference_framework="onnx",
    vad_threshold=0.0,
    enable_speex_noise_suppression=False,
)
frame_milliseconds = 80
hop_milliseconds = 40
frame_size = int(16000 * frame_milliseconds / 1000)
hop_size = int(16000 * hop_milliseconds / 1000)
best_label = None
best_score = 0.0


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


with wave.open(wav_path, "rb") as wav_file:
    total_frames = wav_file.getnframes()
    remaining_frames = total_frames
    sample_width = wav_file.getsampwidth()
    channel_count = wav_file.getnchannels()
    target_bytes = frame_size * sample_width * channel_count
    hop_bytes = hop_size * sample_width * channel_count
    buffer = bytearray()

    while remaining_frames > 0:
        raw_frame = wav_file.readframes(min(hop_size, remaining_frames))
        remaining_frames -= min(hop_size, remaining_frames)

        if not raw_frame:
            break

        buffer.extend(raw_frame)

        while len(buffer) >= target_bytes:
            raw_frame = bytes(buffer[:target_bytes])
            del buffer[:hop_bytes]

            frame = np.frombuffer(raw_frame, dtype=np.int16)
            predictions = model.predict(frame)
            for label, score in iter_scores(predictions):
                if score > best_score:
                    best_score = score
                    best_label = label

        if best_score >= threshold:
            break

    if buffer and best_score < threshold:
        if len(buffer) < target_bytes:
            buffer.extend(b"\0" * (target_bytes - len(buffer)))

        frame = np.frombuffer(bytes(buffer[:target_bytes]), dtype=np.int16)
        predictions = model.predict(frame)
        for label, score in iter_scores(predictions):
            if score > best_score:
                best_score = score
                best_label = label

print(json.dumps({"Score": best_score, "Label": best_label}))
'@

$tempScript = Join-Path ([System.IO.Path]::GetTempPath()) "callsign-openwakeword-score.py"
Set-Content -LiteralPath $tempScript -Value $script -Encoding UTF8

try {
    $output = & $python.Command @($python.Args + @($tempScript, $WavPath, $ModelPath, $Threshold.ToString([System.Globalization.CultureInfo]::InvariantCulture)))
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
