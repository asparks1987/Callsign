<#
Builds Callsign for Windows and emits an installer-style executable in repo root.

Behavior:
1. dotnet publish a self-contained Windows desktop binary package.
2. If Inno Setup Compiler (`iscc`) is installed, generate and build a real installer
   named Callsign-Setup.exe in the repo root.
3. If Inno Setup is not available, use Windows IExpress to generate a per-user
   installer executable named Callsign-Setup.exe in the repo root.
4. Always create a root-level portable executable fallback (Callsign-Run.exe).
#>

[CmdletBinding()]
param(
    [string]$ProjectPath,
    [string]$ServiceProjectPath,
    [string]$SetupProjectPath,
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$InstallerName = "Callsign-Setup",
    [string]$PortableName = "Callsign-Run.exe",
    [string]$ServiceName = "Callsign-Service.exe",
    [string]$ProductName = "Callsign",
    [string]$Publisher = "Callsign Project",
    [string]$WakeModelPath,
    [switch]$Clean,
    [switch]$ForceRepackVoiceAssets,
    [switch]$AllowMissingPrivateVoiceAssets,
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

$root = if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    $PSScriptRoot
}
elseif (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
    Split-Path -Parent $PSCommandPath
}
elseif ($MyInvocation.MyCommand.Path) {
    Split-Path -Parent $MyInvocation.MyCommand.Path
}
else {
    (Get-Location).Path
}

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $root "src\Callsign.UI\Callsign.UI.csproj"
}
if ([string]::IsNullOrWhiteSpace($ServiceProjectPath)) {
    $ServiceProjectPath = Join-Path $root "src\Callsign.Service\Callsign.Service.csproj"
}
if ([string]::IsNullOrWhiteSpace($SetupProjectPath)) {
    $SetupProjectPath = Join-Path $root "src\Callsign.Setup\Callsign.Setup.csproj"
}

if (!(Test-Path -LiteralPath $ProjectPath)) {
    throw "Project file not found: $ProjectPath"
}

$buildDir = Join-Path $root "build"
$cacheDir = Join-Path $buildDir "cache"
$cacheManifestPath = Join-Path $cacheDir "build-manifest.json"
$publishDir = Join-Path $buildDir "publish"
$installerOutput = Join-Path $root "$InstallerName.exe"
$portableOutput = Join-Path $root $PortableName
$serviceOutput = Join-Path $root $ServiceName
$servicePublishDir = Join-Path $buildDir "service-publish"
$fzfRepoDir = Join-Path $root "fzf"
$fzfBuildOutput = Join-Path $buildDir "fzf.exe"
$fzfStagedSource = $null
$fzfPrebuiltCandidates = @(
    (Join-Path $root "fzf.exe"),
    (Join-Path $root "closed-source\tools\fzf.exe"),
    (Join-Path $root "tools\fzf.exe")
)
$setupPayloadDir = Join-Path (Split-Path -Parent $SetupProjectPath) "Payload"
$setupPayloadFzf = Join-Path $setupPayloadDir "fzf.exe"
$setupPayloadService = Join-Path $setupPayloadDir "Callsign.Service.exe"
$setupPayloadWakeModel = Join-Path $setupPayloadDir "callsign.onnx"
$setupPayloadIcon = Join-Path $setupPayloadDir "callsign.ico"
$setupPayloadOpenWakeWordSetup = Join-Path $setupPayloadDir "setupopenwakeword.ps1"
$setupPayloadOpenWakeWordTest = Join-Path $setupPayloadDir "testopenwakeword.ps1"
$setupPayloadOpenWakeWordResources = Join-Path $setupPayloadDir "openwakeword-resources.zip"
$setupPayloadOpenWakeWordRuntime = Join-Path $setupPayloadDir "openwakeword-runtime.zip"
$setupPayloadPythonRuntime = Join-Path $setupPayloadDir "python-runtime-win-x64.zip"
$setupPayloadOpenWakeWordWheelhouse = Join-Path $setupPayloadDir "openwakeword-wheelhouse.zip"
$setupPayloadPyannoteSetup = Join-Path $setupPayloadDir "setuppyannote.ps1"
$setupPayloadPyannoteTest = Join-Path $setupPayloadDir "testpyannote.ps1"
$setupPayloadPyannoteRuntime = Join-Path $setupPayloadDir "pyannote-runtime.zip"
$setupPayloadPyannoteWheelhouse = Join-Path $setupPayloadDir "pyannote-wheelhouse.zip"
$setupPayloadPyannoteModelCache = Join-Path $setupPayloadDir "pyannote-model-cache.zip"
$setupPayloadPyannoteSource = Join-Path $setupPayloadDir "pyannote_audio-4.0.4.tar.gz"
$setupPayloadThirdPartySources = Join-Path $setupPayloadDir "THIRD_PARTY_SOURCES.md"
$iconSource = Join-Path $root "assets\callsign.ico"
$openWakeWordSetupSource = Join-Path $root "setupopenwakeword.ps1"
$openWakeWordTestSource = Join-Path $root "testopenwakeword.ps1"
$openWakeWordResourceCandidates = @(
    (Join-Path $root "closed-source\openwakeword-resources"),
    (Join-Path $root "closed-source\openwakeword\models"),
    (Join-Path $root "third-party\openwakeword-resources")
)
$pythonRuntimeCandidates = @(
    (Join-Path $root "closed-source\python-runtime-win-x64"),
    (Join-Path $root "closed-source\python310"),
    (Join-Path $root "third-party\python-runtime-win-x64")
)
$openWakeWordWheelhouseCandidates = @(
    (Join-Path $root "closed-source\openwakeword-wheelhouse"),
    (Join-Path $root "closed-source\openwakeword\wheelhouse"),
    (Join-Path $root "third-party\openwakeword-wheelhouse")
)
$pyannoteSetupSource = Join-Path $root "setuppyannote.ps1"
$pyannoteTestSource = Join-Path $root "testpyannote.ps1"
$pyannoteSourceTarball = Join-Path $root "closed-source\pyannote_audio-4.0.4.tar.gz"
$thirdPartySourcesSource = Join-Path $root "THIRD_PARTY_SOURCES.md"
$pyannoteWheelhouseCandidates = @(
    (Join-Path $root "closed-source\pyannote-wheelhouse"),
    (Join-Path $root "closed-source\python-wheelhouse"),
    (Join-Path $root "third-party\pyannote-wheelhouse")
)
$pyannoteModelCacheCandidates = @(
    (Join-Path $root "closed-source\pyannote-model-cache"),
    (Join-Path $root "closed-source\pyannote\hub"),
    (Join-Path $root "closed-source\pyannote-cache")
)
$runtimeSnapshotDir = Join-Path $buildDir "runtime-snapshots"
$openWakeWordRuntimeSnapshotDir = Join-Path $runtimeSnapshotDir "openwakeword"
$pyannoteRuntimeSnapshotDir = Join-Path $runtimeSnapshotDir "pyannote"
$wakeModelCandidates = @()
if (-not [string]::IsNullOrWhiteSpace($WakeModelPath)) {
    $wakeModelCandidates += $WakeModelPath
}
$wakeModelCandidates += (Join-Path $root "closed-source\callsign.onnx")
$wakeModelCandidates += (Join-Path $root "closed-source\models\callsign.onnx")
$wakeModelCandidates += (Join-Path $root "models\callsign.onnx")
[string[]]$restoreArgs = if ($NoRestore) { @("--no-restore") } else { @() }
$releaseRequiresPrivateVoiceAssets = $Configuration.Equals("Release", [System.StringComparison]::OrdinalIgnoreCase) -and -not $AllowMissingPrivateVoiceAssets

function Copy-WithRetry([string]$Source, [string]$Destination, [int]$Attempts = 8) {
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            Copy-Item -LiteralPath $Source -Destination $Destination -Force
            return
        }
        catch {
            if ($attempt -eq $Attempts) {
                throw
            }

            Start-Sleep -Milliseconds (500 * $attempt)
        }
    }
}

function Build-FzfHelper([string]$RepositoryPath, [string]$OutputPath) {
    if (!(Test-Path -LiteralPath $RepositoryPath)) {
        Write-Warning "fzf repo was not found at $RepositoryPath. Callsign will fall back to built-in file search."
        return $false
    }

    $go = Get-Command go -ErrorAction SilentlyContinue
    if (-not $go) {
        Write-Warning "Go was not found on PATH. Callsign will fall back to built-in file search."
        return $false
    }

    Push-Location $RepositoryPath
    try {
        if (Test-Path -LiteralPath $OutputPath) {
            Remove-Item -LiteralPath $OutputPath -Force
        }

        & $go.Source build -o $OutputPath .
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "fzf build failed with exit code $LASTEXITCODE. Callsign will fall back to built-in file search."
            return $false
        }

        return Test-Path -LiteralPath $OutputPath
    }
    finally {
        Pop-Location
    }
}

function Require-BuildPayload([string]$Path, [string]$Description) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required alpha installer payload missing: $Description ($Path)"
    }
}

function Get-BuildPayloadReport([string]$Path, [string]$Description) {
    if (Test-Path -LiteralPath $Path) {
        $item = Get-Item -LiteralPath $Path
        return [PSCustomObject]@{
            description = $Description
            path = $Path
            present = $true
            length = $item.Length
            last_write_utc = $item.LastWriteTimeUtc.ToString("o")
            sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
        }
    }

    return [PSCustomObject]@{
        description = $Description
        path = $Path
        present = $false
        length = $null
        last_write_utc = $null
        sha256 = $null
    }
}

function Remove-BuildOutputs([string]$ProjectPathToClean) {
    $projectDir = Split-Path -Parent $ProjectPathToClean
    foreach ($folder in @("bin", "obj")) {
        $path = Join-Path $projectDir $folder
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force
        }
    }
}

$script:BuildManifest = $null

function Initialize-BuildCacheManifest {
    New-Item -ItemType Directory -Path $cacheDir -Force | Out-Null

    if (Test-Path -LiteralPath $cacheManifestPath) {
        try {
            $raw = Get-Content -LiteralPath $cacheManifestPath -Raw
            if (-not [string]::IsNullOrWhiteSpace($raw)) {
                $manifest = $raw | ConvertFrom-Json
                if ($manifest -and $manifest.PSObject.Properties.Name -contains "stages") {
                    return $manifest
                }
            }
        }
        catch {
            Write-Warning "Build cache manifest could not be loaded and will be rebuilt: $($_.Exception.Message)"
        }
    }

    [PSCustomObject]@{
        schemaVersion = 1
        stages = [PSCustomObject]@{}
        updatedUtc = $null
    }
}

function Save-BuildCacheManifest {
    param([Parameter(Mandatory = $true)]$Manifest)

    New-Item -ItemType Directory -Path $cacheDir -Force | Out-Null
    $Manifest.updatedUtc = (Get-Date).ToUniversalTime().ToString("o")
    $Manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $cacheManifestPath -Encoding UTF8
}

function Get-StringSha256 {
    param([string]$Value = "")

    if ([string]::IsNullOrWhiteSpace($Value)) {
        $Value = [string]::Empty
    }

    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
        $hash = $sha.ComputeHash($bytes)
        return ([System.BitConverter]::ToString($hash) -replace '-', '').ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
    }
}

function Get-FileFingerprint {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [string[]]$ExcludePatterns = @("\bin\", "\obj\", "\build\", "\.git\", "\artifacts\")
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return "empty:"
    }

    if (-not (Test-Path -LiteralPath $Path)) {
        return "missing:$Path"
    }

    $item = Get-Item -LiteralPath $Path
    if ($item.PSIsContainer) {
        return Get-DirectoryFingerprint -Path $Path -ExcludePatterns $ExcludePatterns
    }

    try {
        $hash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    }
    catch {
        $hash = "locked:$($item.Length):$($item.LastWriteTimeUtc.ToString('o'))"
    }

    return "file:$Path|$($item.Length)|$($item.LastWriteTimeUtc.ToString('o'))|$hash"
}

function Get-DirectoryFingerprint {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [string[]]$ExcludePatterns = @("\bin\", "\obj\", "\build\", "\.git\", "\artifacts\")
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return "empty:"
    }

    if (-not (Test-Path -LiteralPath $Path)) {
        return "missing:$Path"
    }

    $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
    $files = Get-ChildItem -LiteralPath $resolvedPath -Recurse -File -Force | Where-Object {
        $fullName = $_.FullName
        $excluded = $false
        foreach ($pattern in $ExcludePatterns) {
            if ($fullName -like "*$pattern*") {
                $excluded = $true
                break
            }
        }

        -not $excluded
    } | Sort-Object FullName

    $builder = [System.Text.StringBuilder]::new()
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($resolvedPath.Length).TrimStart('\', '/')
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        [void]$builder.AppendLine("$relative|$($file.Length)|$($file.LastWriteTimeUtc.ToString('o'))|$hash")
    }

    return "dir:$Path|$($files.Count)|$(Get-StringSha256 -Value $builder.ToString())"
}

function Get-PathsFingerprint {
    param(
        [Parameter(Mandatory = $true)][string[]]$Paths,
        [string[]]$ExcludePatterns = @("\bin\", "\obj\", "\build\", "\.git\", "\artifacts\")
    )

    $entries = foreach ($path in $Paths) {
        if ([string]::IsNullOrWhiteSpace($path)) {
            continue
        }

        Get-FileFingerprint -Path $path -ExcludePatterns $ExcludePatterns
    }

    return Get-StringSha256 -Value (($entries | Sort-Object) -join "`n")
}

function Get-ManifestStage {
    param(
        [Parameter(Mandatory = $true)]$Manifest,
        [Parameter(Mandatory = $true)][string]$StageName
    )

    $stageProperty = $Manifest.stages.PSObject.Properties[$StageName]
    if ($null -eq $stageProperty) {
        return $null
    }

    return $stageProperty.Value
}

function Set-ManifestStage {
    param(
        [Parameter(Mandatory = $true)]$Manifest,
        [Parameter(Mandatory = $true)][string]$StageName,
        [Parameter(Mandatory = $true)]$StageEntry
    )

    if ($null -eq $Manifest.stages.PSObject.Properties[$StageName]) {
        $Manifest.stages | Add-Member -MemberType NoteProperty -Name $StageName -Value $StageEntry
    }
    else {
        $Manifest.stages.$StageName = $StageEntry
    }
}

function Test-OutputsExist {
    param([Parameter(Mandatory = $true)][string[]]$Outputs)

    foreach ($output in $Outputs) {
        if (-not (Test-Path -LiteralPath $output)) {
            return $false
        }
    }

    return $true
}

function Get-OutputsFingerprint {
    param([Parameter(Mandatory = $true)][string[]]$Outputs)

    return Get-PathsFingerprint -Paths $Outputs -ExcludePatterns @()
}

function Invoke-CachedStage {
    param(
        [Parameter(Mandatory = $true)][string]$StageName,
        [Parameter(Mandatory = $true)][scriptblock]$GetInputs,
        [Parameter(Mandatory = $true)][scriptblock]$Build,
        [Parameter(Mandatory = $true)][string[]]$Outputs,
        [switch]$Force
    )

    $manifest = $script:BuildManifest
    $inputFingerprint = & $GetInputs
    $existingStage = Get-ManifestStage -Manifest $manifest -StageName $StageName
    $outputsReady = Test-OutputsExist -Outputs $Outputs
    $outputFingerprint = if ($outputsReady) { Get-OutputsFingerprint -Outputs $Outputs } else { $null }

    $cacheHit = -not $Force -and $existingStage -and $outputsReady -and $existingStage.inputFingerprint -eq $inputFingerprint -and (
        $existingStage.outputFingerprint -eq $outputFingerprint -or
        ($outputFingerprint -is [string] -and $outputFingerprint.StartsWith('locked:', [System.StringComparison]::Ordinal))
    )

    if ($cacheHit) {
        Write-Host "cache hit: $StageName reused"
        return $existingStage
    }

    if ($Force) {
        Write-Host "forced rebuild: $StageName"
    }
    elseif ($existingStage -and -not $outputsReady) {
        Write-Host "missing output: $StageName rebuilding"
    }
    elseif ($existingStage -and $existingStage.inputFingerprint -ne $inputFingerprint) {
        Write-Host "input hash changed: $StageName rebuilding"
    }
    else {
        Write-Host "cache miss: $StageName rebuilding"
    }

    & $Build

    if (-not (Test-OutputsExist -Outputs $Outputs)) {
        throw "$StageName did not produce the expected outputs: $($Outputs -join ', ')"
    }

    $newOutputFingerprint = Get-OutputsFingerprint -Outputs $Outputs
    Set-ManifestStage -Manifest $manifest -StageName $StageName -StageEntry ([PSCustomObject]@{
        inputFingerprint = $inputFingerprint
        outputFingerprint = $newOutputFingerprint
        outputs = $Outputs
        updatedUtc = (Get-Date).ToUniversalTime().ToString("o")
    })
    $script:BuildManifest = $manifest
    Save-BuildCacheManifest -Manifest $manifest
    return Get-ManifestStage -Manifest $manifest -StageName $StageName
}

function Invoke-CachedFileCopyStage {
    param(
        [Parameter(Mandatory = $true)][string]$StageName,
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination,
        [switch]$Force
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        if (Test-Path -LiteralPath $Destination) {
            Remove-Item -LiteralPath $Destination -Force
        }

        return $null
    }

    $sourceFingerprint = Get-FileFingerprint -Path $Source
    $buildAction = {
        for ($attempt = 1; $attempt -le 8; $attempt++) {
            try {
                Copy-Item -LiteralPath $Source -Destination $Destination -Force
                return
            }
            catch {
                if ($attempt -eq 8) {
                    throw
                }

                Start-Sleep -Milliseconds (500 * $attempt)
            }
        }
    }.GetNewClosure()
    return Invoke-CachedStage -StageName $StageName -GetInputs { $sourceFingerprint } -Build $buildAction -Outputs @($Destination) -Force:$Force
}

function Invoke-CachedArchiveStage {
    param(
        [Parameter(Mandatory = $true)][string]$StageName,
        [Parameter(Mandatory = $true)][string]$SourceDirectory,
        [Parameter(Mandatory = $true)][string]$DestinationArchive,
        [switch]$Force
    )

    if (-not (Test-Path -LiteralPath $SourceDirectory)) {
        if (Test-Path -LiteralPath $DestinationArchive) {
            Remove-Item -LiteralPath $DestinationArchive -Force
        }

        return $null
    }

    $sourceFingerprint = Get-DirectoryFingerprint -Path $SourceDirectory
    $buildAction = {
        Compress-Archive -Path (Join-Path $SourceDirectory "*") -DestinationPath $DestinationArchive -Force
    }.GetNewClosure()
    return Invoke-CachedStage -StageName $StageName -GetInputs { $sourceFingerprint } -Build $buildAction -Outputs @($DestinationArchive) -Force:$Force
}

function Expand-ArchiveToDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$ArchivePath,
        [Parameter(Mandatory = $true)][string]$DestinationDirectory
    )

    if (Test-Path -LiteralPath $DestinationDirectory) {
        Remove-Item -LiteralPath $DestinationDirectory -Recurse -Force
    }

    New-Item -ItemType Directory -Path $DestinationDirectory -Force | Out-Null
    Expand-Archive -LiteralPath $ArchivePath -DestinationPath $DestinationDirectory -Force
}

function Find-PythonExeInDirectory {
    param([Parameter(Mandatory = $true)][string]$RootDirectory)

    $candidates = @(
        (Join-Path $RootDirectory "python.exe"),
        (Join-Path $RootDirectory "python310\python.exe"),
        (Join-Path $RootDirectory "venv\Scripts\python.exe"),
        (Join-Path $RootDirectory "Scripts\python.exe")
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "Unable to locate a Python executable under '$RootDirectory'."
}

function New-RuntimeSnapshotArchive {
    param(
        [Parameter(Mandatory = $true)][string]$SnapshotRoot,
        [Parameter(Mandatory = $true)][string]$OutputArchive
    )

    for ($attempt = 1; $attempt -le 5; $attempt++) {
        try {
            if (Test-Path -LiteralPath $OutputArchive) {
                try {
                    Remove-Item -LiteralPath $OutputArchive -Force -ErrorAction Stop
                }
                catch {
                    if ($attempt -eq 5) {
                        throw
                    }

                    Start-Sleep -Seconds $attempt
                    continue
                }
            }

            Compress-Archive -Path (Join-Path $SnapshotRoot "*") -DestinationPath $OutputArchive -Force -ErrorAction Stop
            return
        }
        catch {
            if ($attempt -eq 5) {
                throw
            }

            Start-Sleep -Seconds $attempt
        }
    }
}

function Install-WheelhouseIntoRuntimeSnapshot {
    param(
        [Parameter(Mandatory = $true)][string]$PythonExe,
        [Parameter(Mandatory = $true)][string]$WheelhouseDir,
        [Parameter(Mandatory = $true)][string[]]$Packages
    )

    & $PythonExe -m pip --version *> $null
    if ($LASTEXITCODE -ne 0) {
        & $PythonExe -m ensurepip --upgrade
        if ($LASTEXITCODE -ne 0) {
            throw "pip could not be bootstrapped inside the runtime snapshot."
        }
    }

    $env:PIP_PROGRESS_BAR = "on"
    $env:PIP_DISABLE_PIP_VERSION_CHECK = "1"
    $env:PIP_NO_INPUT = "1"
    $installArgs = @("-m", "pip", "install", "--upgrade", "--only-binary=:all:", "--no-compile", "--no-index", "--no-warn-script-location", "--find-links", $WheelhouseDir)
    $installArgs += $Packages
    & $PythonExe @installArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install packages into the runtime snapshot."
    }
}

function Copy-OpenWakeWordFeatureModelsIntoSnapshot {
    param(
        [Parameter(Mandatory = $true)][string]$SnapshotRoot,
        [Parameter(Mandatory = $true)][string]$FeatureModelsSource
    )

    $resourceDir = Join-Path $SnapshotRoot "Lib\site-packages\openwakeword\resources\models"
    New-Item -ItemType Directory -Path $resourceDir -Force | Out-Null
    foreach ($modelName in @("melspectrogram.onnx", "embedding_model.onnx")) {
        $source = Join-Path $FeatureModelsSource $modelName
        if (-not (Test-Path -LiteralPath $source)) {
            throw "Bundled openWakeWord feature model missing: $source"
        }

        Copy-Item -LiteralPath $source -Destination (Join-Path $resourceDir $modelName) -Force
    }
}

function Build-OpenWakeWordRuntimeSnapshot {
    param(
        [Parameter(Mandatory = $true)][string]$SourceArchive,
        [Parameter(Mandatory = $true)][string]$WheelhouseArchive,
        [Parameter(Mandatory = $true)][string]$FeatureArchive,
        [Parameter(Mandatory = $true)][string]$SnapshotRoot,
        [Parameter(Mandatory = $true)][string]$OutputArchive
    )

    if (Test-Path -LiteralPath $SnapshotRoot) {
        Remove-Item -LiteralPath $SnapshotRoot -Recurse -Force
    }

    Expand-ArchiveToDirectory -ArchivePath $SourceArchive -DestinationDirectory $SnapshotRoot
    $pythonExe = Find-PythonExeInDirectory -RootDirectory $SnapshotRoot

    $wheelhouseTemp = Join-Path $runtimeSnapshotDir "openwakeword-wheelhouse-input"
    $featureTemp = Join-Path $runtimeSnapshotDir "openwakeword-feature-input"
    Expand-ArchiveToDirectory -ArchivePath $WheelhouseArchive -DestinationDirectory $wheelhouseTemp
    Expand-ArchiveToDirectory -ArchivePath $FeatureArchive -DestinationDirectory $featureTemp

    Install-WheelhouseIntoRuntimeSnapshot -PythonExe $pythonExe -WheelhouseDir $wheelhouseTemp -Packages @("openwakeword", "onnxruntime", "numpy")
    Copy-OpenWakeWordFeatureModelsIntoSnapshot -SnapshotRoot $SnapshotRoot -FeatureModelsSource $featureTemp
    New-RuntimeSnapshotArchive -SnapshotRoot $SnapshotRoot -OutputArchive $OutputArchive

    Remove-Item -LiteralPath $wheelhouseTemp -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $featureTemp -Recurse -Force -ErrorAction SilentlyContinue
}

function Build-PyannoteRuntimeSnapshot {
    param(
        [Parameter(Mandatory = $true)][string]$SourceArchive,
        [Parameter(Mandatory = $true)][string]$WheelhouseArchive,
        [Parameter(Mandatory = $true)][string]$ModelCacheArchive,
        [Parameter(Mandatory = $true)][string]$SnapshotRoot,
        [Parameter(Mandatory = $true)][string]$OutputArchive
    )

    if (Test-Path -LiteralPath $SnapshotRoot) {
        Remove-Item -LiteralPath $SnapshotRoot -Recurse -Force
    }

    Expand-ArchiveToDirectory -ArchivePath $SourceArchive -DestinationDirectory $SnapshotRoot
    $pythonExe = Find-PythonExeInDirectory -RootDirectory $SnapshotRoot

    $wheelhouseTemp = Join-Path $runtimeSnapshotDir "pyannote-wheelhouse-input"
    $cacheTemp = Join-Path $runtimeSnapshotDir "pyannote-model-cache-input"
    Expand-ArchiveToDirectory -ArchivePath $WheelhouseArchive -DestinationDirectory $wheelhouseTemp
    Expand-ArchiveToDirectory -ArchivePath $ModelCacheArchive -DestinationDirectory $cacheTemp

    Install-WheelhouseIntoRuntimeSnapshot -PythonExe $pythonExe -WheelhouseDir $wheelhouseTemp -Packages @("pyannote.audio", "torch", "torchaudio", "numpy", "scipy", "soundfile", "huggingface_hub", "omegaconf")

    $cacheTarget = Join-Path $SnapshotRoot "hub"
    New-Item -ItemType Directory -Path $cacheTarget -Force | Out-Null
    Copy-Item -Path (Join-Path $cacheTemp "*") -Destination $cacheTarget -Recurse -Force

    New-RuntimeSnapshotArchive -SnapshotRoot $SnapshotRoot -OutputArchive $OutputArchive
    Remove-Item -LiteralPath $wheelhouseTemp -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $cacheTemp -Recurse -Force -ErrorAction SilentlyContinue
}

function Invoke-CachedDotnetPublishStage {
    param(
        [Parameter(Mandatory = $true)][string]$StageName,
        [Parameter(Mandatory = $true)][string]$ProjectPathToPublish,
        [Parameter(Mandatory = $true)][string]$OutputDirectory,
        [Parameter(Mandatory = $true)][string[]]$AdditionalInputPaths,
        [Parameter(Mandatory = $true)][string[]]$RestoreArguments,
        [string[]]$FallbackExecutablePaths = @(),
        [switch]$Force
    )

    $projectDir = Split-Path -Parent $ProjectPathToPublish
    $rootConfigs = @(
        (Join-Path $root "Directory.Build.props"),
        (Join-Path $root "Directory.Build.targets"),
        (Join-Path $root "Directory.Packages.props"),
        (Join-Path $root "global.json"),
        (Join-Path $root "NuGet.config")
    ) | Where-Object { Test-Path -LiteralPath $_ }

    $inputRoots = @($ProjectPathToPublish, $projectDir) + $AdditionalInputPaths + $rootConfigs |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    $inputFingerprint = Get-StringSha256 -Value (($Configuration, $Runtime, ($inputRoots | ForEach-Object { Get-FileFingerprint -Path $_ })) -join "`n")
    $runtimeId = $Runtime
    $configurationName = $Configuration
    $noRestore = $NoRestore
    $publishRetried = $false

    $invokePublish = {
        dotnet publish $ProjectPathToPublish `
            -c $configurationName `
            -r $runtimeId `
            @RestoreArguments `
            --self-contained true `
            -p:PublishSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:IncludeAllContentForSelfExtract=true `
            -p:EnableCompressionInSingleFile=true `
            -p:DebugType=None `
            -p:DebugSymbols=false `
            -o $OutputDirectory
    }.GetNewClosure()

    $buildAction = {
        if (Test-Path -LiteralPath $OutputDirectory) {
            Remove-Item -LiteralPath $OutputDirectory -Recurse -Force
        }

        New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
        if (-not $noRestore) {
            dotnet restore $ProjectPathToPublish --runtime $runtimeId
            if ($LASTEXITCODE -ne 0) {
                throw "dotnet restore failed with exit code $LASTEXITCODE"
            }
        }

        & $invokePublish

        if ($LASTEXITCODE -ne 0) {
            if ($noRestore -and -not $publishRetried) {
                $publishRetried = $true
                Write-Host "dotnet publish failed with exit code $LASTEXITCODE for $StageName. Running a targeted runtime restore and retrying once."
                dotnet restore $ProjectPathToPublish --runtime $runtimeId
                if ($LASTEXITCODE -eq 0) {
                    & $invokePublish
                }
            }

        }

        if ($LASTEXITCODE -ne 0) {
            $fallbackExecutable = $null
            foreach ($candidate in $FallbackExecutablePaths) {
                if (-not [string]::IsNullOrWhiteSpace($candidate) -and (Test-Path -LiteralPath $candidate)) {
                    $fallbackExecutable = $candidate
                    break
                }
            }

            if ($fallbackExecutable) {
                Write-Warning "dotnet publish failed with exit code $LASTEXITCODE for $StageName. Reusing existing built executable: $fallbackExecutable"
                $fallbackTarget = Join-Path $OutputDirectory ([System.IO.Path]::GetFileName($fallbackExecutable))
                Copy-Item -LiteralPath $fallbackExecutable -Destination $fallbackTarget -Force
                return
            }

            throw "dotnet publish failed with exit code $LASTEXITCODE"
        }
    }.GetNewClosure()

    return Invoke-CachedStage -StageName $StageName -GetInputs { $inputFingerprint } -Build $buildAction -Outputs @($OutputDirectory) -Force:$Force
}

if ($Clean) {
    if (Test-Path -LiteralPath $buildDir) {
        Remove-Item -LiteralPath $buildDir -Recurse -Force
    }
    if (Test-Path -LiteralPath $setupPayloadDir) {
        Remove-Item -LiteralPath $setupPayloadDir -Recurse -Force
    }
}

New-Item -ItemType Directory -Path $buildDir -Force | Out-Null
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path $servicePublishDir -Force | Out-Null
New-Item -ItemType Directory -Path $setupPayloadDir -Force | Out-Null
$script:BuildManifest = Initialize-BuildCacheManifest
$openWakeWordResourceSource = $openWakeWordResourceCandidates | Where-Object {
    (Test-Path -LiteralPath (Join-Path $_ "melspectrogram.onnx")) -and
    (Test-Path -LiteralPath (Join-Path $_ "embedding_model.onnx"))
} | Select-Object -First 1
if ($openWakeWordResourceSource) {
    Invoke-CachedArchiveStage -StageName "openwakeword-resources" -SourceDirectory $openWakeWordResourceSource -DestinationArchive $setupPayloadOpenWakeWordResources -Force:$ForceRepackVoiceAssets | Out-Null
}
elseif ($releaseRequiresPrivateVoiceAssets) {
    throw "Release installer requires bundled openWakeWord feature resources. Add melspectrogram.onnx and embedding_model.onnx under closed-source\openwakeword-resources, closed-source\openwakeword\models, or third-party\openwakeword-resources; or pass -AllowMissingPrivateVoiceAssets for a developer build."
}
else {
    Write-Warning "No bundled openWakeWord feature resources found. Repair Wakeword can download them online for developer builds, but release builds should package melspectrogram.onnx and embedding_model.onnx."
}
$pythonRuntimeSource = $pythonRuntimeCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if ($pythonRuntimeSource) {
    Invoke-CachedArchiveStage -StageName "python-runtime-win-x64" -SourceDirectory $pythonRuntimeSource -DestinationArchive $setupPayloadPythonRuntime -Force:$ForceRepackVoiceAssets | Out-Null
}
elseif ($releaseRequiresPrivateVoiceAssets) {
    throw "Release installer requires a bundled CPython 3.10 runtime. Add a portable Python runtime under closed-source\python-runtime-win-x64, closed-source\python310, or third-party\python-runtime-win-x64; or pass -AllowMissingPrivateVoiceAssets for a developer build."
}
else {
    Write-Warning "No bundled CPython runtime found. Installer repairs will fall back to a developer Python if one is available."
}
$openWakeWordWheelhouseSource = $openWakeWordWheelhouseCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if ($openWakeWordWheelhouseSource) {
    Invoke-CachedArchiveStage -StageName "openwakeword-wheelhouse" -SourceDirectory $openWakeWordWheelhouseSource -DestinationArchive $setupPayloadOpenWakeWordWheelhouse -Force:$ForceRepackVoiceAssets | Out-Null
}
elseif ($releaseRequiresPrivateVoiceAssets) {
    throw "Release installer requires a bundled openWakeWord wheelhouse. Add prebuilt wheels under closed-source\openwakeword-wheelhouse, closed-source\openwakeword\wheelhouse, or third-party\openwakeword-wheelhouse; or pass -AllowMissingPrivateVoiceAssets for a developer build."
}
else {
    Write-Warning "No bundled openWakeWord wheelhouse found. openWakeWord setup will fall back to online wheels for developer builds only."
}
if (Test-Path -LiteralPath $pyannoteSetupSource) {
    Invoke-CachedFileCopyStage -StageName "pyannote-setup-script" -Source $pyannoteSetupSource -Destination $setupPayloadPyannoteSetup | Out-Null
}
if (Test-Path -LiteralPath $pyannoteTestSource) {
    Invoke-CachedFileCopyStage -StageName "pyannote-test-script" -Source $pyannoteTestSource -Destination $setupPayloadPyannoteTest | Out-Null
}
if (Test-Path -LiteralPath $thirdPartySourcesSource) {
    Invoke-CachedFileCopyStage -StageName "third-party-sources" -Source $thirdPartySourcesSource -Destination $setupPayloadThirdPartySources | Out-Null
}
if (Test-Path -LiteralPath $pyannoteSourceTarball) {
    Invoke-CachedFileCopyStage -StageName "pyannote-source-tarball" -Source $pyannoteSourceTarball -Destination $setupPayloadPyannoteSource | Out-Null
}
else {
    Write-Warning "pyannote.audio source tarball was not found at $pyannoteSourceTarball. Identity setup will fall back to package-index installation if local wheels are unavailable."
}
$pyannoteWheelhouseSource = $pyannoteWheelhouseCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if ($pyannoteWheelhouseSource) {
    Invoke-CachedArchiveStage -StageName "pyannote-wheelhouse" -SourceDirectory $pyannoteWheelhouseSource -DestinationArchive $setupPayloadPyannoteWheelhouse -Force:$ForceRepackVoiceAssets | Out-Null
}
elseif ($releaseRequiresPrivateVoiceAssets) {
    throw "Release installer requires a bundled pyannote wheelhouse. Add wheels under closed-source\pyannote-wheelhouse, closed-source\python-wheelhouse, or third-party\pyannote-wheelhouse; or pass -AllowMissingPrivateVoiceAssets for a developer build."
}
else {
    Write-Warning "No pyannote wheelhouse found. Identity setup can still install packages online. To package dependencies, place wheels under closed-source\pyannote-wheelhouse, closed-source\python-wheelhouse, or third-party\pyannote-wheelhouse."
}
$pyannoteModelCacheSource = $pyannoteModelCacheCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if ($pyannoteModelCacheSource) {
    Invoke-CachedArchiveStage -StageName "pyannote-model-cache" -SourceDirectory $pyannoteModelCacheSource -DestinationArchive $setupPayloadPyannoteModelCache -Force:$ForceRepackVoiceAssets | Out-Null
}
elseif ($releaseRequiresPrivateVoiceAssets) {
    throw "Release installer requires a bundled pyannote model cache. Add the accepted pyannote/embedding cache under closed-source\pyannote-model-cache; or pass -AllowMissingPrivateVoiceAssets for a developer build."
}
else {
    Write-Warning "No pyannote model cache found. Identity setup can still download the model online for developer builds after HF_TOKEN is provided."
}

$openWakeWordRuntimeSourceArchive = $setupPayloadPythonRuntime
$openWakeWordRuntimeWheelhouseArchive = $setupPayloadOpenWakeWordWheelhouse
$openWakeWordRuntimeFeatureArchive = $setupPayloadOpenWakeWordResources
$pyannoteRuntimeSourceArchive = $setupPayloadPythonRuntime
$pyannoteRuntimeWheelhouseArchive = $setupPayloadPyannoteWheelhouse

if ((Test-Path -LiteralPath $openWakeWordRuntimeSourceArchive) -and
    (Test-Path -LiteralPath $openWakeWordRuntimeWheelhouseArchive) -and
    (Test-Path -LiteralPath $openWakeWordRuntimeFeatureArchive)) {
    $openWakeWordRuntimeInputs = Get-PathsFingerprint -Paths @($openWakeWordRuntimeSourceArchive, $openWakeWordRuntimeWheelhouseArchive, $openWakeWordRuntimeFeatureArchive) -ExcludePatterns @()
    Invoke-CachedStage -StageName "openwakeword-runtime" `
        -GetInputs { $openWakeWordRuntimeInputs } `
        -Build {
            Build-OpenWakeWordRuntimeSnapshot `
                -SourceArchive $openWakeWordRuntimeSourceArchive `
                -WheelhouseArchive $openWakeWordRuntimeWheelhouseArchive `
                -FeatureArchive $openWakeWordRuntimeFeatureArchive `
                -SnapshotRoot $openWakeWordRuntimeSnapshotDir `
                -OutputArchive $setupPayloadOpenWakeWordRuntime
        } `
        -Outputs @($setupPayloadOpenWakeWordRuntime) `
        -Force:$ForceRepackVoiceAssets | Out-Null
}
elseif ($releaseRequiresPrivateVoiceAssets) {
    throw "Release installer requires a prebuilt openWakeWord runtime snapshot. Ensure python-runtime-win-x64.zip, openwakeword-wheelhouse.zip, and openwakeword-resources.zip can be staged."
}
else {
    Write-Warning "No openWakeWord runtime snapshot could be built; the installer will keep the legacy repair flow for developer builds."
}

if ((Test-Path -LiteralPath $pyannoteRuntimeSourceArchive) -and
    (Test-Path -LiteralPath $pyannoteRuntimeWheelhouseArchive) -and
    (Test-Path -LiteralPath $setupPayloadPyannoteModelCache)) {
    $pyannoteRuntimeInputs = Get-PathsFingerprint -Paths @($pyannoteRuntimeSourceArchive, $pyannoteRuntimeWheelhouseArchive, $setupPayloadPyannoteModelCache) -ExcludePatterns @()
    Invoke-CachedStage -StageName "pyannote-runtime" `
        -GetInputs { $pyannoteRuntimeInputs } `
        -Build {
            Build-PyannoteRuntimeSnapshot `
                -SourceArchive $pyannoteRuntimeSourceArchive `
                -WheelhouseArchive $pyannoteRuntimeWheelhouseArchive `
                -ModelCacheArchive $setupPayloadPyannoteModelCache `
                -SnapshotRoot $pyannoteRuntimeSnapshotDir `
                -OutputArchive $setupPayloadPyannoteRuntime
        } `
        -Outputs @($setupPayloadPyannoteRuntime) `
        -Force:$ForceRepackVoiceAssets | Out-Null
}
elseif ($releaseRequiresPrivateVoiceAssets) {
    throw "Release installer requires a prebuilt pyannote runtime snapshot. Ensure python-runtime-win-x64.zip, pyannote-wheelhouse.zip, and pyannote-model-cache.zip can be staged."
}
else {
    Write-Warning "No pyannote runtime snapshot could be built; the installer will keep the legacy repair flow for developer builds."
}

$fzfPrebuilt = $fzfPrebuiltCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
$goCommand = Get-Command go -ErrorAction SilentlyContinue
if ((Test-Path -LiteralPath $fzfRepoDir) -and $goCommand) {
    $fzfBuildAction = {
        if (!(Test-Path -LiteralPath $fzfRepoDir)) {
            throw "fzf repo was not found at $fzfRepoDir."
        }

        Push-Location $fzfRepoDir
        try {
            if (Test-Path -LiteralPath $fzfBuildOutput) {
                Remove-Item -LiteralPath $fzfBuildOutput -Force
            }

            & $goCommand.Source build -buildvcs=false -o $fzfBuildOutput .
            if ($LASTEXITCODE -ne 0) {
                throw "fzf build failed with exit code $LASTEXITCODE"
            }
        }
        finally {
            Pop-Location
        }
    }.GetNewClosure()
    Invoke-CachedStage -StageName "fzf-build" -GetInputs { Get-DirectoryFingerprint -Path $fzfRepoDir } -Build $fzfBuildAction -Outputs @($fzfBuildOutput) -Force:$Clean | Out-Null
    if (Test-Path -LiteralPath $fzfBuildOutput) {
        Invoke-CachedFileCopyStage -StageName "fzf-payload-copy" -Source $fzfBuildOutput -Destination $setupPayloadFzf -Force:$Clean | Out-Null
        $fzfStagedSource = $fzfBuildOutput
    }
}

if (-not $fzfStagedSource -and $fzfPrebuilt) {
    Invoke-CachedFileCopyStage -StageName "fzf-prebuilt-copy" -Source $fzfPrebuilt -Destination $setupPayloadFzf -Force:$Clean | Out-Null
    $fzfStagedSource = $fzfPrebuilt
}

if (-not $fzfStagedSource) {
    throw "fzf.exe is required for the testing-ready alpha installer. Install Go and make sure the fzf repo exists at $fzfRepoDir, or provide a prebuilt fzf.exe at repo root, tools\fzf.exe, or closed-source\tools\fzf.exe."
}

$wakeModelSource = $wakeModelCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if ($wakeModelSource) {
    Invoke-CachedFileCopyStage -StageName "wake-model" -Source $wakeModelSource -Destination $setupPayloadWakeModel -Force:$ForceRepackVoiceAssets | Out-Null
}
elseif ($releaseRequiresPrivateVoiceAssets) {
    throw "Release installer requires the custom Callsign openWakeWord model. Add closed-source\callsign.onnx, closed-source\models\callsign.onnx, models\callsign.onnx, or pass -WakeModelPath."
}
else {
    Write-Warning "No clean custom openWakeWord model found. Alpha voice wake events will be disabled in the installed app until callsign.onnx is installed to %LOCALAPPDATA%\Callsign\Models. To package openWakeWord wake detection, pass -WakeModelPath <custom-callsign.onnx> or provide closed-source\callsign.onnx, closed-source\models\callsign.onnx, or models\callsign.onnx."
}

if (Test-Path -LiteralPath $iconSource) {
    Invoke-CachedFileCopyStage -StageName "icon-payload-copy" -Source $iconSource -Destination $setupPayloadIcon -Force:$Clean | Out-Null
    Write-Host "Callsign icon staged for installer payload: $setupPayloadIcon"
}

Write-Host "Publishing Callsign to: $publishDir"
Invoke-CachedDotnetPublishStage -StageName "ui-publish" -ProjectPathToPublish $ProjectPath -OutputDirectory $publishDir -AdditionalInputPaths @((Join-Path $root "src\Callsign.UI"), (Join-Path $root "assets\callsign.ico"), (Join-Path $root "callsign.gif")) -RestoreArguments @("--no-restore") -FallbackExecutablePaths @(
    (Join-Path $root "src\Callsign.UI\bin\Release\net10.0-windows\win-x64\Callsign.UI.exe"),
    (Join-Path $root "src\Callsign.UI\bin\Release\net10.0-windows\Callsign.UI.exe"),
    (Join-Path $root "src\Callsign.UI\bin\Debug\net10.0-windows\Callsign.UI.exe")
) -Force:$Clean | Out-Null

if (Test-Path -LiteralPath $ServiceProjectPath) {
    Write-Host "Publishing Callsign service to: $servicePublishDir"
    Invoke-CachedDotnetPublishStage -StageName "service-publish" -ProjectPathToPublish $ServiceProjectPath -OutputDirectory $servicePublishDir -AdditionalInputPaths @((Join-Path $root "src\Callsign.Service"), (Join-Path $root "src\Callsign.UI"), (Join-Path $root "assets\callsign.ico")) -RestoreArguments @("--no-restore") -FallbackExecutablePaths @(
        (Join-Path $root "src\Callsign.Service\bin\Release\net10.0-windows\win-x64\Callsign.Service.exe"),
        (Join-Path $root "src\Callsign.Service\bin\Release\net10.0-windows\Callsign.Service.exe"),
        (Join-Path $root "src\Callsign.Service\bin\Debug\net10.0-windows\Callsign.Service.exe")
    ) -Force:$Clean | Out-Null

    $serviceExe = Get-ChildItem -Path $servicePublishDir -Filter "*.exe" -File |
        Select-Object -First 1
    if ($serviceExe) {
        Invoke-CachedFileCopyStage -StageName "service-root-exe" -Source $serviceExe.FullName -Destination $serviceOutput -Force:$Clean | Out-Null
        Invoke-CachedFileCopyStage -StageName "service-payload-exe" -Source $serviceExe.FullName -Destination $setupPayloadService -Force:$Clean | Out-Null
        Write-Host "Service executable staged: $serviceOutput"
    }
}

$projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
$publishedExe = Get-ChildItem -Path $publishDir -Filter "$projectName.exe" -File |
    Select-Object -First 1

if (-not $publishedExe) {
    $publishedExe = Get-ChildItem -Path $publishDir -Filter "*.exe" -File |
        Select-Object -First 1
}

if (-not $publishedExe) {
    throw "No built executable found under $publishDir"
}

Write-Host "Published executable: $($publishedExe.FullName)"
Invoke-CachedFileCopyStage -StageName "portable-executable" -Source $publishedExe.FullName -Destination $portableOutput -Force:$Clean | Out-Null
Write-Host "Always-generated launchable executable: $portableOutput"
if ($fzfStagedSource -and (Test-Path -LiteralPath $fzfStagedSource)) {
    $rootFzfOutput = Join-Path $root "fzf.exe"
    Invoke-CachedFileCopyStage -StageName "fzf-root-copy" -Source $fzfStagedSource -Destination $rootFzfOutput -Force:$Clean | Out-Null
    Invoke-CachedFileCopyStage -StageName "fzf-publish-copy" -Source $fzfStagedSource -Destination (Join-Path $publishDir "fzf.exe") -Force:$Clean | Out-Null
}
foreach ($helper in @(
    @{ Source = $openWakeWordSetupSource; Payload = $setupPayloadOpenWakeWordSetup; Stage = "openwakeword-setup-script" },
    @{ Source = $openWakeWordTestSource; Payload = $setupPayloadOpenWakeWordTest; Stage = "openwakeword-test-script" },
    @{ Source = $pyannoteSetupSource; Payload = $setupPayloadPyannoteSetup; Stage = "pyannote-setup-script" },
    @{ Source = $pyannoteTestSource; Payload = $setupPayloadPyannoteTest; Stage = "pyannote-test-script" }
)) {
    if (Test-Path -LiteralPath $helper.Source) {
        Invoke-CachedFileCopyStage -StageName $helper.Stage -Source $helper.Source -Destination $helper.Payload -Force:$Clean | Out-Null
        Invoke-CachedFileCopyStage -StageName ($helper.Stage + "-publish") -Source $helper.Source -Destination (Join-Path $publishDir (Split-Path -Leaf $helper.Source)) -Force:$Clean | Out-Null
    }
}
if (Test-Path -LiteralPath $setupPayloadPyannoteSource) {
    Invoke-CachedFileCopyStage -StageName "publish-pyannote-source" -Source $setupPayloadPyannoteSource -Destination (Join-Path $publishDir "pyannote_audio-4.0.4.tar.gz") -Force:$Clean | Out-Null
}
if (Test-Path -LiteralPath $setupPayloadOpenWakeWordResources) {
    Invoke-CachedFileCopyStage -StageName "publish-openwakeword-resources" -Source $setupPayloadOpenWakeWordResources -Destination (Join-Path $publishDir "openwakeword-resources.zip") -Force:$Clean | Out-Null
}
if (Test-Path -LiteralPath $setupPayloadOpenWakeWordRuntime) {
    Invoke-CachedFileCopyStage -StageName "publish-openwakeword-runtime" -Source $setupPayloadOpenWakeWordRuntime -Destination (Join-Path $publishDir "openwakeword-runtime.zip") -Force:$Clean | Out-Null
}
if (Test-Path -LiteralPath $setupPayloadPythonRuntime) {
    Invoke-CachedFileCopyStage -StageName "publish-python-runtime" -Source $setupPayloadPythonRuntime -Destination (Join-Path $publishDir "python-runtime-win-x64.zip") -Force:$Clean | Out-Null
}
if (Test-Path -LiteralPath $setupPayloadOpenWakeWordWheelhouse) {
    Invoke-CachedFileCopyStage -StageName "publish-openwakeword-wheelhouse" -Source $setupPayloadOpenWakeWordWheelhouse -Destination (Join-Path $publishDir "openwakeword-wheelhouse.zip") -Force:$Clean | Out-Null
}
if (Test-Path -LiteralPath $setupPayloadPyannoteWheelhouse) {
    Invoke-CachedFileCopyStage -StageName "publish-pyannote-wheelhouse" -Source $setupPayloadPyannoteWheelhouse -Destination (Join-Path $publishDir "pyannote-wheelhouse.zip") -Force:$Clean | Out-Null
}
if (Test-Path -LiteralPath $setupPayloadPyannoteRuntime) {
    Invoke-CachedFileCopyStage -StageName "publish-pyannote-runtime" -Source $setupPayloadPyannoteRuntime -Destination (Join-Path $publishDir "pyannote-runtime.zip") -Force:$Clean | Out-Null
}
if (Test-Path -LiteralPath $setupPayloadPyannoteModelCache) {
    Invoke-CachedFileCopyStage -StageName "publish-pyannote-model-cache" -Source $setupPayloadPyannoteModelCache -Destination (Join-Path $publishDir "pyannote-model-cache.zip") -Force:$Clean | Out-Null
}
if (Test-Path -LiteralPath $setupPayloadThirdPartySources) {
    Invoke-CachedFileCopyStage -StageName "publish-third-party-sources" -Source $setupPayloadThirdPartySources -Destination (Join-Path $publishDir "THIRD_PARTY_SOURCES.md") -Force:$Clean | Out-Null
}

# Callsign.Setup is the canonical alpha installer because it installs the
# configuration manager, service payload, desktop/startup shortcuts, icon,
# fzf helper, and openWakeWord helper scripts into the per-user runtime layout.
# Do not switch to a generic Inno install path until it reproduces that behavior.
$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if (-not $iscc) {
    $isccPaths = @(
        "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )
    foreach ($path in $isccPaths) {
        if (Test-Path -LiteralPath $path) {
            $iscc = [PSCustomObject]@{ Source = (Resolve-Path -LiteralPath $path).Path }
            break
        }
    }
}
$iscc = $null

function Expand-InnoEscaped([string]$Value) {
    return ($Value -replace '"', '\"')
}

if ($iscc) {
    $issPath = Join-Path $buildDir "CallsignInstaller.iss"
    $appId = [guid]::NewGuid().ToString()
    $publishedExeEsc = Expand-InnoEscaped -Value $publishedExe.Name
    $sourceFiles = (Join-Path $publishDir "*")
    $appNameEsc = Expand-InnoEscaped -Value $ProductName
    $publisherEsc = Expand-InnoEscaped -Value $Publisher

    $iss = @"
[Setup]
AppId=$appId
AppName=$appNameEsc
AppVersion=1.0.0
AppPublisher=$publisherEsc
DefaultDirName={autopf}\$ProductName
DefaultGroupName=$ProductName
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
OutputDir=$root
OutputBaseFilename=$InstallerName
SetupIconFile=
Compression=lzma
SolidCompression=yes

[Files]
Source: "$sourceFiles"; DestDir: "{app}\"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\$ProductName"; Filename: "{app}\$($publishedExe.Name)"

[Run]
Filename: "{app}\$publishedExeEsc"; Description: "Launch $appNameEsc"; Flags: nowait postinstall skipifsilent
"@
    Set-Content -Path $issPath -Value $iss -Encoding UTF8

    Write-Host "Building installer with Inno Setup (`iscc`)."
    & $iscc.Source $issPath

    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup compilation failed with exit code $LASTEXITCODE"
    }

    if (Test-Path -LiteralPath $installerOutput) {
        Write-Host "Installer created: $installerOutput"
    }
    else {
        throw "Inno Setup did not produce the expected installer: $installerOutput"
    }
}
else {
    Write-Host "Building the bundled Callsign alpha installer."

    if (!(Test-Path -LiteralPath $SetupProjectPath)) {
        throw "Setup project file not found: $SetupProjectPath"
    }

    $setupPayloadDir = Join-Path (Split-Path -Parent $SetupProjectPath) "Payload"
    $setupOutputDir = Join-Path $buildDir "setup"
    New-Item -ItemType Directory -Path $setupPayloadDir -Force | Out-Null
    New-Item -ItemType Directory -Path $setupOutputDir -Force | Out-Null
    Invoke-CachedFileCopyStage -StageName "setup-ui-exe" -Source $publishedExe.FullName -Destination (Join-Path $setupPayloadDir "Callsign.UI.exe") -Force:$Clean | Out-Null
    Require-BuildPayload -Path (Join-Path $setupPayloadDir "Callsign.UI.exe") -Description "configuration manager executable"
    Require-BuildPayload -Path $setupPayloadService -Description "background service executable"
    Require-BuildPayload -Path $setupPayloadFzf -Description "fzf file-search helper"
    Require-BuildPayload -Path $setupPayloadIcon -Description "Callsign icon"
    Require-BuildPayload -Path $setupPayloadOpenWakeWordSetup -Description "openWakeWord setup helper"
    Require-BuildPayload -Path $setupPayloadOpenWakeWordTest -Description "openWakeWord test helper"
    Require-BuildPayload -Path $setupPayloadPyannoteSetup -Description "pyannote setup helper"
    Require-BuildPayload -Path $setupPayloadPyannoteTest -Description "pyannote test helper"
    Require-BuildPayload -Path $setupPayloadThirdPartySources -Description "third-party source notice"
    if ($releaseRequiresPrivateVoiceAssets) {
        Require-BuildPayload -Path $setupPayloadPythonRuntime -Description "bundled CPython runtime"
        Require-BuildPayload -Path $setupPayloadOpenWakeWordWheelhouse -Description "openWakeWord wheelhouse"
        Require-BuildPayload -Path $setupPayloadOpenWakeWordRuntime -Description "openWakeWord runtime snapshot"
        Require-BuildPayload -Path $setupPayloadWakeModel -Description "openWakeWord Callsign model"
        Require-BuildPayload -Path $setupPayloadOpenWakeWordResources -Description "openWakeWord feature resources"
        Require-BuildPayload -Path $setupPayloadPyannoteWheelhouse -Description "pyannote Python wheelhouse"
        Require-BuildPayload -Path $setupPayloadPyannoteRuntime -Description "pyannote runtime snapshot"
        Require-BuildPayload -Path $setupPayloadPyannoteModelCache -Description "pyannote model cache"
    }
    $payloadManifestPath = Join-Path $buildDir "alpha-installer-payload.json"
    @(
        Get-BuildPayloadReport -Path (Join-Path $setupPayloadDir "Callsign.UI.exe") -Description "configuration manager executable"
        Get-BuildPayloadReport -Path $setupPayloadService -Description "background service executable"
        Get-BuildPayloadReport -Path $setupPayloadFzf -Description "fzf file-search helper"
        Get-BuildPayloadReport -Path $setupPayloadIcon -Description "Callsign icon"
        Get-BuildPayloadReport -Path $setupPayloadOpenWakeWordSetup -Description "openWakeWord setup helper"
        Get-BuildPayloadReport -Path $setupPayloadOpenWakeWordTest -Description "openWakeWord test helper"
        Get-BuildPayloadReport -Path $setupPayloadOpenWakeWordResources -Description "openWakeWord feature resources"
        Get-BuildPayloadReport -Path $setupPayloadOpenWakeWordRuntime -Description "openWakeWord runtime snapshot"
        Get-BuildPayloadReport -Path $setupPayloadPythonRuntime -Description "bundled CPython runtime"
        Get-BuildPayloadReport -Path $setupPayloadOpenWakeWordWheelhouse -Description "openWakeWord wheelhouse"
        Get-BuildPayloadReport -Path $setupPayloadPyannoteSetup -Description "pyannote setup helper"
        Get-BuildPayloadReport -Path $setupPayloadPyannoteTest -Description "pyannote test helper"
        Get-BuildPayloadReport -Path $setupPayloadPyannoteWheelhouse -Description "optional pyannote Python wheelhouse"
        Get-BuildPayloadReport -Path $setupPayloadPyannoteRuntime -Description "pyannote runtime snapshot"
        Get-BuildPayloadReport -Path $setupPayloadPyannoteModelCache -Description "pyannote model cache"
        Get-BuildPayloadReport -Path $setupPayloadPyannoteSource -Description "pyannote.audio source tarball"
        Get-BuildPayloadReport -Path $setupPayloadWakeModel -Description "optional openWakeWord Callsign model"
        Get-BuildPayloadReport -Path $setupPayloadThirdPartySources -Description "third-party source notice"
    ) | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $payloadManifestPath -Encoding UTF8
    Write-Host "Alpha installer payload manifest created: $payloadManifestPath"

    Invoke-CachedDotnetPublishStage -StageName "setup-publish" -ProjectPathToPublish $SetupProjectPath -OutputDirectory $setupOutputDir -AdditionalInputPaths @($setupPayloadDir) -RestoreArguments @("--no-restore") -Force:$Clean | Out-Null

    $setupExe = Get-ChildItem -Path $setupOutputDir -Filter "Callsign-Setup.exe" -File |
        Select-Object -First 1

    if (-not $setupExe) {
        throw "No setup executable found under $setupOutputDir"
    }

    Copy-WithRetry -Source $setupExe.FullName -Destination $installerOutput
    Write-Host "Installer created: $installerOutput"
}

Write-Host "Done."
