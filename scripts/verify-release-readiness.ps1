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

function Get-LocalWebsitePreviewInstallerHash(
    [string]$InstallerPath,
    [string]$LocalPreviewInstallerPath = ''
)
{
    if (-not [string]::IsNullOrWhiteSpace($LocalPreviewInstallerPath) -and (Test-Path -LiteralPath $LocalPreviewInstallerPath))
    {
        $localPreviewItem = Get-Item -LiteralPath $LocalPreviewInstallerPath;
        return [PSCustomObject]@{
            hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $LocalPreviewInstallerPath).Hash.ToUpperInvariant()
            sizeBytes = [Int64]$localPreviewItem.Length
        };
    }

    $docker = Get-Command docker -ErrorAction SilentlyContinue;
    if (-not $docker)
    {
        return $null;
    }

    try
    {
        $containerHash = & $docker.Source exec callsign-website sha256sum $InstallerPath 2>$null;
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($containerHash))
        {
            return $null;
        }

        $containerSize = & $docker.Source exec callsign-website stat -c '%s' $InstallerPath 2>$null;
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($containerSize))
        {
            return $null;
        }

        $hashText = (($containerHash -split '\s+')[0]).ToUpperInvariant();
        $sizeText = $containerSize.Trim();
        $parsedSize = 0L;
        if (-not [int64]::TryParse($sizeText, [ref]$parsedSize))
        {
            return $null;
        }

        return [PSCustomObject]@{
            hash = $hashText
            sizeBytes = $parsedSize
        };
    }
    catch
    {
        return $null;
    }
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
$siteInstallerPath = Join-Path $siteOutputDir 'downloads\Callsign-Setup.exe';
$homeIndex = Join-Path $siteOutputDir 'index.html';
$manualEvidenceTemplatePath = Join-Path $repoRoot 'build\voice-access-parity-manual-evidence.template.json';
$manualEvidenceChecklistPath = Join-Path $repoRoot 'build\voice-access-parity-manual-evidence.checklist.md';
$manualProofNotesFolder = Join-Path $repoRoot 'build\manual-proof-notes';
$releasePacketSummaryPath = Join-Path $repoRoot 'build\release-packet-summary.json';
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

    if (Test-Path -LiteralPath $manualEvidenceTemplatePath)
    {
        Write-Step 'Validating the manual checklist companion and proof-note folder beside the generated manual evidence template.';
        if (-not (Test-Path -LiteralPath $manualEvidenceChecklistPath))
        {
            Fail "Generated manual evidence template exists but the checklist companion is missing: $manualEvidenceChecklistPath.";
            $hasFailure = $true;
        }
        else
        {
            try
            {
                $manualEvidenceTemplate = Get-Content -LiteralPath $manualEvidenceTemplatePath -Raw | ConvertFrom-Json;
                if ([string]$manualEvidenceTemplate.schema -ne 'callsign.voice_access_parity.manual_evidence.v1')
                {
                    Fail "Generated manual evidence template has an unexpected schema: $($manualEvidenceTemplate.schema).";
                    $hasFailure = $true;
                }

                if ($null -eq $manualEvidenceTemplate.evidence_header)
                {
                    Fail 'Generated manual evidence template is missing the evidence_header block.';
                    $hasFailure = $true;
                }

                $templateChecks = @($manualEvidenceTemplate.checks);
                if ($templateChecks.Count -lt 40)
                {
                    Fail "Generated manual evidence template should contain at least 40 manual/live checks, but found $($templateChecks.Count): $manualEvidenceTemplatePath.";
                    $hasFailure = $true;
                }
            }
            catch
            {
                Fail "Generated manual evidence template is unreadable or malformed: $($_.Exception.Message)";
                $hasFailure = $true;
            }

            try
            {
                $manualChecklistText = Get-Content -LiteralPath $manualEvidenceChecklistPath -Raw;
                $requiredChecklistFragments = @(
                    '# Callsign Manual Evidence Checklist',
                    '## Evidence header',
                    '- Commit:',
                    '- Build ID:',
                    '- Artifact hashes:',
                    '- Wake/identity/transcription models:',
                    '- Result: [ ] Pass  [ ] Fail  [ ] Blocked',
                    '- Observed result:',
                    '- Evidence paths:',
                    '- Sensitive-data review: [ ] no raw audio',
                    '- Remaining uncertainty:',
                    '- Release recommendation:'
                );

                $missingChecklistFragments = @(
                    foreach ($fragment in $requiredChecklistFragments)
                    {
                        if ($manualChecklistText.IndexOf($fragment, [System.StringComparison]::OrdinalIgnoreCase) -lt 0)
                        {
                            $fragment;
                        }
                    }
                );

                if ($missingChecklistFragments.Count -gt 0)
                {
                    Fail "Generated manual evidence checklist is missing required release-proof fields: $($missingChecklistFragments -join ', ').";
                    $hasFailure = $true;
                }

                $checklistSectionCount = [Math]::Max(0, [regex]::Matches($manualChecklistText, '(?m)^##\s+').Count - 1);
                if ($checklistSectionCount -lt 40)
                {
                    Fail "Generated manual evidence checklist should contain at least 40 check sections, but found $($checklistSectionCount): $manualEvidenceChecklistPath.";
                    $hasFailure = $true;
                }
            }
            catch
            {
                Fail "Generated manual evidence checklist is unreadable or malformed: $($_.Exception.Message)";
                $hasFailure = $true;
            }
        }

        if (-not (Test-Path -LiteralPath $manualProofNotesFolder))
        {
            Fail "Generated manual evidence template exists but the proof-note folder is missing: $manualProofNotesFolder.";
            $hasFailure = $true;
        }
        else
        {
            $manualProofNotes = @(Get-ChildItem -LiteralPath $manualProofNotesFolder -Filter '*.md' -File -ErrorAction SilentlyContinue);
            if ($manualProofNotes.Count -lt 40)
            {
                Fail "Generated manual proof-note folder should contain at least 40 markdown notes, but found $($manualProofNotes.Count): $manualProofNotesFolder.";
                $hasFailure = $true;
            }
            else
            {
                $requiredProofNoteFragments = @(
                    '# Callsign Manual Proof Note',
                    '- Check:',
                    '## Evidence Command',
                    '## Expected Result',
                    '## Observed Result',
                    '## Artifact References',
                    '## Privacy Review',
                    '## Remaining Uncertainty',
                    '## Release Recommendation',
                    '- [ ] pass',
                    '- [ ] fail',
                    '- [ ] blocked'
                );

                $invalidProofNotes = New-Object System.Collections.Generic.List[string];
                foreach ($manualProofNote in $manualProofNotes)
                {
                    try
                    {
                        $manualProofNoteText = Get-Content -LiteralPath $manualProofNote.FullName -Raw;
                        $missingFragments = @(
                            foreach ($fragment in $requiredProofNoteFragments)
                            {
                                if ($manualProofNoteText.IndexOf($fragment, [System.StringComparison]::OrdinalIgnoreCase) -lt 0)
                                {
                                    $fragment;
                                }
                            }
                        );

                        if ($missingFragments.Count -gt 0)
                        {
                            [void]$invalidProofNotes.Add("$($manualProofNote.Name): missing $($missingFragments -join ', ')");
                        }
                    }
                    catch
                    {
                        [void]$invalidProofNotes.Add("$($manualProofNote.Name): unreadable ($($_.Exception.Message))");
                    }
                }

                if ($invalidProofNotes.Count -gt 0)
                {
                    Fail "Generated manual proof-note folder contains malformed notes: $($invalidProofNotes -join '; ').";
                    $hasFailure = $true;
                }
                else
                {
                    Write-Step "Manual proof-note folder validates at $manualProofNotesFolder ($($manualProofNotes.Count) markdown files with required evidence sections).";
                }
            }
        }
    }

    if (Test-Path -LiteralPath $releasePacketSummaryPath)
    {
        try
        {
            $releasePacketSummary = Get-Content -LiteralPath $releasePacketSummaryPath -Raw | ConvertFrom-Json;
            $summaryChecklistPath = [string]$releasePacketSummary.manual_checklist_path;
            if (-not [string]::IsNullOrWhiteSpace($summaryChecklistPath) -and -not (Test-Path -LiteralPath $summaryChecklistPath))
            {
                Fail "Release packet summary points to a missing manual checklist: $summaryChecklistPath.";
                $hasFailure = $true;
            }
            elseif (-not [string]::IsNullOrWhiteSpace($summaryChecklistPath))
            {
                Write-Step "Release packet summary validates the manual checklist companion at $summaryChecklistPath.";
            }
        }
        catch
        {
            Fail "Unable to inspect release packet summary for manual checklist validation: $($_.Exception.Message)";
            $hasFailure = $true;
        }
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
        $localWebsiteUrl = 'http://localhost:8085/downloads/Callsign-Setup.exe';
        $localWebsiteReachable = $false;

        try
        {
            $localWebsiteReachable = [bool](Test-NetConnection -ComputerName 'localhost' -Port 8085 -InformationLevel Quiet);
        }
        catch
        {
            $localWebsiteReachable = $false;
        }

        if ($localWebsiteReachable)
        {
            $WebsiteDownloadUrl = $localWebsiteUrl;
            Write-Step "No WebsiteDownloadUrl provided. Using local website preview at $WebsiteDownloadUrl.";
        }
        else
        {
            Write-Host "[CALLSIGN READINESS] SKIP: No WebsiteDownloadUrl provided and localhost:8085 is not reachable. Pass -WebsiteDownloadUrl to verify a public download endpoint." -ForegroundColor Yellow;
            if ($RequireWebsiteVerification)
            {
                Fail 'Website verification is required, but no WebsiteDownloadUrl was provided and the local website preview is not reachable.';
                $hasFailure = $true;
            }
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($WebsiteDownloadUrl))
    {
        $tmp = $null;
        try
        {
            $websiteUri = [uri]$WebsiteDownloadUrl;
            if ($websiteUri.Host -in @('localhost', '127.0.0.1'))
            {
                $previewHash = Get-LocalWebsitePreviewInstallerHash -InstallerPath '/usr/share/nginx/html/downloads/Callsign-Setup.exe' -LocalPreviewInstallerPath $siteInstallerPath;
                if ($null -ne $previewHash)
                {
                    Write-Step "Verifying local website preview installer: $($previewHash.sizeBytes) bytes, SHA-256 $($previewHash.hash)";
                    if ([string]::IsNullOrWhiteSpace($localHash))
                    {
                        Write-Host '[CALLSIGN READINESS] Local installer hash unavailable for comparison.' -ForegroundColor Yellow;
                        if ($RequireWebsiteVerification)
                        {
                            $hasFailure = $true;
                        }
                    }
                    elseif (-not $localHash.Equals($previewHash.hash, [System.StringComparison]::OrdinalIgnoreCase))
                    {
                        Fail 'Local installer and website preview hashes differ.';
                        $hasFailure = $true;
                    }
                    else
                    {
                        Write-Step 'Website installer hash matches local Callsign-Setup.exe.';
                    }

                    if ($hasFailure)
                    {
                        throw 'Local website preview verification failed.';
                    }

                    return;
                }
            }

            $tmp = Join-Path $env:TEMP "callsign-setup-remote-$(Get-Random).exe";
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
            if (-not [string]::IsNullOrWhiteSpace($tmp) -and (Test-Path -LiteralPath $tmp))
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
