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
    [string]$ProjectPath = "$PSScriptRoot\src\Callsign.UI\Callsign.UI.csproj",
    [string]$SetupProjectPath = "$PSScriptRoot\src\Callsign.Setup\Callsign.Setup.csproj",
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$InstallerName = "Callsign-Setup",
    [string]$PortableName = "Callsign-Run.exe",
    [string]$ProductName = "Callsign",
    [string]$Publisher = "Callsign Project"
)

$ErrorActionPreference = "Stop"

if (!(Test-Path -LiteralPath $ProjectPath)) {
    throw "Project file not found: $ProjectPath"
}

$root = $PSScriptRoot
$buildDir = Join-Path $root "build"
$publishDir = Join-Path $buildDir "publish"
$installerOutput = Join-Path $root "$InstallerName.exe"
$portableOutput = Join-Path $root $PortableName
$fzfRepoDir = Join-Path $root "fzf"
$fzfBuildOutput = Join-Path $buildDir "fzf.exe"
$setupPayloadFzf = Join-Path (Join-Path (Split-Path -Parent $SetupProjectPath) "Payload") "fzf.exe"

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

function Remove-BuildOutputs([string]$ProjectPathToClean) {
    $projectDir = Split-Path -Parent $ProjectPathToClean
    foreach ($folder in @("bin", "obj")) {
        $path = Join-Path $projectDir $folder
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force
        }
    }
}

if (Test-Path -LiteralPath $buildDir) {
    Remove-Item -LiteralPath $buildDir -Recurse -Force
}
New-Item -ItemType Directory -Path $buildDir -Force | Out-Null
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
Remove-BuildOutputs $ProjectPath
if (Test-Path -LiteralPath $SetupProjectPath) {
    Remove-BuildOutputs $SetupProjectPath
}

if (Test-Path -LiteralPath $setupPayloadFzf) {
    Remove-Item -LiteralPath $setupPayloadFzf -Force
}
Build-FzfHelper -RepositoryPath $fzfRepoDir -OutputPath $fzfBuildOutput | Out-Null
if (Test-Path -LiteralPath $fzfBuildOutput) {
    Copy-WithRetry -Source $fzfBuildOutput -Destination $setupPayloadFzf
    Write-Host "fzf helper staged for installer payload: $setupPayloadFzf"
}

Write-Host "Publishing Callsign to: $publishDir"
dotnet restore $ProjectPath -r $Runtime
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore for Callsign failed with exit code $LASTEXITCODE"
}

dotnet publish $ProjectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
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
Copy-WithRetry -Source $publishedExe.FullName -Destination $portableOutput
Write-Host "Always-generated launchable executable: $portableOutput"
if (Test-Path -LiteralPath $fzfBuildOutput) {
    Copy-WithRetry -Source $fzfBuildOutput -Destination (Join-Path $root "fzf.exe")
    Copy-WithRetry -Source $fzfBuildOutput -Destination (Join-Path $publishDir "fzf.exe")
}

# Prefer a real installer by discovering `iscc` in PATH or common install locations.
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
    Write-Warning "Inno Setup compiler (`iscc`) not found. Building the bundled Callsign installer."

    if (!(Test-Path -LiteralPath $SetupProjectPath)) {
        throw "Setup project file not found: $SetupProjectPath"
    }

    $setupPayloadDir = Join-Path (Split-Path -Parent $SetupProjectPath) "Payload"
    $setupOutputDir = Join-Path $buildDir "setup"
    New-Item -ItemType Directory -Path $setupPayloadDir -Force | Out-Null
    New-Item -ItemType Directory -Path $setupOutputDir -Force | Out-Null
    Copy-WithRetry -Source $publishedExe.FullName -Destination (Join-Path $setupPayloadDir "Callsign.UI.exe")

    dotnet restore $SetupProjectPath -r $Runtime
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore for Callsign setup failed with exit code $LASTEXITCODE"
    }

    dotnet publish $SetupProjectPath `
        -c $Configuration `
        -r $Runtime `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -o $setupOutputDir

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish for Callsign setup failed with exit code $LASTEXITCODE"
    }

    $setupExe = Get-ChildItem -Path $setupOutputDir -Filter "Callsign-Setup.exe" -File |
        Select-Object -First 1

    if (-not $setupExe) {
        throw "No setup executable found under $setupOutputDir"
    }

    Copy-WithRetry -Source $setupExe.FullName -Destination $installerOutput
    Write-Host "Installer created: $installerOutput"
}

Write-Host "Done."
