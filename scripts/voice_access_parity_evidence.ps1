param(
    [string]$EvidencePath = "",
    [string]$ManualEvidencePath = "",
    [string]$ManualEvidenceTemplatePath = "",
    [string]$Root,
    [string]$ReleaseMode = "",
    [string]$WebsiteDownloadUrl = "",
    [string]$WebsiteInstallerHash = "",
    [Int64]$WebsiteInstallerSizeBytes = 0,
    [switch]$RequireManualEvidence,
    [switch]$WriteManualEvidenceTemplate,
    [switch]$RunSmoke
)

$ErrorActionPreference = "Stop"

$repoRoot = if ([string]::IsNullOrWhiteSpace($Root)) {
    Split-Path -Parent $PSScriptRoot
}
else {
    $Root
}
$matrixPath = Join-Path $repoRoot "docs\reference\VOICE_ACCESS_PARITY_MATRIX.md"
$testPlanPath = Join-Path $repoRoot "docs\reference\TEST_PLAN.md"
$generatedParityPagePath = Join-Path $repoRoot "docs\pages\voice-access-parity.html"
$generatedVoiceUxPagePath = Join-Path $repoRoot "docs\pages\voice-ux.html"
$generatedTierPagePath = Join-Path $repoRoot "docs\pages\tier-architecture.html"
$generatedSecurityPagePath = Join-Path $repoRoot "docs\pages\security-model.html"
$wakeHelperPath = Join-Path $repoRoot "src\Callsign.Setup\Payload\testopenwakeword.ps1"
$wakeModelPath = Join-Path $repoRoot "src\Callsign.Setup\Payload\callsign.onnx"
$wakePythonPath = Join-Path $repoRoot "build\runtime-snapshots\openwakeword\python.exe"
$mainFormPath = Join-Path $repoRoot "src\Callsign.UI\MainForm.cs"
$visualStylePath = Join-Path $repoRoot "src\Callsign.UI\CallsignVisualStyle.cs"
$mouseGridOverlayPath = Join-Path $repoRoot "src\Callsign.UI\MouseGridOverlayForm.cs"
$visibleControlsOverlayPath = Join-Path $repoRoot "src\Callsign.UI\VisibleControlsOverlayForm.cs"
$alphaCommandRouterPath = Join-Path $repoRoot "src\Callsign.UI\Services\AlphaCommandRouter.cs"
$alphaSessionStateMachinePath = Join-Path $repoRoot "src\Callsign.UI\Services\AlphaSessionStateMachine.cs"
$dictationTargetSafetyPath = Join-Path $repoRoot "src\Callsign.UI\Services\DictationTargetSafetyService.cs"
$runtimeStatusFormatterPath = Join-Path $repoRoot "src\Callsign.UI\Services\RuntimeStatusFormatter.cs"
$browserLaunchServicePath = Join-Path $repoRoot "src\Callsign.UI\Services\BrowserLaunchService.cs"
$updateCheckServicePath = Join-Path $repoRoot "src\Callsign.UI\Services\UpdateCheckService.cs"
$startMenuLauncherPath = Join-Path $repoRoot "src\Callsign.UI\Services\StartMenuLauncher.cs"
$serviceWorkerPath = Join-Path $repoRoot "src\Callsign.Service\CallsignRuntimeWorker.cs"
$commandDiscoveryPath = Join-Path $repoRoot "src\Callsign.UI\Services\CommandDiscoveryService.cs"
$startupWalkthroughPath = Join-Path $repoRoot "src\Callsign.UI\StartupWalkthroughForm.cs"
$alphaSmokeProgramPath = Join-Path $repoRoot "tests\Callsign.AlphaSmoke\Program.cs"
$smokeProject = Join-Path $repoRoot "tests\Callsign.AlphaSmoke\Callsign.AlphaSmoke.csproj"
$verifyReleaseReadinessPath = Join-Path $repoRoot "scripts\verify-release-readiness.ps1"
$installerPath = Join-Path $repoRoot "Callsign-Setup.exe"

if ([string]::IsNullOrWhiteSpace($EvidencePath)) {
    $EvidencePath = Join-Path $repoRoot "build\voice-access-parity-evidence.json"
}

function Get-DefaultWebsiteDownloadUrl {
    param([string]$CandidateUrl)

    if (-not [string]::IsNullOrWhiteSpace($CandidateUrl)) {
        $script:WebsiteDownloadUrlWasInferred = $false
        return $CandidateUrl
    }

    try {
        if ([bool](Test-NetConnection -ComputerName 'localhost' -Port 8085 -InformationLevel Quiet)) {
            $script:WebsiteDownloadUrlWasInferred = $true
            return 'http://localhost:8085/downloads/Callsign-Setup.exe'
        }
    }
    catch {
    }

    $script:WebsiteDownloadUrlWasInferred = $false
    return $CandidateUrl
}

$WebsiteDownloadUrl = Get-DefaultWebsiteDownloadUrl -CandidateUrl $WebsiteDownloadUrl
$ReleaseMode = if ([string]::IsNullOrWhiteSpace($ReleaseMode)) { "" } else { $ReleaseMode.Trim() }

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
    "installed_end_to_end_automated_checks",
    "human_spoken_core_walkthrough",
    "failure_state_walkthrough",
    "clean_windows_user_or_vm_test",
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
    "startup_walkthrough_browser_overlay_helpers",
    "update_check_state_survives_restart",
    "update_downloaded_installer_path_survives_restart",
    "update_version_match_ignores_installer_hash",
    "read_updates_status_voice_command",
    "read_release_proof_voice_command",
    "read_voice_mode_status_voice_command",
    "read_check_in_status_voice_command",
    "read_visual_status_voice_command",
    "free_parity_paid_gating_walkthrough",
    "read_restart_proof_voice_command",
    "open_release_evidence_folder_action",
    "open_manual_evidence_template_action",
    "startup_walkthrough_open_manual_evidence_checklist_action",
    "startup_walkthrough_open_release_evidence_action",
    "startup_walkthrough_drop_dll_folder_action",
    "update_splash_manifest_walkthrough",
    "update_splash_feature_highlights_walkthrough",
    "import_splash_replay_walkthrough",
    "startup_walkthrough_stop_badge",
    "update_splash_stop_badge",
    "startup_walkthrough_release_proof",
    "startup_walkthrough_release_proof_summary",
    "apple_style_visual_polish_walkthrough",
    "public_website_installer_hash_match"
)

$manualEvidenceDescriptions = @{
    clean_install_public_installer = "Clean install from the public website installer."
    installed_end_to_end_automated_checks = "Installed end-to-end automated checks against the built installer and installed runtime."
    human_spoken_core_walkthrough = "Human-spoken core walkthrough covering wake, identity, overlay/readout, command capture, and visible action."
    failure_state_walkthrough = "Failure-state walkthrough covering wrong identity, timeout, cancel/reset, microphone/runtime failures, and safe recovery."
    clean_windows_user_or_vm_test = "Clean Windows user or VM test proving the public installer works without developer state."
    microphone_setup_voice_enrollment = "Microphone setup and live voice enrollment."
    identity_failure_timeout_cancel_reset = "Wrong identity, stale identity, timeout, cancel, and reset flows."
    show_numbers_and_mouse_grid_common_apps = "Show numbers and mouse grid on common Windows apps."
    dictation_notepad_review_correction_formatting = "Dictation into Notepad with review, correction, formatting, and safe paste behavior."
    browser_edge_or_chrome_navigation = "Browser navigation in Edge or Chrome, including page find, tabs, private window, print, save page, and scrolling."
    app_switching_window_management_snap_layouts_settings = "App switching, Task View, virtual desktop navigation, window management, Snap Layouts, and safe settings surfaces."
    keyboard_mouse_media_file_search = "Keyboard, mouse, media, and Explorer-backed file-search walkthroughs."
    help_command_discovery_palette = "Help and command discovery through the visible command palette, including pack-management shortcuts and browser overlay quick filters such as browser show numbers, browser show grid, browser hide overlays, and drop dll folder."
    voice_shortcuts_create_manage_execute = "Local voice shortcuts create/manage/execute walkthrough."
    community_extension_import_manage = "Community extension import through the visible Packs UI, including import pack, import folder, drop dll folder, disabled-by-default enablement, disable, rollback, and reimport walkthrough."
    startup_walkthrough_browser_overlay_helpers = "Startup walkthrough browser overlay helper discovery, including Voice Help, Visible Controls, Show Numbers, Show Grid, Show Keyboard, Browser Show Numbers, Browser Show Grid, and Browser Hide Overlays."
    update_check_state_survives_restart = "Update check state survives a restart."
    update_downloaded_installer_path_survives_restart = "Downloaded installer path survives a restart."
    update_version_match_ignores_installer_hash = "Update version-match check proving a manifest with the installed version does not force an update only because the installer hash differs."
    read_updates_status_voice_command = "Use Read Updates Status to read the current Updates-tab status, release proof, and restart proof aloud."
    read_release_proof_voice_command = "Use Read Release Proof to read the installer hash comparison and public download target aloud."
    read_voice_mode_status_voice_command = "Use Read Voice Mode to read the current voice mode aloud."
    read_check_in_status_voice_command = "Use Read Check-In Status to read the current last phone-home check-in aloud."
    read_visual_status_voice_command = "Use Read Visual Status to read the macOS Voice Control visual target, shared evidence tokens, STOP affordance, browser-helper discovery, and Free open-core boundary aloud."
    free_parity_paid_gating_walkthrough = "Free parity and paid gating walkthrough proving Windows Voice Access parity commands stay in the Free open core while paid Pro or Advanced pack commands remain listed for discovery only and will not route without entitlement."
    read_restart_proof_voice_command = "Use Read Restart Proof to read the current restart-proof line and installer-download state aloud."
    open_release_evidence_folder_action = "Open the local release evidence folder from the Updates tab or the startup walkthrough's direct Open Release Evidence button."
    open_manual_evidence_template_action = "Open the manual evidence template from the Updates tab or the startup walkthrough's direct Open Manual Evidence button."
    startup_walkthrough_open_manual_evidence_checklist_action = "Open the startup walkthrough and confirm the manual evidence checklist route stays visible beside the release-proof reminder."
    startup_walkthrough_open_release_evidence_action = "Startup walkthrough direct Open Release Evidence button for the release evidence folder."
    startup_walkthrough_drop_dll_folder_action = "Startup walkthrough direct Drop DLL Folder button for the folder-based community pack import route."
    update_splash_manifest_walkthrough = "Update splash manifest walkthrough for newly added or changed commands, feature highlights, or extension-pack changes, including the visible repeat-summary action."
    update_splash_feature_highlights_walkthrough = "Update splash feature-only walkthrough for manifests that add feature highlights without command changes."
    import_splash_replay_walkthrough = "Pack import splash walkthrough for imported packs and the visible repeat-import action."
    startup_walkthrough_stop_badge = "Startup walkthrough compact STOP badge proof."
    update_splash_stop_badge = "Update splash compact STOP badge proof."
    startup_walkthrough_release_proof = "Startup walkthrough release-proof step and visible installer/site comparison, plus the Updates-tab release-proof line that compares the local installer SHA-256 against the public download target and the visible packs-import entrypoints."
    startup_walkthrough_release_proof_summary = "Startup walkthrough visible release-proof summary line that shows the installer hash and size comparison alongside the public download target."
    apple_style_visual_polish_walkthrough = "Apple Voice Control-style visual polish walkthrough across Callsign visible surfaces, including the main shell visual target, the startup walkthrough step/current badges, the visible STOP badges and stop/cancel boundary on the wake overlay, visible-controls HUD, mouse grid, command palette, startup walkthrough, update splash, and import splash, the visible repeat-summary action in the update splash, the visible repeat-import action in the import splash, the feature-highlight recap in the update splash, the visible-control discovery jumps for Voice Help, Visible Controls, Show Numbers, Show Grid, and Show Keyboard, and the startup walkthrough packs-import entrypoints."
    public_website_installer_hash_match = "Public website download hash comparison against the local Callsign-Setup.exe, including the visible Updates-tab release-proof line and the startup walkthrough release-proof reminder."
}

$manualEvidenceCommands = @{
    clean_install_public_installer = "Download /downloads/Callsign-Setup.exe from the public website, install on a clean Windows profile, launch Callsign, and record installer URL, hash, size, and install result."
    installed_end_to_end_automated_checks = "Install the current Callsign-Setup.exe, run the installed-runtime AlphaSmoke check, verify UI/service shortcuts, runtime state, logs, wake model/runtime files, and record sanitized command output plus artifact hashes."
    human_spoken_core_walkthrough = "With a real microphone, speak Callsign, verify the overlay/readout appears, speak the enrolled callsign, issue a visible command such as open Notepad, then record transcript/readout, identity result, visible action, and audit/evidence artifacts."
    failure_state_walkthrough = "Run controlled failures for wrong callsign, identity timeout, cancel/reset, microphone disconnect or unavailable device, stopped runtime, and tampered/missing wake model; record that commands do not execute and recovery guidance stays visible."
    clean_windows_user_or_vm_test = "Use a clean Windows user profile or clean Windows VM, download the public website installer, install without repo-local state, complete first-run setup, run wake/identity/app-launch proof, and record Windows build, architecture, microphone, installer hash, and artifact paths."
    microphone_setup_voice_enrollment = "Open Callsign, create/select a profile, record at least three voice samples, calibrate wake/identity if needed, and record enrollment status plus microphone device."
    identity_failure_timeout_cancel_reset = "Run wake -> wrong identity, wake -> timeout, wake -> cancel, and wake -> reset flows; record that no command executes and visible status/readout returns to idle."
    show_numbers_and_mouse_grid_common_apps = "Open Notepad, Settings, and File Explorer; say show numbers/show grid, activate a numbered target, refine/click grid cells, and record visible overlay behavior."
    dictation_notepad_review_correction_formatting = "Dictate into Callsign review, correct text, apply formatting/symbol commands, paste into Notepad only after review, and record sensitive-target blocking evidence."
    browser_edge_or_chrome_navigation = "Run browser open/search/navigation, tabs, find, zoom, print/save-page dialogs, private window, and scroll commands in Edge or Chrome; record visible status."
    app_switching_window_management_snap_layouts_settings = "Run app switching, Task View, virtual desktop, minimize/maximize/restore/close, snap layouts, and safe Settings-page commands; record visible outcomes."
    keyboard_mouse_media_file_search = "Run keyboard, mouse, scroll, media, and Explorer-backed file search/open/reveal workflows; record that actions are visible, bounded, and policy-gated."
    help_command_discovery_palette = "Say what can I say, search and select several built-in and extension commands, inspect risk/examples/details, review pack-management shortcuts such as import pack, import folder, refresh packs, update pack, rollback pack, enable pack, disable pack, remove pack, and open packs folder, then dismiss the command palette by voice."
    voice_shortcuts_create_manage_execute = "Open the Shortcuts surface, create a shortcut with command and wait steps, save it, disable and re-enable it, run it by voice, and record the visible surfaces each step triggers."
    community_extension_import_manage = "Import a community DLL or folder through the Packs UI, including the visible Drop DLL Folder route, confirm it starts disabled, inspect metadata/signature/entitlement status, enable it, disable it, remove it, and reimport it."
    startup_walkthrough_browser_overlay_helpers = "Use the startup walkthrough to review Voice Help, Visible Controls, Show Numbers, Show Grid, Show Keyboard, and browser overlay helper discovery from the same visible onboarding surface."
    update_check_state_survives_restart = "Run an update check against a manifest that returns a new version, restart the update service, confirm the pending manifest and last-known version reload from disk, and record the visible next-due status after restart."
    update_downloaded_installer_path_survives_restart = "Run an update check that downloads an installer, restart the update service, and confirm the downloaded installer path plus the visible restart-proof status survive the restart."
    update_version_match_ignores_installer_hash = "Run an update check where the manifest version matches the installed Callsign version but the installer hash differs, and confirm Callsign still reports the app as up to date instead of forcing a false update."
    read_updates_status_voice_command = "Open Updates, say read updates status, and confirm the visible status, release proof, and restart proof are read aloud locally."
    read_release_proof_voice_command = "Open Updates, say read release proof, and confirm the visible release-proof line and installer hash comparison are read aloud locally."
    read_voice_mode_status_voice_command = "Open Voice, say read voice mode status, and confirm the visible voice-mode selection is read aloud locally."
    read_check_in_status_voice_command = "Open Updates, say read check-in status, and confirm the visible last phone-home check-in is read aloud locally."
    read_visual_status_voice_command = "From the main shell, say read visual status, read visual polish status, read visual contract, read accessibility mode, read high contrast status, read text scale status, or read reduced motion status, and confirm the macOS Voice Control target, accessibility mode with palette mode, bounded text scale, reduced-motion-safe state, contrast/opacity/radius evidence tokens, STOP badge, browser-helper discovery, and Free open-core boundary are read aloud locally."
    free_parity_paid_gating_walkthrough = "Open the command palette, search free parity and confirm only Free Open Core parity commands are shown; search paid or Pro entitlement and confirm paid commands remain visible as discovery metadata with entitlement-required availability and will-not-route details; open Plans and Packs to confirm entitlement only controls paid pack loading and policy still controls execution."
    read_restart_proof_voice_command = "Open Updates, say read restart proof, and confirm the visible restart-proof line and installer-download state are read aloud locally."
    open_release_evidence_folder_action = "Open the local release evidence folder from the Updates tab or the startup walkthrough's direct Open Release Evidence button, and confirm the generated parity evidence artifacts, release packet summary, and manual evidence template are visible."
    open_manual_evidence_template_action = "Open the manual evidence template from the Updates tab or the startup walkthrough's direct Open Manual Evidence button, and confirm the canonical manual-evidence file is visible for the public clean-install proof."
    startup_walkthrough_open_manual_evidence_checklist_action = "Open the startup walkthrough and confirm the Open Checklist route remains visible beside the release-proof reminder."
    startup_walkthrough_open_release_evidence_action = "Open the startup walkthrough, press Open Release Evidence, and confirm the release evidence folder opens directly from the walkthrough while the visible step and release-proof reminders stay on screen."
    startup_walkthrough_drop_dll_folder_action = "Open the startup walkthrough, press Drop DLL Folder, and confirm the folder-based community pack import route opens directly from the walkthrough while the visible step and Packs reminders stay on screen."
    update_splash_manifest_walkthrough = "Load an update manifest with added/changed/removed commands or extension-pack changes, confirm the splash lists those changes, reads the summary, can be replayed with the visible repeat-summary action, can be dismissed by voice, and confirm available updates are downloaded and launched visibly."
    update_splash_feature_highlights_walkthrough = "Load an update manifest with feature highlights but no command delta, confirm the splash still appears, shows the feature count and feature-highlight section, can replay the summary, and does not bypass policy or entitlement."
    import_splash_replay_walkthrough = "Import a community pack, confirm the import splash lists the imported pack and commands, replay the summary with the visible repeat-import action, and confirm the imported pack remains disabled by default."
    startup_walkthrough_stop_badge = "Open the startup walkthrough and confirm the compact STOP badge remains visible beside the other status badges while the first-run flow stays on screen."
    update_splash_stop_badge = "Open the update splash and confirm the compact STOP badge appears beside the update summary while manifest details are visible."
    startup_walkthrough_release_proof = "Open the startup walkthrough, choose the visible release-proof step, confirm it points at the installer/site comparison reminder, confirm the Updates tab also shows the release-proof line with the local installer SHA-256 plus the expected manifest SHA-256 and size, confirm the packs-import entrypoints remain visible, and record the visible step/current badges while the walkthrough stays open."
    startup_walkthrough_release_proof_summary = "Open the startup walkthrough, choose the visible release-proof step, and confirm the dedicated release-proof summary line shows the installer hash and size comparison while the visible step/current badges stay on screen."
    apple_style_visual_polish_walkthrough = "Exercise the wake overlay, visible-controls HUD, mouse grid, keyboard overlay, command palette, correction chooser, update splash, import splash, startup walkthrough, and main shell; record screenshots or video proving compact translucent surfaces, readable 4.5:1-style text contrast, rounded 20-26 px geometry, non-activating overlays, accessible names, visible step/current badges in the startup walkthrough, the shell's macOS Voice Control target, compact STOP badges and stop/cancel boundary copy on the wake overlay, visible-controls HUD, mouse grid, command palette, startup walkthrough, update splash, and import splash, the update splash repeat-summary action, the update splash feature-highlight recap, the import splash repeat-import action, visible-control discovery jumps for Voice Help, Visible Controls, Show Numbers, Show Grid, and Show Keyboard, startup walkthrough packs-import entrypoints, and visible stop/cancel/status affordances."
    public_website_installer_hash_match = "Compare SHA-256 and size of the public website /downloads/Callsign-Setup.exe against the local Callsign-Setup.exe built by release readiness, confirm the manifest SHA-256 and size in the Updates tab release-proof line, and verify the same comparison is visible in the startup walkthrough release-proof reminder."
}

$manualEvidenceExpectedResults = @{
    clean_install_public_installer = "Callsign installs from the public website installer on a clean Windows profile and launches into the visible setup flow."
    installed_end_to_end_automated_checks = "The installed app, user-runtime/service, shortcuts, logs, runtime snapshot, wake assets, and bundled helpers pass the installed-runtime smoke checks from the installed location."
    human_spoken_core_walkthrough = "A human-spoken Callsign session reaches wake, identity, command capture, visible execution, and audit/readout proof without transcript-only wake or hidden action."
    failure_state_walkthrough = "Failure paths block execution, preserve visible status/recovery guidance, and return safely to idle or repair flow."
    clean_windows_user_or_vm_test = "A clean Windows user or VM can install from the public website installer and complete the core wake, identity, overlay, and visible app-launch flow without developer-only state."
    microphone_setup_voice_enrollment = "The selected profile records fresh voice samples, reports enrollment status, and shows the microphone device used for wake and identity verification."
    identity_failure_timeout_cancel_reset = "Wrong identity, timeout, cancel, and reset leave no command executed and return the visible session/readout to idle."
    show_numbers_and_mouse_grid_common_apps = "Numbered controls and mouse grid remain visible, accessible, and usable across common Windows apps without hidden actions."
    dictation_notepad_review_correction_formatting = "Dictated text is reviewed, corrected, formatted, and pasted visibly, with sensitive targets blocked before paste."
    browser_edge_or_chrome_navigation = "Browser commands update visible Edge or Chrome surfaces for navigation, tabs, find, zoom, print/save dialogs, private windows, and scrolling."
    app_switching_window_management_snap_layouts_settings = "App switching, Task View, virtual desktop, window management, Snap Layouts, and settings commands produce visible reversible Windows actions."
    keyboard_mouse_media_file_search = "Keyboard, mouse, media, and file-search commands remain visible, bounded, policy-gated, and Explorer-backed for open/reveal flows."
    help_command_discovery_palette = "The command palette shows searchable built-in and extension commands with risk, examples, availability, approval, pack-management shortcuts, browser overlay quick filters, and voice dismissal."
    voice_shortcuts_create_manage_execute = "Local voice shortcuts save, execute visible command/wait sequences, remain enableable/disableable, and do not bypass policy or audit."
    community_extension_import_manage = "Community packs import disabled by default, expose metadata/gates, can be enabled, disabled, removed, reimported, and dropped through a folder route without bypassing policy."
    startup_walkthrough_browser_overlay_helpers = "The startup walkthrough keeps browser overlay helper discovery visible from the onboarding surface, including Voice Help, Visible Controls, Show Numbers, Show Grid, and Show Keyboard routes."
    update_check_state_survives_restart = "The update service reloads the last known manifest, pending update state, and next-due timing after a restart."
    update_downloaded_installer_path_survives_restart = "The downloaded installer path remains available after restart and is reflected in the visible Updates tab."
    update_version_match_ignores_installer_hash = "A matching manifest version is treated as up to date even when the installer package hash differs."
    read_updates_status_voice_command = "The Updates tab status, release proof, and restart proof can be read aloud locally by voice without changing installed commands."
    read_release_proof_voice_command = "The Updates tab release-proof line and installer hash comparison can be read aloud locally by voice without changing installed commands."
    read_voice_mode_status_voice_command = "The Voice tab voice-mode selection can be read aloud locally by voice without changing the current mode."
    read_check_in_status_voice_command = "The Updates tab check-in state can be read aloud locally by voice without changing installed commands."
    read_visual_status_voice_command = "The shell visual contract can be read aloud locally by voice, including the macOS Voice Control target, shared evidence tokens, STOP affordance, browser-helper discovery, and Free open-core boundary."
    free_parity_paid_gating_walkthrough = "The Free Parity view contains Free Open Core Voice Access parity commands, paid Pro or Advanced commands are excluded from that view, paid command discovery visibly says entitlement required and will not route, and Plans/Packs show that policy and visibility gates still apply after entitlement."
    read_restart_proof_voice_command = "The Updates tab restart-proof line and installer-download state can be read aloud locally by voice without changing installed commands."
    open_release_evidence_folder_action = "The Updates tab or startup walkthrough opens the local release evidence folder so the generated parity evidence artifacts stay visible."
    open_manual_evidence_template_action = "The Updates tab or startup walkthrough opens the manual evidence template so the public clean-install proof stays visible."
    startup_walkthrough_open_manual_evidence_checklist_action = "The startup walkthrough keeps an Open Checklist route visible so the manual evidence checklist stays reachable from the release-proof step."
    startup_walkthrough_open_release_evidence_action = "The startup walkthrough's direct Open Release Evidence button opens the release evidence folder without hiding the visible release-proof step."
    startup_walkthrough_drop_dll_folder_action = "The startup walkthrough's direct Drop DLL Folder button opens the folder-based community extension import route without hiding the visible Packs step."
    update_splash_manifest_walkthrough = "The update splash reads manifest changes, lists added/changed/removed commands or packs, can replay the summary with the visible repeat action, dismisses by voice, and confirms available updates are downloaded and launched visibly."
    update_splash_feature_highlights_walkthrough = "The update splash still appears for feature-only manifests, lists the feature highlights, can replay the summary with the visible repeat action, and keeps policy and entitlement gates visible."
    import_splash_replay_walkthrough = "The import splash reads imported pack changes, can replay the summary with the visible repeat-import action, and keeps imported packs disabled by default."
    startup_walkthrough_release_proof = "The startup walkthrough visibly reaches the release-proof step, keeps the installer/site comparison reminder on screen, shows the expected manifest SHA-256 and size in the Updates tab release-proof line, and exposes the packs-import entrypoints."
    startup_walkthrough_release_proof_summary = "The startup walkthrough release-proof summary line visibly shows the local installer hash and size comparison alongside the public download target."
    apple_style_visual_polish_walkthrough = "All core visible surfaces demonstrate compact, high-contrast, translucent, non-activating, accessible, visible-status design consistent with the CallsignVisualStyle contract, including the main shell visual target, the startup walkthrough step/current badges, compact STOP badges and stop/cancel boundary copy on the wake overlay, visible-controls HUD, mouse grid, command palette, startup walkthrough, update splash, and import splash, the update splash repeat-summary control, the import splash repeat-import control, the startup walkthrough packs-import entrypoints, and the direct visible-control discovery jumps."
    startup_walkthrough_stop_badge = "The startup walkthrough keeps a compact visible STOP badge on screen so the stop state stays obvious during the first-run flow."
    update_splash_stop_badge = "The update splash keeps a compact visible STOP badge on screen so the stop state stays obvious while reviewing manifest changes."
    public_website_installer_hash_match = "The public website installer download URL, SHA-256, and size match the local release installer, and the Updates tab release-proof line shows the expected manifest SHA-256 and size."
}

$manualEvidenceCategories = @{
    clean_install_public_installer = @("App launch", "Voice access controls")
    installed_end_to_end_automated_checks = @("App launch", "Voice access controls", "Update splash")
    human_spoken_core_walkthrough = @("App launch", "Voice access controls", "Help and command discovery")
    failure_state_walkthrough = @("Voice access controls", "Safe system/settings control")
    clean_windows_user_or_vm_test = @("App launch", "Voice access controls", "Help and command discovery", "Update splash")
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
    startup_walkthrough_browser_overlay_helpers = @("Help and command discovery")
    update_check_state_survives_restart = @("Update splash")
    update_downloaded_installer_path_survives_restart = @("Update splash")
    update_version_match_ignores_installer_hash = @("Update splash")
    read_updates_status_voice_command = @("Voice access controls", "Update splash")
    read_release_proof_voice_command = @("Voice access controls", "Update splash")
    read_voice_mode_status_voice_command = @("Voice access controls")
    read_check_in_status_voice_command = @("Voice access controls", "Update splash")
    read_visual_status_voice_command = @("Voice access controls", "Help and command discovery")
    free_parity_paid_gating_walkthrough = @("Help and command discovery", "Extension packs")
    read_restart_proof_voice_command = @("Voice access controls", "Update splash")
    open_release_evidence_folder_action = @("Update splash")
    open_manual_evidence_template_action = @("Update splash")
    startup_walkthrough_open_manual_evidence_checklist_action = @("Help and command discovery", "Update splash")
    startup_walkthrough_open_release_evidence_action = @("Help and command discovery", "Update splash")
    startup_walkthrough_drop_dll_folder_action = @("Help and command discovery", "Extension packs")
    update_splash_manifest_walkthrough = @("Update splash")
    update_splash_feature_highlights_walkthrough = @("Update splash")
    import_splash_replay_walkthrough = @("Extension packs", "Update splash")
    startup_walkthrough_release_proof = @("Help and command discovery", "Update splash")
    startup_walkthrough_release_proof_summary = @("Help and command discovery", "Update splash")
    apple_style_visual_polish_walkthrough = @("Voice access controls", "Visible control numbers", "Mouse grid", "Dictation review", "Correction alternatives", "Help and command discovery", "Update splash")
    startup_walkthrough_stop_badge = @("Voice access controls", "Update splash")
    update_splash_stop_badge = @("Voice access controls", "Update splash")
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
        if ($uri.Scheme -ne "https" -and $uri.Scheme -ne "http") {
            return $false
        }

        $artifactExtension = [System.IO.Path]::GetExtension($uri.AbsolutePath)
        $allowedRemoteArtifactExtensions = @(
            ".csv",
            ".gif",
            ".jpeg",
            ".jpg",
            ".json",
            ".jsonl",
            ".log",
            ".md",
            ".mov",
            ".mp4",
            ".pdf",
            ".png",
            ".txt",
            ".webm",
            ".webp",
            ".zip"
        )

        return $allowedRemoteArtifactExtensions -contains $artifactExtension.ToLowerInvariant()
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
        $insideWorkspace = -not $relativePath.StartsWith("..", [System.StringComparison]::Ordinal) -and -not [System.IO.Path]::IsPathRooted($relativePath)
        return $insideWorkspace -and (Test-Path -LiteralPath $candidatePath -PathType Leaf)
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

function Test-ManualPrivacyReview {
    param([object]$Object)

    $privacyReview = Get-PropertyValue $Object "privacy_review"
    if ($null -eq $privacyReview) {
        return $false
    }

    foreach ($field in @("no_raw_audio", "no_personal_transcript", "paths_usernames_redacted", "no_tokens_or_secrets", "screenshots_reviewed")) {
        if ((Get-PropertyValue $privacyReview $field) -ne $true) {
            return $false
        }
    }

    return $true
}

function Test-ManualAccessibilityVisualAudit {
    param([object]$Object)

    $audit = Get-PropertyValue $Object "accessibility_visual_audit"
    if ($null -eq $audit) {
        return $false
    }

    foreach ($field in @(
        "keyboard_only",
        "logical_tab_order",
        "visible_focus",
        "screen_reader_labels",
        "no_color_only_state",
        "high_contrast",
        "text_scaling_200",
        "reduced_motion",
        "multi_monitor_dpi",
        "no_audio_fallback",
        "visible_stop_cancel",
        "does_not_steal_focus")) {
        if ((Get-PropertyValue $audit $field) -ne $true) {
            return $false
        }
    }

    return $true
}

function Test-ManualEvidenceHeaderArtifactHashes {
    param(
        [object]$ManualEvidenceHeader,
        [string]$ExpectedInstallerHash
    )

    $artifactHashes = Get-PropertyValue $ManualEvidenceHeader "artifact_hashes"
    if ($null -eq $artifactHashes) {
        return $false
    }

    $hasAnyHash = $false
    foreach ($artifactHash in @($artifactHashes)) {
        $artifactHashText = [string]$artifactHash
        if ([string]::IsNullOrWhiteSpace($artifactHashText)) {
            continue
        }

        $hasAnyHash = $true
        $expectedHashProvided = -not [string]::IsNullOrWhiteSpace($ExpectedInstallerHash)
        $hashMatchesCurrentInstaller = $expectedHashProvided -and $artifactHashText.IndexOf($ExpectedInstallerHash, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
        if ($hashMatchesCurrentInstaller) {
            return $true
        }
    }

    return $hasAnyHash -and [string]::IsNullOrWhiteSpace($ExpectedInstallerHash)
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
    if (-not [DateTimeOffset]::TryParse($Value, [ref]$parsed)) {
        return $false
    }

    return $parsed.Offset -eq [TimeSpan]::Zero
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

function Find-LatestWakeSamplePath {
    $profileRoot = Join-Path $env:LOCALAPPDATA "Callsign\Profiles"
    if (-not (Test-Path -LiteralPath $profileRoot)) {
        return $null
    }

    $sample = Get-ChildItem -LiteralPath $profileRoot -Recurse -File |
        Where-Object {
            $_.Extension -ieq ".wav" -and (
                $_.Name -ieq "latest-wake.wav" -or
                $_.Name -like "wake-*.wav"
            )
        } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $sample) {
        return $null
    }

    return $sample.FullName
}

function Invoke-WakeSampleProof {
    param(
        [string]$HelperPath,
        [string]$SamplePath,
        [string]$ModelPath,
        [string]$PythonPath
    )

    if ([string]::IsNullOrWhiteSpace($HelperPath) -or -not (Test-Path -LiteralPath $HelperPath)) {
        return $null
    }

    if ([string]::IsNullOrWhiteSpace($SamplePath) -or -not (Test-Path -LiteralPath $SamplePath)) {
        return $null
    }

    if ([string]::IsNullOrWhiteSpace($ModelPath) -or -not (Test-Path -LiteralPath $ModelPath)) {
        return $null
    }

    if ([string]::IsNullOrWhiteSpace($PythonPath) -or -not (Test-Path -LiteralPath $PythonPath)) {
        return $null
    }

    $output = @()
    $exitCode = 0
    try {
        $output = & $HelperPath -WavPath $SamplePath -ModelPath $ModelPath -PythonPath $PythonPath *>&1
        $exitCode = $LASTEXITCODE
        if ($null -eq $exitCode) {
            $exitCode = 0
        }
    }
    catch {
        $output += $_
        $exitCode = 1
    }

    $text = ($output | ForEach-Object { [string]$_ }) -join "`n"

    $score = $null
    $effectiveThreshold = $null
    $margin = $null
    $detected = $null
    $label = $null

    if ($text -match 'openWakeWord score:\s*([0-9.]+)') { $score = [double]$Matches[1] }
    if ($text -match 'Effective threshold:\s*([0-9.]+)') { $effectiveThreshold = [double]$Matches[1] }
    if ($text -match 'Margin:\s*([0-9.\-]+)') { $margin = [double]$Matches[1] }
    if ($text -match 'Detected:\s*(True|False)') { $detected = $Matches[1] -eq 'True' }
    if ($text -match 'Label:\s*(.+)') { $label = $Matches[1].Trim() }

    return [pscustomobject]@{
        supplied = $true
        passed = $exitCode -eq 0
        sample_path = $SamplePath
        model_path = $ModelPath
        python_path = $PythonPath
        score = $score
        effective_threshold = $effectiveThreshold
        margin = $margin
        detected = $detected
        label = $label
        output = @($output | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    }
}

function New-ManualEvidenceTemplate {
    param(
        [string]$InstallerHash,
        [Int64]$InstallerSizeBytes,
        [string]$WebsiteDownloadUrl = "",
        [string]$WebsiteInstallerHash = "",
        [Int64]$WebsiteInstallerSizeBytes = 0
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
            privacy_review = [pscustomobject]@{
                no_raw_audio = $false
                no_personal_transcript = $false
                paths_usernames_redacted = $false
                no_tokens_or_secrets = $false
                screenshots_reviewed = $false
            }
            accessibility_visual_audit = [pscustomobject]@{
                keyboard_only = $false
                logical_tab_order = $false
                visible_focus = $false
                screen_reader_labels = $false
                no_color_only_state = $false
                high_contrast = $false
                text_scaling_200 = $false
                reduced_motion = $false
                multi_monitor_dpi = $false
                no_audio_fallback = $false
                visible_stop_cancel = $false
                does_not_steal_focus = $false
            }
            notes = ""
        }
    }

    $testMachine = if ([string]::IsNullOrWhiteSpace($env:COMPUTERNAME)) { "" } else { $env:COMPUTERNAME }
    $windowsVersion = ""
    try {
        $os = Get-CimInstance -ClassName Win32_OperatingSystem -ErrorAction Stop
        if ($null -ne $os) {
            $windowsVersion = if ([string]::IsNullOrWhiteSpace($os.Caption)) {
                [string]$os.Version
            }
            else {
                "$($os.Caption) $($os.Version)"
            }
        }
    }
    catch {
        $windowsVersion = ""
    }

    $callsignVersion = ""
    if (Test-Path -LiteralPath $installerPath) {
        try {
            $installerVersionInfo = (Get-Item -LiteralPath $installerPath).VersionInfo
            if ($null -ne $installerVersionInfo) {
                $callsignVersion = if ([string]::IsNullOrWhiteSpace($installerVersionInfo.ProductVersion)) {
                    [string]$installerVersionInfo.FileVersion
                }
                else {
                    [string]$installerVersionInfo.ProductVersion
                }
            }
        }
        catch {
            $callsignVersion = ""
        }
    }

    return [pscustomobject]@{
        schema = "callsign.voice_access_parity.manual_evidence.v1"
        generated_utc = [DateTime]::UtcNow.ToString("o")
        local_installer_sha256 = $InstallerHash
        local_installer_size_bytes = $InstallerSizeBytes
        website_installer_sha256 = $WebsiteInstallerHash
        website_installer_size_bytes = $WebsiteInstallerSizeBytes
        website_download_url = $WebsiteDownloadUrl
        website_download_url_was_inferred = [bool]$WebsiteDownloadUrlWasInferred
        evidence_header = [pscustomobject]@{
            commit = ""
            build_id = ""
            artifact_hashes = @()
            tested_utc = ""
            tester = ""
            windows_version_edition_build = $windowsVersion
            architecture = $env:PROCESSOR_ARCHITECTURE
            machine_or_vm = $testMachine
            microphone = ""
            install_mode = ""
            ui_runtime_versions = $callsignVersion
            wake_identity_transcription_models = ""
        }
        release_proof = [pscustomobject]@{
            local_installer_path = $installerPath
            local_installer_sha256 = $InstallerHash
            local_installer_size_bytes = $InstallerSizeBytes
            website_download_url = $WebsiteDownloadUrl
            website_download_url_was_inferred = [bool]$WebsiteDownloadUrlWasInferred
            website_installer_sha256 = $WebsiteInstallerHash
            website_installer_size_bytes = $WebsiteInstallerSizeBytes
            comparison_summary = if ([string]::IsNullOrWhiteSpace($WebsiteDownloadUrl)) {
                "Release proof is waiting for a website download URL."
            }
            else {
                "Compare the local Callsign-Setup.exe installer to $WebsiteDownloadUrl and confirm the website installer SHA-256 and size match."
            }
            update_readback_summary = "Read Updates Status, Read Check-In Status, Read Visual Status, and Read Restart Proof keep the Updates and visual-contract evidence visible."
        }
        test_machine = $testMachine
        windows_version = $windowsVersion
        callsign_version = $callsignVersion
        checks = $templateChecks
    }
}

function New-ManualEvidenceChecklist {
    param(
        [string]$TemplatePath
    )

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Callsign Manual Evidence Checklist")
    $lines.Add("")
    $lines.Add("Template: $TemplatePath")
    $lines.Add("")
    $lines.Add("Use this checklist with the canonical JSON template while capturing live release evidence.")
    $lines.Add("Account and Help tabs also expose direct Open Manual Evidence, Open Checklist, and Open Release Evidence routes.")
    $lines.Add("")
    $lines.Add("## Evidence header")
    $lines.Add("")
    $lines.Add("Fill this header before running the walkthrough so the proof can stand on its own.")
    $lines.Add("")
    $lines.Add("- Commit:")
    $lines.Add("- Build ID:")
    $lines.Add("- Artifact hashes:")
    $lines.Add("- Date/time:")
    $lines.Add("- Tester:")
    $lines.Add("- Windows version/edition/build:")
    $lines.Add("- Architecture:")
    $lines.Add("- Machine/VM:")
    $lines.Add("- Microphone:")
    $lines.Add("- Install mode:")
    $lines.Add("- UI/runtime versions:")
    $lines.Add("- Wake/identity/transcription models:")
    $lines.Add("")
    foreach ($manualCheckId in $requiredManualEvidenceChecks) {
        $lines.Add("## $manualCheckId")
        $lines.Add("")
        $lines.Add("- Description: $($manualEvidenceDescriptions[$manualCheckId])")
        $lines.Add("- Prompt: $($manualEvidenceCommands[$manualCheckId])")
        $lines.Add("- Expected result: $($manualEvidenceExpectedResults[$manualCheckId])")
        $lines.Add("- Categories: $(Join-ManualEvidenceCategories $manualEvidenceCategories[$manualCheckId])")
        $lines.Add("- Result: [ ] Pass  [ ] Fail  [ ] Blocked")
        $lines.Add("- Observed result:")
        $lines.Add("- Evidence paths:")
        $lines.Add("- Sensitive-data review: [ ] no raw audio  [ ] no personal transcript  [ ] paths/usernames redacted  [ ] no tokens or secrets  [ ] screenshots reviewed")
        if ($manualCheckId -eq "apple_style_visual_polish_walkthrough") {
            $lines.Add("- Accessibility/visual audit: [ ] keyboard-only  [ ] logical tab order  [ ] visible focus  [ ] screen-reader labels  [ ] no color-only state  [ ] high contrast  [ ] 200% text scaling  [ ] reduced motion  [ ] multi-monitor/DPI  [ ] no-audio fallback  [ ] visible stop/cancel  [ ] does not steal focus")
        }
        $lines.Add("- Remaining uncertainty:")
        $lines.Add("- Release recommendation:")
        $lines.Add("")
    }

    return ($lines -join [Environment]::NewLine)
}

function ConvertTo-ManualProofNoteFileName {
    param([string]$Value)

    $normalized = ([regex]::Replace($Value.Trim().ToLowerInvariant(), '[^a-z0-9]+', '-')).Trim('-')
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return "manual-proof"
    }

    if ($normalized.Length -le 80) {
        return $normalized
    }

    return $normalized.Substring(0, 80).Trim('-')
}

function New-ManualProofNoteMarkdown {
    param([string]$ManualCheckId)

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Callsign Manual Proof Note")
    $lines.Add("")
    $lines.Add("- Check: $ManualCheckId")
    $lines.Add("- Created UTC: $([DateTimeOffset]::UtcNow.ToString('O'))")
    $lines.Add("- Status: pending")
    $lines.Add("- Description: $($manualEvidenceDescriptions[$ManualCheckId])")
    $lines.Add("- Categories: $(Join-ManualEvidenceCategories $manualEvidenceCategories[$ManualCheckId])")
    $lines.Add("")
    $lines.Add("## Evidence Command")
    $lines.Add("")
    $lines.Add([string]$manualEvidenceCommands[$ManualCheckId])
    $lines.Add("")
    $lines.Add("## Expected Result")
    $lines.Add("")
    $lines.Add([string]$manualEvidenceExpectedResults[$ManualCheckId])
    $lines.Add("")
    $lines.Add("## Observed Result")
    $lines.Add("")
    $lines.Add("- ")
    $lines.Add("")
    $lines.Add("## Artifact References")
    $lines.Add("")
    $lines.Add("- ")
    $lines.Add("")
    $lines.Add("## Privacy Review")
    $lines.Add("")
    $lines.Add("- [ ] No raw audio attached unless explicitly intended.")
    $lines.Add("- [ ] No personal transcript beyond the minimum proof summary.")
    $lines.Add("- [ ] Paths and usernames are redacted where practical.")
    $lines.Add("- [ ] No tokens, passwords, keys, or secrets are present.")
    $lines.Add("- [ ] Screenshots or videos were reviewed before attaching.")
    if ($ManualCheckId -eq "apple_style_visual_polish_walkthrough") {
        $lines.Add("")
        $lines.Add("## Accessibility / Visual Audit")
        $lines.Add("")
        $lines.Add("- [ ] Keyboard-only operation")
        $lines.Add("- [ ] Logical tab order")
        $lines.Add("- [ ] Visible focus")
        $lines.Add("- [ ] Screen-reader labels")
        $lines.Add("- [ ] No color-only state")
        $lines.Add("- [ ] High contrast")
        $lines.Add("- [ ] 200% text scaling")
        $lines.Add("- [ ] Reduced motion")
        $lines.Add("- [ ] Multi-monitor/DPI behavior")
        $lines.Add("- [ ] No-audio fallback")
        $lines.Add("- [ ] Visible stop/cancel")
        $lines.Add("- [ ] Does not steal focus")
    }
    $lines.Add("")
    $lines.Add("## Remaining Uncertainty")
    $lines.Add("")
    $lines.Add("- ")
    $lines.Add("")
    $lines.Add("## Release Recommendation")
    $lines.Add("")
    $lines.Add("- [ ] pass")
    $lines.Add("- [ ] fail")
    $lines.Add("- [ ] blocked")
    $lines.Add("")

    return ($lines -join [Environment]::NewLine)
}

function Write-ManualProofNotes {
    param([string]$OutputFolder)

    New-Item -ItemType Directory -Force -Path $OutputFolder | Out-Null
    $written = New-Object System.Collections.Generic.List[string]
    foreach ($manualCheckId in $requiredManualEvidenceChecks) {
        $fileName = "$(ConvertTo-ManualProofNoteFileName $manualCheckId).md"
        $notePath = Join-Path $OutputFolder $fileName
        if (-not (Test-Path -LiteralPath $notePath)) {
            New-ManualProofNoteMarkdown $manualCheckId | Set-Content -LiteralPath $notePath -Encoding UTF8
        }

        [void]$written.Add($notePath)
    }

    return @($written)
}

$checks = New-Object System.Collections.Generic.List[object]
$matrix = Read-Text $matrixPath
$testPlan = Read-Text $testPlanPath
$generatedParityPage = Read-Text $generatedParityPagePath
$generatedVoiceUxPage = Read-Text $generatedVoiceUxPagePath
$generatedTierPage = Read-Text $generatedTierPagePath
$generatedSecurityPage = Read-Text $generatedSecurityPagePath
$mainFormSource = Read-Text $mainFormPath
$visualStyleSource = Read-Text $visualStylePath
$mouseGridOverlaySource = Read-Text $mouseGridOverlayPath
$visibleControlsOverlaySource = Read-Text $visibleControlsOverlayPath
$alphaCommandRouterSource = Read-Text $alphaCommandRouterPath
$alphaSessionStateMachineSource = Read-Text $alphaSessionStateMachinePath
$dictationTargetSafetySource = Read-Text $dictationTargetSafetyPath
$runtimeStatusFormatterSource = Read-Text $runtimeStatusFormatterPath
$browserLaunchServiceSource = Read-Text $browserLaunchServicePath
$updateCheckServiceSource = Read-Text $updateCheckServicePath
$startMenuLauncherSource = Read-Text $startMenuLauncherPath
$serviceWorkerSource = Read-Text $serviceWorkerPath
$commandDiscoverySource = Read-Text $commandDiscoveryPath
$startupWalkthroughSource = Read-Text $startupWalkthroughPath
$alphaSmokeProgramSource = Read-Text $alphaSmokeProgramPath
$verifyReleaseReadinessSource = Read-Text $verifyReleaseReadinessPath
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

$wakeSamplePath = Find-LatestWakeSamplePath
$wakeSampleProof = Invoke-WakeSampleProof -HelperPath $wakeHelperPath -SamplePath $wakeSamplePath -ModelPath $wakeModelPath -PythonPath $wakePythonPath
if ($null -ne $wakeSampleProof) {
    $checks.Add((New-Check "Wake sample proof available" $true $wakeSampleProof.sample_path))
    $checks.Add((New-Check "Wake sample proof detected Callsign" ($wakeSampleProof.passed -and $wakeSampleProof.detected -eq $true) $(if ($null -ne $wakeSampleProof.score) { "score $($wakeSampleProof.score)" } else { $wakeSampleProof.sample_path })))
    $checks.Add((New-Check "Wake sample proof reports margin" ($wakeSampleProof.passed -and $null -ne $wakeSampleProof.margin) $(if ($null -ne $wakeSampleProof.margin) { "margin $($wakeSampleProof.margin)" } else { $wakeSampleProof.sample_path })))
}

if ($WriteManualEvidenceTemplate) {
    if ([string]::IsNullOrWhiteSpace($ManualEvidenceTemplatePath)) {
        $ManualEvidenceTemplatePath = Join-Path $repoRoot "build\voice-access-parity-manual-evidence.template.json"
    }

    $templateDir = Split-Path -Parent $ManualEvidenceTemplatePath
    if (-not [string]::IsNullOrWhiteSpace($templateDir)) {
        New-Item -ItemType Directory -Force -Path $templateDir | Out-Null
    }

    New-ManualEvidenceTemplate $installerHash $installerSizeBytes $WebsiteDownloadUrl $WebsiteInstallerHash $WebsiteInstallerSizeBytes | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $ManualEvidenceTemplatePath -Encoding UTF8
    $manualEvidenceChecklistPath = Join-Path $templateDir "voice-access-parity-manual-evidence.checklist.md"
    New-ManualEvidenceChecklist $ManualEvidenceTemplatePath | Set-Content -LiteralPath $manualEvidenceChecklistPath -Encoding UTF8
    $manualProofNotesFolder = Join-Path $templateDir "manual-proof-notes"
    $manualProofNotes = @(Write-ManualProofNotes $manualProofNotesFolder)
    Write-Host "Manual evidence template written: $ManualEvidenceTemplatePath" -ForegroundColor Cyan
    Write-Host "Manual evidence checklist written: $manualEvidenceChecklistPath" -ForegroundColor Cyan
    Write-Host "Manual proof notes written: $manualProofNotesFolder ($($manualProofNotes.Count) files)" -ForegroundColor Cyan
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
$checks.Add((New-Check "Generated parity page includes service-runtime audit contract" ($generatedParityPage.IndexOf("alpha.service_command_execution", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("service_runtime", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedParityPagePath))
$checks.Add((New-Check "Generated security page includes service-runtime audit contract" ($generatedSecurityPage.IndexOf("alpha.service_command_execution", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("service_runtime", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated parity page includes visible audit-failure warning contract" ($generatedParityPage.IndexOf("Audit warning", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("bounded recovery guidance", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedParityPagePath))
$checks.Add((New-Check "Generated security page includes visible audit-failure warning contract" ($generatedSecurityPage.IndexOf("Audit warning", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("silently suppressed", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated parity page includes voice-control audit contract" ($generatedParityPage.IndexOf("voice-control status", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedParityPagePath))
$checks.Add((New-Check "Generated parity page includes command-level entitlement gating" ($generatedParityPage.IndexOf("command-level entitlement gating", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedParityPagePath))
$checks.Add((New-Check "Generated parity page includes shortcut indirect-loop gate" ($generatedParityPage.IndexOf("indirect shortcut loops", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("rejected", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedParityPagePath))
$checks.Add((New-Check "Generated parity page includes shortcut configured-wake follow-up gate" ($generatedParityPage.IndexOf("configured wake word", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("default", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("Callsign", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedParityPagePath))
$checks.Add((New-Check "Main shell shortcut follow-ups use configured wake word" ($mainFormSource.IndexOf("var wakeWord = string.IsNullOrWhiteSpace(_activeProfile.Settings.WakeWord)", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("ParseVerifiedTranscript(transcript, wakeWord, _activeProfile.Callsign)", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Generated parity page includes service-side shortcut follow-up routing" ($generatedParityPage.IndexOf("background service consumes shortcut follow-up steps", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("keeps the verified session alive", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedParityPagePath))
$checks.Add((New-Check "Service worker routes shortcut follow-ups through verified pipeline" ($serviceWorkerSource.IndexOf("ExecuteServiceVoiceShortcutFollowUpSteps", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("completeSession: false", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("AlphaVoiceIntentParser.ParseVerifiedTranscript(transcript, wakeWord, profile.Callsign)", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $serviceWorkerPath))
$checks.Add((New-Check "Service worker bounds shortcut follow-up depth and loops" ($serviceWorkerSource.IndexOf("const int maxServiceShortcutDepth = 4", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("Service voice shortcut execution detected a loop and was blocked", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $serviceWorkerPath))
$checks.Add((New-Check "Smoke tests cover shortcut configured-wake follow-ups" ($alphaSmokeProgramSource.IndexOf("Voice shortcut follow-up steps should use the active profile wake word", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("must not hard-code the default wake word", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $alphaSmokeProgramPath))
$checks.Add((New-Check "Generated parity page includes counted sentence editing" ($generatedParityPage.IndexOf("move previous two sentences", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("highlight next four sentences", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedParityPagePath))
$checks.Add((New-Check "Generated parity page includes counted paragraph editing" ($generatedParityPage.IndexOf("move previous two paragraphs", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("delete previous three paragraphs", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedParityPagePath))
$checks.Add((New-Check "Generated parity page documents terminal-state authorization cleanup" ($generatedParityPage.IndexOf("terminal states must clear verified identity", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("stale authorization cannot be reused", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedParityPagePath))
$checks.Add((New-Check "Alpha session state machine clears terminal authorization" ($alphaSessionStateMachineSource.IndexOf("ClearAuthorization()", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSessionStateMachineSource.IndexOf("AlphaSessionState.Completed", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSessionStateMachineSource.IndexOf("Session timed out", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $alphaSessionStateMachinePath))
$checks.Add((New-Check "Alpha session state machine limits fresh identity to active states" ($alphaSessionStateMachineSource.IndexOf("WaitingForCommand or AlphaSessionState.ReadyToLaunch or AlphaSessionState.Launching", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $alphaSessionStateMachinePath))
$checks.Add((New-Check "Smoke tests cover terminal authorization cleanup" ($alphaSmokeProgramSource.IndexOf("SessionStateMachineClearsAuthorizationAfterTerminalStates", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Completed sessions must not retain fresh identity authorization", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Timed-out sessions must not retain fresh identity authorization", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $alphaSmokeProgramPath))
$checks.Add((New-Check "Generated parity page documents service-side extension identity handoff" ($generatedParityPage.IndexOf("Service-side extension execution", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("fresh-identity state", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("hard-coding authorization", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedParityPagePath))
$checks.Add((New-Check "Service worker derives extension identity from session" ($serviceWorkerSource.IndexOf("var identityVerified = string.Equals(_session.VerifiedCallsign, profile.Callsign", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("var freshIdentityVerified = _session.HasFreshIdentity(ExtensionCommandIdentityFreshness)", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $serviceWorkerPath))
$checks.Add((New-Check "Service worker passes session-derived identity to registry" ($serviceWorkerSource.IndexOf("identityVerified: identityVerified", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("freshIdentityVerified: freshIdentityVerified", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $serviceWorkerPath))
$checks.Add((New-Check "Smoke tests cover service-side extension identity handoff" ($alphaSmokeProgramSource.IndexOf("Service-side extension commands should derive identity verification from the active session and profile", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Service-side extension execution must not hard-code identity and fresh-identity proof", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $alphaSmokeProgramPath))
$checks.Add((New-Check "Generated parity page documents service-side built-in policy" ($generatedParityPage.IndexOf("background service evaluates built-in browser policy", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("background service evaluates built-in file-search policy", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("background service evaluates built-in dictation policy", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("blocks approval-required system actions", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedParityPagePath))
$checks.Add((New-Check "Generated parity and Voice UX pages document dictation capture bounds" ($generatedParityPage.IndexOf("10-minute capture window", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("12,000 reviewed characters", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("128 service segments", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("10-minute capture window", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("preserves the review buffer", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedParityPagePath))
$checks.Add((New-Check "Dictation paste blocks sensitive and external submission targets" ($dictationTargetSafetySource.IndexOf("ExternalSubmissionTerms", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $dictationTargetSafetySource.IndexOf("ExternalSubmissionProcesses", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $dictationTargetSafetySource.IndexOf("external communication or submission target", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $dictationTargetSafetySource.IndexOf("external communication target", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $dictationTargetSafetySource.IndexOf("credential", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("External message compose titles should block dictation paste", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("External communication process names should block dictation paste", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Teams-style communication process names should block dictation paste", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Upload surfaces should block dictation paste", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("message compose", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("external-target title and process paste-block tests", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $dictationTargetSafetyPath))
$checks.Add((New-Check "Dictation paste failures preserve review buffer" ($mainFormSource.IndexOf("BuildDictationTransferPreservedMessage", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("Review text is preserved in Callsign so you can retry, copy, edit, or discard it", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("paste_transfer_failed_review_preserved", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("paste_request_sent_review_preserved", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Failed dictation transfers should use a shared review-preservation message", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("failed paste or transfer preserves the visible review text", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Update check-in payload builder hashes raw identifiers" ($updateCheckServiceSource.IndexOf("BuildCheckInPayloadSnapshot", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $updateCheckServiceSource.IndexOf("BuildPrivacyPreservingIdentifier(""account"", accountId)", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $updateCheckServiceSource.IndexOf("BuildPrivacyPreservingIdentifier(""device"", deviceId)", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Check-in snapshot must not preserve the raw account/callsign value", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Expected serialized check-in snapshot to avoid the raw device id", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("raw account, callsign, and device identifiers", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("hashed before phone-home check-ins", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $updateCheckServicePath))
$checks.Add((New-Check "Updates evidence status names manual evidence supply state" ($mainFormSource.IndexOf("manual_evidence_supplied", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("manual evidence not supplied", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Expected evidence summary to name missing manual evidence", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("manual evidence was supplied", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Manual evidence JSON includes documentation-pack evidence header" ($alphaSmokeProgramSource.IndexOf("Manual evidence template should include an evidence_header block", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $testPlan.IndexOf("canonical JSON and human-readable checklist both begin", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $PSCommandPath -and (Read-Text $PSCommandPath).IndexOf("wake_identity_transcription_models", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $alphaSmokeProgramPath))
$checks.Add((New-Check "Manual evidence submission requires completed evidence header" ($alphaSmokeProgramSource.IndexOf("Parity evidence script should require a manual evidence header", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("manual evidence timestamps to use UTC offset zero", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $testPlan.IndexOf('Submitted manual evidence must fill that `evidence_header`', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $testPlan.IndexOf('UTC offset zero', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $testPlan.IndexOf('current local `Callsign-Setup.exe` SHA-256', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $PSCommandPath -and (Read-Text $PSCommandPath).IndexOf("Manual evidence header tested_utc is valid", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and (Read-Text $PSCommandPath).IndexOf('parsed.Offset -eq [TimeSpan]::Zero', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and (Read-Text $PSCommandPath).IndexOf("Manual evidence header artifact_hashes include current installer SHA-256", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and (Read-Text $PSCommandPath).IndexOf('Test-ManualEvidenceHeaderArtifactHashes $manualEvidenceHeader $installerHash', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $alphaSmokeProgramPath))
$checks.Add((New-Check "Release readiness validates manual template and checklist contents" ($verifyReleaseReadinessSource.IndexOf("callsign.voice_access_parity.manual_evidence.v1", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $verifyReleaseReadinessSource.IndexOf("at least 40 manual/live checks", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $verifyReleaseReadinessSource.IndexOf("at least 40 check sections", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $verifyReleaseReadinessSource.IndexOf("Generated manual evidence checklist is missing required release-proof fields", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Release readiness script should validate the manual evidence template schema", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Release readiness script should validate the manual evidence checklist section count", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $testPlan.IndexOf("checklist content", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $verifyReleaseReadinessPath))
$checks.Add((New-Check "Manual evidence checklist includes documentation-pack evidence header" ($alphaSmokeProgramSource.IndexOf("Manual evidence checklist should include the documentation-pack evidence header", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $testPlan.IndexOf("documentation-pack evidence header", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $PSCommandPath -and (Read-Text $PSCommandPath).IndexOf("Wake/identity/transcription models:", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $alphaSmokeProgramPath))
$checks.Add((New-Check "Manual evidence checklist includes per-check result fields" ($alphaSmokeProgramSource.IndexOf("Manual evidence checklist should provide per-check pass/fail/blocked fields", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $testPlan.IndexOf("pass/fail/blocked", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $PSCommandPath -and (Read-Text $PSCommandPath).IndexOf("Sensitive-data review: [ ] no raw audio", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $alphaSmokeProgramPath))
$checks.Add((New-Check "Manual evidence script writes per-check proof notes" ($alphaSmokeProgramSource.IndexOf("Manual proof notes should include evidence-command sections", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $testPlan.IndexOf("build/manual-proof-notes/*.md", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $PSCommandPath -and (Read-Text $PSCommandPath).IndexOf("Write-ManualProofNotes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and (Read-Text $PSCommandPath).IndexOf("Manual proof notes written", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $alphaSmokeProgramPath))
$checks.Add((New-Check "Release readiness validates manual proof-note folder" ($verifyReleaseReadinessSource.IndexOf("manual-proof-notes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $verifyReleaseReadinessSource.IndexOf("at least 40 markdown notes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $verifyReleaseReadinessSource.IndexOf("required evidence sections", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $verifyReleaseReadinessSource.IndexOf("## Evidence Command", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $verifyReleaseReadinessSource.IndexOf("## Privacy Review", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $verifyReleaseReadinessSource.IndexOf("## Release Recommendation", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Release readiness script should validate the manual proof-note folder companion", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Release readiness script should validate the required proof-note evidence sections", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $testPlan.IndexOf("build/manual-proof-notes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $verifyReleaseReadinessPath))
$checks.Add((New-Check "Startup walkthrough surfaces accessibility visual audit proof" ($startupWalkthroughSource.IndexOf("accessibility visual audit", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $testPlan.IndexOf("accessibility visual audit", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("accessibility visual audit", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Expected walkthrough voice cue to mention the accessibility visual audit", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $startupWalkthroughPath))
$checks.Add((New-Check "Updates evidence status previews remaining manual proof" ($mainFormSource.IndexOf("TryGetManualEvidenceRemainingPreview", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("remaining proof:", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("TryGetJsonStringArray(manualEvidence, ""remaining"", maxItems: 3)", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Updates evidence status should preview the remaining manual proof items", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("remaining manual proof", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Updates evidence status previews missing manual categories" ($mainFormSource.IndexOf("TryGetManualEvidenceMissingCategoryPreview", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("missing categories:", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("TryGetJsonStringArray(manualEvidence, ""categories_missing"", maxItems: 3)", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Expected evidence summary to preview first three missing categories", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Fourth category should stay hidden", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("first missing manual categories", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Updates tab exposes manual evidence progress strip" ($mainFormSource.IndexOf("Manual evidence progress strip", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("BuildManualEvidenceProgressSummary()", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("UpdatesManualEvidenceStripTexts", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("Proof: none remaining", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("Category: none missing", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Expected manual-evidence strip to include remaining count", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Expected manual-evidence strip to include the first remaining proof item", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Expected manual-evidence strip to include the first missing category", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("Manual evidence progress strip", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("first remaining proof", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Updates tab exposes evidence-header readback" ($alphaCommandRouterSource.IndexOf("UiActionReadEvidenceHeader", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaCommandRouterSource.IndexOf("what header is missing", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("Read Evidence Header", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("BuildEvidenceHeaderReadout()", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("artifact_hashes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $commandDiscoverySource.IndexOf("read evidence header", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Read evidence header should route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("Read Evidence Header", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Updates tab exposes next-proof readback" ($alphaCommandRouterSource.IndexOf("UiActionReadNextProof", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaCommandRouterSource.IndexOf("what proof is next", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("Read Next Proof", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("BuildNextProofReadout()", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $commandDiscoverySource.IndexOf("read next proof", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Read next proof should route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("Read Next Proof", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Updates tab exposes next-proof instructions readback" ($alphaCommandRouterSource.IndexOf("UiActionReadNextProofInstructions", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaCommandRouterSource.IndexOf("how do i prove the next check", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("Read Proof Steps", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("BuildNextProofInstructionsReadout()", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("evidence_command", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("expected_result", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $commandDiscoverySource.IndexOf("read proof steps", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Read proof steps should route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("Read Proof Steps", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Updates tab creates next-proof artifact note" ($alphaCommandRouterSource.IndexOf("UiActionCreateNextProofNote", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaCommandRouterSource.IndexOf("create proof note", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("Create Proof Note", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("CreateNextProofNoteFile()", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("manual-proof-notes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("## Privacy Review", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $commandDiscoverySource.IndexOf("create proof note", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Create proof note should route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("Create Proof Note", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Updates tab prepares all manual proof notes" ($alphaCommandRouterSource.IndexOf("UiActionCreateAllProofNotes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaCommandRouterSource.IndexOf("prepare all manual proof notes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("Create All Notes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("CreateAllProofNoteFiles()", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("updates_create_all_proof_notes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $commandDiscoverySource.IndexOf("create all proof notes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Create all proof notes should route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("Create All Notes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Updates tab creates prefilled manual evidence draft" ($alphaCommandRouterSource.IndexOf("UiActionCreateEvidenceDraft", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaCommandRouterSource.IndexOf("prefill manual evidence", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("Create Evidence Draft", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("CreateManualEvidenceDraftFile()", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("voice-access-parity-manual-evidence.draft.json", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("artifact_hashes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $commandDiscoverySource.IndexOf("create evidence draft", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Create evidence draft should route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("Create Evidence Draft", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Updates tab opens manual evidence draft" ($alphaCommandRouterSource.IndexOf("UiActionOpenEvidenceDraft", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaCommandRouterSource.IndexOf("open manual evidence draft", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("Open Evidence Draft", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("OpenManualEvidenceDraft()", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("TryGetManualEvidenceDraftPath()", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $commandDiscoverySource.IndexOf("open evidence draft", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Open evidence draft should route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("Open Evidence Draft", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Updates tab exposes manual evidence draft readback" ($alphaCommandRouterSource.IndexOf("UiActionReadEvidenceDraft", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaCommandRouterSource.IndexOf("what remains in the evidence draft", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("Read Evidence Draft", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("BuildEvidenceDraftReadout()", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("TryGetManualEvidenceDraftPath()", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $commandDiscoverySource.IndexOf("read evidence draft", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Read evidence draft should route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("Read Evidence Draft", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Updates evidence-draft readback names next unchecked proof" ($mainFormSource.IndexOf("BuildNextEvidenceDraftCheckText", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("Next draft check:", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("evidence_command", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("expected_result", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Updates evidence-draft readback should identify the next unchecked draft proof item", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("next unchecked proof item", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Updates tab exposes proof-notes folder status" ($alphaCommandRouterSource.IndexOf("UiActionOpenProofNotes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaCommandRouterSource.IndexOf("open proof notes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("Proof notes: not checked yet.", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("Open Proof Notes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("BuildProofNotesStatusText()", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("TryGetProofNotesFolder", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $commandDiscoverySource.IndexOf("open proof notes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Open proof notes should route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("Open Proof Notes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Updates tab exposes proof-notes status readback" ($alphaCommandRouterSource.IndexOf("UiActionReadProofNotesStatus", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaCommandRouterSource.IndexOf("what proof notes exist", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("Read Proof Notes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("ReadProofNotesStatusAloud()", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("BuildProofNotesStatusReadout()", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $commandDiscoverySource.IndexOf("read proof notes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Read proof notes should route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("Read Proof Notes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Updates tab exposes release-gates readback" ($alphaCommandRouterSource.IndexOf("UiActionReadReleaseGates", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaCommandRouterSource.IndexOf("what gates remain", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("Release gates strip", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("Read Gates", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("BuildReleaseGatesReadout()", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $commandDiscoverySource.IndexOf("read release gates", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Read release gates should route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("Read Gates", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Updates tab exposes release-blocker readback" ($alphaCommandRouterSource.IndexOf("UiActionReadReleaseBlockers", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaCommandRouterSource.IndexOf("why is release blocked", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("Read Blockers", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("BuildReleaseBlockersReadout()", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $commandDiscoverySource.IndexOf("read release blockers", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Read release blockers should route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("Read Blockers", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Start menu launcher blocks elevated installer and security intents" ($startMenuLauncherSource.IndexOf("UnsafeLaunchPhrases", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $startMenuLauncherSource.IndexOf("run as administrator", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $startMenuLauncherSource.IndexOf("installer", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $startMenuLauncherSource.IndexOf("windows security", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $startMenuLauncherSource.IndexOf("security settings", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Elevated launch phrasing should be rejected", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Security-setting launch phrasing should be rejected", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $startMenuLauncherPath))
$checks.Add((New-Check "Browser launcher blocks credential-bearing targets" ($browserLaunchServiceSource.IndexOf("HasCredentialUserInfo", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $browserLaunchServiceSource.IndexOf("credential-bearing web addresses", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $browserLaunchServiceSource.IndexOf("uri.UserInfo", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Credential-bearing URLs should not be browser targets", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("User-info-shaped email text should not be upgraded into a browser URL", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("credential-bearing web addresses", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $browserLaunchServicePath))
$checks.Add((New-Check "External side-effect phrases route to visible blocked action" ($alphaCommandRouterSource.IndexOf("UiBlockedExternalSideEffectPrefixes", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("blocked_external_side_effect", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("will not submit, send, upload, post, pay, accept terms, or run downloaded software", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("ui-blocked-external-side-effect", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $commandDiscoverySource.IndexOf("blocked external side effect", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Expected UiAction for blocked submit", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedParityPage.IndexOf("External side-effect phrases", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $alphaCommandRouterPath))
$checks.Add((New-Check "Service worker routes built-in parity families through policy" ($serviceWorkerSource.IndexOf("TryExecuteBrowserCommand(intent, profile", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("TryExecuteFileSearchCommand(intent, profile", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("TryExecuteDictationCommand(intent, profile", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("TryExecuteSystemControlCommand(intent, profile", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("TryAuthorizeServiceBuiltInIntent", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $serviceWorkerPath))
$checks.Add((New-Check "Service worker routes broad browser actions before URL fallback" ($serviceWorkerSource.IndexOf("IsBrowserActionTarget(intent.Target)", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("_browserLaunchService.TryExecuteBrowserAction(intent.Target", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf('target.Trim().StartsWith("browser-"', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("browser-private-window", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("browser-find-text:", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("BrowserLaunchServiceCanExecute", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $serviceWorkerPath))
$checks.Add((New-Check "Service worker requests visible surfaces for built-in families" ($serviceWorkerSource.IndexOf("RequestUiMode(""Browser"")", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("RequestUiMode(""Files"")", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("RequestUiMode(""Dictation"")", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("RequestUiMode(""System"")", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $serviceWorkerPath))
$checks.Add((New-Check "Dictation capture bounds are enforced in UI and service paths" ($mainFormSource.IndexOf("StopDictationBecauseBoundReached", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("MaxCaptureSeconds", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("AppendReviewedTextWithBounds", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("MaxServiceDictationSegments", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("MaxReviewCharacters", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("Review text is preserved", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("DictationCaptureBoundsPreserveVisibleReviewText", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Service worker blocks approval-required built-in actions visibly" ($serviceWorkerSource.IndexOf("requires visible approval in the visible Callsign surface before execution", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("CallsignCommandPolicy.Evaluate(definition, identityVerified, freshIdentity)", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $serviceWorkerPath))
$checks.Add((New-Check "Service worker writes service-runtime action audit records" ($serviceWorkerSource.IndexOf("private readonly AlphaAuditLog _auditLog", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("alpha.service_command_execution", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("auditSource: ""service_runtime""", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("verificationMethod: ""visible_status""", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $serviceWorkerPath))
$checks.Add((New-Check "Service worker surfaces audit-write failures visibly" ($serviceWorkerSource.IndexOf("Audit warning:", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("Service audit logging failed; review profile storage and disk permissions before trusting this action history", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("if (string.Equals(_statusMessage, message, StringComparison.Ordinal))", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $serviceWorkerPath))
$checks.Add((New-Check "Service worker passes active profile into service action audit records" ($serviceWorkerSource.IndexOf("RecordServiceAction(""browser"", intent.Target, browserMessage, succeeded: browserSucceeded, profile)", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("RecordServiceAction(""file_search"", intent.Target, fileSearchMessage, succeeded: fileSearchSucceeded, profile)", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("RecordServiceAction(""dictation"", ""service dictation"", dictationMessage, succeeded: dictationSucceeded, profile)", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $serviceWorkerSource.IndexOf("RecordServiceAction(""system"", intent.Target, systemMessage, succeeded: systemSucceeded, profile)", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $serviceWorkerPath))
$checks.Add((New-Check "Smoke tests cover service-side built-in policy routing" ($alphaSmokeProgramSource.IndexOf("Service verified command execution should route browser intents with the active profile", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Service dictation commands should require fresh identity through built-in policy", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Service approval-required system commands should fail closed", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $alphaSmokeProgramPath))
$checks.Add((New-Check "Alpha command router includes bounded counted sentence route" ($alphaCommandRouterSource.IndexOf("TryRouteCountedSentenceAction", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaCommandRouterSource.IndexOf("Math.Min(count, 5)", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $alphaCommandRouterPath))
$checks.Add((New-Check "Alpha command router includes bounded counted paragraph route" ($alphaCommandRouterSource.IndexOf("TryRouteCountedParagraphAction", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaCommandRouterSource.IndexOf("Math.Min(count, 3)", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $alphaCommandRouterPath))
$checks.Add((New-Check "Command discovery includes counted sentence and paragraph examples" ($commandDiscoverySource.IndexOf("move previous two sentences", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $commandDiscoverySource.IndexOf("select next three paragraphs", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $commandDiscoveryPath))
$checks.Add((New-Check "Smoke tests cover counted sentence and paragraph routing" ($alphaSmokeProgramSource.IndexOf("system-repeat:system-select-next-sentence:3", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("system-repeat:system-delete-previous-paragraph:3", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $alphaSmokeProgramPath))
$checks.Add((New-Check "Generated security page includes shortcut indirect-loop gate" ($generatedSecurityPage.IndexOf("indirect shortcut loops", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("rejected", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated Voice UX page includes shared visual contract" ($generatedVoiceUxPage.IndexOf("CallsignVisualStyle", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes main shell visual target" ($generatedVoiceUxPage.IndexOf("main shell visual target", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes repeat summary action" ($generatedVoiceUxPage.IndexOf("Read Summary Again", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes macOS Voice Control target" ($generatedVoiceUxPage.IndexOf("macOS Voice Control", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes compact visual principle" ($generatedVoiceUxPage.IndexOf("compact", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes high-contrast visual principle" ($generatedVoiceUxPage.IndexOf("high-contrast", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes translucent visual principle" ($generatedVoiceUxPage.IndexOf("translucent", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes non-activating visual principle" ($generatedVoiceUxPage.IndexOf("non-activating", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes accessible visual principle" ($generatedVoiceUxPage.IndexOf("accessible", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes high-contrast awareness" ($generatedVoiceUxPage.IndexOf("high-contrast-aware", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("high-contrast readiness", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes text-scaling awareness" ($generatedVoiceUxPage.IndexOf("text-scaling-aware", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("text-scaling readiness", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes reduced-motion awareness" ($generatedVoiceUxPage.IndexOf("reduced-motion-aware", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("reduced-motion-safe", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes visible status principle" ($generatedVoiceUxPage.IndexOf("visible status", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes stale runtime health contract" ($generatedVoiceUxPage.IndexOf("stale runtime snapshots", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("unknown current service health", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("snapshot=fresh", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("snapshot=stale", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Runtime authority formatter marks stale snapshots unknown" ($runtimeStatusFormatterSource.IndexOf("IsFreshSnapshot", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $runtimeStatusFormatterSource.IndexOf("Runtime snapshot stale; current service health unknown", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $runtimeStatusFormatterSource.IndexOf("snapshot={snapshotFreshness}", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $runtimeStatusFormatterSource.IndexOf("restart or reconnect the Callsign service", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $runtimeStatusFormatterPath))
$checks.Add((New-Check "Main shell source exists" (Test-Path -LiteralPath $mainFormPath) $mainFormPath))
$checks.Add((New-Check "Main shell exposes visible macOS Voice Control badge" ($mainFormSource.IndexOf("Visual: macOS Voice Control", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Main shell visual badge exposes contrast evidence" ($mainFormSource.IndexOf("4.5:1 contrast", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Main shell visual badge exposes opacity evidence" ($mainFormSource.IndexOf("0.86-0.99 opacity", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Main shell visual badge exposes compact radius evidence" ($mainFormSource.IndexOf("20-26px HUD radius", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Shared visual style source exists" (Test-Path -LiteralPath $visualStylePath) $visualStylePath))
$checks.Add((New-Check "Shared visual style defines contrast evidence token" ($visualStyleSource.IndexOf("contrast>=4.5:1", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $visualStyleSource.IndexOf("MinimumTextContrastRatio = 4.5", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $visualStylePath))
$checks.Add((New-Check "Shared visual style defines translucency evidence token" ($visualStyleSource.IndexOf("opacity=0.86-0.99", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $visualStyleSource.IndexOf("MinimumOverlayOpacity = 0.86", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $visualStyleSource.IndexOf("MaximumSurfaceOpacity = 0.99", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $visualStylePath))
$checks.Add((New-Check "Shared visual style defines compact radius evidence token" ($visualStyleSource.IndexOf("radius=20-26px", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $visualStyleSource.IndexOf("CompactRadius = 20", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $visualStyleSource.IndexOf("LargeSurfaceRadius = 26", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $visualStylePath))
$checks.Add((New-Check "Shared visual style defines high-contrast evidence token" ($visualStyleSource.IndexOf("high-contrast-aware", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $visualStyleSource.IndexOf("high-contrast-ready", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $visualStylePath))
$checks.Add((New-Check "Shared visual style defines text-scaling evidence token" ($visualStyleSource.IndexOf("text-scaling-aware", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $visualStyleSource.IndexOf("text-scale-ready", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $visualStylePath))
$checks.Add((New-Check "Shared visual style defines reduced-motion evidence token" ($visualStyleSource.IndexOf("reduced-motion-aware", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $visualStyleSource.IndexOf("reduced-motion-safe", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $visualStylePath))
$checks.Add((New-Check "Shared visual style exposes high-contrast palette API" ($visualStyleSource.IndexOf("GetPalette(bool highContrast)", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $visualStyleSource.IndexOf("SystemColors.WindowText", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $visualStyleSource.IndexOf("SystemColors.Highlight", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $visualStylePath))
$checks.Add((New-Check "Shared visual style exposes bounded text-scale API" ($visualStyleSource.IndexOf("ClampTextScale", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $visualStyleSource.IndexOf("MinimumTextScale = 1.0f", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $visualStyleSource.IndexOf("MaximumTextScale = 1.6f", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $visualStylePath))
$checks.Add((New-Check "Generated Voice UX page documents palette and text-scale APIs" ($generatedVoiceUxPage.IndexOf("CallsignVisualStyle.GetPalette", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("ClampTextScale", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("DescribeAccessibilityMode", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page documents visual-status accessibility mode readback" ($generatedVoiceUxPage.IndexOf("active accessibility mode", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("palette mode", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("bounded text scale", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Main shell visual readback includes accessibility mode" ($mainFormSource.IndexOf("Accessibility mode:", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("DescribeAccessibilityMode", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Visual-status router includes accessibility aliases" ($alphaCommandRouterSource.IndexOf("read accessibility mode", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaCommandRouterSource.IndexOf("read high contrast status", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaCommandRouterSource.IndexOf("read reduced motion status", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $alphaCommandRouterPath))
$checks.Add((New-Check "Command discovery includes accessibility-mode visual-status aliases" ($commandDiscoverySource.IndexOf("read accessibility mode", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $commandDiscoverySource.IndexOf("read text scale status", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $commandDiscoverySource.IndexOf("read reduced motion status", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $commandDiscoveryPath))
$checks.Add((New-Check "Shared visual style defines stop-visible evidence token" ($visualStyleSource.IndexOf("stop-visible", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $visualStylePath))
$checks.Add((New-Check "Generated Voice UX page includes contrast evidence token" ($generatedVoiceUxPage.IndexOf("4.5:1", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes translucency range evidence token" ($generatedVoiceUxPage.IndexOf("0.86", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("0.99", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes compact radius evidence token" ($generatedVoiceUxPage.IndexOf("20", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("26 px", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes visible stop affordance token" ($generatedVoiceUxPage.IndexOf("visible stop", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or $generatedVoiceUxPage.IndexOf("stop/cancel/status", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes targeting HUD stop badges" ($generatedVoiceUxPage.IndexOf("visible-controls and mouse-grid targeting HUDs", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("same compact stop badge pattern", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes mouse-grid stop badge contract" ($generatedVoiceUxPage.IndexOf("status strip includes a compact", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("STOP", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("stop/cancel boundary remains visible", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Mouse grid overlay source exposes STOP badge" ($mouseGridOverlaySource.IndexOf('CreateStatusBadge("STOP"', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mouseGridOverlaySource.IndexOf("hide grid remain visible", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mouseGridOverlayPath))
$checks.Add((New-Check "Visible controls overlay source exposes STOP badge" ($visibleControlsOverlaySource.IndexOf('CreateStatusBadge("STOP"', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $visibleControlsOverlaySource.IndexOf("stop, cancel, and reset remain visible", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $visibleControlsOverlayPath))
$checks.Add((New-Check "Generated tier page includes command-level entitlement gate" ($generatedTierPage.IndexOf("bundled inside a Free pack", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Plans surfaces keep Voice Access parity free and paid packs beyond parity" ($mainFormSource.IndexOf("full Windows Voice Access parity baseline", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $mainFormSource.IndexOf("paid packs start beyond parity", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $commandDiscoverySource.IndexOf("Free Parity", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("full Windows Voice Access parity baseline", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("paid packs start beyond parity", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $alphaSmokeProgramSource.IndexOf("Plans tab should spell out that Voice Access parity stays in the open-source Free core", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $mainFormPath))
$checks.Add((New-Check "Generated security page includes command-level entitlement gate" ($generatedSecurityPage.IndexOf("paid-tier command may route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated tier page includes invalid command metadata gate" ($generatedTierPage.IndexOf("InvalidPack", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("cannot route or execute commands", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated security page includes invalid command metadata gate" ($generatedSecurityPage.IndexOf("InvalidPack", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("cannot route or execute commands", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated tier page includes pack filename import gate" ($generatedTierPage.IndexOf("normal Windows", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf(".dll", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("Reserved device names", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated security page includes pack filename import gate" ($generatedSecurityPage.IndexOf("normal Windows", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf(".dll", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("Reserved device names", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated tier page includes kept-pack disabled refresh gate" ($generatedTierPage.IndexOf("kept on disk", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("must not silently reactivate", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated security page includes kept-pack disabled refresh gate" ($generatedSecurityPage.IndexOf("kept on disk", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("cannot silently reactivate", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated tier page includes registry execution policy gate" ($generatedTierPage.IndexOf("direct pack execution fails closed without identity proof", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated security page includes registry execution policy gate" ($generatedSecurityPage.IndexOf("command registry enforces policy again at the pack execution boundary", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("fail closed without identity proof", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("approval-required commands fail until explicit approval is supplied", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated tier page includes structured policy outcome metadata" ($generatedTierPage.IndexOf("PolicyDecision", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("PolicyApprovalRequirement", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("PolicyRiskTier", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("PolicyVisibleActionRequired", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated security page includes structured policy outcome metadata" ($generatedSecurityPage.IndexOf("PolicyDecision", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("PolicyApprovalRequirement", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("PolicyRiskTier", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("PolicyVisibleActionRequired", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated tier page includes paid discovery non-routing status" ($generatedTierPage.IndexOf("will not route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("command palette", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated Voice UX page includes gated discovery non-routing status" ($generatedVoiceUxPage.IndexOf("will not route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("paid-tier requirement", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes command availability column contract" ($generatedVoiceUxPage.IndexOf("dedicated availability column", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("entitlement-required", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated Voice UX page includes selected-command routing gate" ($generatedVoiceUxPage.IndexOf("selected-command details", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("dedicated routing gate line", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedVoiceUxPage.IndexOf("will not route", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedVoiceUxPagePath))
$checks.Add((New-Check "Generated tier page includes entitlement downgrade rerouting gate" ($generatedTierPage.IndexOf("downgrade back to Free-only", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("commands stop routing", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated security page includes entitlement downgrade rerouting gate" ($generatedSecurityPage.IndexOf("downgrading back to Free-only", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("commands stop routing", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated tier page includes background approval gate" ($generatedTierPage.IndexOf("BackgroundAllowedWithApproval", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated security page includes background approval gate" ($generatedSecurityPage.IndexOf("BackgroundAllowedWithApproval", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated tier page includes visible-required gate" ($generatedTierPage.IndexOf("VisibleRequired", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated security page includes visible-required gate" ($generatedSecurityPage.IndexOf("VisibleRequired", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated tier page includes high-impact privacy approval gate" ($generatedTierPage.IndexOf("Clipboard", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("FileContents", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("ScreenshotOrOcr", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("ExternalData", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated security page includes high-impact privacy approval gate" ($generatedSecurityPage.IndexOf("Clipboard", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("FileContents", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("ScreenshotOrOcr", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("ExternalData", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))
$checks.Add((New-Check "Generated tier page includes misdeclared visibility approval gate" ($generatedTierPage.IndexOf("misdeclare", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("visibility as preferred", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedTierPage.IndexOf("visible approval surface", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedTierPagePath))
$checks.Add((New-Check "Generated security page includes misdeclared visibility approval gate" ($generatedSecurityPage.IndexOf("misdeclared", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("visibility-preferred", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $generatedSecurityPage.IndexOf("visible approval surface", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) $generatedSecurityPagePath))

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

    $manualEvidenceHeader = Get-PropertyValue $manualEvidence "evidence_header"
    $checks.Add((New-Check "Manual evidence includes evidence_header" ($null -ne $manualEvidenceHeader) $(if ($null -ne $manualEvidenceHeader) { "evidence_header" } else { "Missing evidence_header" })))
    $requiredHeaderFieldChecks = @(
        @{ name = "commit"; check_name = "Manual evidence header includes commit" },
        @{ name = "build_id"; check_name = "Manual evidence header includes build_id" },
        @{ name = "tester"; check_name = "Manual evidence header includes tester" },
        @{ name = "windows_version_edition_build"; check_name = "Manual evidence header includes windows_version_edition_build" },
        @{ name = "architecture"; check_name = "Manual evidence header includes architecture" },
        @{ name = "machine_or_vm"; check_name = "Manual evidence header includes machine_or_vm" },
        @{ name = "microphone"; check_name = "Manual evidence header includes microphone" },
        @{ name = "install_mode"; check_name = "Manual evidence header includes install_mode" },
        @{ name = "ui_runtime_versions"; check_name = "Manual evidence header includes ui_runtime_versions" },
        @{ name = "wake_identity_transcription_models"; check_name = "Manual evidence header includes wake_identity_transcription_models" }
    )
    foreach ($requiredHeaderField in $requiredHeaderFieldChecks) {
        $requiredHeaderFieldName = [string]$requiredHeaderField.name
        $requiredHeaderFieldCheckName = [string]$requiredHeaderField.check_name
        $checks.Add((New-Check $requiredHeaderFieldCheckName (Test-NonEmptyProperty $manualEvidenceHeader $requiredHeaderFieldName) $(if (Test-NonEmptyProperty $manualEvidenceHeader $requiredHeaderFieldName) { [string](Get-PropertyValue $manualEvidenceHeader $requiredHeaderFieldName) } else { "Missing evidence_header.$requiredHeaderFieldName" })))
    }
    $manualEvidenceHeaderTestedUtc = [string](Get-PropertyValue $manualEvidenceHeader "tested_utc")
    $checks.Add((New-Check "Manual evidence header tested_utc is valid" (Test-IsoUtcTimestamp $manualEvidenceHeaderTestedUtc) $(if ($manualEvidenceHeaderTestedUtc) { $manualEvidenceHeaderTestedUtc } else { "Missing evidence_header.tested_utc" })))
    $checks.Add((New-Check "Manual evidence header artifact_hashes include current installer SHA-256" (Test-ManualEvidenceHeaderArtifactHashes $manualEvidenceHeader $installerHash) $(if ($null -ne (Get-PropertyValue $manualEvidenceHeader "artifact_hashes")) { (@(Get-PropertyValue $manualEvidenceHeader "artifact_hashes") -join ", ") } else { "Missing evidence_header.artifact_hashes" })))

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
        $checks.Add((New-Check "Manual parity evidence privacy review passed: $manualCheckId" ((-not (Test-ManualEvidenceCheck $manualEvidence $manualCheckId)) -or (Test-ManualPrivacyReview $manualCheck)) $(if ($null -ne $manualCheck) { "privacy_review" } else { "Missing check" })))
        if ($manualCheckId -eq "apple_style_visual_polish_walkthrough") {
            $checks.Add((New-Check "Manual visual polish accessibility audit passed: $manualCheckId" ((-not (Test-ManualEvidenceCheck $manualEvidence $manualCheckId)) -or (Test-ManualAccessibilityVisualAudit $manualCheck)) $(if ($null -ne $manualCheck) { "accessibility_visual_audit" } else { "Missing check" })))
        }
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
            "Manual evidence check $manualCheckId is marked passed but has invalid or missing local artifact references."
        }

        if ((Test-ManualEvidenceCheck $manualEvidence $manualCheckId) -and -not (Test-ManualPrivacyReview $manualCheck)) {
            "Manual evidence check $manualCheckId is marked passed but has not completed the privacy review checklist."
        }

        $isVisualPolishManualCheck = $manualCheckId -eq "apple_style_visual_polish_walkthrough"
        $visualPolishManualCheckPassed = $isVisualPolishManualCheck -and (Test-ManualEvidenceCheck $manualEvidence $manualCheckId)
        $visualPolishAuditMissing = $visualPolishManualCheckPassed -and -not (Test-ManualAccessibilityVisualAudit $manualCheck)
        if ($visualPolishAuditMissing) {
            "Manual evidence check $manualCheckId is marked passed but has not completed the accessibility visual audit checklist."
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

$releaseBlockerSummary = [pscustomobject]@{
    manual_evidence_supplied = [bool]$manualEvidenceLoaded
    manual_checks_remaining_count = @($manualChecksRemaining).Count
    manual_categories_missing_count = @($manualCategoriesMissing).Count
    failed_automated_checks_count = $failed
    blocker_count = @($releaseBlockers).Count
    next_action = if (-not $manualEvidenceLoaded) {
        "Complete build/voice-access-parity-manual-evidence.template.json, attach local artifacts, then rerun with -RequireManualEvidence."
    }
    elseif (@($manualChecksRemaining).Count -gt 0 -or @($manualCategoriesMissing).Count -gt 0) {
        "Finish the remaining manual/live parity evidence checks and rerun with -RequireManualEvidence."
    }
    elseif ($failed -gt 0) {
        "Fix failed automated parity evidence checks before claiming release parity."
    }
    else {
        "Release parity evidence is ready."
    }
}

$evidence = [pscustomobject]@{
    generated_utc = [DateTime]::UtcNow.ToString("o")
    passed = $failed -eq 0
    release_ready = $releaseReady
    release_mode = $ReleaseMode
    release_blockers = @($releaseBlockers)
    release_blocker_summary = $releaseBlockerSummary
    passed_count = $passed
    failed_count = $failed
    installer = [pscustomobject]@{
        path = $installerPath
        sha256 = $installerHash
        size_bytes = $installerSizeBytes
    }
    release_proof = [pscustomobject]@{
        local_installer_path = $installerPath
        local_installer_sha256 = $installerHash
        local_installer_size_bytes = $installerSizeBytes
        website_download_url = $WebsiteDownloadUrl
        website_download_url_was_inferred = [bool]$WebsiteDownloadUrlWasInferred
        website_installer_sha256 = $WebsiteInstallerHash
        website_installer_size_bytes = $WebsiteInstallerSizeBytes
        comparison_summary = if ([string]::IsNullOrWhiteSpace($WebsiteDownloadUrl)) {
            "Release proof is waiting for a website download URL."
        }
        else {
            "Compare the local Callsign-Setup.exe installer to $WebsiteDownloadUrl and confirm the website installer SHA-256 and size match."
        }
        update_readback_summary = "Read Updates Status, Read Check-In Status, Read Visual Status, and Read Restart Proof keep the Updates and visual-contract evidence visible."
    }
    wake_sample_proof = $wakeSampleProof
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
