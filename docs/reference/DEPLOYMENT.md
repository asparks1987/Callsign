# Deployment

## Current state

Callsign currently ships as a Windows setup and onboarding app.

The alpha build flow is:

1. Run `buildcallsign.ps1`.
2. Publish the WinForms app.
3. Emit a launchable executable in the repo root.
4. Optionally build a real installer if Inno Setup is available.

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

```bash
python -m http.server 8000 -d docs
```

Open:

```text
http://localhost:8000
```

## Windows package layout

Current alpha output is intentionally small:

```text
Callsign-Run.exe
Callsign-Setup.exe
build/
docs/
```

Future app packaging can add:

- A real background service.
- A tray helper.
- Better update handling.
- Optional premium cloud-connected features.

## Linux roadmap

Linux packaging is a later phase and should not be treated as a current alpha promise.

