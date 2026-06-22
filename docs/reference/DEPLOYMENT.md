# Deployment

## Current state

Callsign currently ships as a Windows-first alpha with a setup/onboarding app and service runtime.

The public package is the Free open-source core. Future paid Pro and Advanced capabilities may ship as closed-source extension libraries, but those libraries are not required for the current Free install path.

The update server and its Docker/Compose deployment now live in the separate local repo at `update-server-repo/`.

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

The script starts the Compose stack remotely and verifies the installer endpoint without storing SSH credentials in the repository.

The default remote path is `/home/aryns/callsign/website`, which avoids requiring sudo on the Pi.

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
