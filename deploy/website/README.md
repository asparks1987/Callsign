# Callsign Website Docker Deployment

This Compose stack serves the generated Callsign public website on port `8085`.

The image includes:

- generated docs from `docs/`
- the offline Windows installer at `/downloads/Callsign-Setup.exe`
- links to documentation and source material on the homepage

## Build and run

From the Callsign repository root:

```powershell
python scripts/build_site.py
docker compose -f deploy/website/docker-compose.yml up -d --build
```

Open:

```text
http://localhost:8085
```

## Pi deployment

Copy the repository folder, including `Callsign-Setup.exe`, to the Pi and run:

```bash
docker compose -f deploy/website/docker-compose.yml up -d --build
```

The site is then available on:

```text
http://<pi-hostname-or-ip>:8085
```

Or deploy from this Windows workstation with:

```powershell
.\deploy\website\deploy-pi.ps1
```

The script regenerates the site, refreshes `docs/downloads/Callsign-Setup.exe` from the newest root installer, copies only the website assets and installer, rebuilds the website image on the Pi with `--no-cache`, starts Compose, and checks the installer URL. It intentionally does not store SSH passwords. It resolves the repo root from the script location, so you can launch it from any working directory inside the Callsign tree.

If `-WebsiteDownloadUrl` is omitted and a local preview is already listening on port `8085`, the release-readiness and release-packet scripts will automatically verify `http://localhost:8085/downloads/Callsign-Setup.exe` instead of skipping the download check. When the local preview container is available, release-readiness can use a direct container-side installer hash check instead of downloading the large file over HTTP.

If `-WebsiteDownloadUrl` is omitted during Pi deploy, the helper derives `http://<remote-host>:8085/downloads/Callsign-Setup.exe` from the SSH target and uses that for the public installer hash check.

The helper uses a short SSH connect timeout so an offline Pi fails quickly instead of hanging during deploy.

Pass `-WebsiteDownloadUrl` to make the helper verify the public installer endpoint after deployment with the same hash-comparison logic used by the release-readiness script. Add `-RequireWebsiteVerification` if you want that public download check to fail the deploy command instead of only reporting it.

If the Pi is temporarily offline, pass `-LocalPreviewOnly` to regenerate the site, rebuild the local website container, and verify the local `/downloads/Callsign-Setup.exe` endpoint without attempting SSH deployment.

The remote deployment uses `docker-compose.remote.yml`, which builds from the copied website folder on the Pi.

By default the script deploys to `/home/aryns/callsign/website` so it does not require sudo access.
