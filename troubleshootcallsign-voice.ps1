param(
    [string]$Callsign = "womprat",
    [int]$SegmentLookbackMinutes = 30,
    [int]$MaxSegments = 20,
    [string]$ReportPath = ".\build\callsign-voice-troubleshooting.json",
    [switch]$EnableWakeDiagnostics
)

$ErrorActionPreference = "Stop"

$root = if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { $PSScriptRoot } else { (Get-Location).Path }
$localRoot = Join-Path $env:LOCALAPPDATA "Callsign"
$profileDir = Join-Path $localRoot "Profiles\$Callsign"
$settingsPath = Join-Path $profileDir "settings.json"
$identityPath = Join-Path $profileDir "voice-identity\embedding.json"
$identityMetadataPath = Join-Path $profileDir "voice-identity\voice-identity.json"
$latestSamplePath = Join-Path $profileDir "voice-samples\latest.wav"
$statePath = Join-Path $localRoot "Runtime\state.json"
$segmentsDir = Join-Path $localRoot "Logs\segments"
$openWakeWordTest = Join-Path $root "testopenwakeword.ps1"
$pyannoteTest = Join-Path $root "testpyannote.ps1"
$wakeModelPath = Join-Path $localRoot "Models\callsign.onnx"
$pyannotePython = Join-Path $localRoot "Runtime\pyannote\venv\Scripts\python.exe"
$pyannoteCache = Join-Path $localRoot "Runtime\pyannote\hub"

function Read-JsonFile {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        return $null
    }

    try {
        return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    }
    catch {
        return [PSCustomObject]@{
            error = $_.Exception.Message
            path = $Path
        }
    }
}

function Get-InstalledProcesses {
    Get-Process -ErrorAction SilentlyContinue |
        Where-Object { $_.ProcessName -like "Callsign*" } |
        ForEach-Object {
            $startTime = $null
            $path = $null
            try { $startTime = $_.StartTime.ToString("O") } catch { }
            try { $path = $_.Path } catch { }
            [PSCustomObject]@{
                process_name = $_.ProcessName
                id = $_.Id
                start_time = $startTime
                path = $path
                has_path = -not [string]::IsNullOrWhiteSpace($path)
            }
        }
}

function Get-UserRuntimeProcesses {
    try {
        Get-CimInstance Win32_Process -Filter "Name = 'Callsign.Service.exe'" |
            Where-Object { $_.CommandLine -like '*--user-runtime*' } |
            ForEach-Object {
                [PSCustomObject]@{
                    process_name = $_.Name
                    process_id = $_.ProcessId
                    command_line = $_.CommandLine
                    start_time = $_.CreationDate
                }
            }
    }
    catch {
        @()
    }
}

function Get-NestedValue {
    param(
        [object]$Object,
        [string[]]$Path,
        [object]$Default = $null
    )

    $current = $Object
    foreach ($name in $Path) {
        if ($null -eq $current) {
            return $Default
        }

        $property = $current.PSObject.Properties[$name]
        if ($null -eq $property) {
            return $Default
        }

        $current = $property.Value
    }

    if ($null -eq $current) { return $Default }
    return $current
}

function Get-WavStats {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return [PSCustomObject]@{
            path = $Path
            exists = $false
        }
    }

    $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    try {
        $reader = [System.IO.BinaryReader]::new($stream)
        $riff = [System.Text.Encoding]::ASCII.GetString($reader.ReadBytes(4))
        [void]$reader.ReadUInt32()
        $wave = [System.Text.Encoding]::ASCII.GetString($reader.ReadBytes(4))
        if ($riff -ne "RIFF" -or $wave -ne "WAVE") {
            throw "Not a RIFF/WAVE file."
        }

        $audioFormat = 0
        $channels = 0
        $sampleRate = 0
        $bitsPerSample = 0
        $data = $null

        while ($stream.Position -lt $stream.Length) {
            $chunkId = [System.Text.Encoding]::ASCII.GetString($reader.ReadBytes(4))
            $chunkSize = [int]$reader.ReadUInt32()
            $chunkStart = $stream.Position

            if ($chunkId -eq "fmt ") {
                $audioFormat = $reader.ReadUInt16()
                $channels = $reader.ReadUInt16()
                $sampleRate = [int]$reader.ReadUInt32()
                [void]$reader.ReadUInt32()
                [void]$reader.ReadUInt16()
                $bitsPerSample = $reader.ReadUInt16()
            }
            elseif ($chunkId -eq "data") {
                $data = $reader.ReadBytes($chunkSize)
            }

            $stream.Position = $chunkStart + $chunkSize
            if (($chunkSize % 2) -eq 1 -and $stream.Position -lt $stream.Length) {
                $stream.Position++
            }
        }

        if ($null -eq $data -or $channels -le 0 -or $sampleRate -le 0) {
            throw "WAV data chunk was not found."
        }

        $sumSquares = 0.0
        $peak = 0.0
        $clipped = 0
        $samples = 0

        if ($audioFormat -eq 1 -and $bitsPerSample -eq 16) {
            for ($index = 0; $index + 1 -lt $data.Length; $index += 2) {
                $value = [System.BitConverter]::ToInt16($data, $index) / 32768.0
                $abs = [Math]::Abs($value)
                $sumSquares += $value * $value
                if ($abs -gt $peak) { $peak = $abs }
                if ($abs -ge 0.98) { $clipped++ }
                $samples++
            }
        }
        elseif ($audioFormat -eq 3 -and $bitsPerSample -eq 32) {
            for ($index = 0; $index + 3 -lt $data.Length; $index += 4) {
                $value = [System.BitConverter]::ToSingle($data, $index)
                $abs = [Math]::Abs($value)
                $sumSquares += $value * $value
                if ($abs -gt $peak) { $peak = $abs }
                if ($abs -ge 0.98) { $clipped++ }
                $samples++
            }
        }

        $rms = if ($samples -gt 0) { [Math]::Sqrt($sumSquares / $samples) } else { 0 }
        $duration = if ($sampleRate -gt 0 -and $channels -gt 0) { $samples / ($sampleRate * $channels) } else { 0 }
        [PSCustomObject]@{
            path = $Path
            exists = $true
            length = (Get-Item -LiteralPath $Path).Length
            last_write_utc = (Get-Item -LiteralPath $Path).LastWriteTimeUtc.ToString("O")
            audio_format = $audioFormat
            channels = $channels
            sample_rate = $sampleRate
            bits_per_sample = $bitsPerSample
            duration_seconds = [Math]::Round($duration, 3)
            rms = [Math]::Round($rms, 6)
            peak = [Math]::Round($peak, 6)
            clipping_ratio = if ($samples -gt 0) { [Math]::Round($clipped / $samples, 6) } else { 0 }
            usable = $samples -gt 0 -and $rms -ge 0.005 -and $peak -ge 0.02
        }
    }
    catch {
        [PSCustomObject]@{
            path = $Path
            exists = $true
            error = $_.Exception.Message
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Invoke-Captured {
    param(
        [string]$FilePath,
        [string[]]$Arguments
    )

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & $FilePath @Arguments 2>&1
        [PSCustomObject]@{
            exit_code = $LASTEXITCODE
            output = @($output | ForEach-Object { "$_" })
        }
    }
    catch {
        [PSCustomObject]@{
            exit_code = if ($LASTEXITCODE -ne $null) { $LASTEXITCODE } else { 1 }
            output = @($_.Exception.Message)
        }
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

function Get-WakeScore {
    param(
        [string]$WavPath,
        [double]$Threshold
    )

    if (-not (Test-Path -LiteralPath $openWakeWordTest) -or -not (Test-Path -LiteralPath $WavPath)) {
        return $null
    }

    $result = Invoke-Captured -FilePath "powershell.exe" -Arguments @(
        "-NoProfile",
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        $openWakeWordTest,
        "-WavPath",
        $WavPath,
        "-Threshold",
        "0"
    )
    $score = $null
    $label = $null
    foreach ($line in $result.output) {
        if ($line -match "openWakeWord score:\s*(?<score>[0-9.eE+-]+)") {
            $score = [double]$Matches.score
        }
        elseif ($line -match "Label:\s*(?<label>.+)$") {
            $label = $Matches.label.Trim()
        }
    }

    [PSCustomObject]@{
        exit_code = $result.exit_code
        score = $score
        label = $label
        threshold = $Threshold
        detected = $score -ne $null -and $score -ge $Threshold
        output = $result.output
    }
}

function Get-PyannoteReadiness {
    param([string]$WavPath)

    if (-not (Test-Path -LiteralPath $pyannoteTest) -or -not (Test-Path -LiteralPath $WavPath)) {
        return $null
    }

    $result = Invoke-Captured -FilePath "powershell.exe" -Arguments @(
        "-NoProfile",
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        $pyannoteTest,
        "-WavPath",
        $WavPath
    )

    [PSCustomObject]@{
        exit_code = $result.exit_code
        passed = $result.exit_code -eq 0
        output = $result.output
    }
}

function Get-PyannoteScore {
    param(
        [string]$WavPath,
        [double]$Threshold,
        [double]$NearMatchThreshold
    )

    if (-not (Test-Path -LiteralPath $pyannotePython) -or -not (Test-Path -LiteralPath $identityPath) -or -not (Test-Path -LiteralPath $WavPath)) {
        return $null
    }

    $tempScript = Join-Path ([System.IO.Path]::GetTempPath()) "callsign-pyannote-score-$([guid]::NewGuid().ToString('N')).py"
    $script = @'
import json
import os
import sys
import warnings

warnings.filterwarnings("ignore")
identity_path, wav_path, model_cache = sys.argv[1:4]
os.environ["HF_HOME"] = model_cache
os.environ["HUGGINGFACE_HUB_CACHE"] = model_cache
os.environ["HF_HUB_OFFLINE"] = "1"
os.environ["HF_HUB_DISABLE_SYMLINKS_WARNING"] = "1"

import numpy as np
import soundfile as sf
import torch
from pyannote.audio import Model
from scipy import signal

with open(identity_path, "r", encoding="utf-8") as handle:
    identity = json.load(handle)

enrolled = np.asarray(identity.get("Embedding") or identity.get("embedding") or [], dtype=np.float32).reshape(-1)
if enrolled.size == 0:
    raise SystemExit("enrollment embedding was empty")

model = Model.from_pretrained("pyannote/embedding", cache_dir=model_cache, strict=False, local_files_only=True)
samples, sample_rate = sf.read(wav_path, dtype="float32", always_2d=True)
samples = samples.mean(axis=1)
if sample_rate != 16000:
    samples = signal.resample_poly(samples, 16000, sample_rate).astype("float32")

waveform = torch.from_numpy(samples).float().unsqueeze(0)
with torch.no_grad():
    embedding = model(waveform[None])
candidate = np.asarray(embedding.detach().cpu().numpy(), dtype=np.float32).reshape(-1)

def normalize(vector):
    norm = float(np.linalg.norm(vector))
    return vector if norm == 0 else vector / norm

score = float(np.dot(normalize(enrolled), normalize(candidate)))
print(json.dumps({
    "score": score,
    "distance": 1.0 - score,
    "dimensions": int(candidate.size),
    "candidate_norm": float(np.linalg.norm(candidate)),
}))
'@

    Set-Content -LiteralPath $tempScript -Value $script -Encoding UTF8
    try {
        $previousErrorActionPreference = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        $output = & $pyannotePython $tempScript $identityPath $WavPath $pyannoteCache 2>&1
        $exitCode = $LASTEXITCODE
        $ErrorActionPreference = $previousErrorActionPreference
        $parsed = $null
        if ($exitCode -eq 0) {
            $jsonLine = @($output | ForEach-Object { "$_" } | Where-Object { $_.Trim().StartsWith("{") }) | Select-Object -Last 1
            if (-not [string]::IsNullOrWhiteSpace($jsonLine)) {
                $parsed = $jsonLine | ConvertFrom-Json
            }
        }

        [PSCustomObject]@{
            exit_code = $exitCode
            score = $parsed.score
            distance = $parsed.distance
            threshold = $Threshold
            near_match_threshold = $NearMatchThreshold
            accepted = $parsed -ne $null -and [double]$parsed.score -ge $Threshold
            output = @($output | ForEach-Object { "$_" })
        }
    }
    catch {
        [PSCustomObject]@{
            exit_code = 1
            error = $_.Exception.Message
        }
    }
    finally {
        Remove-Item -LiteralPath $tempScript -Force -ErrorAction SilentlyContinue
    }
}

function Enable-WakeDiagnostics {
    if (-not (Test-Path -LiteralPath $settingsPath)) {
        return $false
    }

    $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    $settings.Settings.VoiceWakeDiagnosticsEnabled = $true
    $settings | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $settingsPath -Encoding UTF8
    return $true
}

$settings = Read-JsonFile -Path $settingsPath
$identityMetadata = Read-JsonFile -Path $identityMetadataPath
$state = Read-JsonFile -Path $statePath
$wakeThreshold = [double](Get-NestedValue -Object $settings -Path @("Settings", "VoiceWakeThreshold") -Default 0.55)
$biometricThreshold = [double](Get-NestedValue -Object $settings -Path @("Settings", "VoiceBiometricThreshold") -Default 0.72)
$nearMatchThreshold = [double](Get-NestedValue -Object $settings -Path @("Settings", "VoiceBiometricNearMatchThreshold") -Default 0.86)
$diagnosticsEnabled = if ($EnableWakeDiagnostics) { Enable-WakeDiagnostics } else { $false }

$cutoff = (Get-Date).ToUniversalTime().AddMinutes(-[Math]::Abs($SegmentLookbackMinutes))
$segments = @()
if (Test-Path -LiteralPath $segmentsDir) {
    $segments = Get-ChildItem -LiteralPath $segmentsDir -File -Filter "*.wav" -ErrorAction SilentlyContinue |
        Where-Object { $_.LastWriteTimeUtc -ge $cutoff } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First $MaxSegments
}

$segmentReports = foreach ($segment in $segments) {
    $stats = Get-WavStats -Path $segment.FullName
    $wake = Get-WakeScore -WavPath $segment.FullName -Threshold $wakeThreshold
    $biometric = Get-PyannoteScore -WavPath $segment.FullName -Threshold $biometricThreshold -NearMatchThreshold $nearMatchThreshold
    [PSCustomObject]@{
        file = $segment.FullName
        audio = $stats
        wake = $wake
        biometric = $biometric
    }
}

$latestSampleAudio = Get-WavStats -Path $latestSamplePath
$latestSampleWake = Get-WakeScore -WavPath $latestSamplePath -Threshold $wakeThreshold
$latestSamplePyannote = Get-PyannoteReadiness -WavPath $latestSamplePath

$processes = @(Get-InstalledProcesses)
$userRuntimeProcesses = @(Get-UserRuntimeProcesses)
$serviceProcesses = @($processes | Where-Object { $_.process_name -eq "Callsign.Service" })
$duplicateUserRuntimeCount = [Math]::Max(0, $userRuntimeProcesses.Count - 1)
$stateAgeSeconds = $null
if ((Get-NestedValue -Object $state -Path @("UpdatedUtc"))) {
    try {
        $stateAgeSeconds = [Math]::Round(((Get-Date).ToUniversalTime() - [DateTime]::Parse((Get-NestedValue -Object $state -Path @("UpdatedUtc"))).ToUniversalTime()).TotalSeconds, 1)
    }
    catch {
        $stateAgeSeconds = $null
    }
}

$findings = New-Object System.Collections.Generic.List[string]
if ($userRuntimeProcesses.Count -ne 1) {
    $findings.Add("Expected exactly one authoritative user runtime, found $($userRuntimeProcesses.Count).")
}
if ($stateAgeSeconds -ne $null -and $stateAgeSeconds -gt 20) {
    $findings.Add("Runtime state is stale: $stateAgeSeconds seconds since last update.")
}
$runtimeCanHearAudio = Get-NestedValue -Object $state -Path @("CanHearAudio")
$lastAudioPacketUtc = Get-NestedValue -Object $state -Path @("LastAudioPacketUtc")
$secondsSinceLastAudioPacket = Get-NestedValue -Object $state -Path @("SecondsSinceLastAudioPacket")
$activeMicDevice = Get-NestedValue -Object $state -Path @("ActiveMicrophoneDeviceName")
$lastMicRms = Get-NestedValue -Object $state -Path @("LastMicrophoneRawRms")
$lastWakeScore = Get-NestedValue -Object $state -Path @("LastWakeWordScore")
$lastWakeThreshold = Get-NestedValue -Object $state -Path @("WakeWordThreshold")
$samplesEnrolled = Get-NestedValue -Object $identityMetadata -Path @("SamplesEnrolled")
if ($lastMicRms -ne $null -and [double]$lastMicRms -lt 0.005) {
    $findings.Add("Live runtime microphone RMS is very low: $lastMicRms.")
}
if ($runtimeCanHearAudio -eq $false) {
    $findings.Add("Runtime is listening but canHearAudio is false; microphone packet arrival looks stale or silent.")
}
if ($secondsSinceLastAudioPacket -ne $null -and [double]$secondsSinceLastAudioPacket -gt 5) {
    $findings.Add("Last microphone packet is stale: $secondsSinceLastAudioPacket seconds ago.")
}
if ($lastWakeScore -eq $null) {
    $findings.Add("Runtime has not reported a live wake score yet.")
}
elseif ($lastWakeThreshold -ne $null -and [double]$lastWakeScore -lt [double]$lastWakeThreshold) {
    $findings.Add("Runtime heard a wake candidate but rejected it below threshold: score=$lastWakeScore threshold=$lastWakeThreshold.")
}
if ($segments.Count -eq 0) {
    $findings.Add("No retained diagnostic segments were found in the lookback window. Enable wake diagnostics, restart listening, then speak Callsign, womprat, and open Notepad as separate clips.")
}
if ($samplesEnrolled -ne $null -and [int]$samplesEnrolled -lt 3) {
    $findings.Add("Voice identity has only $samplesEnrolled enrolled biometric sample(s); target at least 3 for stronger fingerprinting.")
}
$latestSampleWakeScore = Get-NestedValue -Object $latestSampleWake -Path @("score")
if ($latestSampleWakeScore -ne $null -and [double]$latestSampleWakeScore -ge $wakeThreshold) {
    $findings.Add("Enrolled latest.wav passes wake scoring; if live wake fails, focus on live capture/segmentation.")
}

$report = [PSCustomObject]@{
    generated_utc = (Get-Date).ToUniversalTime().ToString("O")
    callsign = $Callsign
    diagnostics_enabled_this_run = $diagnosticsEnabled
    paths = [PSCustomObject]@{
        local_root = $localRoot
        profile_dir = $profileDir
        state = $statePath
        segments_dir = $segmentsDir
        wake_model = $wakeModelPath
        pyannote_python = $pyannotePython
    }
    runtime = [PSCustomObject]@{
        state_age_seconds = $stateAgeSeconds
        state = $state
        processes = $processes
        user_runtime_processes = $userRuntimeProcesses
        duplicate_user_runtime_count = $duplicateUserRuntimeCount
        active_microphone_device = $activeMicDevice
        last_audio_packet_utc = $lastAudioPacketUtc
        seconds_since_last_audio_packet = $secondsSinceLastAudioPacket
        can_hear_audio = $runtimeCanHearAudio
    }
    profile = [PSCustomObject]@{
        settings = $settings
        identity_metadata = $identityMetadata
        identity_embedding_exists = Test-Path -LiteralPath $identityPath
    }
    enrollment_sample = [PSCustomObject]@{
        audio = $latestSampleAudio
        wake = $latestSampleWake
        pyannote_readiness = $latestSamplePyannote
        sample_files = @(Get-ChildItem -LiteralPath (Join-Path $profileDir "voice-samples") -File -Filter "sample-*.wav" -ErrorAction SilentlyContinue | Sort-Object Name | ForEach-Object {
            [PSCustomObject]@{
                path = $_.FullName
                last_write_utc = $_.LastWriteTimeUtc.ToString("O")
                age_seconds = [Math]::Round(((Get-Date).ToUniversalTime() - $_.LastWriteTimeUtc).TotalSeconds, 1)
                stats = Get-WavStats -Path $_.FullName
            }
        })
    }
    live_segments = $segmentReports
    transcription = [PSCustomObject]@{
        status = "not_exposed"
        note = "Local Whisper transcription is internal to VoiceCommandService. Add a dedicated transcript helper before treating transcription as verified outside the live service."
    }
    findings = $findings.ToArray()
}

$reportRoot = Split-Path -Parent $ReportPath
if (-not [string]::IsNullOrWhiteSpace($reportRoot)) {
    New-Item -ItemType Directory -Path $reportRoot -Force | Out-Null
}
$report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $ReportPath -Encoding UTF8

Write-Host "Callsign voice troubleshooting report: $ReportPath" -ForegroundColor Cyan
Write-Host "User runtime processes: $($userRuntimeProcesses.Count) (duplicate count: $duplicateUserRuntimeCount)"
Write-Host "Runtime state age seconds: $stateAgeSeconds"
Write-Host "Runtime mic level: $($state.LastMicrophoneLevelState), RMS=$($state.LastMicrophoneRawRms), peak=$($state.LastMicrophonePeak)"
Write-Host "Runtime audio: device=$activeMicDevice can_hear=$runtimeCanHearAudio last_packet=$lastAudioPacketUtc age=$secondsSinceLastAudioPacket"
Write-Host "Enrollment wake score: $($latestSampleWake.score) threshold=$wakeThreshold detected=$($latestSampleWake.detected)"
Write-Host "Recent retained segments: $($segments.Count)"
foreach ($finding in $findings) {
    Write-Host "FINDING: $finding" -ForegroundColor Yellow
}
