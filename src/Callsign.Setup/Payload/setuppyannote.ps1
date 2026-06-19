param(
    [string]$PythonCommand,
    [string[]]$PythonArgs = @(),
    [string]$PythonRuntimePath,
    [string]$HfToken,
    [switch]$InstallPythonPackages,
    [switch]$DownloadModel,
    [switch]$AllowOnlineInstall,
    [switch]$TestEmbedding,
    [switch]$CheckOnly
)

$ErrorActionPreference = "Stop"

$runtimeRoot = Join-Path $env:LOCALAPPDATA "Callsign\Runtime\pyannote"
$venvDir = Join-Path $runtimeRoot "venv"
$venvPython = Join-Path $venvDir "Scripts\python.exe"
$modelCache = Join-Path $runtimeRoot "hub"
$runtimeManifestDir = Join-Path $env:LOCALAPPDATA "Callsign\Runtime\manifests"
$pythonRuntimeManifest = Join-Path $runtimeManifestDir "python-runtime.manifest.json"
$pyannoteWheelhouseManifest = Join-Path $runtimeManifestDir "pyannote-wheelhouse.manifest.json"
$pyannoteModelCacheManifest = Join-Path $runtimeManifestDir "pyannote-model-cache.manifest.json"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$bundledWheelhouse = Join-Path $scriptDir "pyannote-wheelhouse"
$bundledPyannoteSourceCandidates = @(
    (Join-Path $scriptDir "pyannote_audio-4.0.4.tar.gz"),
    (Join-Path $scriptDir "closed-source\pyannote_audio-4.0.4.tar.gz")
)
$bundledPyannoteSource = $bundledPyannoteSourceCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
$logDir = Join-Path $env:LOCALAPPDATA "Callsign\Logs"
$setupLogPath = Join-Path $logDir "pyannote-setup.log"
$packageList = @("pyannote.audio", "torch", "torchaudio", "numpy", "scipy", "soundfile", "huggingface_hub", "omegaconf")

function Write-SetupLog {
    param([string]$Message)
    try {
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
        Add-Content -LiteralPath $setupLogPath -Value "$((Get-Date).ToString("o")) $Message"
    }
    catch {
    }
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

function Write-Manifest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [hashtable]$Data
    )

    New-Item -ItemType Directory -Path $runtimeManifestDir -Force | Out-Null
    $Data.updatedUtc = (Get-Date).ToUniversalTime().ToString("o")
    $Data | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Get-PythonCandidates {
    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($PythonCommand)) {
        $candidates += @{ Command = $PythonCommand; Args = $PythonArgs }
    }

    $candidates += @{ Command = "python"; Args = @() }
    $candidates += @{ Command = "py"; Args = @("-3") }
    return $candidates
}

function Resolve-BundledPythonPath {
    if (-not [string]::IsNullOrWhiteSpace($PythonRuntimePath) -and (Test-Path -LiteralPath $PythonRuntimePath)) {
        return $PythonRuntimePath
    }

    $candidatePaths = @(
        (Join-Path $env:LOCALAPPDATA "Callsign\Runtime\python310\python.exe"),
        (Join-Path $env:LOCALAPPDATA "Callsign\Runtime\pyannote\python310\python.exe"),
        (Join-Path $scriptDir "python-runtime-win-x64\python.exe"),
        (Join-Path $scriptDir "closed-source\python-runtime-win-x64\python.exe")
    )

    foreach ($candidate in $candidatePaths) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    return $null
}

function Resolve-PyannoteWheelhousePath {
    $candidatePaths = @(
        (Join-Path $scriptDir "pyannote-wheelhouse"),
        (Join-Path $scriptDir "closed-source\pyannote-wheelhouse"),
        (Join-Path (Split-Path -Parent $scriptDir) "pyannote-wheelhouse")
    )

    foreach ($candidate in $candidatePaths) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    return $null
}

function Ensure-BundledPythonRuntime {
    if (Test-Path -LiteralPath $venvPython) {
        Write-Host "Using existing bundled Python runtime: $venvPython" -ForegroundColor Green
        return
    }

    $bootstrapPython = Resolve-BundledPythonPath
    $bootstrapArgs = @()
    if ([string]::IsNullOrWhiteSpace($bootstrapPython)) {
        if (-not $AllowOnlineInstall -and [string]::IsNullOrWhiteSpace($PythonCommand)) {
            throw "Bundled CPython runtime was not found. Restore the packaged runtime to '$env:LOCALAPPDATA\Callsign\Runtime\python310' or pass -AllowOnlineInstall/-PythonCommand for a developer repair."
        }

        Write-Host "Bundled CPython runtime was not found; falling back to a developer Python command." -ForegroundColor Yellow
        foreach ($candidate in (Get-PythonCandidates)) {
            try {
                Write-Host "Checking Python candidate: $($candidate.Command) $($candidate.Args -join ' ')" -ForegroundColor DarkCyan
                & $candidate.Command @($candidate.Args + @("-c", "import sys")) *> $null
                if ($LASTEXITCODE -eq 0) {
                    $bootstrapPython = $candidate.Command
                    $bootstrapArgs = @($candidate.Args)
                    break
                }
            }
            catch {
            }
        }
    }

    if ([string]::IsNullOrWhiteSpace($bootstrapPython)) {
        throw "Python was not found. Restore the packaged CPython runtime or pass -PythonCommand."
    }

    Write-Host "Creating bundled Python runtime at $venvDir..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $runtimeRoot -Force | Out-Null
    Invoke-Native -FilePath $bootstrapPython -Arguments ($bootstrapArgs + @("-m", "venv", $venvDir))
}

function Test-PyannoteRuntime {
    if (-not (Test-Path -LiteralPath $venvPython)) {
        return $false
    }

    try {
        & $venvPython -W ignore -c "import pyannote.audio, torch, torchaudio, numpy, scipy, soundfile, huggingface_hub, omegaconf" *> $null
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $false
    }
}

function Set-PyannoteCacheEnvironment {
    New-Item -ItemType Directory -Path $modelCache -Force | Out-Null
    $env:HF_HOME = $modelCache
    $env:HUGGINGFACE_HUB_CACHE = Join-Path $modelCache "hub"
    $env:CALLSIGN_PYANNOTE_CACHE = $modelCache
    $env:HF_HUB_DISABLE_SYMLINKS_WARNING = "1"
}

function Test-PyannoteModelCache {
    if (-not (Test-Path -LiteralPath $modelCache)) {
        return $false
    }

    $modelRoots = Get-ChildItem -LiteralPath $modelCache -Directory -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -eq "models--pyannote--embedding" -or $_.FullName -like "*pyannote*embedding*" } |
        Select-Object -First 1
    if ($modelRoots) {
        return $true
    }

    $config = Get-ChildItem -LiteralPath $modelCache -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -in @("config.yaml", "pytorch_model.bin", "model.safetensors") } |
        Select-Object -First 1
    return $null -ne $config
}

function Test-PyannoteBundleReady {
    if (-not (Test-Path -LiteralPath $pythonRuntimeManifest) -or
        -not (Test-Path -LiteralPath $pyannoteWheelhouseManifest) -or
        -not (Test-Path -LiteralPath $pyannoteModelCacheManifest)) {
        return $false
    }

    if (-not (Test-Path -LiteralPath $venvPython) -or -not (Test-Path -LiteralPath $modelCache)) {
        return $false
    }

    return Test-PyannoteRuntime -and Test-PyannoteModelCache
}

function Install-PyannotePackages {
    if (Test-PyannoteBundleReady) {
        Write-Host "pyannote runtime already ready, skipping package install." -ForegroundColor Green
        Write-SetupLog "pyannote runtime cache hit."
        return
    }

    Ensure-BundledPythonRuntime
    if (Test-PyannoteRuntime) {
        Write-Host "pyannote packages are already installed in the bundled Callsign runtime. Skipping package installation." -ForegroundColor Green
        Write-SetupLog "pyannote package installation skipped because the runtime was already ready."
        return
    }

    $wheelhouse = Resolve-PyannoteWheelhousePath
    if ([string]::IsNullOrWhiteSpace($wheelhouse)) {
        if (-not $AllowOnlineInstall) {
            throw "Bundled pyannote wheelhouse was not found. Restore 'pyannote-wheelhouse' under the installer payload or pass -AllowOnlineInstall for a developer repair."
        }

        Write-Host "Bundled pyannote wheelhouse was not found; using online wheels as a developer fallback." -ForegroundColor Yellow
    }

    $env:PIP_PROGRESS_BAR = "on"
    $env:PYTHONUNBUFFERED = "1"
    $env:PIP_DISABLE_PIP_VERSION_CHECK = "1"
    $env:PIP_NO_INPUT = "1"
    Write-Host "Installing pyannote packages into the bundled Callsign runtime..." -ForegroundColor Cyan
    Write-Host "Step 1/4: verifying pip is available in the bundled runtime..." -ForegroundColor Cyan
    try {
        Invoke-Native -FilePath $venvPython -Arguments @("-m", "pip", "--version")
    }
    catch {
        Write-Host "Bundled pip was missing; bootstrapping it with ensurepip..." -ForegroundColor Yellow
        Invoke-Native -FilePath $venvPython -Arguments @("-m", "ensurepip", "--upgrade")
    }
    if ([string]::IsNullOrWhiteSpace($wheelhouse)) {
        Write-Host "Step 2/4: installing pyannote from online wheels..." -ForegroundColor Cyan
    }
    else {
        Write-Host "Step 2/4: installing pyannote from bundled wheels..." -ForegroundColor Cyan
    }
    $installArgs = @("-m", "pip", "install", "--progress-bar", "on", "--only-binary=:all:", "--no-compile", "--disable-pip-version-check", "--no-input")
    if (-not [string]::IsNullOrWhiteSpace($wheelhouse)) {
        $installArgs += @("--no-index", "--find-links", $wheelhouse)
    }
    $installArgs += $packageList
    Invoke-Native -FilePath $venvPython -Arguments $installArgs
    Write-Host "Step 3/4: verifying the pyannote package import path..." -ForegroundColor Cyan
    Invoke-Native -FilePath $venvPython -Arguments @("-c", "import pyannote.audio, torch, torchaudio, numpy, scipy, soundfile, huggingface_hub, omegaconf")
    Write-Manifest -Path $pythonRuntimeManifest -Data @{
        kind = "python-runtime"
        path = $venvPython
        source = "setuppyannote.ps1"
    }
    Write-Manifest -Path $pyannoteWheelhouseManifest -Data @{
        kind = "pyannote-wheelhouse"
        path = $wheelhouse
        source = "setuppyannote.ps1"
    }
    Write-Host "Step 4/4: pyannote package installation completed." -ForegroundColor Green
}

function Get-EffectiveToken {
    if (-not [string]::IsNullOrWhiteSpace($HfToken)) {
        return $HfToken
    }

    if (-not [string]::IsNullOrWhiteSpace($env:HF_TOKEN)) {
        return $env:HF_TOKEN
    }

    if (-not [string]::IsNullOrWhiteSpace($env:HUGGINGFACE_TOKEN)) {
        return $env:HUGGINGFACE_TOKEN
    }

    return $null
}

function Download-PyannoteModel {
    Ensure-BundledPythonRuntime
    if (Test-PyannoteBundleReady) {
        Write-Host "pyannote model cache already present, skipping download." -ForegroundColor Green
        Write-SetupLog "pyannote model cache hit."
        return
    }
    if (-not (Test-PyannoteRuntime)) {
        Write-Host "pyannote import check failed, repairing package install first..." -ForegroundColor Yellow
        Install-PyannotePackages
    }

    Set-PyannoteCacheEnvironment
    if (Test-PyannoteModelCache) {
        Write-Host "Using bundled pyannote/embedding model cache: $modelCache" -ForegroundColor Green
        Write-SetupLog "Using bundled pyannote model cache at '$modelCache'."
        Write-Manifest -Path $pyannoteModelCacheManifest -Data @{
            kind = "pyannote-model-cache"
            path = $modelCache
            source = "setuppyannote.ps1"
        }
        return
    }

    $token = Get-EffectiveToken
    if ([string]::IsNullOrWhiteSpace($token)) {
        throw "pyannote/embedding model cache is missing. Repair or reinstall Callsign with the bundled pyannote model cache, or use HF_TOKEN only for a developer download after accepting the model terms."
    }

    $env:HF_TOKEN = $token

    $hf = Get-Command hf -ErrorAction SilentlyContinue
    if ($hf) {
        Write-Host "Downloading pyannote/embedding with hf CLI into $modelCache..." -ForegroundColor Cyan
        & $hf.Source download pyannote/embedding --cache-dir $modelCache *> $null
        if ($LASTEXITCODE -eq 0) {
            return
        }
    }

    Write-Host "Downloading pyannote/embedding with huggingface_hub into $modelCache..." -ForegroundColor Cyan
    Invoke-Native -FilePath $venvPython -Arguments @("-c", "import os; from huggingface_hub import snapshot_download; snapshot_download('pyannote/embedding', cache_dir=os.environ['CALLSIGN_PYANNOTE_CACHE'], token=os.environ.get('HF_TOKEN'))")
    Write-Manifest -Path $pyannoteModelCacheManifest -Data @{
        kind = "pyannote-model-cache"
        path = $modelCache
        source = "setuppyannote.ps1"
    }
}

Write-SetupLog "pyannote setup started. InstallPythonPackages=$InstallPythonPackages DownloadModel=$DownloadModel TestEmbedding=$TestEmbedding CheckOnly=$CheckOnly"
New-Item -ItemType Directory -Path $runtimeRoot -Force | Out-Null
Set-PyannoteCacheEnvironment

Write-Host "Callsign pyannote identity setup" -ForegroundColor Cyan
Write-Host "Open-source components and sources:"
Write-Host "  pyannote.audio source tarball: closed-source\pyannote_audio-4.0.4.tar.gz (packaged with the installer when present)"
Write-Host "  pyannote.audio / pyannote.embedding: https://github.com/pyannote/pyannote-audio/ and https://huggingface.co/pyannote/embedding"
Write-Host "  PyTorch / torchaudio: https://pytorch.org/"
Write-Host "  NumPy: https://numpy.org/"
Write-Host "  SciPy: https://scipy.org/"
Write-Host "  SoundFile/libsndfile: https://python-soundfile.readthedocs.io/"
Write-Host "  Hugging Face Hub: https://huggingface.co/docs/huggingface_hub"
Write-Host ""
Write-Host "Package source mode: $($(if (Test-Path -LiteralPath $bundledWheelhouse) { 'bundled wheelhouse' } else { 'online package indexes' }))"

if ($InstallPythonPackages) {
    Install-PyannotePackages
    Write-SetupLog "pyannote packages installed."
}

if ($DownloadModel) {
    Download-PyannoteModel
    Write-SetupLog "pyannote model download requested."
}

if ($InstallPythonPackages -and $DownloadModel) {
    Write-Host "Bundled pyannote runtime and model cache steps have finished; running final readiness checks next." -ForegroundColor Cyan
}

$runtimeReady = Test-PyannoteRuntime
$modelReady = Test-PyannoteModelCache
$bundleReady = Test-PyannoteBundleReady
Write-Host ""
Write-Host "Callsign pyannote readiness:" -ForegroundColor Cyan
Write-Host "  Runtime root: $runtimeRoot"
Write-Host "  Bundled Python: $venvPython"
Write-Host "  Runtime/package ready: $runtimeReady"
Write-Host "  Model cache: $modelCache"
Write-Host "  Model cache ready: $modelReady"
Write-Host "  Manifest bundle ready: $bundleReady"
Write-Host "  HF token present: $(-not [string]::IsNullOrWhiteSpace((Get-EffectiveToken)))"

if ($TestEmbedding) {
    $testScript = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "testpyannote.ps1"
    if (-not (Test-Path -LiteralPath $testScript)) {
        throw "testpyannote.ps1 was not found next to setuppyannote.ps1."
    }

    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $testScript
    if ($LASTEXITCODE -ne 0) {
        throw "pyannote embedding test failed."
    }
}

if ($CheckOnly -and (-not $runtimeReady -or -not $modelReady -or -not $bundleReady)) {
    exit 1
}

if ($runtimeReady -and $modelReady -and $bundleReady) {
    Write-Host "pyannote runtime is ready for Callsign." -ForegroundColor Green
    if (Test-Path -LiteralPath $venvPython) {
        Write-Manifest -Path $pythonRuntimeManifest -Data @{
            kind = "python-runtime"
            path = $venvPython
            source = "setuppyannote.ps1"
        }
    }
    if (Test-Path -LiteralPath $wheelhouse) {
        Write-Manifest -Path $pyannoteWheelhouseManifest -Data @{
            kind = "pyannote-wheelhouse"
            path = $wheelhouse
            source = "setuppyannote.ps1"
        }
    }
    if (Test-Path -LiteralPath $modelCache) {
        Write-Manifest -Path $pyannoteModelCacheManifest -Data @{
            kind = "pyannote-model-cache"
            path = $modelCache
            source = "setuppyannote.ps1"
        }
    }
    Write-SetupLog "pyannote readiness passed."
}
else {
    Write-Host "pyannote runtime is not ready. Repair or reinstall Callsign so the bundled packages and pyannote model cache are restored." -ForegroundColor Yellow
    Write-SetupLog "pyannote readiness failed. RuntimeReady=$runtimeReady ModelReady=$modelReady BundleReady=$bundleReady."
}
