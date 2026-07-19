param(
    [string]$RemoteHost = "aryns@172.16.120.5",
    [string]$RemoteRoot = "/home/aryns/callsign/website",
    [string]$WebsiteDownloadUrl = "",
    [switch]$RequireWebsiteVerification,
    [switch]$LocalPreviewOnly,
    [switch]$SkipSiteBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$remoteInstallerUrl = $WebsiteDownloadUrl
$sshOptions = @('-o', 'ConnectTimeout=10', '-o', 'ServerAliveInterval=5', '-o', 'ServerAliveCountMax=1')
$scpOptions = @('-o', 'ConnectTimeout=10')

function Invoke-Checked([scriptblock]$Script) {
    & $Script
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE."
    }
}

function Test-TcpPort([string]$HostName, [int]$Port, [int]$TimeoutMs = 3000) {
    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $connectTask = $client.ConnectAsync($HostName, $Port)
        if (-not $connectTask.Wait($TimeoutMs)) {
            return $false
        }

        return $client.Connected
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

if (-not (Test-Path -LiteralPath (Join-Path $repoRoot "Callsign-Setup.exe"))) {
    throw "Callsign-Setup.exe was not found in the repository root. Run .\buildcallsign.ps1 before deploying the website."
}

if ($LocalPreviewOnly) {
    Write-Host "Running local website preview instead of Pi deploy..." -ForegroundColor Cyan
    if (-not $SkipSiteBuild) {
        Invoke-Checked { python (Join-Path $repoRoot 'scripts\build_site.py') }
    }
    Invoke-Checked { docker compose -f (Join-Path $repoRoot 'deploy\website\docker-compose.yml') up -d --build }

    $previewUrl = if ([string]::IsNullOrWhiteSpace($WebsiteDownloadUrl)) {
        "http://localhost:8085/downloads/Callsign-Setup.exe"
    }
    else {
        $WebsiteDownloadUrl
    }

    Write-Host "Verifying local website installer..." -ForegroundColor Cyan
    $previewArgs = @(
        '-Root', $repoRoot,
        '-SkipBuild',
        '-SkipSiteBuild',
        '-WebsiteDownloadUrl', $previewUrl
    )
    if ($RequireWebsiteVerification) {
        $previewArgs += '-RequireWebsiteVerification'
    }

    Invoke-Checked { powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\verify-release-readiness.ps1') @previewArgs }
    Write-Host "Callsign local website preview finished." -ForegroundColor Green
    return
}

$remoteHostName = ($RemoteHost -split '@')[-1]
if ($remoteHostName.StartsWith('[') -and $remoteHostName.EndsWith(']')) {
    $remoteHostName = $remoteHostName.TrimStart('[').TrimEnd(']')
}

if (-not (Test-TcpPort -HostName $remoteHostName -Port 22)) {
    throw "Remote host ${remoteHostName}:22 is not reachable. The Pi deploy cannot continue until SSH is available. For a local release-check loop, start the website stack with 'docker compose -f deploy/website/docker-compose.yml up -d --build' and verify 'http://localhost:8085/downloads/Callsign-Setup.exe' against the local Callsign-Setup.exe."
}

Write-Host "Regenerating static site..." -ForegroundColor Cyan
Invoke-Checked { python (Join-Path $repoRoot 'scripts\build_site.py') }

Write-Host "Creating remote website directory..." -ForegroundColor Cyan
Invoke-Checked { ssh @sshOptions $RemoteHost "mkdir -p '$RemoteRoot'" }

Write-Host "Copying website deployment files to the Pi..." -ForegroundColor Cyan
Invoke-Checked { scp @scpOptions -r (Join-Path $repoRoot 'docs') (Join-Path $repoRoot 'Dockerfile.website') (Join-Path $repoRoot '.dockerignore') (Join-Path $repoRoot 'Callsign-Setup.exe') (Join-Path $repoRoot 'deploy\website\docker-compose.remote.yml') "${RemoteHost}:$RemoteRoot/" }

Write-Host "Starting Dockerized website on the Pi..." -ForegroundColor Cyan
Invoke-Checked { ssh @sshOptions $RemoteHost "cd '$RemoteRoot' && mv docker-compose.remote.yml docker-compose.yml && docker compose -f docker-compose.yml build --no-cache && docker compose -f docker-compose.yml up -d" }

Write-Host "Verifying remote website..." -ForegroundColor Cyan
Invoke-Checked { ssh @sshOptions $RemoteHost "curl -fsSI http://localhost:8085/downloads/Callsign-Setup.exe | head" }

if ([string]::IsNullOrWhiteSpace($remoteInstallerUrl)) {
    $remoteInstallerUrl = "http://$remoteHostName:8085/downloads/Callsign-Setup.exe"
    Write-Host "[CALLSIGN DEPLOY] No WebsiteDownloadUrl provided; using $remoteInstallerUrl from the SSH target." -ForegroundColor Cyan
}

if (-not [string]::IsNullOrWhiteSpace($remoteInstallerUrl)) {
    Write-Host "Verifying public website installer hash..." -ForegroundColor Cyan
    $readinessArgs = @(
        '-Root', $repoRoot,
        '-SkipBuild',
        '-SkipSiteBuild',
        '-WebsiteDownloadUrl', $remoteInstallerUrl
    )
    if ($RequireWebsiteVerification) {
        $readinessArgs += '-RequireWebsiteVerification'
    }

    Invoke-Checked { powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\verify-release-readiness.ps1') @readinessArgs }
}

Write-Host "Callsign website deployment finished." -ForegroundColor Green
