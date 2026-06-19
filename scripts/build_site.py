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
        <div class="eyebrow">Open-source voice control for the desktop</div>
        <h1>The open-source Windows Voice Control killer.</h1>
      <p class="lede">Say <strong>Callsign</strong>. Verify it is you. Control your computer through visible, user-approved actions. The alpha experience targets practical Windows Voice Access parity with a stricter trust model: live overlay, explicit identity gating, and Start menu-native execution.</p>
        <div class="actions">
          <a class="button primary" href="pages/canon.html">Read the canon book</a>
          <a class="button" href="pages/burndown.html">View the alpha burndown</a>
        </div>
      </div>
      <div class="hero-device" aria-label="Callsign wake overlay preview">
        <div class="orb-frame">
          <img src="assets/callsign.gif" alt="Callsign wake animation" />
          <div class="live-caption">
            <span>Listening</span>
            <strong>Callsign heard. Say your callsign.</strong>
          </div>
          <div class="transcript-strip" aria-label="Callsign live transcript preview">
            <p><span>Live readout</span><strong>Hearing your callsign...</strong></p>
            <p><span>Heard</span><strong>Heard: womprat</strong></p>
            <p><span>Command</span><strong>Command: open Notepad</strong></p>
          </div>
        </div>
      </div>
    </section>

    <section class="section split">
      <div>
        <div class="eyebrow">Why it matters</div>
        <h2>Voice control should be open, powerful, and safe enough to trust.</h2>
      </div>
      <p>Closed assistants are impressive until they hide what they heard, guess at your intent, or disappear into a black box. Callsign keeps the user in the loop: wake, verify identity, show the transcript, then act visibly.</p>
    </section>

    <section class="section">
      <div class="grid three">
        <article class="card"><h3>Apple-level clarity</h3><p>Setup should feel obvious. Recording, listening, and acting states should be visible at a glance.</p></article>
        <article class="card"><h3>More power than Talon</h3><p>The long-term ceiling is full Windows, WSL, Linux, browser, and workflow control by voice.</p></article>
        <article class="card"><h3>Identity before action</h3><p>The wake word opens a session. Your callsign and voice identity unlock command capture.</p></article>
      </div>
    </section>

    <section class="section" id="alpha">
      <div class="section-heading">
        <div class="eyebrow">Alpha v1 release line</div>
        <h2>Start narrow. Reach parity. Keep it free through alpha.</h2>
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
        <p>The animated wake overlay appears above everything when the wake word is heard. The readout below it shows the phase and transcript, or an animated live hearing cue while speech is arriving: identity, command, or launch. While that cue is active, the card pulses subtly so it feels alive. The overlay also shows a compact recent speech history beneath the live readout, and the Session tab keeps the same recent history so the user can review what Callsign heard without hunting through logs. Callsign can also paint numbered badges over visible controls and keep the focused target highlighted so you can say the number you see, not guess where the cursor is. No guessing. No hidden listening. No silent action. The homepage preview now mirrors that behavior with a wake cue, a live hearing line, a heard transcript, and the next command cue stacked under `callsign.gif`.</p>
      </div>
      <div class="mini-overlay">
        <img src="assets/callsign.gif" alt="Callsign animated wake cue" />
        <p><span>Wake</span> Callsign heard. Say your callsign.</p>
        <p><span>Readout</span> Heard: womprat</p>
        <p><span>History</span> Callsign &bull; womprat &bull; open Notepad</p>
      </div>
    </section>

    <section class="section">
      <div class="grid">
        <article class="card"><h3>Open-source core</h3><p>The alpha core stays inspectable and useful on its own.</p></article>
        <article class="card"><h3>Free through alpha</h3><p>All Alpha v1 features remain free until at least beta.</p></article>
        <article class="card"><h3>Future Pro and Advanced</h3><p>Paid tiers may later fund deeper control, recipes, diagnostics, command catalogs, and richer visible overlays.</p></article>
        <article class="card"><h3>Visible by default</h3><p>Actions happen where the user can see them, with stop, cancel, timeout, and lockout states.</p></article>
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
