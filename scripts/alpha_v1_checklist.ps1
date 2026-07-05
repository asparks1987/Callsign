param(
    [string]$Callsign = "Alpha",
    [string]$AppName = "Notepad",
    [switch]$Verify,
    [switch]$RunSmoke,
    [string]$EvidencePath = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$uiProject = Join-Path $repoRoot "src\Callsign.UI\Callsign.UI.csproj"
$smokeProject = Join-Path $repoRoot "tests\Callsign.AlphaSmoke\Callsign.AlphaSmoke.csproj"
$installerPath = Join-Path $repoRoot "Callsign-Setup.exe"
$wakeHelperPath = Join-Path $repoRoot "src\Callsign.Setup\Payload\testopenwakeword.ps1"
$burndownPath = Join-Path $repoRoot "burndown.md"
$parityMatrixPath = Join-Path $repoRoot "docs\reference\VOICE_ACCESS_PARITY_MATRIX.md"
$startupWalkthroughPath = Join-Path $repoRoot "src\Callsign.UI\StartupWalkthroughForm.cs"
$profileRoot = Join-Path $env:LOCALAPPDATA "Callsign\Profiles\$Callsign"
$settingsPath = Join-Path $profileRoot "settings.json"
$auditPath = Join-Path $profileRoot "alpha-audit.jsonl"
$startupLogPath = Join-Path $env:LOCALAPPDATA "Callsign\Logs\startup-error.log"

function New-Check {
    param(
        [string]$Name,
        [bool]$Passed,
        [string]$Detail
    )

    [pscustomobject]@{
        name = $Name
        passed = $Passed
        detail = $Detail
    }
}

function Test-FileContains {
    param(
        [string]$Path,
        [string]$Pattern
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return $false
    }

    return (Get-Content -LiteralPath $Path -Raw).IndexOf($Pattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
}

Write-Host "Callsign Alpha v1 manual verification checklist" -ForegroundColor Cyan
Write-Host "Status: implemented path is not alpha-ready until every manual check below passes." -ForegroundColor Yellow
Write-Host ""
Write-Host "Inputs"
Write-Host "  Callsign: $Callsign"
Write-Host "  App name: $AppName"
Write-Host "  UI project: $uiProject"
Write-Host ""
Write-Host "Expected phrase"
Write-Host "  Callsign $Callsign open $AppName"
Write-Host ""
Write-Host "Manual checks"
Write-Host "  1. Run: dotnet build `"$uiProject`""
Write-Host "  2. Run/open Callsign.UI."
Write-Host "  3. Create or select callsign '$Callsign'."
Write-Host "  4. Click Start Listening. It may activate voice for the saved callsign."
Write-Host "  5. Use Test Phrase Launch with: Callsign $Callsign open $AppName"
Write-Host "  6. Confirm $AppName launches through Start search. This proves the account/parser/launcher path, not microphone recognition."
Write-Host "  7. Confirm microphone phrase works separately: Callsign $Callsign open $AppName"
Write-Host "  8. Confirm two-step phrase works: Callsign $Callsign, then $AppName"
Write-Host "  9. Confirm stop listening halts the listener without launching anything."
Write-Host " 10. Confirm shell/path/URL/WSL-style requests are rejected."
Write-Host ""
Write-Host "Local proof paths"
Write-Host "  Settings: $settingsPath"
Write-Host "  Alpha audit: $auditPath"
Write-Host "  Startup/runtime error log: $startupLogPath"
Write-Host ""
Write-Host "Automation"
Write-Host "  Add -Verify to check local release/walkthrough artifacts."
Write-Host "  Add -RunSmoke with -Verify to run the alpha smoke suite."
Write-Host ""

if (-not $Verify) {
    Write-Host "This script printed the manual alpha proof checklist. Re-run with -Verify for local artifact checks."
    return
}

$checks = New-Object System.Collections.Generic.List[object]
$checks.Add((New-Check "UI project exists" (Test-Path -LiteralPath $uiProject) $uiProject))
$checks.Add((New-Check "Smoke project exists" (Test-Path -LiteralPath $smokeProject) $smokeProject))
$checks.Add((New-Check "Local installer exists" (Test-Path -LiteralPath $installerPath) $installerPath))
$checks.Add((New-Check "Wake helper is packaged" (Test-Path -LiteralPath $wakeHelperPath) $wakeHelperPath))
$checks.Add((New-Check "Startup walkthrough source exists" (Test-Path -LiteralPath $startupWalkthroughPath) $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough names macOS visual target" (Test-FileContains $startupWalkthroughPath "macOS Voice Control") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough covers profile creation" (Test-FileContains $startupWalkthroughPath "Create or pick a callsign profile") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough covers voice enrollment" (Test-FileContains $startupWalkthroughPath "Record at least three voice samples") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough covers wake overlay" (Test-FileContains $startupWalkthroughPath "visible wake overlay") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough covers Start search launch" (Test-FileContains $startupWalkthroughPath "Start search") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes Packs jump" (Test-FileContains $startupWalkthroughPath "Open Packs") $startupWalkthroughPath))
$checks.Add((New-Check "Burndown tracks clean-install walkthrough" (Test-FileContains $burndownPath "Complete clean-install release walkthrough") $burndownPath))
$checks.Add((New-Check "Parity matrix still requires clean install" (Test-FileContains $parityMatrixPath "a clean install walkthrough passes") $parityMatrixPath))
$checks.Add((New-Check "Wake helper reports effective threshold" (Test-FileContains $wakeHelperPath "Effective threshold") $wakeHelperPath))

if ($RunSmoke) {
    Write-Host "Running alpha smoke suite..." -ForegroundColor Cyan
    dotnet run --project $smokeProject -c Release --no-build
    $checks.Add((New-Check "Alpha smoke suite" ($LASTEXITCODE -eq 0) "dotnet run --project $smokeProject -c Release --no-build"))
}

$passed = @($checks | Where-Object { $_.passed }).Count
$failed = @($checks | Where-Object { -not $_.passed }).Count
foreach ($check in $checks) {
    $prefix = if ($check.passed) { "PASS" } else { "FAIL" }
    $color = if ($check.passed) { "Green" } else { "Red" }
    Write-Host "${prefix}: $($check.name) - $($check.detail)" -ForegroundColor $color
}

$evidence = [pscustomobject]@{
    generated_utc = [DateTime]::UtcNow.ToString("o")
    callsign = $Callsign
    app_name = $AppName
    passed = $failed -eq 0
    passed_count = $passed
    failed_count = $failed
    checks = $checks
    manual_checks_remaining = @(
        "Install from the public website installer on a clean profile.",
        "Record live voice samples.",
        "Say Callsign and verify callsign identity.",
        "Confirm callsign.gif overlay and live readout appear.",
        "Launch $AppName through visible Start search.",
        "Confirm stop/cancel/reset flows."
    )
}

if ([string]::IsNullOrWhiteSpace($EvidencePath)) {
    $EvidencePath = Join-Path $repoRoot "build\alpha-v1-walkthrough-evidence.json"
}

$evidenceDir = Split-Path -Parent $EvidencePath
if (-not [string]::IsNullOrWhiteSpace($evidenceDir)) {
    New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
}

$evidence | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $EvidencePath -Encoding UTF8
Write-Host "Evidence written: $EvidencePath" -ForegroundColor Cyan

if ($failed -gt 0) {
    exit 1
}
