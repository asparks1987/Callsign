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

The script regenerates the site, copies only the website assets and installer, rebuilds the website image on the Pi with `--no-cache`, starts Compose, and checks the installer URL. It intentionally does not store SSH passwords.

The remote deployment uses `docker-compose.remote.yml`, which builds from the copied website folder on the Pi.

By default the script deploys to `/home/aryns/callsign/website` so it does not require sudo access.
