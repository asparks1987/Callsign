param(
    [switch]$SkipBuild,
    [switch]$NoRestore,
    [switch]$UseExistingSmokeBinary,
    [switch]$Install,
    [switch]$LiveActions,
    [switch]$RequireOpenWakeWord,
    [string]$WakeModelPath,
    [switch]$WatchVoiceAction,
    [int]$WatchSeconds = 90,
    [switch]$RequireAlphaReady,
    [string]$ReportPath
)

$ErrorActionPreference = "Stop"
$script:VerificationReport = @()
$script:LiveActionCoverageStartedUtc = $null

function Get-LiveActionCoverageMarkerPath {
    return (Join-Path $env:LOCALAPPDATA "Callsign\Runtime\live-action-coverage-started.utc")
}

function Write-VerificationReport {
    if ([string]::IsNullOrWhiteSpace($ReportPath)) {
        return
    }

    $reportRoot = Split-Path -Parent $ReportPath
    if (-not [string]::IsNullOrWhiteSpace($reportRoot)) {
        New-Item -ItemType Directory -Path $reportRoot -Force | Out-Null
    }

    $artifacts = Get-ArtifactReport
    $buildPayloadManifest = Get-BuildPayloadManifestReport
    $installedRuntime = Get-InstalledRuntimeReport

    [PSCustomObject]@{
        generated_utc = (Get-Date).ToUniversalTime().ToString("O")
        host = $env:COMPUTERNAME
        user = $env:USERNAME
        artifacts = $artifacts
        build_payload_manifest = $buildPayloadManifest
        installed_runtime = $installedRuntime
        alpha_readiness = Get-AlphaReadinessReport -Artifacts $artifacts -InstalledRuntime $installedRuntime -RequireActionCoverage:$LiveActions
        steps = $script:VerificationReport
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $ReportPath -Encoding UTF8
}

function Get-BuildPayloadManifestReport {
    $path = Join-Path $root "build\alpha-installer-payload.json"
    if (-not (Test-Path -LiteralPath $path)) {
        return [PSCustomObject]@{
            path = $path
            exists = $false
            length = $null
            last_write_utc = $null
            sha256 = $null
        }
    }

    $item = Get-Item -LiteralPath $path
    $hash = Get-FileHash -LiteralPath $path -Algorithm SHA256
    return [PSCustomObject]@{
        path = $item.FullName
        exists = $true
        length = $item.Length
        last_write_utc = $item.LastWriteTimeUtc.ToString("O")
        sha256 = $hash.Hash
    }
}

function Get-ArtifactReport {
    $artifactPaths = @(
        (Join-Path $root "Callsign-Setup.exe"),
        (Join-Path $root "Callsign-Run.exe"),
        (Join-Path $root "Callsign-Service.exe"),
        (Join-Path $root "build\alpha-installer-payload.json")
    )

    $artifacts = @()
    foreach ($path in $artifactPaths) {
        if (Test-Path -LiteralPath $path) {
            $item = Get-Item -LiteralPath $path
            $hash = Get-FileHash -LiteralPath $path -Algorithm SHA256
            $artifacts += [PSCustomObject]@{
                path = $item.FullName
                exists = $true
                length = $item.Length
                last_write_utc = $item.LastWriteTimeUtc.ToString("O")
                sha256 = $hash.Hash
            }
        }
        else {
            $artifacts += [PSCustomObject]@{
                path = $path
                exists = $false
                length = $null
                last_write_utc = $null
                sha256 = $null
            }
        }
    }

    return $artifacts
}

function Get-AlphaReadinessReport {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Artifacts,
        [Parameter(Mandatory = $true)]
        [object]$InstalledRuntime,
        [switch]$RequireActionCoverage
    )

    $setupArtifact = $Artifacts | Where-Object { $_.path -like "*Callsign-Setup.exe" } | Select-Object -First 1
    $portableArtifact = $Artifacts | Where-Object { $_.path -like "*Callsign-Run.exe" } | Select-Object -First 1
    $serviceArtifact = $Artifacts | Where-Object { $_.path -like "*Callsign-Service.exe" } | Select-Object -First 1
    $payloadManifestArtifact = $Artifacts | Where-Object { $_.path -like "*alpha-installer-payload.json" } | Select-Object -First 1
    $stateFresh = $InstalledRuntime.state_is_stale -eq $false
    $serviceRegistered = $InstalledRuntime.windows_service.registered -eq $true
    $serviceRunning = [string]$InstalledRuntime.windows_service.status -eq "Running"
    $runtimeRoleReady = [string]$InstalledRuntime.runtime_role -eq "user-runtime"
    $userRuntimeReady = ($InstalledRuntime.user_runtime_process_count -eq 1 `
            -or ($InstalledRuntime.user_runtime_process_count -eq 0 -and $runtimeRoleReady -and $stateFresh)) `
        -and -not $InstalledRuntime.duplicate_user_runtime_detected
    $wakeEngine = [string]$InstalledRuntime.current_wakeword_engine
    $voiceRuntimeReady = $InstalledRuntime.is_listening -eq $true `
        -and $wakeEngine -match "openWakeWord" `
        -and $wakeEngine -notmatch "unavailable|Compatibility keyword wake detector"
    $verifierProfileReady = $InstalledRuntime.verifier_profile.exists -eq $true -and $InstalledRuntime.verifier_profile.voice_ready -eq $true
    $lastActionAgeSeconds = $null
    if ($InstalledRuntime.last_service_action_utc) {
        try {
            $lastActionAgeSeconds = [int]((Get-Date).ToUniversalTime() - [DateTime]::Parse([string]$InstalledRuntime.last_service_action_utc).ToUniversalTime()).TotalSeconds
        }
        catch {
            $lastActionAgeSeconds = $null
        }
    }
    $hasRecentServiceAction = $InstalledRuntime.last_service_action_succeeded -eq $true `
        -and $null -ne $lastActionAgeSeconds `
        -and $lastActionAgeSeconds -le 300
    $coverageStartedUtc = if ($RequireActionCoverage) { Get-LiveActionCoverageStartedUtc } else { $null }
    $recentSuccessfulActions = @($InstalledRuntime.recent_service_actions | Where-Object {
        $isRecentSuccessfulAction = $false
        if ($_.Succeeded -eq $true -and $_.Utc) {
            try {
                $actionUtc = [DateTime]::Parse([string]$_.Utc).ToUniversalTime()
                $isRecentSuccessfulAction = ((Get-Date).ToUniversalTime() - $actionUtc).TotalSeconds -le 900
                if ($coverageStartedUtc) {
                    $isRecentSuccessfulAction = $isRecentSuccessfulAction -and $actionUtc -ge $coverageStartedUtc.AddSeconds(-1)
                }
            }
            catch {
                $isRecentSuccessfulAction = $false
            }
        }

        $isRecentSuccessfulAction
    })
    $recentFailedActions = @($InstalledRuntime.recent_service_actions | Where-Object {
        $isRecentFailedAction = $false
        if ($_.Succeeded -eq $false -and $_.Utc) {
            try {
                $actionUtc = [DateTime]::Parse([string]$_.Utc).ToUniversalTime()
                $isRecentFailedAction = ((Get-Date).ToUniversalTime() - $actionUtc).TotalSeconds -le 900
                if ($coverageStartedUtc) {
                    $isRecentFailedAction = $isRecentFailedAction -and $actionUtc -ge $coverageStartedUtc.AddSeconds(-1)
                }
            }
            catch {
                $isRecentFailedAction = $false
            }
        }

        $isRecentFailedAction
    })
    $recentActionKinds = @($recentSuccessfulActions | ForEach-Object { [string]$_.Kind } | Sort-Object -Unique)
    $recentFailedActionKinds = @($recentFailedActions | ForEach-Object { [string]$_.Kind } | Sort-Object -Unique)
    $requiredActionKinds = @("start_menu_launch", "browser", "file_search", "dictation")
    $missingActionKinds = @($requiredActionKinds | Where-Object { $recentActionKinds -notcontains $_ })
    $installedUiPath = [string]$InstalledRuntime.installed_ui_path
    $installedServicePath = [string]$InstalledRuntime.installed_service_path
    $installedIconPath = [string]$InstalledRuntime.installed_icon_path
    $desktopShortcutReady = $InstalledRuntime.desktop_shortcut.exists -eq $true `
        -and (Compare-PathText $InstalledRuntime.desktop_shortcut.target_path $installedUiPath) `
        -and (Compare-ShortcutIconPath $InstalledRuntime.desktop_shortcut.icon_location $installedIconPath) `
        -and [int]$InstalledRuntime.desktop_shortcut.window_style -eq 1
    $startMenuShortcutReady = $InstalledRuntime.start_menu_shortcut.exists -eq $true `
        -and (Compare-PathText $InstalledRuntime.start_menu_shortcut.target_path $installedUiPath) `
        -and (Compare-ShortcutIconPath $InstalledRuntime.start_menu_shortcut.icon_location $installedIconPath) `
        -and [int]$InstalledRuntime.start_menu_shortcut.window_style -eq 1
    $startupShortcutReady = $InstalledRuntime.startup_runtime_shortcut.exists -eq $true `
        -and (Compare-PathText $InstalledRuntime.startup_runtime_shortcut.target_path $installedServicePath) `
        -and (Compare-ShortcutIconPath $InstalledRuntime.startup_runtime_shortcut.icon_location $installedIconPath) `
        -and ([string]$InstalledRuntime.startup_runtime_shortcut.arguments) -eq "--user-runtime --service-installed" `
        -and [int]$InstalledRuntime.startup_runtime_shortcut.window_style -eq 7

    $gates = [ordered]@{
        installer_exe_present = $setupArtifact.exists -eq $true
        portable_exe_present = $portableArtifact.exists -eq $true
        service_exe_present = $serviceArtifact.exists -eq $true
        build_payload_manifest_present = $payloadManifestArtifact.exists -eq $true
        installed_app_folder_present = $InstalledRuntime.app_dir_present -eq $true
        installed_logs_folder_present = $InstalledRuntime.logs_dir_present -eq $true
        installed_config_manager_present = $InstalledRuntime.installed_ui_present -eq $true
        installed_runtime_present = $InstalledRuntime.installed_service_present -eq $true
        desktop_icon_present = $InstalledRuntime.installed_icon_present -eq $true
        desktop_shortcut_ready = $desktopShortcutReady
        start_menu_shortcut_ready = $startMenuShortcutReady
        startup_user_runtime_shortcut_ready = $startupShortcutReady
        windows_service_registered = $serviceRegistered
        windows_service_running = $serviceRunning
        exactly_one_user_runtime = $userRuntimeReady
        fresh_user_runtime_state = $stateFresh -and $runtimeRoleReady
        voice_runtime_listening = $stateFresh -and $runtimeRoleReady -and $voiceRuntimeReady
        verifier_profile_ready = if ($RequireActionCoverage) { $verifierProfileReady } else { $true }
        file_search_helper_ready = $InstalledRuntime.file_search_helper_ready -eq $true
        openwakeword_helpers_ready = $InstalledRuntime.openwakeword_helpers_ready -eq $true
        bundled_openwakeword_runtime_ready = $InstalledRuntime.bundled_openwakeword_runtime_ready -eq $true
        browser_commands_ready = $InstalledRuntime.installed_service_present -eq $true
        recent_action_history_covers_all_alpha_actions = if ($RequireActionCoverage) { $missingActionKinds.Count -eq 0 } else { $true }
    }
    $nonBlocking = [ordered]@{
        chrome_available = $InstalledRuntime.browser_readiness.chrome_present -eq $true
        recent_service_action_succeeded = $hasRecentServiceAction
        recent_action_history_covers_all_alpha_actions = $missingActionKinds.Count -eq 0
    }
    $recentAction = [PSCustomObject]@{
        kind = $InstalledRuntime.last_service_action_kind
        target = $InstalledRuntime.last_service_action_target
        succeeded = $InstalledRuntime.last_service_action_succeeded
        utc = $InstalledRuntime.last_service_action_utc
        age_seconds = $lastActionAgeSeconds
        fresh_success = $hasRecentServiceAction
    }
    $actionCoverage = [PSCustomObject]@{
        window_seconds = 900
        coverage_started_utc = if ($coverageStartedUtc) { $coverageStartedUtc.ToString("O") } else { $null }
        required_for_readiness = [bool]$RequireActionCoverage
        required_kinds = $requiredActionKinds
        observed_successful_kinds = $recentActionKinds
        observed_failed_kinds = $recentFailedActionKinds
        missing_kinds = $missingActionKinds
        recent_successful_actions = $recentSuccessfulActions
        recent_failed_actions = $recentFailedActions
    }

    $blocking = @($gates.GetEnumerator() | Where-Object { -not $_.Value } | ForEach-Object { $_.Key })

    return [PSCustomObject]@{
        ready = $blocking.Count -eq 0
        gates = [PSCustomObject]$gates
        non_blocking_capabilities = [PSCustomObject]$nonBlocking
        recent_service_action = $recentAction
        recent_action_coverage = $actionCoverage
        blocking_gates = $blocking
        note = "Chrome availability is non-blocking because browser commands can fall back to the default browser when Chrome is not installed."
    }
}

function Assert-AlphaReadiness {
    $artifacts = Get-ArtifactReport
    $installedRuntime = Get-InstalledRuntimeReport
    $readiness = Get-AlphaReadinessReport -Artifacts $artifacts -InstalledRuntime $installedRuntime -RequireActionCoverage:$LiveActions

    if ($readiness.ready -eq $true) {
        Write-Host "Alpha readiness gates passed." -ForegroundColor Green
        return
    }

    $blocking = @($readiness.blocking_gates)
    throw "Alpha readiness gates failed: $($blocking -join ', ')"
}

function Get-LiveActionCoverageStartedUtc {
    if ($script:LiveActionCoverageStartedUtc) {
        return $script:LiveActionCoverageStartedUtc
    }

    $markerPath = Get-LiveActionCoverageMarkerPath
    if (-not (Test-Path -LiteralPath $markerPath)) {
        return $null
    }

    try {
        return [DateTime]::Parse((Get-Content -LiteralPath $markerPath -Raw).Trim()).ToUniversalTime()
    }
    catch {
        return $null
    }
}

function Set-LiveActionCoverageStartedUtc {
    $script:LiveActionCoverageStartedUtc = (Get-Date).ToUniversalTime()
    $markerPath = Get-LiveActionCoverageMarkerPath
    $markerDir = Split-Path -Parent $markerPath
    New-Item -ItemType Directory -Path $markerDir -Force | Out-Null
    Set-Content -LiteralPath $markerPath -Value $script:LiveActionCoverageStartedUtc.ToString("O") -Encoding UTF8
    Write-Host "Runtime action coverage marker: $($script:LiveActionCoverageStartedUtc.ToString("O"))"
}

function Compare-PathText {
    param(
        [string]$Actual,
        [string]$Expected
    )

    if ([string]::IsNullOrWhiteSpace($Actual) -or [string]::IsNullOrWhiteSpace($Expected)) {
        return $false
    }

    try {
        return [IO.Path]::GetFullPath($Actual).Equals([IO.Path]::GetFullPath($Expected), [StringComparison]::OrdinalIgnoreCase)
    }
    catch {
        return $false
    }
}

function Compare-ShortcutIconPath {
    param(
        [string]$ActualIconLocation,
        [string]$ExpectedIconPath
    )

    if ([string]::IsNullOrWhiteSpace($ActualIconLocation) -or [string]::IsNullOrWhiteSpace($ExpectedIconPath)) {
        return $false
    }

    $actualIconPath = $ActualIconLocation.Split(',', 2)[0].Trim()
    return Compare-PathText $actualIconPath $ExpectedIconPath
}

function Get-ShortcutReport {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $exists = Test-Path -LiteralPath $Path
    $targetPath = $null
    $iconLocation = $null
    $arguments = $null
    $windowStyle = $null
    $errorMessage = $null

    if ($exists) {
        try {
            $shellType = [type]::GetTypeFromProgID("WScript.Shell")
            if ($null -eq $shellType) {
                throw "Windows Script Host is not available."
            }

            $shell = [Activator]::CreateInstance($shellType)
            $shortcut = $shell.CreateShortcut($Path)
            $targetPath = [string]$shortcut.TargetPath
            $iconLocation = [string]$shortcut.IconLocation
            $arguments = [string]$shortcut.Arguments
            $windowStyle = $shortcut.WindowStyle
        }
        catch {
            $errorMessage = $_.Exception.Message
        }
    }

    return [PSCustomObject]@{
        path = $Path
        exists = $exists
        target_path = $targetPath
        icon_location = $iconLocation
        arguments = $arguments
        window_style = $windowStyle
        error = $errorMessage
    }
}

function Get-WindowsServiceReport {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    try {
        $service = Get-Service -Name $Name -ErrorAction Stop
        return [PSCustomObject]@{
            name = $Name
            registered = $true
            status = [string]$service.Status
            start_type = if ($service.PSObject.Properties.Name -contains "StartType") { [string]$service.StartType } else { $null }
            error = $null
        }
    }
    catch {
        return [PSCustomObject]@{
            name = $Name
            registered = $false
            status = $null
            start_type = $null
            error = $_.Exception.Message
        }
    }
}

function Get-LogTail {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [int]$Tail = 20
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return [PSCustomObject]@{
            path = $Path
            exists = $false
            lines = @()
        }
    }

    return [PSCustomObject]@{
        path = $Path
        exists = $true
        lines = @(Get-Content -LiteralPath $Path -Tail $Tail)
    }
}

function Get-ChromeReadinessReport {
    $candidates = @()
    foreach ($rootPath in @(
        [Environment]::GetFolderPath("ProgramFiles"),
        [Environment]::GetFolderPath("ProgramFilesX86"),
        [Environment]::GetFolderPath("LocalApplicationData")
    )) {
        if (-not [string]::IsNullOrWhiteSpace($rootPath)) {
            $candidates += (Join-Path $rootPath "Google\Chrome\Application\chrome.exe")
        }
    }

    $pathValue = [Environment]::GetEnvironmentVariable("PATH")
    if (-not [string]::IsNullOrWhiteSpace($pathValue)) {
        foreach ($pathEntry in $pathValue.Split([IO.Path]::PathSeparator, [StringSplitOptions]::RemoveEmptyEntries)) {
            if (-not [string]::IsNullOrWhiteSpace($pathEntry)) {
                $candidates += (Join-Path $pathEntry.Trim() "chrome.exe")
            }
        }
    }

    $existing = @($candidates | Select-Object -Unique | Where-Object { Test-Path -LiteralPath $_ })
    return [PSCustomObject]@{
        chrome_present = $existing.Count -gt 0
        chrome_path = if ($existing.Count -gt 0) { $existing[0] } else { $null }
        checked_paths = @($candidates | Select-Object -Unique)
    }
}

function Get-ProfileReadinessReport {
    param(
        [string]$Callsign = "echo one"
    )

    $profileDir = Join-Path $env:LOCALAPPDATA "Callsign\Profiles\$Callsign"
    $settingsPath = Join-Path $profileDir "settings.json"
    $exists = Test-Path -LiteralPath $settingsPath
    $voiceReady = $false
    $voiceStatus = $null
    $samplesRecorded = $null
    $samplesRequired = $null
    $errorMessage = $null

    if ($exists) {
        try {
            $profile = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
            $voiceStatus = [string]$profile.Settings.VoiceEnrollmentStatus
            $samplesRecorded = [int]$profile.Settings.VoiceSamplesRecorded
            $samplesRequired = [int]$profile.Settings.VoiceSamplesRequired
            $voiceReady = $samplesRecorded -ge $samplesRequired
        }
        catch {
            $errorMessage = $_.Exception.Message
        }
    }

    return [PSCustomObject]@{
        callsign = $Callsign
        settings_path = $settingsPath
        exists = $exists
        voice_ready = $voiceReady
        voice_status = $voiceStatus
        voice_samples_recorded = $samplesRecorded
        voice_samples_required = $samplesRequired
        error = $errorMessage
    }
}

function Get-RecentServiceActions {
    param(
        [object]$State,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $actions = @()
    if ($State -and $State.RecentServiceActions) {
        $actions += @($State.RecentServiceActions)
    }

    if (Test-Path -LiteralPath $Path) {
        try {
            $persisted = @(Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json)
            $actions += $persisted
        }
        catch {
            # Ignore malformed persisted history; state.json remains the primary runtime snapshot.
        }
    }

    return @($actions | Where-Object { $_ } | Sort-Object Utc, Kind, Target -Unique)
}

function Test-BundledOpenWakeWordRuntimeReady {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PythonPath
    )

    if (-not (Test-Path -LiteralPath $PythonPath)) {
        return $false
    }

    try {
        & $PythonPath -c "import openwakeword, onnxruntime, numpy" *> $null
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $false
    }
}

function Get-InstalledRuntimeReport {
    $statePath = Join-Path $env:LOCALAPPDATA "Callsign\Runtime\state.json"
    $appDir = Join-Path $env:LOCALAPPDATA "Callsign\App"
    $modelPath = Join-Path $env:LOCALAPPDATA "Callsign\Models\callsign.onnx"
    $bundledRuntimePython = Join-Path $env:LOCALAPPDATA "Callsign\Runtime\openwakeword\venv\Scripts\python.exe"
    $iconPath = Join-Path $appDir "callsign.ico"
    $uiPath = Join-Path $appDir "Callsign.UI.exe"
    $servicePath = Join-Path $appDir "Callsign.Service.exe"
    $fzfPath = Join-Path $appDir "fzf.exe"
    $runtimeStartupLog = Join-Path $env:LOCALAPPDATA "Callsign\Logs\runtime-startup.log"
    $runtimeControlLog = Join-Path $env:LOCALAPPDATA "Callsign\Logs\runtime-control.log"
    $openWakeWordSetupLog = Join-Path $env:LOCALAPPDATA "Callsign\Logs\openwakeword-setup.log"
    $logsDir = Join-Path $env:LOCALAPPDATA "Callsign\Logs"
    $recentActionsPath = Join-Path $env:LOCALAPPDATA "Callsign\Runtime\recent-service-actions.json"
    $desktopShortcut = Join-Path ([Environment]::GetFolderPath("DesktopDirectory")) "Callsign.lnk"
    $startMenuShortcut = Join-Path ([Environment]::GetFolderPath("ApplicationData")) "Microsoft\Windows\Start Menu\Programs\Callsign\Callsign.lnk"
    $startupShortcut = Join-Path ([Environment]::GetFolderPath("Startup")) "Callsign Runtime.lnk"

    $serviceProcesses = @(Get-Process -Name Callsign.Service -ErrorAction SilentlyContinue)
    $serviceProcess = $serviceProcesses | Select-Object -First 1
    $uiProcess = Get-Process -Name Callsign.UI -ErrorAction SilentlyContinue | Select-Object -First 1
    $serviceReport = Get-WindowsServiceReport -Name "Callsign"
    $serviceProcessDetails = @()
    try {
        $serviceProcessDetails = @(Get-CimInstance Win32_Process -Filter "Name = 'Callsign.Service.exe'" | ForEach-Object {
            [PSCustomObject]@{
                process_id = $_.ProcessId
                command_line = $_.CommandLine
                is_user_runtime = ([string]$_.CommandLine) -match "--user-runtime"
                is_windows_service_runtime = ([string]$_.CommandLine) -match "--run-service"
            }
        })
    }
    catch {
        $serviceProcessDetails = @([PSCustomObject]@{
            process_id = $null
            command_line = $null
            is_user_runtime = $false
            is_windows_service_runtime = $false
            error = $_.Exception.Message
        })
    }

    $state = $null
    $stateAgeSeconds = $null
    if (Test-Path -LiteralPath $statePath) {
        try {
            $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
            if ($state.UpdatedUtc) {
                $stateUpdated = [DateTime]::Parse([string]$state.UpdatedUtc).ToUniversalTime()
                $stateAgeSeconds = [int]((Get-Date).ToUniversalTime() - $stateUpdated).TotalSeconds
            }
        }
        catch {
            $state = $null
            $stateAgeSeconds = $null
        }
    }

    return [PSCustomObject]@{
        state_path = $statePath
        state_exists = Test-Path -LiteralPath $statePath
        app_dir = $appDir
        app_dir_present = Test-Path -LiteralPath $appDir
        installed_ui_path = $uiPath
        installed_ui_present = Test-Path -LiteralPath $uiPath
        installed_service_path = $servicePath
        installed_service_present = Test-Path -LiteralPath $servicePath
        installed_fzf_path = $fzfPath
        installed_fzf_present = Test-Path -LiteralPath $fzfPath
        file_search_helper_ready = Test-Path -LiteralPath $fzfPath
        installed_openwakeword_setup_path = Join-Path $appDir "setupopenwakeword.ps1"
        installed_openwakeword_setup_present = Test-Path -LiteralPath (Join-Path $appDir "setupopenwakeword.ps1")
        installed_openwakeword_test_path = Join-Path $appDir "testopenwakeword.ps1"
        installed_openwakeword_test_present = Test-Path -LiteralPath (Join-Path $appDir "testopenwakeword.ps1")
        openwakeword_helpers_ready = (Test-Path -LiteralPath (Join-Path $appDir "setupopenwakeword.ps1")) -and (Test-Path -LiteralPath (Join-Path $appDir "testopenwakeword.ps1"))
        browser_readiness = Get-ChromeReadinessReport
        verifier_profile = Get-ProfileReadinessReport -Callsign "echo one"
        installed_icon_path = $iconPath
        installed_icon_present = Test-Path -LiteralPath $iconPath
        desktop_shortcut = Get-ShortcutReport -Path $desktopShortcut
        start_menu_shortcut = Get-ShortcutReport -Path $startMenuShortcut
        startup_runtime_shortcut = Get-ShortcutReport -Path $startupShortcut
        windows_service = $serviceReport
        service_process_running = $null -ne $serviceProcess
        service_process_count = $serviceProcesses.Count
        user_runtime_process_count = @($serviceProcessDetails | Where-Object { $_.is_user_runtime }).Count
        duplicate_user_runtime_detected = (@($serviceProcessDetails | Where-Object { $_.is_user_runtime }).Count -gt 1)
        service_processes = $serviceProcessDetails
        logs_dir = $logsDir
        logs_dir_present = Test-Path -LiteralPath $logsDir
        runtime_startup_log = Get-LogTail -Path $runtimeStartupLog
        runtime_control_log = Get-LogTail -Path $runtimeControlLog
        openwakeword_setup_log = Get-LogTail -Path $openWakeWordSetupLog
        service_process_id = if ($serviceProcess) { $serviceProcess.Id } else { $null }
        ui_process_running = $null -ne $uiProcess
        ui_process_id = if ($uiProcess) { $uiProcess.Id } else { $null }
        openwakeword_model_path = $modelPath
        openwakeword_model_present = Test-Path -LiteralPath $modelPath
        bundled_openwakeword_runtime_path = $bundledRuntimePython
        bundled_openwakeword_runtime_present = Test-Path -LiteralPath $bundledRuntimePython
        bundled_openwakeword_runtime_ready = Test-BundledOpenWakeWordRuntimeReady -PythonPath $bundledRuntimePython
        service_state = if ($state) { $state.ServiceState } else { $null }
        runtime_role = if ($state) { $state.RuntimeRole } else { $null }
        mode_description = if ($state) { $state.ModeDescription } else { $null }
        current_wakeword_engine = if ($state) { $state.CurrentWakeWordEngine } else { $null }
        active_callsign = if ($state) { $state.ActiveCallsign } else { $null }
        is_listening = if ($state) { $state.IsListening } else { $null }
        updated_utc = if ($state) { $state.UpdatedUtc } else { $null }
        state_age_seconds = $stateAgeSeconds
        state_is_stale = if ($null -ne $stateAgeSeconds) { $stateAgeSeconds -gt 30 } else { $null }
        last_service_action_kind = if ($state) { $state.LastServiceActionKind } else { $null }
        last_service_action_target = if ($state) { $state.LastServiceActionTarget } else { $null }
        last_service_action_succeeded = if ($state) { $state.LastServiceActionSucceeded } else { $null }
        last_service_action_utc = if ($state) { $state.LastServiceActionUtc } else { $null }
        recent_service_action_history_path = $recentActionsPath
        recent_service_action_history_present = Test-Path -LiteralPath $recentActionsPath
        recent_service_actions = Get-RecentServiceActions -State $state -Path $recentActionsPath
    }
}

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    Write-Host ""
    Write-Host "== $Name ==" -ForegroundColor Cyan
    $started = Get-Date
    try {
        & $Action
        $finished = Get-Date
        $script:VerificationReport += [PSCustomObject]@{
            name = $Name
            status = "passed"
            started_utc = $started.ToUniversalTime().ToString("O")
            finished_utc = $finished.ToUniversalTime().ToString("O")
            duration_ms = [int]($finished - $started).TotalMilliseconds
            error = $null
        }
    }
    catch {
        $finished = Get-Date
        $script:VerificationReport += [PSCustomObject]@{
            name = $Name
            status = "failed"
            started_utc = $started.ToUniversalTime().ToString("O")
            finished_utc = $finished.ToUniversalTime().ToString("O")
            duration_ms = [int]($finished - $started).TotalMilliseconds
            error = $_.Exception.Message
        }
        Write-VerificationReport
        throw
    }
}

function Invoke-Native {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

function Test-OpenWakeWordReady {
    $modelPath = Join-Path $env:LOCALAPPDATA "Callsign\Models\callsign.onnx"
    $runtimePythonPath = Join-Path $env:LOCALAPPDATA "Callsign\Runtime\openwakeword\venv\Scripts\python.exe"
    if (-not (Test-Path $modelPath)) {
        Write-Host "WARN: openWakeWord model missing: $modelPath" -ForegroundColor Yellow
        return $false
    }

    if (Test-Path $runtimePythonPath) {
        try {
            & $runtimePythonPath -c "import openwakeword, onnxruntime, numpy" *> $null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "INFO: bundled openWakeWord Python runtime ready: $runtimePythonPath"
                return $true
            }
        }
        catch {
            # Fall through to the warning below.
        }
    }

    Write-Host "WARN: bundled openWakeWord runtime is not ready. Run setupopenwakeword.ps1 -InstallPythonPackages to create or repair the local venv." -ForegroundColor Yellow
    return $false
}

function Test-InstalledRuntimeOpenWakeWord {
    $statePath = Join-Path $env:LOCALAPPDATA "Callsign\Runtime\state.json"
    if (-not (Test-Path $statePath)) {
        throw "Runtime state file was not found: $statePath"
    }

    $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    $mode = [string]$state.ModeDescription
    $wakeEngine = [string]$state.CurrentWakeWordEngine
    Write-Host "INFO: Installed runtime mode: $mode"
    Write-Host "INFO: Installed wake engine: $wakeEngine"
    if (($wakeEngine -notmatch "openWakeWord" -and $mode -notmatch "openWakeWord") -or $wakeEngine -match "unavailable" -or $mode -match "unavailable") {
        throw "openWakeWord was required, but installed runtime is not using it."
    }

    if ($wakeEngine -match "Compatibility keyword wake detector" -or $mode -match "Compatibility keyword wake detector") {
        throw "openWakeWord was required, but installed runtime is still using compatibility wake detection."
    }
}

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$smokeProject = "tests/Callsign.AlphaSmoke/Callsign.AlphaSmoke.csproj"
$smokeDll = Join-Path $root "tests\Callsign.AlphaSmoke\bin\Debug\net10.0-windows\Callsign.AlphaSmoke.dll"
$installer = Join-Path $root "Callsign-Setup.exe"
[string[]]$restoreArgs = if ($NoRestore) { @("--no-restore") } else { @() }

if ($NoRestore -and -not $UseExistingSmokeBinary) {
    Write-Host "WARN: -NoRestore still requires valid restored NuGet assets. In restricted/offline environments, prefer -UseExistingSmokeBinary." -ForegroundColor Yellow
}

function Invoke-Smoke {
    param(
        [string[]]$Arguments = @()
    )

    if ($UseExistingSmokeBinary) {
        if (-not (Test-Path $smokeDll)) {
            throw "Existing smoke binary was requested but not found: $smokeDll"
        }

        Invoke-Native dotnet $smokeDll @Arguments
        return
    }

    Invoke-Native dotnet run @restoreArgs --project $smokeProject -- @Arguments
}

function Invoke-InstalledRuntimeTranscript {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Transcript,
        [Parameter(Mandatory = $true)]
        [string]$ExpectedKind,
        [int]$TimeoutSeconds = 45
    )

    $runtimeDir = Join-Path $env:LOCALAPPDATA "Callsign\Runtime"
    $requestPath = Join-Path $runtimeDir "scripted-transcript.request"
    $statePath = Join-Path $runtimeDir "state.json"
    New-Item -ItemType Directory -Path $runtimeDir -Force | Out-Null
    Clear-InstalledRuntimeScriptedRequest -RequestPath $requestPath
    Reset-InstalledRuntimeScriptedSession -RequestPath $requestPath

    $identityTranscript = "echo one"
    $commandTranscript = $Transcript -replace '^\s*(?i:(callsign|call sign|paul sign|wall sign))\s+echo\s+one\s+', ''

    Set-Content -LiteralPath $requestPath -Value $identityTranscript -Encoding UTF8
    Write-Host "Queued installed runtime identity transcript: $identityTranscript"

    $identityDeadline = (Get-Date).AddSeconds([Math]::Min($TimeoutSeconds, 20))
    $identityAccepted = $false
    while ((Get-Date) -lt $identityDeadline) {
        Start-Sleep -Milliseconds 750
        if (-not (Test-Path -LiteralPath $statePath)) {
            continue
        }

        try {
            $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
            if ([string]$state.VerifiedCallsign -eq "echo one" -or $state.LastIdentityAccepted -eq $true) {
                $identityAccepted = $true
                break
            }
        }
        catch {
            # State may be mid-write; keep waiting until timeout.
        }
    }

    if (-not $identityAccepted) {
        throw "Timed out waiting for installed runtime identity verification. $(Get-RuntimeStateDiagnostic -StatePath $statePath)"
    }

    $startedUtc = (Get-Date).ToUniversalTime()
    Set-Content -LiteralPath $requestPath -Value $commandTranscript -Encoding UTF8
    Write-Host "Queued installed runtime command transcript: $commandTranscript"

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 750
        if (-not (Test-Path -LiteralPath $statePath)) {
            continue
        }

        try {
            $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
            $recent = @($state.RecentServiceActions)
            foreach ($action in $recent) {
                if ([string]$action.Kind -ne $ExpectedKind -or $action.Succeeded -ne $true -or -not $action.Utc) {
                    continue
                }

                $actionUtc = [DateTime]::Parse([string]$action.Utc).ToUniversalTime()
                if ($actionUtc -ge $startedUtc.AddSeconds(-1)) {
                    Write-Host "Observed installed runtime action: kind=$($action.Kind); target=$($action.Target); message=$($action.Message)"
                    if ($ExpectedKind -eq "dictation") {
                        Reset-InstalledRuntimeScriptedSession -RequestPath $requestPath
                    }
                    return
                }
            }
        }
        catch {
            # State may be mid-write; keep waiting until timeout.
        }
    }

    throw "Timed out waiting for installed runtime action kind '$ExpectedKind' after transcript '$Transcript'. $(Get-RuntimeStateDiagnostic -StatePath $statePath)"
}

function Get-RuntimeStateDiagnostic {
    param(
        [Parameter(Mandatory = $true)]
        [string]$StatePath
    )

    if (-not (Test-Path -LiteralPath $StatePath)) {
        return "Runtime state file is missing: $StatePath"
    }

    try {
        $state = Get-Content -LiteralPath $StatePath -Raw | ConvertFrom-Json
        $recentKinds = @($state.RecentServiceActions | ForEach-Object {
            "$($_.Kind):$($_.Succeeded):$($_.Target)"
        })
        return "Runtime state: role=$($state.RuntimeRole); service_state=$($state.ServiceState); active_callsign=$($state.ActiveCallsign); status='$($state.StatusMessage)'; last_action=$($state.LastServiceActionKind):$($state.LastServiceActionSucceeded):$($state.LastServiceActionTarget); recent_actions=[$($recentKinds -join '; ')]."
    }
    catch {
        return "Runtime state was unreadable: $($_.Exception.Message)"
    }
}

function Clear-InstalledRuntimeScriptedRequest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RequestPath
    )

    if (Test-Path -LiteralPath $RequestPath) {
        Remove-Item -LiteralPath $RequestPath -Force
    }
}

function Clear-InstalledRuntimeControlRequests {
    $runtimeDir = Join-Path $env:LOCALAPPDATA "Callsign\Runtime"
    foreach ($requestName in @("scripted-transcript.request", "stop-user-runtime.request", "clear-action-history.request", "recent-service-actions.json")) {
        $requestPath = Join-Path $runtimeDir $requestName
        if (Test-Path -LiteralPath $requestPath) {
            Remove-Item -LiteralPath $requestPath -Force
            Write-Host "Cleared stale runtime request: $requestPath"
        }
    }
}

function Clear-InstalledRuntimeActionHistory {
    $runtimeDir = Join-Path $env:LOCALAPPDATA "Callsign\Runtime"
    $statePath = Join-Path $runtimeDir "state.json"
    New-Item -ItemType Directory -Path $runtimeDir -Force | Out-Null
    $requestPath = Join-Path $runtimeDir "clear-action-history.request"
    Set-Content -LiteralPath $requestPath -Value ((Get-Date).ToUniversalTime().ToString("O")) -Encoding UTF8

    $deadline = (Get-Date).AddSeconds(5)
    $requestConsumed = $false
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 250
        if (-not (Test-Path -LiteralPath $requestPath)) {
            $requestConsumed = $true
            break
        }
    }

    if (-not $requestConsumed) {
        Remove-Item -LiteralPath $requestPath -Force -ErrorAction SilentlyContinue
        throw "Timed out waiting for installed runtime to consume clear-action-history request."
    }

    $deadline = (Get-Date).AddSeconds(5)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 250
        if (-not (Test-Path -LiteralPath $statePath)) {
            continue
        }

        try {
            $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
            if (@($state.RecentServiceActions).Count -eq 0 -and -not $state.LastServiceActionKind) {
                Write-Host "Installed runtime action history cleared."
                return
            }
        }
        catch {
            # State may be mid-write; keep waiting until timeout.
        }
    }

    throw "Installed runtime action history did not clear before live action coverage started."
}

function Reset-InstalledRuntimeScriptedSession {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RequestPath
    )

    Set-Content -LiteralPath $RequestPath -Value "cancel" -Encoding UTF8
    $deadline = (Get-Date).AddSeconds(5)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 250
        if (-not (Test-Path -LiteralPath $RequestPath)) {
            return
        }
    }

    Clear-InstalledRuntimeScriptedRequest -RequestPath $RequestPath
}

function Ensure-InstalledUserRuntime {
    param(
        [int]$TimeoutSeconds = 30
    )

    $runtimeDir = Join-Path $env:LOCALAPPDATA "Callsign\Runtime"
    $statePath = Join-Path $runtimeDir "state.json"
    $serviceExe = Join-Path $env:LOCALAPPDATA "Callsign\App\Callsign.Service.exe"
    if (-not (Test-Path -LiteralPath $serviceExe)) {
        throw "Installed Callsign user runtime executable was not found: $serviceExe"
    }

    $runtime = Get-InstalledRuntimeReport
    if ($runtime.user_runtime_process_count -ne 1) {
        Write-Host "Starting installed Callsign user-runtime: $serviceExe"
        Start-Process -FilePath $serviceExe `
            -ArgumentList "--user-runtime --service-installed" `
            -WorkingDirectory (Split-Path -Parent $serviceExe) `
            -WindowStyle Hidden | Out-Null
    }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 750
        if (-not (Test-Path -LiteralPath $statePath)) {
            continue
        }

        try {
            $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
            if ([string]$state.RuntimeRole -ne "user-runtime" -or $state.IsListening -ne $true -or -not $state.UpdatedUtc) {
                continue
            }

            if ([string]$state.ActiveCallsign -ne "echo one") {
                continue
            }

            $ageSeconds = [int]((Get-Date).ToUniversalTime() - [DateTime]::Parse([string]$state.UpdatedUtc).ToUniversalTime()).TotalSeconds
            if ($ageSeconds -le 30) {
                Write-Host "Installed user-runtime is fresh, listening, and using verifier profile 'echo one'."
                return
            }
        }
        catch {
            # State may be mid-write; keep waiting until timeout.
        }
    }

    throw "Timed out waiting for installed user-runtime to write a fresh listening state with active callsign 'echo one'."
}

function Ensure-AlphaVerifierProfile {
    $profileDir = Join-Path $env:LOCALAPPDATA "Callsign\Profiles\echo one"
    $settingsPath = Join-Path $profileDir "settings.json"
    New-Item -ItemType Directory -Path $profileDir -Force | Out-Null

    $now = (Get-Date).ToUniversalTime().ToString("O")
    [PSCustomObject]@{
        DisplayName = "Alpha Verifier"
        Email = $null
        Department = "Alpha"
        Notes = "Created by verifycallsign-alpha.ps1 for installed runtime action coverage."
        CreatedUtc = $now
        UpdatedUtc = $now
        Settings = [PSCustomObject]@{
            WakeWord = "Callsign"
            PreferredTheme = "Dark"
            LanguageCode = "en-US"
            AutoSaveIntervalSeconds = 30
            StartWithWindows = $true
            ShowCommandFeed = $true
            DashboardTitle = "Callsign"
            VoiceEnrollmentStatus = "Activated"
            VoiceSamplesRecorded = 3
            VoiceSamplesRequired = 3
            VoiceEnrolledUtc = $now
            VoiceRecognitionMode = "Local"
            VoiceModelPath = $null
            VoiceWakeThreshold = 0.55
            VoiceWakeSensitivity = "Balanced"
            VoiceWakeDiagnosticsEnabled = $false
            VoiceSilenceMilliseconds = 850
            VoiceCommandConfidenceThreshold = 0.65
            VoiceCloudOptIn = $false
            UseVoiceActivityDetection = $true
            UseNoiseSuppression = $false
            LastLaunchedApp = $null
        }
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $settingsPath -Encoding UTF8

    Write-Host "Ensured alpha verifier profile: $settingsPath"
}

if (-not $UseExistingSmokeBinary) {
    Invoke-Step "Build alpha smoke project" {
        Invoke-Native dotnet build $smokeProject @restoreArgs
    }
}
else {
    Write-Host "Using existing smoke binary: $smokeDll" -ForegroundColor Yellow
}

Invoke-Step "Run alpha smoke checks" {
    Invoke-Smoke
}

if ($RequireOpenWakeWord) {
    Invoke-Step "Require openWakeWord readiness" {
        if (-not (Test-OpenWakeWordReady)) {
            throw "openWakeWord was required, but the custom model and bundled runtime are not ready. Run .\setupopenwakeword.ps1 -ModelPath <custom-callsign.onnx> -InstallPythonPackages"
        }
    }
}

if (-not $SkipBuild) {
    Invoke-Step "Build Callsign installer payload" {
        $buildArgs = @()
        if ($NoRestore) {
            $buildArgs += "-NoRestore"
        }
        if (-not [string]::IsNullOrWhiteSpace($WakeModelPath)) {
            $buildArgs += @("-WakeModelPath", $WakeModelPath)
        }
        & (Join-Path $root "buildcallsign.ps1") @buildArgs
        if ($LASTEXITCODE -ne 0) {
            throw "buildcallsign.ps1 failed with exit code $LASTEXITCODE"
        }
    }
}

if (-not (Test-Path $installer)) {
    throw "Installer was not found at $installer"
}

if ($Install) {
    Invoke-Step "Install latest Callsign payload" {
        $process = Start-Process -FilePath $installer -WorkingDirectory $root -PassThru
        if (-not $process.WaitForExit(600000)) {
            try {
                Stop-Process -Id $process.Id -Force
            }
            catch {
                # Best-effort cleanup if the installer is still running.
            }
            throw "Installer did not exit within 600 seconds."
        }

        if ($process.ExitCode -ne 0) {
            throw "Installer exited with code $($process.ExitCode)."
        }
    }

    Invoke-Step "Verify installed runtime" {
        Invoke-Smoke @("--installed-runtime")
    }

    if ($RequireOpenWakeWord) {
        Invoke-Step "Verify installed runtime is using openWakeWord" {
            Test-InstalledRuntimeOpenWakeWord
        }
    }
}

if ($LiveActions) {
    Invoke-Step "Live Start menu launch check" {
        Invoke-Smoke @("--live-launch", "Notepad")
    }

    Invoke-Step "Live Chrome/browser check" {
        Invoke-Smoke @("--live-browser", "open crome to example dot com")
    }

    Invoke-Step "Live Explorer file-search check" {
        Invoke-Smoke @("--live-file-search", "Callsign")
    }

    Invoke-Step "Scripted gated Start menu session check" {
        Invoke-Smoke @("--scripted-session", "Callsign echo one open note pad")
    }

    Invoke-Step "Scripted gated Chrome/browser session check" {
        Invoke-Smoke @("--scripted-session", "Callsign echo one open crome to example dot com")
    }

    Invoke-Step "Scripted gated Explorer file-search session check" {
        Invoke-Smoke @("--scripted-session", "Callsign echo one find file Callsign")
    }

    Invoke-Step "Scripted gated dictation session check" {
        Invoke-Smoke @("--scripted-session", "Callsign echo one start dictation")
    }

    Invoke-Step "Ensure installed runtime verifier profile" {
        Clear-InstalledRuntimeControlRequests
        Ensure-AlphaVerifierProfile
    }

    Invoke-Step "Ensure installed user-runtime is listening" {
        Ensure-InstalledUserRuntime
        Clear-InstalledRuntimeActionHistory
    }

    Set-LiveActionCoverageStartedUtc

    Invoke-Step "Installed runtime scripted Start menu action history check" {
        Invoke-InstalledRuntimeTranscript -Transcript "Callsign echo one open note pad" -ExpectedKind "start_menu_launch"
    }

    Invoke-Step "Installed runtime scripted browser action history check" {
        Invoke-InstalledRuntimeTranscript -Transcript "Callsign echo one open crome to example dot com" -ExpectedKind "browser"
    }

    Invoke-Step "Installed runtime scripted Explorer file-search action history check" {
        Invoke-InstalledRuntimeTranscript -Transcript "Callsign echo one find file Callsign" -ExpectedKind "file_search"
    }

    Invoke-Step "Installed runtime scripted dictation action history check" {
        Invoke-InstalledRuntimeTranscript -Transcript "Callsign echo one start dictation" -ExpectedKind "dictation"
    }
}

if ($WatchVoiceAction) {
    Invoke-Step "Watch installed service for human-spoken voice action" {
        Invoke-Smoke @("--watch-service-action", "$WatchSeconds")
    }
}
else {
    Write-Host ""
    Write-Host "Manual final voice proof is still explicit." -ForegroundColor Yellow
    Write-Host "Run this, then speak: Callsign echo one open Notepad"
    Write-Host "  .\verifycallsign-alpha.ps1 -SkipBuild -Install -WatchVoiceAction -WatchSeconds $WatchSeconds"
    Write-Host ""
    Write-Host "To repair openWakeWord after install, open Callsign and click Account > Run Wake Setup."
    Write-Host "For command-line repair, install a clean custom model and Python dependencies:"
    Write-Host "  .\setupopenwakeword.ps1 -ModelPath C:\path\to\custom-callsign.onnx -InstallPythonPackages"
    Write-Host "After install, the same helper is staged beside the app:"
    Write-Host "  powershell -ExecutionPolicy Bypass -File `"$env:LOCALAPPDATA\Callsign\App\setupopenwakeword.ps1`" -ModelPath C:\path\to\custom-callsign.onnx -InstallPythonPackages"
    Write-Host "Or build a model-enabled installer with:"
    Write-Host "  .\verifycallsign-alpha.ps1 -WakeModelPath C:\path\to\custom-callsign.onnx -Install -RequireOpenWakeWord"
    Write-Host "Then verify with:"
    Write-Host "  .\verifycallsign-alpha.ps1 -RequireOpenWakeWord"
    Write-Host "For a release-gate readiness report, add:"
    Write-Host "  .\verifycallsign-alpha.ps1 -Install -LiveActions -RequireAlphaReady -ReportPath .\build\alpha-readiness.json"
}

if ($RequireAlphaReady) {
    Invoke-Step "Require alpha readiness gates" {
        Assert-AlphaReadiness
    }
}

Write-Host ""
Write-Host "Callsign alpha verification script completed." -ForegroundColor Green
Write-VerificationReport
if (-not [string]::IsNullOrWhiteSpace($ReportPath)) {
    Write-Host "Verification report written to: $ReportPath" -ForegroundColor Green
}
