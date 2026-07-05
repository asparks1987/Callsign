param(
    [string]$WebsiteDownloadUrl = '',
    [string]$Root,
    [switch]$SkipBuild,
    [switch]$SkipSiteBuild,
    [switch]$SkipWebsiteVerification,
    [switch]$RequireWebsiteVerification
)

$ErrorActionPreference = 'Stop';
Set-StrictMode -Version Latest;

function Write-Step([string]$Message)
{
    Write-Host "[CALLSIGN READINESS] $Message" -ForegroundColor Cyan;
}

function Fail([string]$Message)
{
    Write-Host "[CALLSIGN READINESS] FAILED: $Message" -ForegroundColor Red;
}

$repoRoot = if ([string]::IsNullOrWhiteSpace($Root))
{
    Split-Path -Parent $PSScriptRoot
}
else
{
    $Root
}

$installerPath = Join-Path $repoRoot 'Callsign-Setup.exe';
$siteOutputDir = Join-Path $repoRoot 'docs';
$homeIndex = Join-Path $siteOutputDir 'index.html';
$localHash = '';
$hasFailure = $false;

Write-Step "Repository root: $repoRoot";
if (Test-Path -LiteralPath $installerPath)
{
    $localHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $installerPath).Hash.ToUpperInvariant();
    Write-Step "Local installer found: $installerPath ($((Get-Item $installerPath).Length) bytes, SHA-256 $localHash)";
}
else
{
    Write-Host "[CALLSIGN READINESS] LOCAL CHECK: Callsign-Setup.exe not found. Run buildcallsign.ps1 first." -ForegroundColor Yellow;
}

if (-not $SkipBuild)
{
    $localPackageCache = Join-Path $env:USERPROFILE '.nuget\packages';
    if (Test-Path -LiteralPath $localPackageCache)
    {
        Write-Step "Preparing win-x64 restore from local NuGet cache: $localPackageCache";
        Push-Location $repoRoot;
        try
        {
            dotnet restore 'src/Callsign.UI/Callsign.UI.csproj' --runtime win-x64 --source $localPackageCache -v minimal;
        }
        finally
        {
            Pop-Location;
        }

        if ($LASTEXITCODE -ne 0)
        {
            Fail "Runtime restore from local package cache returned exit code $LASTEXITCODE.";
            exit 1;
        }
    }
    else
    {
        Write-Host "[CALLSIGN READINESS] Local NuGet cache not found at $localPackageCache. buildcallsign.ps1 may need network package sources." -ForegroundColor Yellow;
    }

    Write-Step 'Running .\buildcallsign.ps1 -NoRestore';
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'buildcallsign.ps1') -NoRestore;
    if ($LASTEXITCODE -ne 0)
    {
        Fail "buildcallsign.ps1 returned exit code $LASTEXITCODE.";
        exit 1;
    }

    if (Test-Path -LiteralPath $installerPath)
    {
        $newHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $installerPath).Hash.ToUpperInvariant();
        Write-Step "Installer updated after build: SHA-256 $newHash";
    }
    else
    {
        Fail 'buildcallsign.ps1 completed without producing Callsign-Setup.exe.';
        exit 1;
    }
}

if (-not $SkipSiteBuild)
{
    Write-Step 'Rebuilding docs site with python scripts/build_site.py';
    Push-Location $repoRoot;
    try
    {
        python scripts/build_site.py;
    }
    finally
    {
        Pop-Location;
    }

    if (-not (Test-Path -LiteralPath $homeIndex))
    {
        Fail 'Generated docs site is missing docs/index.html.';
        $hasFailure = $true;
    }

    if (-not (Test-Path -LiteralPath (Join-Path $siteOutputDir 'pages/voice-access-parity.html')))
    {
        Fail 'Generated docs missing parity page at docs/pages/voice-access-parity.html.';
        $hasFailure = $true;
    }
}

if ($SkipWebsiteVerification)
{
    Write-Step 'Skipping website download verification by request.';
}
else
{
    if ([string]::IsNullOrWhiteSpace($WebsiteDownloadUrl))
    {
        Write-Host "[CALLSIGN READINESS] SKIP: No WebsiteDownloadUrl provided. Pass -WebsiteDownloadUrl to verify a public download endpoint." -ForegroundColor Yellow;
    }
    else
    {
        $tmp = Join-Path $env:TEMP "callsign-setup-remote-$(Get-Random).exe";
        try
        {
            Write-Step "Verifying remote installer from $WebsiteDownloadUrl";
            Invoke-WebRequest -Uri $WebsiteDownloadUrl -OutFile $tmp -UseBasicParsing -TimeoutSec 60;
            $remoteHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $tmp).Hash.ToUpperInvariant();
            $remoteLength = (Get-Item $tmp).Length;
            Write-Step "Remote installer downloaded: $remoteLength bytes, SHA-256 $remoteHash";

            if ([string]::IsNullOrWhiteSpace($localHash))
            {
                Write-Host '[CALLSIGN READINESS] Local installer hash unavailable for comparison.' -ForegroundColor Yellow;
                if ($RequireWebsiteVerification)
                {
                    $hasFailure = $true;
                }
            }
            else
            {
                if (-not $localHash.Equals($remoteHash, [System.StringComparison]::OrdinalIgnoreCase))
                {
                    Fail 'Local installer and website download hashes differ.';
                    $hasFailure = $true;
                }
                else
                {
                    Write-Step 'Website installer hash matches local Callsign-Setup.exe.';
                }
            }
        }
        catch
        {
            Fail "Unable to verify website download URL ($WebsiteDownloadUrl): $($_.Exception.Message)";
            $hasFailure = $true;
        }
        finally
        {
            if (Test-Path -LiteralPath $tmp)
            {
                Remove-Item -LiteralPath $tmp -Force;
            }
        }
    }
}

if ($hasFailure -and $RequireWebsiteVerification)
{
    Fail 'Release readiness blocked by required checks. Do not mark the run complete until resolved.';
    exit 1;
}

if ($hasFailure)
{
    Write-Host '[CALLSIGN READINESS] Non-fatal release checks failed.' -ForegroundColor Yellow;
    exit 2;
}

Write-Host '[CALLSIGN READINESS] Release readiness checks passed.' -ForegroundColor Green;
exit 0;
