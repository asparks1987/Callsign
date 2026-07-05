param(
    [string]$RemoteHost = "aryns@172.16.120.5",
    [string]$RemoteRoot = "/home/aryns/callsign/website"
)

$ErrorActionPreference = "Stop"

function Invoke-Checked([scriptblock]$Script) {
    & $Script
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath "Callsign-Setup.exe")) {
    throw "Callsign-Setup.exe was not found in the repository root. Run .\buildcallsign.ps1 before deploying the website."
}

Write-Host "Regenerating static site..." -ForegroundColor Cyan
Invoke-Checked { python scripts/build_site.py }

Write-Host "Creating remote website directory..." -ForegroundColor Cyan
Invoke-Checked { ssh $RemoteHost "mkdir -p '$RemoteRoot'" }

Write-Host "Copying website deployment files to the Pi..." -ForegroundColor Cyan
Invoke-Checked { scp -r docs Dockerfile.website .dockerignore Callsign-Setup.exe deploy/website/docker-compose.remote.yml "${RemoteHost}:$RemoteRoot/" }

Write-Host "Starting Dockerized website on the Pi..." -ForegroundColor Cyan
Invoke-Checked { ssh $RemoteHost "cd '$RemoteRoot' && mv docker-compose.remote.yml docker-compose.yml && docker compose -f docker-compose.yml build --no-cache && docker compose -f docker-compose.yml up -d" }

Write-Host "Verifying remote website..." -ForegroundColor Cyan
Invoke-Checked { ssh $RemoteHost "curl -fsSI http://localhost:8085/downloads/Callsign-Setup.exe | head" }

Write-Host "Callsign website deployment finished." -ForegroundColor Green
