# Deployment

## Current state

Callsign currently ships as a Windows-first alpha with a setup/onboarding app and service runtime.

The public package is the Free open-source core. Future paid Pro and Advanced capabilities may ship as closed-source extension libraries, but those libraries are not required for the current Free install path.

The update server and its Docker/Compose deployment now live in the separate local repo at `update-server-repo/`, along with the WinForms Update Manager used to publish release manifests and deploy the stack.

The public website can also run as a Dockerized static site on port `8085`. The website image serves generated docs from `docs/` and the offline installer at `/downloads/Callsign-Setup.exe`.

## Alpha build flow

From the repository root:

```powershell
.\buildcallsign.ps1
dotnet run --project tests/Callsign.AlphaSmoke/Callsign.AlphaSmoke.csproj
python scripts/build_site.py
```

The build currently produces or refreshes:

- `Callsign-Setup.exe`
- `Callsign-Run.exe`
- published UI/service binaries under `build/`
- generated static documentation under `docs/`
- `docs/downloads/Callsign-Setup.exe` for the website installer endpoint

## GitHub Pages deployment

The repository is arranged for GitHub Pages from `docs/`.

Steps:

1. Push the repository to GitHub.
2. Open repository settings.
3. Open Pages.
4. Choose deploy from branch.
5. Select the branch and the `docs/` folder.
6. Save.

The landing page is:

```text
docs/index.html
```

## Local site preview

From the repository root:

```powershell
python -m http.server 8000 --directory docs
```

Open:

```text
http://localhost:8000
```

## Docker website preview

From the repository root:

```powershell
python scripts/build_site.py
docker compose -f deploy/website/docker-compose.yml up -d --build
```

Open:

```text
http://localhost:8085
```

For Pi deployment, copy the repository folder including `Callsign-Setup.exe` to the Pi and run the same Compose command. The generated site links to the offline installer at:

```text
http://<pi-hostname-or-ip>:8085/downloads/Callsign-Setup.exe
```

From the Windows workstation, the helper script can copy only the generated website, Dockerfile, Compose file, and installer to the Pi:

```powershell
.\deploy\website\deploy-pi.ps1
```

The script rebuilds the website image on the Pi with `--no-cache`, starts the Compose stack remotely, and verifies the installer endpoint without storing SSH credentials in the repository. It resolves the repo root from the script path, so it can be launched from any working directory inside the Callsign tree.

If `-WebsiteDownloadUrl` is omitted and a local preview is already listening on port `8085`, the release-readiness and release-packet scripts automatically verify `http://localhost:8085/downloads/Callsign-Setup.exe` instead of skipping the download check. When `-RequireWebsiteVerification` is present, missing both an explicit website URL and a reachable local preview is a hard release blocker. When the local preview container is running, release-readiness can use a direct container-side installer hash check instead of downloading the large file over HTTP.

The Pi deploy helper also derives `http://<remote-host>:8085/downloads/Callsign-Setup.exe` from the SSH target when `-WebsiteDownloadUrl` is omitted, so the public installer hash check runs automatically after deployment.

The helper uses a short SSH connect timeout, so an offline Pi fails quickly instead of hanging during deploy.

If the Pi is temporarily offline, pass `-LocalPreviewOnly` to regenerate the site, rebuild the local website container, and verify the local `/downloads/Callsign-Setup.exe` endpoint without attempting SSH deployment.

The same `-LocalPreviewOnly` switch is available on `scripts/prepare-release-packet.ps1` so release packet generation can still complete a local website verification loop when the Pi is unavailable. When `docs/downloads/Callsign-Setup.exe` already exists, the packet flow hashes that generated website installer directly and avoids requiring Docker just to prove the local-preview artifact; otherwise it passes `-SkipSiteBuild` to the local preview deploy helper because the site has already been rebuilt by the readiness step.

Pass `-RequireManualEvidence` to `scripts/prepare-release-packet.ps1` when you need the packet to fail hard unless a completed manual parity-evidence file is supplied. The packet summary records whether that gate was requested so release artifacts can show whether the run was evidence-only or release-candidate strict.

The packet summary is written on any failure path, including readiness failures, so operators still get a failure artifact with the packet status and error text.

The default remote path is `/home/aryns/callsign/website`, which avoids requiring sudo on the Pi.

You can also pass `-WebsiteDownloadUrl` to the deploy helper so it verifies the public installer hash after the remote stack comes up, and `-RequireWebsiteVerification` if the deployment should fail when that public download check cannot be completed.

## Package boundary

The Free package should include:

- setup/onboarding app
- background service runtime
- wake/identity runtime dependencies needed for Free
- `callsign.gif` overlay asset
- local profile/enrollment storage
- docs and license material

The Free package should not require:

- paid account login
- private command packs
- entitlement secrets
- closed-source libraries

## Future extension packaging

Beta-or-later packaging may add:

- extension discovery
- signed extension validation
- Pro/Advanced entitlement checks
- closed-source command libraries
- extension update channels
- clearer tier labeling in UI

Closed-source material belongs in `/closed-source/` during local development and must not be committed to the public repo.
