param(
    [string]$EvidencePath = "",
    [string]$ManualEvidencePath = "",
    [string]$ManualEvidenceTemplatePath = "",
    [switch]$RequireManualEvidence,
    [switch]$WriteManualEvidenceTemplate,
    [switch]$RunSmoke
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$matrixPath = Join-Path $repoRoot "docs\reference\VOICE_ACCESS_PARITY_MATRIX.md"
$testPlanPath = Join-Path $repoRoot "docs\reference\TEST_PLAN.md"
$generatedParityPagePath = Join-Path $repoRoot "docs\pages\voice-access-parity.html"
$generatedVoiceUxPagePath = Join-Path $repoRoot "docs\pages\voice-ux.html"
$generatedTierPagePath = Join-Path $repoRoot "docs\pages\tier-architecture.html"
$generatedSecurityPagePath = Join-Path $repoRoot "docs\pages\security-model.html"
$visualStylePath = Join-Path $repoRoot "src\Callsign.UI\CallsignVisualStyle.cs"
$smokeProject = Join-Path $repoRoot "tests\Callsign.AlphaSmoke\Callsign.AlphaSmoke.csproj"
$installerPath = Join-Path $repoRoot "Callsign-Setup.exe"

if ([string]::IsNullOrWhiteSpace($EvidencePath)) {
    $EvidencePath = Join-Path $repoRoot "build\voice-access-parity-evidence.json"
}

$requiredCategories = @(
    "App launch",
    "Voice access controls",
    "Voice shortcuts",
    "App switching",
    "Window management",
    "Visible control numbers",
    "Mouse grid",
    "Mouse and scrolling",
    "Keyboard commands",
    "Dictation review",
    "Text editing",
    "Correction alternatives",
    "Browser navigation",
    "File search",
    "Safe system/settings control",
    "Help and command discovery",
    "Extension packs",
    "Update splash"
)

$requiredManualEvidenceChecks = @(
    "clean_install_public_installer",
    "microphone_setup_voice_enrollment",
    "identity_failure_timeout_cancel_reset",
    "show_numbers_and_mouse_grid_common_apps",
    "dictation_notepad_review_correction_formatting",
    "browser_edge_or_chrome_navigation",
    "app_switching_window_management_snap_layouts_settings",
    "keyboard_mouse_media_file_search",
    "help_command_discovery_palette",
    "voice_shortcuts_create_manage_execute",
    "community_extension_import_manage",
    "update_splash_manifest_walkthrough",
    "apple_style_visual_polish_walkthrough",
    "public_website_installer_hash_match"
)

$manualEvidenceDescriptions = @{
    clean_install_public_installer = "Clean install from the public website installer."
    microphone_setup_voice_enrollment = "Microphone setup and live voice enrollment."
    identity_failure_timeout_cancel_reset = "Wrong identity, stale identity, timeout, cancel, and reset flows."
    show_numbers_and_mouse_grid_common_apps = "Show numbers and mouse grid on common Windows apps."
    dictation_notepad_review_correction_formatting = "Dictation into Notepad with review, correction, formatting, and safe paste behavior."
    browser_edge_or_chrome_navigation = "Browser navigation in Edge or Chrome, including page find, tabs, private window, print, save page, and scrolling."
    app_switching_window_management_snap_layouts_settings = "App switching, Task View, virtual desktop navigation, window management, Snap Layouts, and safe settings surfaces."
    keyboard_mouse_media_file_search = "Keyboard, mouse, media, and Explorer-backed file-search walkthroughs."
    help_command_discovery_palette = "Help and command discovery through the visible command palette."
    voice_shortcuts_create_manage_execute = "Local voice shortcuts create/manage/execute walkthrough."
    community_extension_import_manage = "Community extension import, disabled-by-default enablement, disable, and rollback walkthrough."
    update_splash_manifest_walkthrough = "Update splash manifest walkthrough for newly added or changed commands."
    apple_style_visual_polish_walkthrough = "Apple Voice Control-style visual polish walkthrough across Callsign visible surfaces."
    public_website_installer_hash_match = "Public website download hash comparison against the local Callsign-Setup.exe."
}

$manualEvidenceCommands = @{
    clean_install_public_installer = "Download /downloads/Callsign-Setup.exe from the public website, install on a clean Windows profile, launch Callsign, and record installer URL, hash, size, and install result."
    microphone_setup_voice_enrollment = "Open Callsign, create/select a profile, record at least three voice samples, calibrate wake/identity if needed, and record enrollment status plus microphone device."
    identity_failure_timeout_cancel_reset = "Run wake -> wrong identity, wake -> timeout, wake -> cancel, and wake -> reset flows; record that no command executes and visible status/readout returns to idle."
    show_numbers_and_mouse_grid_common_apps = "Open Notepad, Settings, and File Explorer; say show numbers/show grid, activate a numbered target, refine/click grid cells, and record visible overlay behavior."
    dictation_notepad_review_correction_formatting = "Dictate into Callsign review, correct text, apply formatting/symbol commands, paste into Notepad only after review, and record sensitive-target blocking evidence."
    browser_edge_or_chrome_navigation = "Run browser open/search/navigation, tabs, find, zoom, print/save-page dialogs, private window, and scroll commands in Edge or Chrome; record visible status."
    app_switching_window_management_snap_layouts_settings = "Run app switching, Task View, virtual desktop, minimize/maximize/restore/close, snap layouts, and safe Settings-page commands; record visible outcomes."
    keyboard_mouse_media_file_search = "Run keyboard, mouse, scroll, media, and Explorer-backed file search/open/reveal workflows; record that actions are visible, bounded, and policy-gated."
    help_command_discovery_palette = "Say what can I say, search and select several built-in and extension commands, inspect risk/examples/details, then dismiss the command palette by voice."
    voice_shortcuts_create_manage_execute = "Open the Shortcuts surface, create a shortcut with command and wait steps, save it, disable and re-enable it, run it by voice, and record the visible surfaces each step triggers."
    community_extension_import_manage = "Import a community DLL or folder through the Packs UI, confirm it starts disabled, inspect metadata/signature/entitlement status, enable it, disable it, remove it, and reimport it."
    update_splash_manifest_walkthrough = "Load an update manifest with added/changed/removed commands or extension-pack changes, confirm the splash lists those changes, reads the summary, and can be dismissed by voice."
    apple_style_visual_polish_walkthrough = "Exercise the wake overlay, visible-controls HUD, mouse grid, keyboard overlay, command palette, correction chooser, update splash, and startup walkthrough; record screenshots or video proving compact translucent surfaces, readable 4.5:1-style text contrast, rounded 20-26 px geometry, non-activating overlays, accessible names, and visible stop/cancel/status affordances."
    public_website_installer_hash_match = "Compare SHA-256 and size of the public website /downloads/Callsign-Setup.exe against the local Callsign-Setup.exe built by release readiness."
}

$manualEvidenceExpectedResults = @{
    clean_install_public_installer = "Callsign installs from the public website installer on a clean Windows profile and launches into the visible setup flow."
    microphone_setup_voice_enrollment = "The selected profile records fresh voice samples, reports enrollment status, and shows the microphone device used for wake and identity verification."
    identity_failure_timeout_cancel_reset = "Wrong identity, timeout, cancel, and reset leave no command executed and return the visible session/readout to idle."
    show_numbers_and_mouse_grid_common_apps = "Numbered controls and mouse grid remain visible, accessible, and usable across common Windows apps without hidden actions."
    dictation_notepad_review_correction_formatting = "Dictated text is reviewed, corrected, formatted, and pasted visibly, with sensitive targets blocked before paste."
    browser_edge_or_chrome_navigation = "Browser commands update visible Edge or Chrome surfaces for navigation, tabs, find, zoom, print/save dialogs, private windows, and scrolling."
    app_switching_window_management_snap_layouts_settings = "App switching, Task View, virtual desktop, window management, Snap Layouts, and settings commands produce visible reversible Windows actions."
    keyboard_mouse_media_file_search = "Keyboard, mouse, media, and file-search commands remain visible, bounded, policy-gated, and Explorer-backed for open/reveal flows."
    help_command_discovery_palette = "The command palette shows searchable built-in and extension commands with risk, examples, availability, approval, and voice dismissal."
    voice_shortcuts_create_manage_execute = "Local voice shortcuts save, execute visible command/wait sequences, remain enableable/disableable, and do not bypass policy or audit."
    community_extension_import_manage = "Community packs import disabled by default, expose metadata/gates, can be enabled, disabled, removed, and reimported without bypassing policy."
    update_splash_manifest_walkthrough = "The update splash reads manifest changes, lists added/changed/removed commands or packs, and dismisses by voice."
    apple_style_visual_polish_walkthrough = "All core visible surfaces demonstrate compact, high-contrast, translucent, non-activating, accessible, visible-status design consistent with the CallsignVisualStyle contract."
    public_website_installer_hash_match = "The public website installer download URL, SHA-256, and size match the local release installer."
}

$manualEvidenceCategories = @{
    clean_install_public_installer = @("App launch", "Voice access controls")
    microphone_setup_voice_enrollment = @("Voice access controls")
    identity_failure_timeout_cancel_reset = @("Voice access controls")
    show_numbers_and_mouse_grid_common_apps = @("Visible control numbers", "Mouse grid")
    dictation_notepad_review_correction_formatting = @("Dictation review", "Text editing", "Correction alternatives")
    browser_edge_or_chrome_navigation = @("Browser navigation")
    app_switching_window_management_snap_layouts_settings = @("App switching", "Window management", "Safe system/settings control")
    keyboard_mouse_media_file_search = @("Mouse and scrolling", "Keyboard commands", "File search", "Safe system/settings control")
    help_command_discovery_palette = @("Help and command discovery")
    voice_shortcuts_create_manage_execute = @("Voice shortcuts")
    community_extension_import_manage = @("Extension packs")
    update_splash_manifest_walkthrough = @("Update splash")
    apple_style_visual_polish_walkthrough = @("Voice access controls", "Visible control numbers", "Mouse grid", "Dictation review", "Correction alternatives", "Help and command discovery", "Update splash")
    public_website_installer_hash_match = @()
}

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

function Read-Text {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        return ""
    }

    return Get-Content -LiteralPath $Path -Raw
}

function Get-ParityRows {
    param([string]$Markdown)

    $rows = New-Object System.Collections.Generic.List[object]
    foreach ($line in ($Markdown -split "`r?`n")) {
        if (-not $line.StartsWith("|")) {
            continue
        }

        if ($line -match '^\|\s*-+') {
            continue
        }

        $cells = $line.Trim().Trim("|").Split("|") | ForEach-Object { $_.Trim() }
        if ($cells.Count -lt 5 -or $cells[0] -eq "Category") {
            continue
        }

        $rows.Add([pscustomobject]@{
            category = $cells[0]
            release_target = $cells[1]
            status = $cells[2]
            acceptance_criteria = $cells[3]
            verification = $cells[4]
        })
    }

    return $rows
}

function Get-PropertyValue {
    param(
        [object]$Object,
        [string]$Name
    )

    if ($null -eq $Object) {
        return $null
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Test-ManualEvidenceCheck {
    param(
        [object]$ManualEvidence,
        [string]$Id
    )

    $checks = Get-PropertyValue $ManualEvidence "checks"
    if ($null -eq $checks) {
        return $false
    }

    foreach ($check in @($checks)) {
        $checkId = Get-PropertyValue $check "id"
        $passed = Get-PropertyValue $check "passed"
        if ($checkId -eq $Id -and $passed -eq $true) {
            return $true
        }
    }

    return $false
}

function Get-ManualEvidenceCheck {
    param(
        [object]$ManualEvidence,
        [string]$Id
    )

    $checks = Get-PropertyValue $ManualEvidence "checks"
    if ($null -eq $checks) {
        return $null
    }

    foreach ($check in @($checks)) {
        $checkId = Get-PropertyValue $check "id"
        if ($checkId -eq $Id) {
            return $check
        }
    }

    return $null
}

function Test-NonEmptyProperty {
    param(
        [object]$Object,
        [string]$Name
    )

    $value = Get-PropertyValue $Object $Name
    return -not [string]::IsNullOrWhiteSpace([string]$value)
}

function Test-NonEmptyArrayProperty {
    param(
        [object]$Object,
        [string]$Name
    )

    $value = Get-PropertyValue $Object $Name
    if ($null -eq $value) {
        return $false
    }

    foreach ($item in @($value)) {
        if (-not [string]::IsNullOrWhiteSpace([string]$item)) {
            return $true
        }
    }

    return $false
}

function Test-ArtifactReference {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    $trimmed = $Value.Trim()
    $uri = $null
    if ([Uri]::TryCreate($trimmed, [UriKind]::Absolute, [ref]$uri)) {
        return $uri.Scheme -eq "https" -or $uri.Scheme -eq "http"
    }

    try {
        $candidatePath = if ([System.IO.Path]::IsPathRooted($trimmed)) {
            [System.IO.Path]::GetFullPath($trimmed)
        }
        else {
            [System.IO.Path]::GetFullPath((Join-Path $repoRoot $trimmed))
        }

        $repoFullPath = [System.IO.Path]::GetFullPath($repoRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar))
        $relativePath = [System.IO.Path]::GetRelativePath($repoFullPath, $candidatePath)
        return -not $relativePath.StartsWith("..", [System.StringComparison]::Ordinal) -and -not [System.IO.Path]::IsPathRooted($relativePath)
    }
    catch {
        return $false
    }
}

function Test-ArtifactReferences {
    param(
        [object]$Object,
        [string]$Name
    )

    $value = Get-PropertyValue $Object $Name
    if ($null -eq $value) {
        return $false
    }

    $items = @($value)
    if ($items.Count -eq 0) {
        return $false
    }

    foreach ($item in $items) {
        if (-not (Test-ArtifactReference ([string]$item))) {
            return $false
        }
    }

    return $true
}

function Get-DuplicateManualEvidenceCheckIds {
    param([object]$ManualEvidence)

    $checks = Get-PropertyValue $ManualEvidence "checks"
    if ($null -eq $checks) {
        return @()
    }

    $seen = @{}
    $duplicates = New-Object System.Collections.Generic.List[string]
    foreach ($check in @($checks)) {
        $checkId = [string](Get-PropertyValue $check "id")
        if ([string]::IsNullOrWhiteSpace($checkId)) {
            continue
        }

        if ($seen.ContainsKey($checkId)) {
            if (-not $duplicates.Contains($checkId)) {
                $duplicates.Add($checkId)
            }
        }
        else {
            $seen[$checkId] = $true
        }
    }

    return @($duplicates)
}

function Test-IntegerPropertyEquals {
    param(
        [object]$Object,
        [string]$Name,
        [Int64]$Expected
    )

    $value = Get-PropertyValue $Object $Name
    if ($null -eq $value) {
        return $false
    }

    $parsed = [Int64]0
    if (-not [Int64]::TryParse([string]$value, [ref]$parsed)) {
        return $false
    }

    return $parsed -eq $Expected
}

function Test-IsoUtcTimestamp {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    $parsed = [DateTimeOffset]::MinValue
    return [DateTimeOffset]::TryParse($Value, [ref]$parsed)
}

function Join-ManualEvidenceCategories {
    param([object]$Categories)

    if ($null -eq $Categories) {
        return ""
    }

    return (@($Categories) | ForEach-Object { [string]$_ }) -join "|"
}

function Test-InstallerDownloadUrl {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    $uri = $null
    if (-not [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$uri)) {
        return $false
    }

    if ($uri.Scheme -ne "https" -and $uri.Scheme -ne "http") {
        return $false
    }

    return $uri.AbsolutePath.EndsWith("/Callsign-Setup.exe", [System.StringComparison]::OrdinalIgnoreCase)
}

function New-ManualEvidenceTemplate {
    param(
        [string]$InstallerHash,
        [Int64]$InstallerSizeBytes
    )

    $templateChecks = foreach ($manualCheckId in $requiredManualEvidenceChecks) {
        [pscustomobject]@{
            id = $manualCheckId
            description = $manualEvidenceDescriptions[$manualCheckId]
            evidence_command = $manualEvidenceCommands[$manualCheckId]
            expected_result = $manualEvidenceExpectedResults[$manualCheckId]
            parity_categories = @($manualEvidenceCategories[$manualCheckId])
            passed = $false
            evidence_utc = ""
            operator = ""
            environment = ""
            observed_result = ""
            artifacts = @()
            notes = ""
        }
    }

    return [pscustomobject]@{
        schema = "callsign.voice_access_parity.manual_evidence.v1"
        generated_utc = [DateTime]::UtcNow.ToString("o")
        local_installer_sha256 = $InstallerHash
        local_installer_size_bytes = $InstallerSizeBytes
        website_installer_sha256 = ""
        website_installer_size_bytes = 0
        website_download_url = ""
        test_machine = ""
        windows_version = ""
        callsign_version = ""
        checks = $templateChecks
    }
}

$checks = New-Object System.Collections.Generic.List[object]
$matrix = Read-Text $matrixPath
$testPlan = Read-Text $testPlanPath
$generatedParityPage = Read-Text $generatedParityPagePath
$generatedVoiceUxPage = Read-Text $generatedVoiceUxPagePath
$generatedTierPage = Read-Text $generatedTierPagePath
$generatedSecurityPage = Read-Text $generatedSecurityPagePath
$visualStyleSource = Read-Text $visualStylePath
$rows = @(Get-ParityRows $matrix)
$installerHash = $null
$installerSizeBytes = 0
$canonicalManualCategoriesCovered = New-Object System.Collections.Generic.HashSet[string]
foreach ($manualCheckId in $requiredManualEvidenceChecks) {
    foreach ($category in @($manualEvidenceCategories[$manualCheckId])) {
        if (-not [string]::IsNullOrWhiteSpace([string]$category)) {
            [void]$canonicalManualCategoriesCovered.Add([string]$category)
        }
    }
}

$canonicalManualCategoriesMissing = foreach ($category in $requiredCategories) {
    if (-not $canonicalManualCategoriesCovered.Contains($category)) {
        $category
    }
}

$checks.Add((New-Check "Parity matrix exists" (Test-Path -LiteralPath $matrixPath) $matrixPath))
$checks.Add((New-Check "Test plan exists" (Test-Path -LiteralPath $testPlanPath) $testPlanPath))
$checks.Add((New-Check "Generated parity page exists" (Test-Path -LiteralPath $generatedParityPagePath) $generatedParityPagePath))
$checks.Add((New-Check "Generated Voice UX page exists" (Test-Path -LiteralPath $generatedVoiceUxPagePath) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated tier page exists" (Test-Path -LiteralPath $generatedTierPagePath) $generatedTierPagePath))
$checks.Add((New-Check "Generated security page exists" (Test-Path -LiteralPath $generatedSecurityPagePath) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated parity page is current enough to include the matrix title" ($generatedParityPage.IndexOf("Voice Access Parity Matrix", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedParityPagePath))
$checks.Add((New-Check "Installer exists" (Test-Path -LiteralPath $installerPath) $installerPath))
$checks.Add((New-Check "Canonical manual evidence map covers every parity category" (@($canonicalManualCategoriesMissing).Count -eq 0) $(if (@($canonicalManualCategoriesMissing).Count -eq 0) { "All required categories are mapped to manual/live proof checks." } else { @($canonicalManualCategoriesMissing) -join ", " })))

if (Test-Path -LiteralPath $installerPath) {
    $installerItem = Get-Item -LiteralPath $installerPath
    $installerHash = (Get-FileHash -LiteralPath $installerPath -Algorithm SHA256).Hash
    $installerSizeBytes = [Int64]$installerItem.Length
}

if ($WriteManualEvidenceTemplate) {
    if ([string]::IsNullOrWhiteSpace($ManualEvidenceTemplatePath)) {
        $ManualEvidenceTemplatePath = Join-Path $repoRoot "build\voice-access-parity-manual-evidence.template.json"
    }

    $templateDir = Split-Path -Parent $ManualEvidenceTemplatePath
    if (-not [string]::IsNullOrWhiteSpace($templateDir)) {
        New-Item -ItemType Directory -Force -Path $templateDir | Out-Null
    }

    New-ManualEvidenceTemplate $installerHash $installerSizeBytes | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $ManualEvidenceTemplatePath -Encoding UTF8
    Write-Host "Manual evidence template written: $ManualEvidenceTemplatePath" -ForegroundColor Cyan
    return
}

foreach ($category in $requiredCategories) {
    $row = $rows | Where-Object { $_.category -eq $category } | Select-Object -First 1
    $checks.Add((New-Check "Matrix category present: $category" ($null -ne $row) $matrixPath))
    $checks.Add((New-Check "Generated parity page includes category: $category" ($generatedParityPage.IndexOf($category, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedParityPagePath))
    if ($null -ne $row) {
        $checks.Add((New-Check "Matrix category marked Done: $category" ($row.status -eq "Done") $row.status))
        $checks.Add((New-Check "Matrix category has verification text: $category" (-not [string]::IsNullOrWhiteSpace($row.verification)) $row.verification))
    }
}

$checks.Add((New-Check "Test plan keeps manual parity coverage explicit" ($testPlan.IndexOf("Manual parity coverage must include", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $testPlanPath))
$checks.Add((New-Check "Release acceptance requires public website installer proof" ($matrix.IndexOf("public website serves the latest installer", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $matrixPath))
$checks.Add((New-Check "Generated parity page includes visible-status audit contract" ($generatedParityPage.IndexOf("visible_status", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedParityPagePath))
$checks.Add((New-Check "Generated parity page includes voice-control audit contract" ($generatedParityPage.IndexOf("voice-control status", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedParityPagePath))
$checks.Add((New-Check "Generated parity page includes command-level entitlement gating" ($generatedParityPage.IndexOf("command-level entitlement gating", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedParityPagePath))
$checks.Add((New-Check "Generated Voice UX page includes shared visual contract" ($generatedVoiceUxPage.IndexOf("CallsignVisualStyle", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes macOS Voice Control target" ($generatedVoiceUxPage.IndexOf("macOS Voice Control", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes compact visual principle" ($generatedVoiceUxPage.IndexOf("compact", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes high-contrast visual principle" ($generatedVoiceUxPage.IndexOf("high-contrast", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes translucent visual principle" ($generatedVoiceUxPage.IndexOf("translucent", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes non-activating visual principle" ($generatedVoiceUxPage.IndexOf("non-activating", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes accessible visual principle" ($generatedVoiceUxPage.IndexOf("accessible", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes visible status principle" ($generatedVoiceUxPage.IndexOf("visible status", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Shared visual style source exists" (Test-Path -LiteralPath $visualStylePath) $visualStylePath))
$checks.Add((New-Check "Shared visual style defines contrast evidence token" ($visualStyleSource.IndexOf("contrast>=4.5:1", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $visualStyleSource.IndexOf("MinimumTextContrastRatio = 4.5", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $visualStylePath))
$checks.Add((New-Check "Shared visual style defines translucency evidence token" ($visualStyleSource.IndexOf("opacity=0.86-0.99", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $visualStyleSource.IndexOf("MinimumOverlayOpacity = 0.86", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $visualStyleSource.IndexOf("MaximumSurfaceOpacity = 0.99", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $visualStylePath))
$checks.Add((New-Check "Shared visual style defines compact radius evidence token" ($visualStyleSource.IndexOf("radius=20-26px", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $visualStyleSource.IndexOf("CompactRadius = 20", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $visualStyleSource.IndexOf("LargeSurfaceRadius = 26", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $visualStylePath))
$checks.Add((New-Check "Shared visual style defines stop-visible evidence token" ($visualStyleSource.IndexOf("stop-visible", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $visualStylePath))
$checks.Add((New-Check "Generated Voice UX page includes contrast evidence token" ($generatedVoiceUxPage.IndexOf("4.5:1", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes translucency range evidence token" ($generatedVoiceUxPage.IndexOf("0.86", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("0.99", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes compact radius evidence token" ($generatedVoiceUxPage.IndexOf("20", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("26 px", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes visible stop affordance token" ($generatedVoiceUxPage.IndexOf("visible stop", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or $generatedVoiceUxPage.IndexOf("stop/cancel/status", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated tier page includes command-level entitlement gate" ($generatedTierPage.IndexOf("bundled inside a Free pack", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated security page includes command-level entitlement gate" ($generatedSecurityPage.IndexOf("paid-tier command may route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated tier page includes invalid command metadata gate" ($generatedTierPage.IndexOf("InvalidPack", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("cannot route or execute commands", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated security page includes invalid command metadata gate" ($generatedSecurityPage.IndexOf("InvalidPack", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("cannot route or execute commands", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated tier page includes registry execution policy gate" ($generatedTierPage.IndexOf("direct pack execution fails closed without identity proof", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated security page includes registry execution policy gate" ($generatedSecurityPage.IndexOf("command registry enforces policy again at the pack execution boundary", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("fail closed without identity proof", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("approval-required commands fail until explicit approval is supplied", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated tier page includes structured policy outcome metadata" ($generatedTierPage.IndexOf("PolicyDecision", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("PolicyApprovalRequirement", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("PolicyRiskTier", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated security page includes structured policy outcome metadata" ($generatedSecurityPage.IndexOf("PolicyDecision", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("PolicyApprovalRequirement", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("PolicyRiskTier", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated tier page includes paid discovery non-routing status" ($generatedTierPage.IndexOf("will not route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("command palette", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated Voice UX page includes gated discovery non-routing status" ($generatedVoiceUxPage.IndexOf("will not route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("paid-tier requirement", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes command availability column contract" ($generatedVoiceUxPage.IndexOf("dedicated availability column", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("entitlement-required", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated tier page includes background approval gate" ($generatedTierPage.IndexOf("BackgroundAllowedWithApproval", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated security page includes background approval gate" ($generatedSecurityPage.IndexOf("BackgroundAllowedWithApproval", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated tier page includes visible-required gate" ($generatedTierPage.IndexOf("VisibleRequired", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated security page includes visible-required gate" ($generatedSecurityPage.IndexOf("VisibleRequired", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated tier page includes high-impact privacy approval gate" ($generatedTierPage.IndexOf("Clipboard", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("FileContents", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("ScreenshotOrOcr", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("ExternalData", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated security page includes high-impact privacy approval gate" ($generatedSecurityPage.IndexOf("Clipboard", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("FileContents", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("ScreenshotOrOcr", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("ExternalData", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))

if ($RunSmoke) {
    Write-Host "Running alpha smoke suite..." -ForegroundColor Cyan
    dotnet run --project $smokeProject -c Release --no-build
    $checks.Add((New-Check "Alpha smoke suite" ($LASTEXITCODE -eq 0) "dotnet run --project $smokeProject -c Release --no-build"))
}

$manualEvidenceLoaded = $false
$manualEvidence = $null
if (-not [string]::IsNullOrWhiteSpace($ManualEvidencePath) -and (Test-Path -LiteralPath $ManualEvidencePath)) {
    $manualEvidence = Get-Content -LiteralPath $ManualEvidencePath -Raw | ConvertFrom-Json
    $manualEvidenceLoaded = $true
}

$checks.Add((New-Check "Manual parity evidence supplied" ($manualEvidenceLoaded -or -not $RequireManualEvidence) $(if ($manualEvidenceLoaded) { $ManualEvidencePath } else { "Not supplied" })))

if ($manualEvidenceLoaded) {
    $manualSchema = [string](Get-PropertyValue $manualEvidence "schema")
    $manualGeneratedUtc = [string](Get-PropertyValue $manualEvidence "generated_utc")
    $manualWebsiteUrl = [string](Get-PropertyValue $manualEvidence "website_download_url")
    $duplicateManualCheckIds = @(Get-DuplicateManualEvidenceCheckIds $manualEvidence)
    $checks.Add((New-Check "Manual evidence schema is supported" ($manualSchema -eq "callsign.voice_access_parity.manual_evidence.v1") $(if ($manualSchema) { $manualSchema } else { "Missing schema" })))
    $checks.Add((New-Check "Manual evidence generated timestamp is valid" (Test-IsoUtcTimestamp $manualGeneratedUtc) $(if ($manualGeneratedUtc) { $manualGeneratedUtc } else { "Missing generated_utc" })))
    $checks.Add((New-Check "Manual evidence website download URL targets Callsign installer" (Test-InstallerDownloadUrl $manualWebsiteUrl) $(if ($manualWebsiteUrl) { $manualWebsiteUrl } else { "Missing website_download_url" })))
    $checks.Add((New-Check "Manual evidence check ids are unique" ($duplicateManualCheckIds.Count -eq 0) $(if ($duplicateManualCheckIds.Count -eq 0) { "No duplicate check ids" } else { "Duplicate check ids: $($duplicateManualCheckIds -join ', ')" })))

    foreach ($requiredTopLevelField in @("test_machine", "windows_version", "callsign_version")) {
        $checks.Add((New-Check "Manual evidence includes $requiredTopLevelField" (Test-NonEmptyProperty $manualEvidence $requiredTopLevelField) $(if (Test-NonEmptyProperty $manualEvidence $requiredTopLevelField) { [string](Get-PropertyValue $manualEvidence $requiredTopLevelField) } else { "Missing $requiredTopLevelField" })))
    }

    foreach ($manualCheckId in $requiredManualEvidenceChecks) {
        $manualCheck = Get-ManualEvidenceCheck $manualEvidence $manualCheckId
        $expectedDescription = [string]$manualEvidenceDescriptions[$manualCheckId]
        $actualDescription = [string](Get-PropertyValue $manualCheck "description")
        $expectedEvidenceCommand = [string]$manualEvidenceCommands[$manualCheckId]
        $actualEvidenceCommand = [string](Get-PropertyValue $manualCheck "evidence_command")
        $expectedResult = [string]$manualEvidenceExpectedResults[$manualCheckId]
        $actualExpectedResult = [string](Get-PropertyValue $manualCheck "expected_result")
        $expectedCategories = Join-ManualEvidenceCategories $manualEvidenceCategories[$manualCheckId]
        $actualCategories = Join-ManualEvidenceCategories (Get-PropertyValue $manualCheck "parity_categories")
        $checks.Add((New-Check "Manual parity evidence passed: $manualCheckId" (Test-ManualEvidenceCheck $manualEvidence $manualCheckId) $ManualEvidencePath))
        $checks.Add((New-Check "Manual parity evidence timestamped: $manualCheckId" (Test-IsoUtcTimestamp ([string](Get-PropertyValue $manualCheck "evidence_utc"))) $(if ($null -ne $manualCheck) { [string](Get-PropertyValue $manualCheck "evidence_utc") } else { "Missing check" })))
        $checks.Add((New-Check "Manual parity evidence has description: $manualCheckId" (Test-NonEmptyProperty $manualCheck "description") $(if ($null -ne $manualCheck) { $actualDescription } else { "Missing check" })))
        $checks.Add((New-Check "Manual parity evidence description matches canonical prompt: $manualCheckId" ($actualDescription -eq $expectedDescription) $(if ($actualDescription) { $actualDescription } else { "Missing description" })))
        $checks.Add((New-Check "Manual parity evidence has evidence_command: $manualCheckId" (Test-NonEmptyProperty $manualCheck "evidence_command") $(if ($null -ne $manualCheck) { $actualEvidenceCommand } else { "Missing check" })))
        $checks.Add((New-Check "Manual parity evidence command matches canonical prompt: $manualCheckId" ($actualEvidenceCommand -eq $expectedEvidenceCommand) $(if ($actualEvidenceCommand) { $actualEvidenceCommand } else { "Missing evidence_command" })))
        $checks.Add((New-Check "Manual parity evidence has expected_result: $manualCheckId" (Test-NonEmptyProperty $manualCheck "expected_result") $(if ($null -ne $manualCheck) { $actualExpectedResult } else { "Missing check" })))
        $checks.Add((New-Check "Manual parity evidence expected_result matches canonical proof target: $manualCheckId" ($actualExpectedResult -eq $expectedResult) $(if ($actualExpectedResult) { $actualExpectedResult } else { "Missing expected_result" })))
        $checks.Add((New-Check "Manual parity evidence categories match canonical matrix coverage: $manualCheckId" ($actualCategories -eq $expectedCategories) $(if ($actualCategories) { $actualCategories } else { "No parity_categories" })))
        $checks.Add((New-Check "Manual parity evidence has operator: $manualCheckId" (Test-NonEmptyProperty $manualCheck "operator") $(if ($null -ne $manualCheck) { [string](Get-PropertyValue $manualCheck "operator") } else { "Missing check" })))
        $checks.Add((New-Check "Manual parity evidence has environment: $manualCheckId" (Test-NonEmptyProperty $manualCheck "environment") $(if ($null -ne $manualCheck) { [string](Get-PropertyValue $manualCheck "environment") } else { "Missing check" })))
        $checks.Add((New-Check "Manual parity evidence has observed_result for passed check: $manualCheckId" ((-not (Test-ManualEvidenceCheck $manualEvidence $manualCheckId)) -or (Test-NonEmptyProperty $manualCheck "observed_result")) $(if ($null -ne $manualCheck) { [string](Get-PropertyValue $manualCheck "observed_result") } else { "Missing check" })))
        $checks.Add((New-Check "Manual parity evidence has artifact references for passed check: $manualCheckId" ((-not (Test-ManualEvidenceCheck $manualEvidence $manualCheckId)) -or (Test-NonEmptyArrayProperty $manualCheck "artifacts")) $(if ($null -ne $manualCheck) { (@(Get-PropertyValue $manualCheck "artifacts") -join ", ") } else { "Missing check" })))
        $checks.Add((New-Check "Manual parity evidence artifact references are valid: $manualCheckId" ((-not (Test-ManualEvidenceCheck $manualEvidence $manualCheckId)) -or (Test-ArtifactReferences $manualCheck "artifacts")) $(if ($null -ne $manualCheck) { (@(Get-PropertyValue $manualCheck "artifacts") -join ", ") } else { "Missing check" })))
        $checks.Add((New-Check "Manual parity evidence has notes: $manualCheckId" (Test-NonEmptyProperty $manualCheck "notes") $(if ($null -ne $manualCheck) { [string](Get-PropertyValue $manualCheck "notes") } else { "Missing check" })))
    }

    $manualInstallerHash = Get-PropertyValue $manualEvidence "local_installer_sha256"
    $manualWebsiteHash = Get-PropertyValue $manualEvidence "website_installer_sha256"
    $checks.Add((New-Check "Manual evidence local installer hash matches current installer" ($manualInstallerHash -eq $installerHash) $(if ($manualInstallerHash) { $manualInstallerHash } else { "Missing local_installer_sha256" })))
    $checks.Add((New-Check "Manual evidence website installer hash matches current installer" ($manualWebsiteHash -eq $installerHash) $(if ($manualWebsiteHash) { $manualWebsiteHash } else { "Missing website_installer_sha256" })))
    $checks.Add((New-Check "Manual evidence local installer size matches current installer" (Test-IntegerPropertyEquals $manualEvidence "local_installer_size_bytes" $installerSizeBytes) $(if ($null -ne (Get-PropertyValue $manualEvidence "local_installer_size_bytes")) { [string](Get-PropertyValue $manualEvidence "local_installer_size_bytes") } else { "Missing local_installer_size_bytes" })))
    $checks.Add((New-Check "Manual evidence website installer size matches current installer" (Test-IntegerPropertyEquals $manualEvidence "website_installer_size_bytes" $installerSizeBytes) $(if ($null -ne (Get-PropertyValue $manualEvidence "website_installer_size_bytes")) { [string](Get-PropertyValue $manualEvidence "website_installer_size_bytes") } else { "Missing website_installer_size_bytes" })))
}
elseif ($RequireManualEvidence) {
    foreach ($manualCheckId in $requiredManualEvidenceChecks) {
        $checks.Add((New-Check "Manual parity evidence passed: $manualCheckId" $false "Manual evidence file was not supplied."))
    }
}

$manualChecksRemaining = foreach ($manualCheckId in $requiredManualEvidenceChecks) {
    if (-not $manualEvidenceLoaded -or -not (Test-ManualEvidenceCheck $manualEvidence $manualCheckId)) {
        $manualEvidenceDescriptions[$manualCheckId]
    }
}

$manualCategoriesCovered = New-Object System.Collections.Generic.HashSet[string]
if ($manualEvidenceLoaded) {
    foreach ($manualCheckId in $requiredManualEvidenceChecks) {
        if (Test-ManualEvidenceCheck $manualEvidence $manualCheckId) {
            foreach ($category in @($manualEvidenceCategories[$manualCheckId])) {
                [void]$manualCategoriesCovered.Add([string]$category)
            }
        }
    }
}

$manualCategoriesMissing = foreach ($category in $requiredCategories) {
    if (-not $manualCategoriesCovered.Contains($category)) {
        $category
    }
}

if ($manualEvidenceLoaded) {
    $duplicateManualCheckIds = @(Get-DuplicateManualEvidenceCheckIds $manualEvidence)
    if ($duplicateManualCheckIds.Count -gt 0) {
        "Manual evidence contains duplicate check ids: $($duplicateManualCheckIds -join ', ')."
    }

    foreach ($requiredTopLevelField in @("test_machine", "windows_version", "callsign_version")) {
        if (-not (Test-NonEmptyProperty $manualEvidence $requiredTopLevelField)) {
            "Manual evidence is missing required field: $requiredTopLevelField."
        }
    }

    foreach ($manualCheckId in $requiredManualEvidenceChecks) {
        $manualCheck = Get-ManualEvidenceCheck $manualEvidence $manualCheckId
        $actualDescription = [string](Get-PropertyValue $manualCheck "description")
        $actualEvidenceCommand = [string](Get-PropertyValue $manualCheck "evidence_command")
        $actualExpectedResult = [string](Get-PropertyValue $manualCheck "expected_result")
        $actualCategories = Join-ManualEvidenceCategories (Get-PropertyValue $manualCheck "parity_categories")
        $expectedCategories = Join-ManualEvidenceCategories $manualEvidenceCategories[$manualCheckId]
        if (-not (Test-NonEmptyProperty $manualCheck "description")) {
            "Manual evidence check $manualCheckId is missing a description."
        }
        elseif ($actualDescription -ne [string]$manualEvidenceDescriptions[$manualCheckId]) {
            "Manual evidence check $manualCheckId description does not match the canonical template."
        }

        if (-not (Test-NonEmptyProperty $manualCheck "evidence_command")) {
            "Manual evidence check $manualCheckId is missing an evidence_command walkthrough prompt."
        }
        elseif ($actualEvidenceCommand -ne [string]$manualEvidenceCommands[$manualCheckId]) {
            "Manual evidence check $manualCheckId evidence_command does not match the canonical template."
        }

        if (-not (Test-NonEmptyProperty $manualCheck "expected_result")) {
            "Manual evidence check $manualCheckId is missing an expected_result proof target."
        }
        elseif ($actualExpectedResult -ne [string]$manualEvidenceExpectedResults[$manualCheckId]) {
            "Manual evidence check $manualCheckId expected_result does not match the canonical proof target."
        }

        if ((Test-ManualEvidenceCheck $manualEvidence $manualCheckId) -and -not (Test-NonEmptyProperty $manualCheck "observed_result")) {
            "Manual evidence check $manualCheckId is marked passed but is missing an observed_result."
        }

        if ((Test-ManualEvidenceCheck $manualEvidence $manualCheckId) -and -not (Test-NonEmptyArrayProperty $manualCheck "artifacts")) {
            "Manual evidence check $manualCheckId is marked passed but has no artifact references."
        }

        if ((Test-ManualEvidenceCheck $manualEvidence $manualCheckId) -and -not (Test-ArtifactReferences $manualCheck "artifacts")) {
            "Manual evidence check $manualCheckId is marked passed but has invalid artifact references."
        }

        if ($actualCategories -ne $expectedCategories) {
            "Manual evidence check $manualCheckId parity_categories do not match the canonical matrix coverage."
        }
    }

    if ((Get-PropertyValue $manualEvidence "local_installer_sha256") -ne $installerHash) {
        "Manual evidence local installer hash does not match the current Callsign-Setup.exe."
    }

    if ((Get-PropertyValue $manualEvidence "website_installer_sha256") -ne $installerHash) {
        "Manual evidence website installer hash does not match the current Callsign-Setup.exe."
    }

    if (-not (Test-IntegerPropertyEquals $manualEvidence "local_installer_size_bytes" $installerSizeBytes)) {
        "Manual evidence local installer size does not match the current Callsign-Setup.exe."
    }

    if (-not (Test-IntegerPropertyEquals $manualEvidence "website_installer_size_bytes" $installerSizeBytes)) {
        "Manual evidence website installer size does not match the current Callsign-Setup.exe."
    }
}

$passed = @($checks | Where-Object { $_.passed }).Count
$failed = @($checks | Where-Object { -not $_.passed }).Count
$releaseReady = $failed -eq 0 -and $manualEvidenceLoaded
$releaseBlockers = New-Object System.Collections.Generic.List[string]
if (-not $manualEvidenceLoaded) {
    $releaseBlockers.Add("Manual/live parity evidence was not supplied; run with a completed manual evidence file before claiming release parity.")
}

foreach ($remainingManualCheck in @($manualChecksRemaining)) {
    $releaseBlockers.Add("Manual/live parity evidence remaining: $remainingManualCheck")
}

foreach ($missingCategory in @($manualCategoriesMissing)) {
    $releaseBlockers.Add("Manual/live category proof missing: $missingCategory")
}

foreach ($failedCheck in @($checks | Where-Object { -not $_.passed })) {
    $releaseBlockers.Add("Failed evidence check: $($failedCheck.name)")
}

$evidence = [pscustomobject]@{
    generated_utc = [DateTime]::UtcNow.ToString("o")
    passed = $failed -eq 0
    release_ready = $releaseReady
    release_blockers = @($releaseBlockers)
    passed_count = $passed
    failed_count = $failed
    installer = [pscustomobject]@{
        path = $installerPath
        sha256 = $installerHash
        size_bytes = $installerSizeBytes
    }
    matrix_rows = $rows
    checks = $checks
    canonical_manual_evidence = [pscustomobject]@{
        categories_covered = @($canonicalManualCategoriesCovered)
        categories_missing = @($canonicalManualCategoriesMissing)
    }
    manual_evidence = [pscustomobject]@{
        supplied = $manualEvidenceLoaded
        path = $ManualEvidencePath
        data = $manualEvidence
        remaining = @($manualChecksRemaining)
        categories_covered = @($manualCategoriesCovered)
        categories_missing = @($manualCategoriesMissing)
    }
}

$evidenceDir = Split-Path -Parent $EvidencePath
if (-not [string]::IsNullOrWhiteSpace($evidenceDir)) {
    New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
}

$evidence | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $EvidencePath -Encoding UTF8

foreach ($check in $checks) {
    $prefix = if ($check.passed) { "PASS" } else { "FAIL" }
    $color = if ($check.passed) { "Green" } else { "Red" }
    Write-Host "${prefix}: $($check.name) - $($check.detail)" -ForegroundColor $color
}

Write-Host "Evidence written: $EvidencePath" -ForegroundColor Cyan

if ($failed -gt 0) {
    exit 1
}
