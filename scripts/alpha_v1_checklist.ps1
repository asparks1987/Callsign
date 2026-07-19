param(
    [string]$Callsign = "Alpha",
    [string]$AppName = "Notepad",
    [switch]$Verify,
    [switch]$RunSmoke,
    [string]$SmokeCheck = "",
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
$mainFormPath = Join-Path $repoRoot "src\Callsign.UI\MainForm.cs"
$updateSplashPath = Join-Path $repoRoot "src\Callsign.UI\UpdateSplashForm.cs"
$releasePacketSummaryPath = Join-Path $repoRoot "build\release-packet-summary.json"
$manualEvidenceChecklistPath = Join-Path $repoRoot "build\voice-access-parity-manual-evidence.checklist.md"
$parityEvidencePath = Join-Path $repoRoot "scripts\voice_access_parity_evidence.ps1"
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

function Read-JsonFile {
    param(
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return $null
    }

    try {
        return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    }
    catch {
        return $null
    }
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
Write-Host " 11. Confirm the walkthrough exposes visible-control discovery jumps for Voice Help, Visible Controls, Show Numbers, Show Grid, Show Keyboard, and browser overlay helpers such as Browser Show Numbers, Browser Show Grid, and Browser Hide Overlays."
Write-Host " 12. Confirm the walkthrough mentions the update-splash repeat-summary action, the feature-highlight recap for feature-only manifests, the import-splash repeat-import action, visible Updates-status readback, visible check-in-status readback, visible visual-status readback, and visible restart-proof readback."
Write-Host " 13. Confirm the walkthrough mentions the public installer, expected manifest hash/size, the release-proof summary, website download release proof, and the manual evidence template."
Write-Host " 13b. Confirm the walkthrough also exposes a Read Release Proof route that reads the installer hash comparison aloud."
Write-Host " 13c. Confirm the Voice tab exposes a Read Voice Mode route that reads the current voice mode aloud."
Write-Host " 13a. Confirm manual evidence remains reachable from the Account, Help, Updates, or walkthrough surfaces, and that the generated checklist file sits beside the canonical template."
Write-Host " 13d. Confirm the Free Parity command-palette filter shows Free Open Core parity commands while paid Pro/Advanced commands stay discovery-only, entitlement-required, and will not route without entitlement."
Write-Host " 14. Confirm the Updates tab or startup walkthrough can open the local release evidence folder."
Write-Host " 15. Confirm the release evidence template includes the release evidence folder walkthrough check and the startup walkthrough's direct Open Release Evidence button."
Write-Host " 16. Confirm the release evidence template includes a restart-safe downloaded-installer path check after update service restart and a version-match update check that ignores installer hash differences."
Write-Host " 17. Confirm `"$manualEvidenceChecklistPath`" exists after running `.\scripts\voice_access_parity_evidence.ps1 -WriteManualEvidenceTemplate`, and use it as the human-readable companion to the canonical JSON template during the release proof pass."
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

$releasePacketSummary = Read-JsonFile $releasePacketSummaryPath
$releasePacketBlockerSummary = $null
if ($null -ne $releasePacketSummary -and $null -ne $releasePacketSummary.parity_evidence) {
    $releasePacketBlockerSummary = $releasePacketSummary.parity_evidence.release_blocker_summary
}

$releasePacketHasBlockerSummary = $null -ne $releasePacketBlockerSummary
$releasePacketHasBlockerCount = $false
$releasePacketHasNextAction = $false
$releasePacketHasManualRemainingCount = $false
$releasePacketHasAutomatedFailureCount = $false
if ($releasePacketHasBlockerSummary) {
    $blockerCountProperty = $releasePacketBlockerSummary.PSObject.Properties["blocker_count"]
    $nextActionProperty = $releasePacketBlockerSummary.PSObject.Properties["next_action"]
    $manualRemainingProperty = $releasePacketBlockerSummary.PSObject.Properties["manual_checks_remaining_count"]
    $automatedFailureProperty = $releasePacketBlockerSummary.PSObject.Properties["failed_automated_checks_count"]

    $releasePacketHasBlockerCount = $null -ne $blockerCountProperty -and [int]$blockerCountProperty.Value -ge 0
    $releasePacketHasNextAction = $null -ne $nextActionProperty -and -not [string]::IsNullOrWhiteSpace([string]$nextActionProperty.Value)
    $releasePacketHasManualRemainingCount = $null -ne $manualRemainingProperty -and [int]$manualRemainingProperty.Value -ge 0
    $releasePacketHasAutomatedFailureCount = $null -ne $automatedFailureProperty -and [int]$automatedFailureProperty.Value -ge 0
}

$checks = New-Object System.Collections.Generic.List[object]
$checks.Add((New-Check "UI project exists" (Test-Path -LiteralPath $uiProject) $uiProject))
$checks.Add((New-Check "Smoke project exists" (Test-Path -LiteralPath $smokeProject) $smokeProject))
$checks.Add((New-Check "Local installer exists" (Test-Path -LiteralPath $installerPath) $installerPath))
$checks.Add((New-Check "Wake helper is packaged" (Test-Path -LiteralPath $wakeHelperPath) $wakeHelperPath))
$checks.Add((New-Check "Startup walkthrough source exists" (Test-Path -LiteralPath $startupWalkthroughPath) $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough names macOS visual target" (Test-FileContains $startupWalkthroughPath "macOS Voice Control") $startupWalkthroughPath))
$checks.Add((New-Check "Main shell exposes macOS visual target badge" (Test-FileContains $mainFormPath "Visual: macOS Voice Control") $mainFormPath))
$checks.Add((New-Check "Main shell visual badge exposes contrast evidence" (Test-FileContains $mainFormPath "4.5:1 contrast") $mainFormPath))
$checks.Add((New-Check "Main shell visual badge exposes opacity evidence" (Test-FileContains $mainFormPath "0.86-0.99 opacity") $mainFormPath))
$checks.Add((New-Check "Main shell visual badge exposes compact radius evidence" (Test-FileContains $mainFormPath "20-26px HUD radius") $mainFormPath))
$checks.Add((New-Check "Walkthrough exposes visible step badge" (Test-FileContains $startupWalkthroughPath "StatusStepBadgeText") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes visible current badge" (Test-FileContains $startupWalkthroughPath "StatusCurrentBadgeText") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes Voice Help jump" (Test-FileContains $startupWalkthroughPath "Open Voice Help") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes Visible Controls jump" (Test-FileContains $startupWalkthroughPath "Open Visible Controls") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes Show Numbers jump" (Test-FileContains $startupWalkthroughPath "Open Show Numbers") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes Show Grid jump" (Test-FileContains $startupWalkthroughPath "Open Show Grid") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes Show Keyboard jump" (Test-FileContains $startupWalkthroughPath "Open Show Keyboard") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough covers profile creation" (Test-FileContains $startupWalkthroughPath "Create or pick a callsign profile") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough covers voice enrollment" (Test-FileContains $startupWalkthroughPath "Record at least three voice samples") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough covers wake overlay" (Test-FileContains $startupWalkthroughPath "visible wake overlay") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough covers Start search launch" (Test-FileContains $startupWalkthroughPath "Start search") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes Plans jump" (Test-FileContains $startupWalkthroughPath "Open Plans") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes Packs jump" (Test-FileContains $startupWalkthroughPath "Open Packs") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes Import Pack jump" (Test-FileContains $startupWalkthroughPath "Import Pack") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes Import Folder jump" (Test-FileContains $startupWalkthroughPath "Import Folder") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes Drop DLL Folder jump" (Test-FileContains $startupWalkthroughPath "Drop DLL Folder") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes drop-DLL-folder pack cue" (Test-FileContains $startupWalkthroughPath "drop DLLs or folders") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes Open Packs Folder jump" (Test-FileContains $startupWalkthroughPath "Open Packs Folder") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes release-proof jump" (Test-FileContains $startupWalkthroughPath "Open Release Proof") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes manual evidence jump" (Test-FileContains $startupWalkthroughPath "Open Manual Evidence") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes release-proof summary" (Test-FileContains $startupWalkthroughPath "Release proof summary") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes release proof cue" (Test-FileContains $startupWalkthroughPath "Release: compare installer + site") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes stop badge" (Test-FileContains $startupWalkthroughPath "StatusStopBadgeText") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes read-release-proof jump" (Test-FileContains $startupWalkthroughPath "Read Release Proof") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough exposes checklist jump" (Test-FileContains $startupWalkthroughPath "Open Checklist") $startupWalkthroughPath))
$checks.Add((New-Check "Walkthrough read-release-proof route mentions installer hash comparison" (Test-FileContains $mainFormPath "Read Release Proof") $mainFormPath))
$checks.Add((New-Check "Voice tab exposes read-voice-mode jump" (Test-FileContains $mainFormPath "Read Voice Mode") $mainFormPath))
$checks.Add((New-Check "Voice tab read-voice-mode route mentions current voice mode" (Test-FileContains $mainFormPath "ReadVoiceModeStatusButtonText") $mainFormPath))
$checks.Add((New-Check "Walkthrough release proof mentions expected manifest hash" (Test-FileContains $mainFormPath "expected manifest SHA-256") $mainFormPath))
$checks.Add((New-Check "Walkthrough release proof mentions expected manifest size" (Test-FileContains $mainFormPath "InstallerSizeBytes") $mainFormPath))
$checks.Add((New-Check "Walkthrough covers release proof reminder" (Test-FileContains $startupWalkthroughPath "Verify the local Callsign-Setup.exe installer, the release evidence folder, the public /downloads/Callsign-Setup.exe download, and the manual evidence checklist before release") $startupWalkthroughPath))
$checks.Add((New-Check "Update splash exposes repeat summary button" (Test-FileContains $updateSplashPath "Read Summary Again") $updateSplashPath))
$checks.Add((New-Check "Update splash exposes feature highlights" (Test-FileContains $updateSplashPath "FeatureHighlights") $updateSplashPath))
$checks.Add((New-Check "Update splash feature highlights can replay" (Test-FileContains $updateSplashPath "Features:") $updateSplashPath))
$checks.Add((New-Check "Update splash exposes stop badge" (Test-FileContains $updateSplashPath "StatusStopBadgeText") $updateSplashPath))
$checks.Add((New-Check "Import splash exposes repeat-import button" (Test-FileContains $updateSplashPath "Read Import Again") $updateSplashPath))
$checks.Add((New-Check "Update splash repeat action reruns narration" (Test-FileContains $updateSplashPath "SpeakNarration()") $updateSplashPath))
$checks.Add((New-Check "Walkthrough exposes check-in-status readback" (Test-FileContains $startupWalkthroughPath "Read Check-In Status") $startupWalkthroughPath))
$checks.Add((New-Check "Updates tab exposes check-in-status readback" (Test-FileContains $mainFormPath "Read Check-In Status") $mainFormPath))
$checks.Add((New-Check "Walkthrough exposes restart-proof readback" (Test-FileContains $startupWalkthroughPath "Read Restart Proof") $startupWalkthroughPath))
$checks.Add((New-Check "Updates tab exposes restart-proof readback" (Test-FileContains $mainFormPath "Read Restart Proof") $mainFormPath))
$checks.Add((New-Check "Walkthrough status count reflects 17 steps" (Test-FileContains $startupWalkthroughPath "Step: 1 / 17") $startupWalkthroughPath))
$checks.Add((New-Check "Release packet summary exists" (Test-Path -LiteralPath $releasePacketSummaryPath) $releasePacketSummaryPath))
$checks.Add((New-Check "Manual evidence checklist exists" (Test-Path -LiteralPath $manualEvidenceChecklistPath) $manualEvidenceChecklistPath))
$checks.Add((New-Check "Manual evidence checklist mentions Open Checklist" (Test-FileContains $manualEvidenceChecklistPath "Open Checklist") $manualEvidenceChecklistPath))
$checks.Add((New-Check "Release packet summary records local-preview mode" ((Test-Path -LiteralPath $releasePacketSummaryPath) -and ((Get-Content -LiteralPath $releasePacketSummaryPath -Raw).IndexOf('"release_mode":  "local-preview"', [System.StringComparison]::OrdinalIgnoreCase) -ge 0)) $releasePacketSummaryPath))
$checks.Add((New-Check "Release packet summary carries parity blocker summary" $releasePacketHasBlockerSummary $releasePacketSummaryPath))
$checks.Add((New-Check "Release packet summary carries parity blocker count" $releasePacketHasBlockerCount $releasePacketSummaryPath))
$checks.Add((New-Check "Release packet summary carries next release-evidence action" $releasePacketHasNextAction $releasePacketSummaryPath))
$checks.Add((New-Check "Release packet summary carries manual-check remaining count" $releasePacketHasManualRemainingCount $releasePacketSummaryPath))
$checks.Add((New-Check "Release packet summary carries automated-failure count" $releasePacketHasAutomatedFailureCount $releasePacketSummaryPath))
$checks.Add((New-Check "Updates tab exposes installer-open action" (Test-FileContains $mainFormPath "Open Installer") $mainFormPath))
$checks.Add((New-Check "Updates tab exposes repeat-summary action" (Test-FileContains $mainFormPath "Read Summary Again") $mainFormPath))
$checks.Add((New-Check "Updates tab exposes read-updates-status action" (Test-FileContains $mainFormPath "Read Updates Status") $mainFormPath))
$checks.Add((New-Check "Parity evidence covers updates-status readback" (Test-FileContains $parityEvidencePath "read_updates_status_voice_command") $parityEvidencePath))
$checks.Add((New-Check "Main shell exposes read-visual-status action" (Test-FileContains $mainFormPath "Read Visual Status") $mainFormPath))
$checks.Add((New-Check "Parity evidence covers visual-status readback" (Test-FileContains $parityEvidencePath "read_visual_status_voice_command") $parityEvidencePath))
$checks.Add((New-Check "Parity evidence covers free parity paid gating walkthrough" (Test-FileContains $parityEvidencePath "free_parity_paid_gating_walkthrough") $parityEvidencePath))
$checks.Add((New-Check "Parity evidence covers browser overlay helper discovery" (Test-FileContains $parityEvidencePath "startup_walkthrough_browser_overlay_helpers") $parityEvidencePath))
$checks.Add((New-Check "Updates tab exposes release-evidence action" (Test-FileContains $mainFormPath "Open Release Evidence") $mainFormPath))
$checks.Add((New-Check "Updates tab exposes manual evidence action" (Test-FileContains $mainFormPath "Open Manual Evidence") $mainFormPath))
$checks.Add((New-Check "Startup walkthrough exposes release-evidence action" (Test-FileContains $startupWalkthroughPath "Open Release Evidence") $startupWalkthroughPath))
$checks.Add((New-Check "Startup walkthrough exposes manual evidence action" (Test-FileContains $startupWalkthroughPath "Open Manual Evidence") $startupWalkthroughPath))
$checks.Add((New-Check "Startup walkthrough exposes read-release-proof action" (Test-FileContains $startupWalkthroughPath "Read Release Proof") $startupWalkthroughPath))
$checks.Add((New-Check "Updates tab routes release-evidence voice command" (Test-FileContains $mainFormPath "ui open release evidence") $mainFormPath))
$checks.Add((New-Check "Updates tab routes read-release-proof voice command" (Test-FileContains $mainFormPath "ui read release proof") $mainFormPath))
$checks.Add((New-Check "Parity evidence covers release evidence folder walkthrough" (Test-FileContains $parityEvidencePath "open_release_evidence_folder_action") $parityEvidencePath))
$checks.Add((New-Check "Startup walkthrough exposes import-summary replay action" (Test-FileContains $startupWalkthroughPath "Read Import Again") $startupWalkthroughPath))
$checks.Add((New-Check "Startup walkthrough packs cue mentions drop DLLs or folders" (Test-FileContains $startupWalkthroughPath "drop DLLs or folders") $startupWalkthroughPath))
$checks.Add((New-Check "Packs import replay routes through the visible splash" (Test-FileContains $mainFormPath "ui read import summary again") $mainFormPath))
$checks.Add((New-Check "Updates tab exposes installer download status" (Test-FileContains $mainFormPath "Installer download: none yet.") $mainFormPath))
$checks.Add((New-Check "Updates tab exposes downloaded installer path" (Test-FileContains $mainFormPath "Last downloaded installer: none yet.") $mainFormPath))
$checks.Add((New-Check "Updates tab exposes website download target" (Test-FileContains $mainFormPath "Website download target: /downloads/Callsign-Setup.exe.") $mainFormPath))
$checks.Add((New-Check "Updates tab exposes restart proof status" (Test-FileContains $mainFormPath "Restart proof: state reloads from disk.") $mainFormPath))
$checks.Add((New-Check "Parity evidence covers restart-safe downloaded installer path" (Test-FileContains $parityEvidencePath "update_downloaded_installer_path_survives_restart") $parityEvidencePath))
$checks.Add((New-Check "Parity evidence covers version-match installer hash rule" (Test-FileContains $parityEvidencePath "update_version_match_ignores_installer_hash") $parityEvidencePath))
$checks.Add((New-Check "Updates tab remembers downloaded installer path" (Test-FileContains $mainFormPath "LastDownloadedInstallerPath") $mainFormPath))
$checks.Add((New-Check "Walkthrough exposes restart-proof readback" (Test-FileContains $startupWalkthroughPath "Read Restart Proof") $startupWalkthroughPath))
$checks.Add((New-Check "Updates tab exposes restart-proof readback" (Test-FileContains $mainFormPath "ReadRestartProofButtonText") $mainFormPath))
$checks.Add((New-Check "Updates tab routes release-proof voice command" (Test-FileContains $mainFormPath "ui open release proof") $mainFormPath))
$checks.Add((New-Check "Updates tab routes open installer voice command" (Test-FileContains $mainFormPath "ui open installer") $mainFormPath))
$checks.Add((New-Check "Burndown tracks clean-install walkthrough" (Test-FileContains $burndownPath "Complete clean-install release walkthrough") $burndownPath))
$checks.Add((New-Check "Parity matrix still requires clean install" (Test-FileContains $parityMatrixPath "a clean install walkthrough passes") $parityMatrixPath))
$checks.Add((New-Check "Wake helper reports effective threshold" (Test-FileContains $wakeHelperPath "Effective threshold") $wakeHelperPath))

if ($RunSmoke) {
    Write-Host "Running alpha smoke suite..." -ForegroundColor Cyan
    $smokeArgs = @("run", "--project", $smokeProject, "-c", "Release", "--no-build")
    if (-not [string]::IsNullOrWhiteSpace($SmokeCheck)) {
        $smokeArgs += "--"
        $smokeArgs += "--check"
        $smokeArgs += $SmokeCheck
    }

    dotnet @smokeArgs
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
    release_packet_blocker_summary = $releasePacketBlockerSummary
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
