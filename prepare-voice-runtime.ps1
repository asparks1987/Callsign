param(
    [string]$PythonCommand,
    [string[]]$PythonArgs = @(),
    [string]$RuntimeRoot = (Join-Path $PSScriptRoot "closed-source\python-runtime-win-x64"),
    [string]$OpenWakeWordWheelhouse = (Join-Path $PSScriptRoot "closed-source\openwakeword-wheelhouse"),
    [string]$PyannoteWheelhouse = (Join-Path $PSScriptRoot "closed-source\pyannote-wheelhouse"),
    [string]$ManifestPath
)

$ErrorActionPreference = "Stop"

function Get-PythonCandidates {
    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($PythonCommand)) {
        $candidates += @{ Command = $PythonCommand; Args = $PythonArgs }
    }

    $candidatePaths = @(
        (Join-Path $RuntimeRoot "python.exe"),
        (Join-Path $env:LOCALAPPDATA "Callsign\Runtime\python310\python.exe"),
        (Get-Command python -ErrorAction SilentlyContinue),
        (Get-Command py -ErrorAction SilentlyContinue)
    )

    foreach ($candidate in $candidatePaths) {
        if ($candidate -is [System.Management.Automation.CommandInfo]) {
            $args = @()
            if ($candidate.Name -eq "py") {
                $args = @("-3")
            }

            $commandPath = $candidate.Path
            if ([string]::IsNullOrWhiteSpace($commandPath)) {
                $commandPath = $candidate.Source
            }
            if ([string]::IsNullOrWhiteSpace($commandPath)) {
                $commandPath = $candidate.Name
            }

            $candidates += @{ Command = $commandPath; Args = $args }
            continue
        }

        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            $candidates += @{ Command = $candidate; Args = @() }
        }
    }

    return $candidates | Select-Object -Unique -Property Command, Args
}

function Invoke-Native {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    Write-Host "Running: $FilePath $($Arguments -join ' ')" -ForegroundColor DarkCyan
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

function Resolve-PythonRuntime {
    $preferredCandidates = @(
        (Join-Path $RuntimeRoot "python.exe"),
        (Join-Path $env:LOCALAPPDATA "Callsign\Runtime\python310\python.exe")
    )

    foreach ($preferred in $preferredCandidates) {
        if (-not (Test-Path -LiteralPath $preferred)) {
            continue
        }

        try {
            $probe = & $preferred -c "import platform, sys; print(f'{sys.version_info.major}.{sys.version_info.minor} {platform.architecture()[0]}')" 2>$null
            if ($LASTEXITCODE -eq 0 -and $probe -match '^3\.10\s+64bit$') {
                return @{ Command = $preferred; Args = @() }
            }
        }
        catch {
        }
    }

    foreach ($candidate in Get-PythonCandidates) {
        try {
            $probe = & $candidate.Command @($candidate.Args + @("-c", "import platform, sys; print(f'{sys.version_info.major}.{sys.version_info.minor} {platform.architecture()[0]}')")) 2>$null
            if ($LASTEXITCODE -eq 0 -and $probe -match '^3\.10\s+64bit$') {
                return $candidate
            }
        }
        catch {
        }
    }

    throw "No usable CPython 3.10 x64 runtime was found. Restore closed-source\python-runtime-win-x64 or pass -PythonCommand."
}

function Assert-Directory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Label was not found: $Path"
    }
}

function Get-DirectoryInventory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $items = Get-ChildItem -LiteralPath $Path -File -Recurse -ErrorAction Stop
    return [PSCustomObject]@{
        label = $Label
        path = $Path
        file_count = $items.Count
        files = @(
            foreach ($item in $items) {
                [PSCustomObject]@{
                    path = $item.FullName.Substring((Resolve-Path -LiteralPath $Path).Path.Length).TrimStart('\')
                    length = $item.Length
                    sha256 = (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash
                }
            }
        )
    }
}

function Assert-PipDryRun {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PythonPath,
        [Parameter(Mandatory = $true)]
        [string]$WheelhousePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Packages,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $args = @("-m", "pip", "install", "--dry-run", "--only-binary=:all:", "--no-index", "--find-links", $WheelhousePath, "--upgrade") + $Packages
    Write-Host "Verifying $Label can install from bundled wheels..." -ForegroundColor Cyan
    & $PythonPath @args
    if ($LASTEXITCODE -ne 0) {
        throw "$Label wheelhouse verification failed with exit code $LASTEXITCODE."
    }
}

Assert-Directory -Path $RuntimeRoot -Label "Bundled Python runtime"
Assert-Directory -Path $OpenWakeWordWheelhouse -Label "openWakeWord wheelhouse"
Assert-Directory -Path $PyannoteWheelhouse -Label "pyannote wheelhouse"

$python = Resolve-PythonRuntime
$pythonPath = $python.Command
$pythonArgs = @($python.Args)

Write-Host "Using Python runtime: $pythonPath $($pythonArgs -join ' ')" -ForegroundColor Green
& $pythonPath @($pythonArgs + @("-c", "import sys; print(f'Python {sys.version_info.major}.{sys.version_info.minor}.{sys.version_info.micro}')"))
if ($LASTEXITCODE -ne 0) {
    throw "Python runtime validation failed."
}

Assert-PipDryRun -PythonPath $pythonPath -WheelhousePath $OpenWakeWordWheelhouse -Packages @("openwakeword", "onnxruntime", "numpy") -Label "openWakeWord"
Assert-PipDryRun -PythonPath $pythonPath -WheelhousePath $PyannoteWheelhouse -Packages @("pyannote.audio", "torch", "torchaudio", "numpy", "scipy", "soundfile", "huggingface_hub", "omegaconf") -Label "pyannote"

$manifest = [PSCustomObject]@{
    runtime = Get-DirectoryInventory -Path $RuntimeRoot -Label "CPython runtime"
    openwakeword_wheelhouse = Get-DirectoryInventory -Path $OpenWakeWordWheelhouse -Label "openWakeWord wheelhouse"
    pyannote_wheelhouse = Get-DirectoryInventory -Path $PyannoteWheelhouse -Label "pyannote wheelhouse"
}

$json = $manifest | ConvertTo-Json -Depth 6
if (-not [string]::IsNullOrWhiteSpace($ManifestPath)) {
    $manifestDir = Split-Path -Parent $ManifestPath
    if (-not [string]::IsNullOrWhiteSpace($manifestDir)) {
        New-Item -ItemType Directory -Path $manifestDir -Force | Out-Null
    }

    Set-Content -LiteralPath $ManifestPath -Value $json -Encoding UTF8
    Write-Host "Manifest written to $ManifestPath" -ForegroundColor Green
}

Write-Host $json
