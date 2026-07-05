param(
    [string]$WebsiteDownloadUrl = '',
    [string]$ManualEvidencePath = '',
    [switch]$WriteManualEvidenceTemplate,
    [switch]$RunSmoke,
    [switch]$RequireWebsiteVerification,
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

$readinessScript = Join-Path $repoRoot 'scripts\verify-release-readiness.ps1'
$parityScript = Join-Path $repoRoot 'scripts\voice_access_parity_evidence.ps1'
$manualEvidenceTemplatePath = Join-Path $repoRoot 'build\voice-access-parity-manual-evidence.template.json'
$evidencePath = Join-Path $repoRoot 'build\voice-access-parity-evidence.json'
$summaryPath = Join-Path $repoRoot 'build\release-packet-summary.json'

Write-Host "[CALLSIGN PACKET] Repository root: $repoRoot" -ForegroundColor Cyan
Write-Host "[CALLSIGN PACKET] Preparing local release artifacts..." -ForegroundColor Cyan

Invoke-Script -Path $readinessScript -Arguments @(
    '-Root', $repoRoot,
    '-SkipWebsiteVerification'
)

$parityArgs = @(
    '-EvidencePath', $evidencePath,
    '-Root', $repoRoot
)
if (-not $PSBoundParameters.ContainsKey('RunSmoke') -or $RunSmoke) {
    $parityArgs += '-RunSmoke'
}
if ($WriteManualEvidenceTemplate) {
    $parityArgs += '-WriteManualEvidenceTemplate'
}
if (-not [string]::IsNullOrWhiteSpace($ManualEvidencePath)) {
    $parityArgs += @('-ManualEvidencePath', $ManualEvidencePath)
}

Invoke-Script -Path $parityScript -Arguments $parityArgs

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

if (Test-Path -LiteralPath $evidencePath) {
    $evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
    $evidenceInstaller = Get-PropertyValue $evidence 'installer'
    $summary = [pscustomobject]@{
        generated_utc = [DateTime]::UtcNow.ToString("o")
        installer = [pscustomobject]@{
            path = Join-Path $repoRoot 'Callsign-Setup.exe'
            sha256 = [string](Get-PropertyValue $evidenceInstaller 'sha256')
            size_bytes = [int64](Get-PropertyValue $evidenceInstaller 'size_bytes')
        }
        parity_evidence = [pscustomobject]@{
            path = $evidencePath
            passed = [bool](Get-PropertyValue $evidence 'passed')
            release_ready = [bool](Get-PropertyValue $evidence 'release_ready')
            release_blockers = @((Get-PropertyValue $evidence 'release_blockers'))
        }
        manual_template_path = $manualEvidenceTemplatePath
        website_download_url = $WebsiteDownloadUrl
        require_website_verification = [bool]$RequireWebsiteVerification
    }

    $summaryDir = Split-Path -Parent $summaryPath
    if (-not [string]::IsNullOrWhiteSpace($summaryDir)) {
        New-Item -ItemType Directory -Force -Path $summaryDir | Out-Null
    }

    $summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $summaryPath -Encoding UTF8
    Write-Host "[CALLSIGN PACKET] Summary written: $summaryPath" -ForegroundColor Green
}

Write-Host "[CALLSIGN PACKET] Release packet prepared." -ForegroundColor Green
Write-Host "[CALLSIGN PACKET] Evidence JSON: $evidencePath" -ForegroundColor Green
Write-Host "[CALLSIGN PACKET] Manual evidence template: $manualEvidenceTemplatePath" -ForegroundColor Green
Write-Host "[CALLSIGN PACKET] Packet summary: $summaryPath" -ForegroundColor Green
exit 0
