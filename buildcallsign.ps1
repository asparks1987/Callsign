<#
Builds Callsign for Windows and emits an installer-style executable in repo root.

Behavior:
1. dotnet publish a self-contained Windows desktop binary package.
2. If Inno Setup Compiler (`iscc`) is installed, generate and build a real installer
   named Callsign-Setup.exe in the repo root.
3. If Inno Setup is not available, create a root-level portable executable fallback
   (Callsign-Portable.exe) and emit clear instructions.
#>

[CmdletBinding()]
param(
    [string]$ProjectPath = "$PSScriptRoot\src\Callsign.UI\Callsign.UI.csproj",
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

if (Test-Path -LiteralPath $buildDir) {
    Remove-Item -LiteralPath $buildDir -Recurse -Force
}
New-Item -ItemType Directory -Path $buildDir -Force | Out-Null
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

Write-Host "Publishing Callsign to: $publishDir"
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
Copy-Item -LiteralPath $publishedExe.FullName -Destination $portableOutput -Force
Write-Host "Always-generated launchable executable: $portableOutput"

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
    Write-Warning "Inno Setup compiler (`iscc`) not found."
    Write-Host "Use launchable executable: $portableOutput"
    Write-Host "To create a real installer, install Inno Setup and re-run this script."
}

Write-Host "Done."
