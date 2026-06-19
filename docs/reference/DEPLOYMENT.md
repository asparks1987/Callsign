# Deployment

## Current state

Callsign currently ships as a Windows-first alpha with a setup/onboarding app and service runtime.

The public package is the Free open-source core. Future paid Pro and Advanced capabilities may ship as closed-source extension libraries, but those libraries are not required for the current Free install path.

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
