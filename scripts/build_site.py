from pathlib import Path
import html
import re
import shutil

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "docs"
REF = DOCS / "reference"
PAGES = DOCS / "pages"
ASSETS = DOCS / "assets"
PAGES.mkdir(parents=True, exist_ok=True)
ASSETS.mkdir(parents=True, exist_ok=True)

GIF_SOURCE = ROOT / "callsign.gif"
if GIF_SOURCE.exists():
    shutil.copy2(GIF_SOURCE, ASSETS / "callsign.gif")

DOC_ORDER = [
    ("Canon", "CANON.md", "canon.html", "The Callsign product book: mission, promise, alpha ladder, and UX bar."),
    ("Product Spec", "PRODUCT_SPEC.md", "product-spec.html", "Alpha v1.0 scope, release ladder, and product principles."),
    ("Tier Architecture", "TIER_ARCHITECTURE.md", "tier-architecture.html", "Free installer, Pro upgrade path, and continuously updated command library."),
    ("Architecture", "ARCHITECTURE.md", "architecture.html", "Service runtime, setup UI, wake overlay, and staged alpha architecture."),
    ("MCP Tools", "MCP_TOOLS.md", "mcp-tools.html", "Future automation contract and tool design."),
    ("Windows Automation", "WINDOWS_AUTOMATION.md", "windows-automation.html", "v1.0 Start menu launch and v1.3 system control direction."),
    ("Security Model", "SECURITY_MODEL.md", "security-model.html", "Identity, local data handling, overlay behavior, and blocked actions."),
    ("Threat Model", "THREAT_MODEL.md", "threat-model.html", "Threats and mitigations for alpha service control."),
    ("Voice UX", "VOICE_UX.md", "voice-ux.html", "Wake word, callsign identity, overlay readout, and launch prompts."),
    ("Data Model", "DATA_MODEL.md", "data-model.html", "Profile storage, enrollment state, and launch history."),
    ("Test Plan", "TEST_PLAN.md", "test-plan.html", "v1.0 service, overlay, identity, launch, and safety checks."),
    ("Deployment", "DEPLOYMENT.md", "deployment.html", "Current build flow, GitHub Pages, and packaging notes."),
    ("Roadmap", "ROADMAP.md", "roadmap.html", "v1.0, v1.1, v1.2, v1.3, and beta-or-later packaging."),
    ("Burndown", "BURNDOWN.md", "burndown.html", "Release checklist and acceptance criteria."),
    ("GitHub Pages", "GITHUB_PAGES.md", "github-pages.html", "How the public site and reference docs fit together."),
]

PUBLIC_DOC_ORDER = [entry for entry in DOC_ORDER if entry[0] != "MCP Tools"]

try:
    import mistune
    _markdown = mistune.create_markdown(plugins=["table", "strikethrough", "task_lists"])
    def render_markdown(text: str) -> str:
        return _markdown(text)
except Exception:
    def inline_md(text: str) -> str:
        text = html.escape(text)
        text = re.sub(r"`([^`]+)`", r"<code>\1</code>", text)
        text = re.sub(r"\*\*([^*]+)\*\*", r"<strong>\1</strong>", text)
        text = re.sub(r"\[([^\]]+)\]\(([^)]+)\)", r'<a href="\2">\1</a>', text)
        return text
    def flush_paragraph(buf, out):
        if buf:
            out.append("<p>" + inline_md(" ".join(buf).strip()) + "</p>")
            buf.clear()
    def is_table_separator(line: str) -> bool:
        stripped = line.strip()
        if "|" not in stripped or not stripped.startswith("|") or not stripped.endswith("|"):
            return False
        cells = [cell.strip() for cell in stripped.strip("|").split("|")]
        return len(cells) >= 1 and all(re.fullmatch(r":?-{3,}:?", cell or "") for cell in cells)
    def split_table_row(line: str) -> list[str]:
        return [cell.strip() for cell in line.strip().strip("|").split("|")]
    def flush_table(table_rows, out):
        if not table_rows:
            return
        header = table_rows[0]
        body_rows = table_rows[1:]
        parts = ["<table><thead><tr>"]
        parts.extend(f"<th>{inline_md(cell)}</th>" for cell in header)
        parts.append("</tr></thead><tbody>")
        for row in body_rows:
            parts.append("<tr>")
            parts.extend(f"<td>{inline_md(cell)}</td>" for cell in row)
            parts.append("</tr>")
        parts.append("</tbody></table>")
        out.append("".join(parts))
    def render_markdown(text: str) -> str:
        out, para, list_buf, table_rows = [], [], [], []
        in_code = False
        code_lang = ""
        code_lines = []
        for line in text.splitlines():
            if line.startswith("```"):
                if not in_code:
                    flush_paragraph(para, out)
                    if list_buf:
                        out.append("<ul>" + "".join(f"<li>{inline_md(x)}</li>" for x in list_buf) + "</ul>")
                        list_buf.clear()
                    flush_table(table_rows, out)
                    table_rows.clear()
                    in_code = True
                    code_lang = line[3:].strip()
                    code_lines = []
                else:
                    cls = f' class="language-{html.escape(code_lang)}"' if code_lang else ""
                    out.append(f"<pre><code{cls}>" + html.escape("\n".join(code_lines)) + "</code></pre>")
                    in_code = False
                continue
            if in_code:
                code_lines.append(line)
                continue
            if not line.strip():
                flush_paragraph(para, out)
                if list_buf:
                    out.append("<ul>" + "".join(f"<li>{inline_md(x)}</li>" for x in list_buf) + "</ul>")
                    list_buf.clear()
                flush_table(table_rows, out)
                table_rows.clear()
                continue
            m = re.match(r"^(#{1,6})\s+(.*)$", line)
            if m:
                flush_paragraph(para, out)
                if list_buf:
                    out.append("<ul>" + "".join(f"<li>{inline_md(x)}</li>" for x in list_buf) + "</ul>")
                    list_buf.clear()
                flush_table(table_rows, out)
                table_rows.clear()
                level = len(m.group(1))
                out.append(f"<h{level}>" + inline_md(m.group(2)) + f"</h{level}>")
                continue
            if line.startswith("- "):
                flush_paragraph(para, out)
                flush_table(table_rows, out)
                table_rows.clear()
                list_buf.append(line[2:].strip())
                continue
            if table_rows or (line.startswith("|") and "|" in line):
                if not table_rows:
                    if not line.startswith("|"):
                        para.append(line.strip())
                        continue
                    table_rows.append(split_table_row(line))
                    continue
                if is_table_separator(line):
                    continue
                if line.startswith("|"):
                    table_rows.append(split_table_row(line))
                    continue
                flush_table(table_rows, out)
                table_rows.clear()
            para.append(line.strip())
        flush_paragraph(para, out)
        if list_buf:
            out.append("<ul>" + "".join(f"<li>{inline_md(x)}</li>" for x in list_buf) + "</ul>")
        flush_table(table_rows, out)
        return "\n".join(out)

nav_links = "\n".join(f'<a href="{out}">{html.escape(title)}</a>' for title, _src, out, _desc in PUBLIC_DOC_ORDER)

PAGE_TEMPLATE = """<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>{title} - Callsign</title>
  <link rel="stylesheet" href="../assets/styles.css" />
</head>
<body>
  <header class="site-header">
    <a class="brand" href="../index.html">Callsign</a>
    <nav>{nav}</nav>
  </header>
  <main class="doc-shell">
    <aside class="doc-sidebar">
      <strong>Docs</strong>
      {nav}
    </aside>
    <article class="doc-content">
      {body}
    </article>
  </main>
  <script src="../assets/site.js"></script>
</body>
</html>
"""

for title, src, out, _desc in DOC_ORDER:
    md_path = REF / src
    if not md_path.exists():
        continue
    md = md_path.read_text(encoding="utf-8")
    body = render_markdown(md)
    (PAGES / out).write_text(PAGE_TEMPLATE.format(title=html.escape(title), nav=nav_links, body=body), encoding="utf-8")

public_docs = [
    ("Canon Book", "pages/canon.html", "The mission, product promise, alpha ladder, and design bar."),
    ("Product Spec", "pages/product-spec.html", "The v1.0 alpha MVP and the Alpha v1 parity line."),
    ("Tier Architecture", "pages/tier-architecture.html", "How Free installs cleanly and Pro unlocks signed command packs over time."),
    ("Roadmap", "pages/roadmap.html", "Wake launch first, then dictation, browser control, system control, and file search."),
    ("Burndown", "pages/burndown.html", "The release checklist from v1.0 MVP to the Alpha v1 parity line."),
    ("Test Plan", "pages/test-plan.html", "How we prove wake, overlay, identity, and visible app launch work safely."),
]

public_cards = "\n".join(
    f'''<article class="card doc-card" data-doc-card>
      <h3><a href="{href}">{html.escape(title)}</a></h3>
      <p>{html.escape(desc)}</p>
    </article>'''
    for title, href, desc in public_docs
)

index = f"""<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Callsign | Open-source voice control for Windows, WSL, and Linux</title>
  <meta name="description" content="Callsign is the open-source voice-control layer for Windows, WSL, and Linux. A visible, identity-first desktop assistant that feels like Apple Voice Control and grows toward Talon-level power." />
  <link rel="stylesheet" href="assets/styles.css" />
</head>
<body>
  <header class="site-header">
    <a class="brand" href="index.html">Callsign</a>
    <nav>
      <a href="#promise">Promise</a>
      <a href="#alpha">Alpha</a>
      <a href="#overlay">Wake overlay</a>
      <a href="#docs">Docs</a>
    </nav>
  </header>

  <main>
    <section class="hero" id="promise">
      <div class="hero-copy">
        <div class="eyebrow">MIT-licensed desktop voice control</div>
        <h1>Callsign is the voice assistant you can inspect.</h1>
        <p class="lede">An open-source, Windows-first desktop assistant built around a simple promise: you see when it wakes, you see what it heard, and your identity gates every action.</p>
        <div class="actions">
          <a class="button primary" href="pages/canon.html">Read the project canon</a>
          <a class="button" href="pages/roadmap.html">See the alpha roadmap</a>
        </div>
      </div>
      <div class="hero-status" aria-label="Callsign session preview">
        <p><span>Wake</span><strong>Callsign heard</strong></p>
        <p><span>Identity</span><strong>Waiting for your callsign</strong></p>
        <p><span>Action</span><strong>Visible Start menu launch</strong></p>
      </div>
    </section>

    <section class="section intro-grid">
      <div>
        <div class="eyebrow">Why open source first</div>
        <h2>A desktop assistant should earn trust in daylight.</h2>
      </div>
      <div class="statement">
        <p>Callsign is for people who want voice control without surrendering their desktop to a mystery box. The Free core is public, auditable, local-first, and designed around visible consent: wake word, callsign verification, live readout, and then a visible action.</p>
        <p>We are also honest about the business path. Callsign is working toward future closed-source extension libraries for Pro and Advanced tiers, stored outside the public core in <code>/closed-source/</code>. The free open-source base remains the public trust layer.</p>
      </div>
    </section>

    <section class="section proof-strip" aria-label="Open-source promises">
      <article><span>License</span><strong>MIT core</strong><p>Fork it, audit it, run it, and build around the public architecture.</p></article>
      <article><span>Runtime</span><strong>Visible by design</strong><p>The overlay and readout make listening, identity, and action states obvious.</p></article>
      <article><span>Safety</span><strong>Identity before action</strong><p>Transcript text alone must not wake or authorize a session.</p></article>
      <article><span>Future</span><strong>Open core, paid extensions</strong><p>Closed-source command libraries can expand the ceiling without hiding the foundation.</p></article>
    </section>

    <section class="section alpha-panel" id="alpha">
      <div class="section-heading">
        <div class="eyebrow">Alpha v1 release line</div>
        <h2>Start narrow. Reach parity. Keep it free through alpha.</h2>
        <p>Alpha is not a teaser tier. The v1 line is where Callsign proves open voice control can feel polished enough for daily use.</p>
      </div>
      <div class="timeline">
        <article><span>v1.0</span><h3>Wake, verify, launch</h3><p>Background service wake detection, identity verification, animated overlay, live readout, and Start menu app launch.</p></article>
        <article><span>v1.1</span><h3>Dictation</h3><p>Visible text review so the user decides when dictated text gets copied, pasted, or inserted.</p></article>
        <article><span>v1.2</span><h3>Browser control</h3><p>Open, search, and navigate visibly with safe boundaries around submissions and external actions.</p></article>
        <article><span>v1.3</span><h3>System control</h3><p>Windows, WSL, and Linux workflows, plus file search results shown or opened through Explorer.</p></article>
      </div>
    </section>

    <section class="section feature-band" id="overlay">
      <div>
        <div class="eyebrow">The wake moment</div>
        <h2>When Callsign hears you, you see it.</h2>
        <p>The animated wake overlay appears when the wake word is heard. The readout shows the phase and transcript, or a live hearing cue while speech is arriving. The point is simple: no hidden listening, no silent action, no automation that the user cannot stop.</p>
      </div>
      <div class="mini-overlay">
        <img src="assets/callsign.gif" alt="Callsign animated wake cue" />
        <p><span>Wake</span> Callsign heard. Say your callsign.</p>
        <p><span>Readout</span> Heard: womprat</p>
        <p><span>History</span> Callsign &bull; womprat &bull; open Notepad</p>
      </div>
    </section>

    <section class="section tier-band">
      <div class="section-heading">
        <div class="eyebrow">Open core, expandable future</div>
        <h2>Free is the foundation. Extensions are the business.</h2>
      </div>
      <div class="grid">
        <article class="card"><h3>Free</h3><p>Open-source callsign identity, wake overlay, live readout, and visible Start menu launching.</p></article>
        <article class="card"><h3>Pro</h3><p>Planned paid command libraries for deeper Windows, WSL, Linux, browser, and workflow control.</p></article>
        <article class="card"><h3>Advanced</h3><p>Future specialized catalogs, recipes, diagnostics, and power-user automation shipped as closed-source extension libraries.</p></article>
        <article class="card"><h3>Boundary</h3><p>Proprietary tier material belongs in <code>/closed-source/</code>, leaving the public repo clean and inspectable.</p></article>
      </div>
    </section>

    <section class="section" id="docs">
      <div class="section-heading">
        <div class="eyebrow">Canon and contributor docs</div>
        <h2>Build from the same book.</h2>
      </div>
      <div class="grid">
        {public_cards}
      </div>
    </section>
  </main>

  <footer class="footer">Callsign - open-source voice control with identity before action.</footer>
  <script src="assets/site.js"></script>
</body>
</html>
"""

(DOCS / "index.html").write_text(index, encoding="utf-8")
print(f"Generated {len(DOC_ORDER)} reference pages and docs/index.html")
