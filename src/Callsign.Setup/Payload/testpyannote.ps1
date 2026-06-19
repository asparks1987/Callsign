param(
    [string]$WavPath,
    [string]$PythonPath = "$env:LOCALAPPDATA\Callsign\Runtime\pyannote\venv\Scripts\python.exe"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $PythonPath)) {
    throw "pyannote Python runtime was not found: $PythonPath"
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "callsign-pyannote-test-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
$fixture = if ([string]::IsNullOrWhiteSpace($WavPath)) { Join-Path $tempRoot "fixture.wav" } else { $WavPath }

$script = @'
import json
import math
import os
import struct
import sys
import warnings
import wave

warnings.filterwarnings("ignore")
runtime_root = os.path.join(os.environ.get("LOCALAPPDATA", ""), "Callsign", "Runtime", "pyannote")
model_cache = os.path.join(runtime_root, "hub")
os.environ.setdefault("HF_HOME", model_cache)
os.environ.setdefault("HUGGINGFACE_HUB_CACHE", os.path.join(model_cache, "hub"))
os.environ.setdefault("HF_HUB_DISABLE_SYMLINKS_WARNING", "1")

wav_path = sys.argv[1]
if not os.path.exists(wav_path):
    with wave.open(wav_path, "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(16000)
        frames = bytearray()
        for sample in range(16000):
            value = int(math.sin(2 * math.pi * 180 * sample / 16000) * 0.4 * 32767)
            frames.extend(struct.pack("<h", value))
        wav.writeframes(bytes(frames))

import numpy as np
import torch
from pyannote.audio import Model
import soundfile as sf
from scipy import signal

MODEL_ID = "pyannote/embedding"
token = os.environ.get("HF_TOKEN") or os.environ.get("HUGGINGFACE_TOKEN")
model = Model.from_pretrained(
    MODEL_ID,
    token=token,
    cache_dir=model_cache,
    strict=False,
    local_files_only=not bool(token),
)
samples, sample_rate = sf.read(wav_path, dtype="float32", always_2d=True)
samples = samples.mean(axis=1)
if sample_rate != 16000:
    samples = signal.resample_poly(samples, 16000, sample_rate).astype("float32")
waveform = torch.from_numpy(samples).float().unsqueeze(0)
with torch.no_grad():
    embedding = model(waveform[None])
array = np.asarray(embedding.detach().cpu().numpy()).reshape(-1)
print(json.dumps({"ModelId": MODEL_ID, "Dimensions": int(array.size), "Norm": float(np.linalg.norm(array))}))
'@

$tempScript = Join-Path $tempRoot "pyannote-test.py"
Set-Content -LiteralPath $tempScript -Value $script -Encoding UTF8

try {
    $output = & $PythonPath $tempScript $fixture
    if ($LASTEXITCODE -ne 0) {
        throw "pyannote test failed with exit code $LASTEXITCODE"
    }

    $result = $output | ConvertFrom-Json
    Write-Host "pyannote model: $($result.ModelId)"
    Write-Host "Embedding dimensions: $($result.Dimensions)"
    Write-Host "Embedding norm: $($result.Norm)"
    if ([int]$result.Dimensions -le 0) {
        throw "pyannote produced an empty embedding."
    }

    Write-Host "pyannote embedding test passed." -ForegroundColor Green
}
finally {
    if ([string]::IsNullOrWhiteSpace($WavPath) -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
