param(
    [string]$WebsiteDownloadUrl = '',
    [string]$WebsiteInstallerHash = '',
    [Int64]$WebsiteInstallerSizeBytes = 0,
    [string]$ManualEvidencePath = '',
    [switch]$WriteManualEvidenceTemplate,
    [switch]$RunSmoke,
    [switch]$SkipSmoke,
    [switch]$RequireManualEvidence,
    [switch]$RequireWebsiteVerification,
    [switch]$LocalPreviewOnly,
    [string]$Root
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = if ([string]::IsNullOrWhiteSpace($Root)) {
    Split-Path -Parent $PSScriptRoot
}
else {
    $Root
}

function Invoke-Script {
    param(
        [string]$Path,
        [string[]]$Arguments
    )

    & powershell -NoProfile -ExecutionPolicy Bypass -File $Path @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Script failed with exit code ${LASTEXITCODE}: $Path"
    }
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

function Get-LocalWebsitePreviewInstallerInfo {
    param(
        [string]$InstallerPath,
        [string]$LocalPreviewInstallerPath = ''
    )

    if (-not [string]::IsNullOrWhiteSpace($LocalPreviewInstallerPath) -and (Test-Path -LiteralPath $LocalPreviewInstallerPath)) {
        $localPreviewItem = Get-Item -LiteralPath $LocalPreviewInstallerPath
        return [PSCustomObject]@{
            hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $LocalPreviewInstallerPath).Hash.ToUpperInvariant()
            sizeBytes = [Int64]$localPreviewItem.Length
        }
    }

    $docker = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $docker) {
        return $null
    }

    try {
        $containerHash = & $docker.Source exec callsign-website sha256sum $InstallerPath 2>$null
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($containerHash)) {
            return $null
        }

        $containerSize = & $docker.Source exec callsign-website stat -c '%s' $InstallerPath 2>$null
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($containerSize)) {
            return $null
        }

        $hashText = (($containerHash -split '\s+')[0]).ToUpperInvariant()
        $sizeText = $containerSize.Trim()
        $parsedSize = 0L
        if (-not [int64]::TryParse($sizeText, [ref]$parsedSize)) {
            return $null
        }

        return [PSCustomObject]@{
            hash = $hashText
            sizeBytes = $parsedSize
        }
    }
    catch {
        return $null
    }
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

function New-ReleaseGateSummary {
    @(
        [pscustomobject]@{
            id = 'C15.11'
            name = 'Run installed end-to-end automated checks'
            evidence_check_id = 'installed_end_to_end_automated_checks'
            status = 'manual_evidence_required'
            source = 'callsign-documentation-pack burndown'
        },
        [pscustomobject]@{
            id = 'C15.12'
            name = 'Run human-spoken core walkthrough'
            evidence_check_id = 'human_spoken_core_walkthrough'
            status = 'manual_evidence_required'
            source = 'callsign-documentation-pack burndown'
        },
        [pscustomobject]@{
            id = 'C15.13'
            name = 'Run failure-state walkthrough'
            evidence_check_id = 'failure_state_walkthrough'
            status = 'manual_evidence_required'
            source = 'callsign-documentation-pack burndown'
        },
        [pscustomobject]@{
            id = 'C15.14'
            name = 'Run clean Windows user or VM test'
            evidence_check_id = 'clean_windows_user_or_vm_test'
            status = 'manual_evidence_required'
            source = 'callsign-documentation-pack burndown'
        }
    )
}

$readinessScript = Join-Path $repoRoot 'scripts\verify-release-readiness.ps1'
$parityScript = Join-Path $repoRoot 'scripts\voice_access_parity_evidence.ps1'
$walkthroughEvidencePath = Join-Path $repoRoot 'build\alpha-v1-walkthrough-evidence.json'
$manualEvidenceTemplatePath = Join-Path $repoRoot 'build\voice-access-parity-manual-evidence.template.json'
$evidencePath = Join-Path $repoRoot 'build\voice-access-parity-evidence.json'
$summaryPath = Join-Path $repoRoot 'build\release-packet-summary.json'
$siteInstallerPath = Join-Path $repoRoot 'docs\downloads\Callsign-Setup.exe'
$releaseMode = 'pi-deploy'
$packetStatus = 'success'
$packetError = ''

function Write-PacketSummary {
    param(
        [string]$EvidencePath,
        [string]$SummaryPath,
        [string]$RepoRoot,
        [string]$ManualEvidenceTemplatePath,
        [string]$WalkthroughEvidencePath,
        [string]$ReleaseMode,
        [string]$WebsiteDownloadUrl,
        [bool]$WebsiteDownloadUrlWasInferred,
        [string]$WebsiteInstallerHash,
        [Int64]$WebsiteInstallerSizeBytes,
        [bool]$RequireWebsiteVerification,
        [bool]$RequireManualEvidence,
        [string]$PacketStatus,
        [string]$PacketError
    )

    $evidence = $null
    if (Test-Path -LiteralPath $EvidencePath) {
        $evidence = Get-Content -LiteralPath $EvidencePath -Raw | ConvertFrom-Json
    }

    $evidenceInstaller = Get-PropertyValue $evidence 'installer'
    $evidenceReleaseBlockerSummary = Get-PropertyValue $evidence 'release_blocker_summary'
    $summary = [pscustomobject]@{
        generated_utc = [DateTime]::UtcNow.ToString("o")
        packet_status = $PacketStatus
        packet_error = $PacketError
        installer = [pscustomobject]@{
            path = Join-Path $RepoRoot 'Callsign-Setup.exe'
            sha256 = [string](Get-PropertyValue $evidenceInstaller 'sha256')
            size_bytes = [int64](Get-PropertyValue $evidenceInstaller 'size_bytes')
        }
        parity_evidence = [pscustomobject]@{
            path = $EvidencePath
            passed = if ($null -ne $evidence) { [bool](Get-PropertyValue $evidence 'passed') } else { $false }
            release_ready = if ($null -ne $evidence) { [bool](Get-PropertyValue $evidence 'release_ready') } else { $false }
            release_blockers = if ($null -ne $evidence) { @((Get-PropertyValue $evidence 'release_blockers')) } else { @($PacketError) }
            release_blocker_summary = if ($null -ne $evidenceReleaseBlockerSummary) {
                $evidenceReleaseBlockerSummary
            }
            else {
                [pscustomobject]@{
                    manual_evidence_supplied = $false
                    manual_checks_remaining_count = 0
                    manual_categories_missing_count = 0
                    failed_automated_checks_count = 0
                    blocker_count = if ([string]::IsNullOrWhiteSpace($PacketError)) { 0 } else { 1 }
                    next_action = if ([string]::IsNullOrWhiteSpace($PacketError)) {
                        "Run voice_access_parity_evidence.ps1 to generate parity evidence."
                    }
                    else {
                        $PacketError
                    }
                }
            }
        }
        release_proof = [pscustomobject]@{
            local_installer_path = Join-Path $RepoRoot 'Callsign-Setup.exe'
            local_installer_sha256 = [string](Get-PropertyValue $evidenceInstaller 'sha256')
            local_installer_size_bytes = [int64](Get-PropertyValue $evidenceInstaller 'size_bytes')
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
            walkthrough_discovery_summary = "Browser overlay helper discovery, including Voice Help, Visible Controls, Show Numbers, Show Grid, Show Keyboard, and Open Checklist, stays visible from the startup walkthrough."
        }
        manual_template_path = $ManualEvidenceTemplatePath
        manual_checklist_path = Join-Path (Split-Path -Parent $ManualEvidenceTemplatePath) 'voice-access-parity-manual-evidence.checklist.md'
        walkthrough_evidence_path = $WalkthroughEvidencePath
        release_gates = @(New-ReleaseGateSummary)
        release_mode = $ReleaseMode
        require_manual_evidence = [bool]$RequireManualEvidence
        website_download_url = $WebsiteDownloadUrl
        website_download_url_was_inferred = [bool]$WebsiteDownloadUrlWasInferred
        website_installer_sha256 = $WebsiteInstallerHash
        website_installer_size_bytes = $WebsiteInstallerSizeBytes
        require_website_verification = [bool]$RequireWebsiteVerification
    }

    $summaryDir = Split-Path -Parent $SummaryPath
    if (-not [string]::IsNullOrWhiteSpace($summaryDir)) {
        New-Item -ItemType Directory -Force -Path $summaryDir | Out-Null
    }

    $summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $SummaryPath -Encoding UTF8
    Write-Host "[CALLSIGN PACKET] Summary written: $SummaryPath" -ForegroundColor Green
}

try {
    Write-Host "[CALLSIGN PACKET] Repository root: $repoRoot" -ForegroundColor Cyan
    Write-Host "[CALLSIGN PACKET] Preparing local release artifacts..." -ForegroundColor Cyan

    Invoke-Script -Path $readinessScript -Arguments @(
        '-Root', $repoRoot,
        '-SkipWebsiteVerification'
    )

    if ($LocalPreviewOnly) {
        $releaseMode = 'local-preview'
        Write-PacketSummary `
            -EvidencePath $evidencePath `
            -SummaryPath $summaryPath `
            -RepoRoot $repoRoot `
            -ManualEvidenceTemplatePath $manualEvidenceTemplatePath `
            -WalkthroughEvidencePath $walkthroughEvidencePath `
            -ReleaseMode $releaseMode `
            -WebsiteDownloadUrl $WebsiteDownloadUrl `
            -WebsiteDownloadUrlWasInferred ([bool]$WebsiteDownloadUrlWasInferred) `
            -WebsiteInstallerHash $WebsiteInstallerHash `
            -WebsiteInstallerSizeBytes $WebsiteInstallerSizeBytes `
            -RequireWebsiteVerification ([bool]$RequireWebsiteVerification) `
            -RequireManualEvidence ([bool]$RequireManualEvidence) `
            -PacketStatus $packetStatus `
            -PacketError $packetError
    }

    Write-Host "[CALLSIGN PACKET] Running alpha walkthrough checklist..." -ForegroundColor Cyan
    $checklistArgs = @('-Verify')
    if (-not $SkipSmoke) {
        $checklistArgs += '-RunSmoke'
    }
    Invoke-Script -Path (Join-Path $repoRoot 'scripts\alpha_v1_checklist.ps1') -Arguments $checklistArgs

    if ($LocalPreviewOnly) {
        Write-Host "[CALLSIGN PACKET] Starting local website preview..." -ForegroundColor Cyan
        $localWebsitePreview = Get-LocalWebsitePreviewInstallerInfo -InstallerPath '/usr/share/nginx/html/downloads/Callsign-Setup.exe' -LocalPreviewInstallerPath $siteInstallerPath
        if ($null -eq $localWebsitePreview) {
            Invoke-Script -Path (Join-Path $repoRoot 'deploy\website\deploy-pi.ps1') -Arguments @(
                '-LocalPreviewOnly',
                '-SkipSiteBuild',
                '-RequireWebsiteVerification'
            )
            $localWebsitePreview = Get-LocalWebsitePreviewInstallerInfo -InstallerPath '/usr/share/nginx/html/downloads/Callsign-Setup.exe' -LocalPreviewInstallerPath $siteInstallerPath
        }
        else {
            Write-Host "[CALLSIGN PACKET] Using generated local website installer for preview proof: $siteInstallerPath" -ForegroundColor Cyan
        }

        if ([string]::IsNullOrWhiteSpace($WebsiteDownloadUrl)) {
            $WebsiteDownloadUrl = 'http://localhost:8085/downloads/Callsign-Setup.exe'
            $script:WebsiteDownloadUrlWasInferred = $true
        }

        if ($null -ne $localWebsitePreview) {
            $WebsiteInstallerHash = [string]$localWebsitePreview.hash
            $WebsiteInstallerSizeBytes = [int64]$localWebsitePreview.sizeBytes
            Write-Host "[CALLSIGN PACKET] Local website preview installer: $WebsiteInstallerSizeBytes bytes, SHA-256 $WebsiteInstallerHash" -ForegroundColor Cyan
        }
    }

    $parityArgs = @(
        '-EvidencePath', $evidencePath,
        '-Root', $repoRoot
    )
    if (-not $SkipSmoke -and (-not $PSBoundParameters.ContainsKey('RunSmoke') -or $RunSmoke)) {
        $parityArgs += '-RunSmoke'
    }
    if ($WriteManualEvidenceTemplate) {
        $parityArgs += '-WriteManualEvidenceTemplate'
    }
    if ($RequireManualEvidence) {
        $parityArgs += '-RequireManualEvidence'
    }
    if (-not [string]::IsNullOrWhiteSpace($ManualEvidencePath)) {
        $parityArgs += @('-ManualEvidencePath', $ManualEvidencePath)
    }
    if (-not [string]::IsNullOrWhiteSpace($releaseMode)) {
        $parityArgs += @('-ReleaseMode', $releaseMode)
    }
    if (-not [string]::IsNullOrWhiteSpace($WebsiteDownloadUrl)) {
        $parityArgs += @('-WebsiteDownloadUrl', $WebsiteDownloadUrl)
    }
    if (-not [string]::IsNullOrWhiteSpace($WebsiteInstallerHash)) {
        $parityArgs += @('-WebsiteInstallerHash', $WebsiteInstallerHash)
    }
    if ($WebsiteInstallerSizeBytes -gt 0) {
        $parityArgs += @('-WebsiteInstallerSizeBytes', $WebsiteInstallerSizeBytes)
    }

    Invoke-Script -Path $parityScript -Arguments $parityArgs

    $manualChecklistPath = Join-Path (Split-Path -Parent $manualEvidenceTemplatePath) 'voice-access-parity-manual-evidence.checklist.md'
    if (($WriteManualEvidenceTemplate -or $RequireManualEvidence) -and -not (Test-Path -LiteralPath $manualChecklistPath)) {
        throw "Expected manual evidence checklist companion at $manualChecklistPath after generating the manual evidence template."
    }

    if (-not [string]::IsNullOrWhiteSpace($WebsiteDownloadUrl) -or $RequireWebsiteVerification) {
        Write-Host "[CALLSIGN PACKET] Verifying website download URL..." -ForegroundColor Cyan
        $readinessArgs = @(
            '-Root', $repoRoot,
            '-SkipBuild',
            '-SkipSiteBuild'
        )
        if (-not [string]::IsNullOrWhiteSpace($WebsiteDownloadUrl)) {
            $readinessArgs += @('-WebsiteDownloadUrl', $WebsiteDownloadUrl)
        }
        if ($RequireWebsiteVerification) {
            $readinessArgs += '-RequireWebsiteVerification'
        }

        Invoke-Script -Path $readinessScript -Arguments $readinessArgs
    }

    Write-PacketSummary `
        -EvidencePath $evidencePath `
        -SummaryPath $summaryPath `
        -RepoRoot $repoRoot `
        -ManualEvidenceTemplatePath $manualEvidenceTemplatePath `
        -WalkthroughEvidencePath $walkthroughEvidencePath `
        -ReleaseMode $releaseMode `
        -WebsiteDownloadUrl $WebsiteDownloadUrl `
        -WebsiteDownloadUrlWasInferred ([bool]$WebsiteDownloadUrlWasInferred) `
        -WebsiteInstallerHash $WebsiteInstallerHash `
        -WebsiteInstallerSizeBytes $WebsiteInstallerSizeBytes `
        -RequireWebsiteVerification ([bool]$RequireWebsiteVerification) `
        -RequireManualEvidence ([bool]$RequireManualEvidence) `
        -PacketStatus $packetStatus `
        -PacketError $packetError

    Write-Host "[CALLSIGN PACKET] Release packet prepared." -ForegroundColor Green
    Write-Host "[CALLSIGN PACKET] Evidence JSON: $evidencePath" -ForegroundColor Green
    Write-Host "[CALLSIGN PACKET] Manual evidence template: $manualEvidenceTemplatePath" -ForegroundColor Green
    Write-Host "[CALLSIGN PACKET] Packet summary: $summaryPath" -ForegroundColor Green
    exit 0
}
catch {
    $packetStatus = 'failed'
    $packetError = $_.Exception.Message
    Write-PacketSummary `
        -EvidencePath $evidencePath `
        -SummaryPath $summaryPath `
        -RepoRoot $repoRoot `
        -ManualEvidenceTemplatePath $manualEvidenceTemplatePath `
        -ReleaseMode $releaseMode `
        -WebsiteDownloadUrl $WebsiteDownloadUrl `
        -WebsiteDownloadUrlWasInferred ([bool]$WebsiteDownloadUrlWasInferred) `
        -WebsiteInstallerHash $WebsiteInstallerHash `
        -WebsiteInstallerSizeBytes $WebsiteInstallerSizeBytes `
        -RequireWebsiteVerification ([bool]$RequireWebsiteVerification) `
        -RequireManualEvidence ([bool]$RequireManualEvidence) `
        -PacketStatus $packetStatus `
        -PacketError $packetError
    throw
}

