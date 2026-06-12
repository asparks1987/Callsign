param(
    [string]$Callsign = "Alpha",
    [string]$AppName = "Notepad"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$uiProject = Join-Path $repoRoot "src\Callsign.UI\Callsign.UI.csproj"
$profileRoot = Join-Path $env:LOCALAPPDATA "Callsign\Profiles\$Callsign"
$settingsPath = Join-Path $profileRoot "settings.json"
$auditPath = Join-Path $profileRoot "alpha-audit.jsonl"
$startupLogPath = Join-Path $env:LOCALAPPDATA "Callsign\Logs\startup-error.log"

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
Write-Host "This script does not build, launch, verify, or automate the app. It only prints the alpha proof checklist."
