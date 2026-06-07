# GitHub Pages Site

## Purpose

The `/docs` folder contains a static GitHub Pages site for Callsign.

The site includes:

- `index.html` landing page.
- `pages/*.html` generated documentation pages.
- `reference/*.md` source Markdown documentation.
- `assets/styles.css` and `assets/site.js`.
- `.nojekyll` to serve the static site directly.

## Publish from `/docs`

GitHub Pages can publish from a selected branch and either the repository root `/` or the `/docs` folder.

Repository setup:

1. Push repository to GitHub.
2. Open repository settings.
3. Select **Pages**.
4. Under **Build and deployment**, choose **Deploy from a branch**.
5. Select branch `main`.
6. Select folder `/docs`.
7. Save.

## Local preview

```bash
python -m http.server 8000 -d docs
```

Then open:

```text
http://localhost:8000
```

## Editing docs

Edit Markdown files in:

```text
docs/reference/
```

Then regenerate HTML pages:

```bash
python scripts/build_site.py
```

## No build dependencies

The generated site uses static HTML, CSS, and JavaScript. No npm, bundler, or remote assets are required.

## Site design notes

The site is intentionally simple:

- Fast static pages.
- Dark theme.
- Search/filter on the landing page.
- Mobile-friendly layout.
- No external tracking.
- No external fonts.
