param(
    [string]$ModelPath,
    [string]$PythonRuntimePath,
    [string]$PythonCommand,
    [string[]]$PythonArgs = @(),
    [switch]$InstallPythonPackages,
    [switch]$AllowOnlineInstall,
    [switch]$CheckOnly,
    [switch]$RestartCallsign
)

$ErrorActionPreference = "Stop"

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
        (Join-Path $env:LOCALAPPDATA "Callsign\Runtime\openwakeword\python310\python.exe"),
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

function Resolve-OpenWakeWordWheelhousePath {
    $candidatePaths = @(
        (Join-Path $scriptDir "openwakeword-wheelhouse"),
        (Join-Path $scriptDir "closed-source\openwakeword-wheelhouse"),
        (Join-Path (Split-Path -Parent $scriptDir) "openwakeword-wheelhouse")
    )

    foreach ($candidate in $candidatePaths) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    return $null
}

function Ensure-BundledRuntimeDirectory {
    New-Item -ItemType Directory -Path $runtimeRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $venvDir -Force | Out-Null
}

function Test-BundledPythonRuntime {
    if (-not (Test-Path $venvPython)) {
        return $null
    }

    try {
        & $venvPython -c "import pathlib, openwakeword, onnxruntime, numpy; root=pathlib.Path(openwakeword.__file__).parent/'resources'/'models'; missing=[name for name in ['melspectrogram.onnx','embedding_model.onnx'] if not (root/name).exists()]; raise SystemExit(1 if missing else 0)" *> $null
        if ($LASTEXITCODE -eq 0) {
            return $venvPython
        }
    }
    catch {
        # Try the repair path below.
    }

    return $null
}

function Get-OpenWakeWordPackageResourceDir {
    if (-not (Test-Path $venvPython)) {
        return $null
    }

    $path = & $venvPython -c "import pathlib, openwakeword; print(pathlib.Path(openwakeword.__file__).parent / 'resources' / 'models')" 2>$null
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($path)) {
        return $null
    }

    return [string]$path
}

function Test-OpenWakeWordBundleReady {
    if (-not (Test-Path -LiteralPath $pythonRuntimeManifest) -or
        -not (Test-Path -LiteralPath $openWakeWordWheelhouseManifest) -or
        -not (Test-Path -LiteralPath $openWakeWordResourcesManifest) -or
        -not (Test-Path -LiteralPath $wakeModelManifest)) {
        return $false
    }

    if (-not (Test-Path -LiteralPath $venvPython) -or -not (Test-Path -LiteralPath $targetModel)) {
        return $false
    }

    if (-not (Test-Path -LiteralPath (Join-Path $bundledResourceDir "melspectrogram.onnx")) -or
        -not (Test-Path -LiteralPath (Join-Path $bundledResourceDir "embedding_model.onnx"))) {
        return $false
    }

    try {
        & $venvPython -c "import pathlib, openwakeword, onnxruntime, numpy; root=pathlib.Path(openwakeword.__file__).parent/'resources'/'models'; missing=[name for name in ['melspectrogram.onnx','embedding_model.onnx'] if not (root/name).exists()]; raise SystemExit(1 if missing else 0)" *> $null
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $false
    }
}

function Repair-OpenWakeWordFeatureModels {
    if (Test-OpenWakeWordBundleReady) {
        Write-Host "openWakeWord feature resources already match the installed bundle, skipping extraction." -ForegroundColor Green
        Write-SetupLog "openWakeWord feature resources cache hit."
        return
    }

    if (-not (Ensure-BundledPythonRuntime)) {
        throw "Unable to create the bundled Callsign Python runtime at $venvDir."
    }

    $resourceDir = Get-OpenWakeWordPackageResourceDir
    if ([string]::IsNullOrWhiteSpace($resourceDir)) {
        throw "Unable to locate the openWakeWord package resources directory."
    }

    New-Item -ItemType Directory -Path $resourceDir -Force | Out-Null
    if (Test-Path -LiteralPath $bundledResourceDir) {
        Write-Host "Restoring bundled openWakeWord feature models from $bundledResourceDir..." -ForegroundColor Cyan
        foreach ($modelName in $requiredFeatureModels) {
            $source = Join-Path $bundledResourceDir $modelName
            if (-not (Test-Path -LiteralPath $source)) {
                throw "Bundled openWakeWord resource is missing: $source"
            }

            Copy-Item -LiteralPath $source -Destination (Join-Path $resourceDir $modelName) -Force
            Write-Host "  restored $modelName" -ForegroundColor Green
        }

        return
    }

    Write-Host "Bundled openWakeWord feature models were not found; downloading openWakeWord feature models for this developer install..." -ForegroundColor Yellow
    Invoke-Native -FilePath $venvPython -Arguments @("-c", "import openwakeword.utils; openwakeword.utils.download_models(model_names=[], target_directory=r'$resourceDir')")
}

function Ensure-BundledPythonRuntime {
    if (Test-Path $venvPython) {
        Write-Host "Using existing bundled Python runtime: $venvPython" -ForegroundColor Green
        return $true
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
                # Try the next candidate.
            }
        }
    }

    if ([string]::IsNullOrWhiteSpace($bootstrapPython)) {
        return $false
    }

    Write-Host "Creating bundled Python runtime at $venvDir..." -ForegroundColor Cyan
    Ensure-BundledRuntimeDirectory
    Invoke-Native -FilePath $bootstrapPython -Arguments ($bootstrapArgs + @("-m", "venv", $venvDir))
    return Test-Path $venvPython
}

function Install-BundledPythonPackages {
    if (Test-OpenWakeWordBundleReady) {
        Write-Host "openWakeWord runtime already ready, skipping package install." -ForegroundColor Green
        Write-SetupLog "openWakeWord runtime cache hit."
        return
    }

    if (-not (Ensure-BundledPythonRuntime)) {
        throw "Unable to create the bundled Callsign Python runtime at $venvDir."
    }

    $wheelhouse = Resolve-OpenWakeWordWheelhousePath
    if ([string]::IsNullOrWhiteSpace($wheelhouse)) {
        if (-not $AllowOnlineInstall) {
            throw "Bundled openWakeWord wheelhouse was not found. Restore 'openwakeword-wheelhouse' under the installer payload or pass -AllowOnlineInstall for a developer repair."
        }

        Write-Host "Bundled openWakeWord wheelhouse was not found; using online wheels as a developer fallback." -ForegroundColor Yellow
    }

    $env:PIP_PROGRESS_BAR = "on"
    $env:PYTHONUNBUFFERED = "1"
    Write-Host "Installing Python packages into the bundled Callsign runtime..." -ForegroundColor Cyan
    Write-Host "Step 1/3: verifying pip is available in the bundled runtime..." -ForegroundColor Cyan
    try {
        Invoke-Native -FilePath $venvPython -Arguments @("-m", "pip", "--version")
    }
    catch {
        Write-Host "Bundled pip was missing; bootstrapping it with ensurepip..." -ForegroundColor Yellow
        Invoke-Native -FilePath $venvPython -Arguments @("-m", "ensurepip", "--upgrade")
    }
    if ([string]::IsNullOrWhiteSpace($wheelhouse)) {
        Write-Host "Step 2/3: installing openWakeWord from online wheels..." -ForegroundColor Cyan
    }
    else {
        Write-Host "Step 2/3: installing openWakeWord from bundled wheels..." -ForegroundColor Cyan
    }
    $openWakeWordArgs = @("-m", "pip", "install", "--progress-bar", "on", "--upgrade", "--only-binary=:all:")
    if (-not [string]::IsNullOrWhiteSpace($wheelhouse)) {
        $openWakeWordArgs += @("--no-index", "--find-links", $wheelhouse)
    }
    $openWakeWordArgs += @("openwakeword", "onnxruntime", "numpy")
    Invoke-Native -FilePath $venvPython -Arguments $openWakeWordArgs
    Write-Host "Step 3/3: openWakeWord package installation completed." -ForegroundColor Green
    Repair-OpenWakeWordFeatureModels
}

function Get-BundledPythonRuntimePath {
    return $venvPython
}

function Resolve-DefaultModelPath {
    $candidatePaths = @(
        (Join-Path $env:LOCALAPPDATA "Callsign\Models\callsign.onnx")
    )

    foreach ($candidate in $candidatePaths) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    return $null
}

$modelDir = Join-Path $env:LOCALAPPDATA "Callsign\Models"
$targetModel = Join-Path $modelDir "callsign.onnx"
$runtimeRoot = Join-Path $env:LOCALAPPDATA "Callsign\Runtime\openwakeword"
$venvDir = Join-Path $runtimeRoot "venv"
$venvPython = Join-Path $venvDir "Scripts\python.exe"
$runtimeManifestDir = Join-Path $env:LOCALAPPDATA "Callsign\Runtime\manifests"
$pythonRuntimeManifest = Join-Path $runtimeManifestDir "python-runtime.manifest.json"
$openWakeWordWheelhouseManifest = Join-Path $runtimeManifestDir "openwakeword-wheelhouse.manifest.json"
$openWakeWordResourcesManifest = Join-Path $runtimeManifestDir "openwakeword-resources.manifest.json"
$wakeModelManifest = Join-Path $runtimeManifestDir "callsign.onnx.manifest.json"
$logDir = Join-Path $env:LOCALAPPDATA "Callsign\Logs"
$setupLogPath = Join-Path $logDir "openwakeword-setup.log"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$bundledResourceDir = Join-Path $scriptDir "openwakeword-resources"
$requiredFeatureModels = @("melspectrogram.onnx", "embedding_model.onnx")

function Write-SetupLog {
    param([string]$Message)

    try {
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
        Add-Content -LiteralPath $setupLogPath -Value "$((Get-Date).ToString("o")) $Message"
    }
    catch {
        # Setup logging is best-effort only.
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

Write-SetupLog "openWakeWord setup helper started. ModelPath='$ModelPath' InstallPythonPackages=$InstallPythonPackages CheckOnly=$CheckOnly RestartCallsign=$RestartCallsign PythonCommand='$PythonCommand'"
Ensure-BundledRuntimeDirectory
Write-Host "Callsign openWakeWord repair/setup started." -ForegroundColor Cyan
Write-Host "Runtime root: $runtimeRoot"
Write-Host "Model target: $targetModel"
Write-Host "Bundled openWakeWord resources: $bundledResourceDir"

if ($InstallPythonPackages) {
    Install-BundledPythonPackages
    Write-SetupLog "Python packages installed or updated."
    if (Test-Path -LiteralPath $venvPython) {
        Write-Manifest -Path $pythonRuntimeManifest -Data @{
            kind = "python-runtime"
            path = $venvPython
            source = "setupopenwakeword.ps1"
        }
    }
}
else {
    Repair-OpenWakeWordFeatureModels
    if (Test-Path -LiteralPath $bundledResourceDir) {
        Write-Manifest -Path $openWakeWordResourcesManifest -Data @{
            kind = "openwakeword-resources"
            path = $bundledResourceDir
            source = "setupopenwakeword.ps1"
        }
    }
}

if ([string]::IsNullOrWhiteSpace($ModelPath)) {
    $ModelPath = Resolve-DefaultModelPath
}

if (-not [string]::IsNullOrWhiteSpace($ModelPath)) {
    if (-not (Test-Path $ModelPath)) {
        throw "The requested Callsign wake model was not found at: $ModelPath"
    }

    if ($ModelPath -ne $targetModel) {
        New-Item -ItemType Directory -Path $modelDir -Force | Out-Null
        Copy-Item -LiteralPath $ModelPath -Destination $targetModel -Force
        Write-Host "Installed custom Callsign openWakeWord model: $targetModel" -ForegroundColor Green
        Write-SetupLog "Installed model to '$targetModel' from '$ModelPath'."
    }
    else {
        Write-Host "Using bundled Callsign openWakeWord model: $targetModel" -ForegroundColor Green
        Write-SetupLog "Using bundled model at '$targetModel'."
    }
}
    elseif (-not (Test-Path $targetModel)) {
    Write-Host "No Callsign openWakeWord model was found in the bundled install location. Reinstall Callsign or repair the installer payload." -ForegroundColor Yellow
    Write-SetupLog "No model available at '$targetModel' and no default source model was found."
}

if (Test-Path -LiteralPath $targetModel) {
    Write-Manifest -Path $wakeModelManifest -Data @{
        kind = "callsign-model"
        path = $targetModel
        source = "setupopenwakeword.ps1"
    }
}

$python = Test-BundledPythonRuntime
$modelPresent = Test-Path $targetModel
$runtimePresent = Test-Path $venvPython
$runtimeReady = $python -ne $null
$bundleReady = Test-OpenWakeWordBundleReady

Write-Host ""
Write-Host "Callsign openWakeWord readiness:" -ForegroundColor Cyan
Write-Host "  Model path: $targetModel"
Write-Host "  Model present: $modelPresent"
Write-Host "  Bundled Python runtime: $venvPython"
Write-Host "  Bundled Python runtime present: $runtimePresent"
Write-Host "  Bundled Python runtime/package ready: $runtimeReady"
Write-Host "  Manifest bundle ready: $bundleReady"

if (-not $modelPresent) {
    Write-Host "  Missing model: use Repair Wakeword or reinstall Callsign so the installed Callsign ONNX model is restored." -ForegroundColor Yellow
}

if (-not $runtimeReady) {
    if (-not $runtimePresent) {
        Write-Host "  Missing bundled runtime: rerun with -InstallPythonPackages so Callsign can create its local Python environment." -ForegroundColor Yellow
    }
    else {
        Write-Host "  Missing packages/runtime: rerun with -InstallPythonPackages so Callsign can repair its local Python environment." -ForegroundColor Yellow
    }
}

if ($CheckOnly -and (-not $modelPresent -or -not $runtimeReady -or -not $bundleReady)) {
    exit 1
}

if ($modelPresent -and $runtimeReady -and $bundleReady) {
    Write-Host "openWakeWord is ready for Callsign." -ForegroundColor Green
    if (Test-Path -LiteralPath $venvPython) {
        Write-Manifest -Path $pythonRuntimeManifest -Data @{
            kind = "python-runtime"
            path = $venvPython
            source = "setupopenwakeword.ps1"
        }
    }
    if (Test-Path -LiteralPath $bundledResourceDir) {
        Write-Manifest -Path $openWakeWordResourcesManifest -Data @{
            kind = "openwakeword-resources"
            path = $bundledResourceDir
            source = "setupopenwakeword.ps1"
        }
    }
    Write-Manifest -Path $openWakeWordWheelhouseManifest -Data @{
        kind = "openwakeword-wheelhouse"
        path = $scriptDir
        source = "setupopenwakeword.ps1"
    }
    Write-Manifest -Path $wakeModelManifest -Data @{
        kind = "callsign-model"
        path = $targetModel
        source = "setupopenwakeword.ps1"
    }
    Write-SetupLog "openWakeWord readiness passed."
}
else {
    Write-Host "Callsign wake events are disabled until openWakeWord is ready." -ForegroundColor Yellow
    Write-SetupLog "openWakeWord readiness failed. ModelPresent=$modelPresent RuntimeReady=$runtimeReady RuntimePresent=$runtimePresent BundleReady=$bundleReady"
}

if ($RestartCallsign) {
    Write-Host ""
    Write-Host "Restarting Callsign so the wake detector can refresh..." -ForegroundColor Cyan
    try {
        sc.exe stop Callsign | Out-Null
        Start-Sleep -Seconds 2
        sc.exe start Callsign | Out-Null
        Write-SetupLog "Windows service restart attempted."
    }
    catch {
        Write-Host "Windows service restart was not available; continuing with the per-user runtime restart." -ForegroundColor Yellow
        Write-SetupLog "Windows service restart failed or was unavailable: $($_.Exception.Message)"
    }

    Get-CimInstance Win32_Process -Filter "Name = 'Callsign.Service.exe'" -ErrorAction SilentlyContinue | Where-Object {
        try {
            ([string]$_.ExecutablePath) -like "*\Callsign\App\Callsign.Service.exe" -and ([string]$_.CommandLine) -match "--user-runtime"
        }
        catch {
            $false
        }
    } | ForEach-Object {
        Stop-Process -Id $_.ProcessId -Force
    }
    Write-SetupLog "Stopped installed per-user Callsign.Service runtime processes before restart."

    $serviceExe = Join-Path $env:LOCALAPPDATA "Callsign\App\Callsign.Service.exe"
    if (Test-Path $serviceExe) {
        Start-Process -FilePath $serviceExe -ArgumentList "--user-runtime --service-installed" -WorkingDirectory (Split-Path -Parent $serviceExe) -WindowStyle Hidden
        Write-Host "Per-user Callsign runtime restarted." -ForegroundColor Green
        Write-SetupLog "Per-user runtime restarted from '$serviceExe'."
    }
    else {
        Write-Host "Installed per-user runtime was not found at $serviceExe. Reinstall Callsign, then rerun with -RestartCallsign." -ForegroundColor Yellow
        Write-SetupLog "Per-user runtime restart failed because '$serviceExe' was missing."
    }
}
