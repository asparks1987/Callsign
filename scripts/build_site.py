from pathlib import Path
import html
import re

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "docs"
REF = DOCS / "reference"
PAGES = DOCS / "pages"
PAGES.mkdir(parents=True, exist_ok=True)

DOC_ORDER = [
    ("Product Spec", "PRODUCT_SPEC.md", "product-spec.html", "Current alpha scope, user flow, and product principles."),
    ("Architecture", "ARCHITECTURE.md", "architecture.html", "Current UI shell and future service split."),
    ("MCP Tools", "MCP_TOOLS.md", "mcp-tools.html", "Future automation contract and tool design."),
    ("Windows Automation", "WINDOWS_AUTOMATION.md", "windows-automation.html", "Visible app launching and future desktop control."),
    ("Security Model", "SECURITY_MODEL.md", "security-model.html", "Identity, local data handling, and blocked actions."),
    ("Threat Model", "THREAT_MODEL.md", "threat-model.html", "Threats and mitigations for the alpha and future service."),
    ("Voice UX", "VOICE_UX.md", "voice-ux.html", "Wake word, callsign identity, and launch prompts."),
    ("Data Model", "DATA_MODEL.md", "data-model.html", "Profile storage, enrollment state, and launch history."),
    ("Test Plan", "TEST_PLAN.md", "test-plan.html", "UI, identity, launch, and safety checks."),
    ("Deployment", "DEPLOYMENT.md", "deployment.html", "Current build flow, GitHub Pages, and packaging notes."),
    ("Roadmap", "ROADMAP.md", "roadmap.html", "Alpha priorities, service work, and future expansion."),
    ("Burndown", "BURNDOWN.md", "burndown.html", "Current backlog and acceptance criteria."),
    ("GitHub Pages", "GITHUB_PAGES.md", "github-pages.html", "How the public site and reference docs fit together."),
]

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

    def render_markdown(text: str) -> str:
        out, para, list_buf = [], [], []
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
                continue
            m = re.match(r"^(#{1,6})\s+(.*)$", line)
            if m:
                flush_paragraph(para, out)
                if list_buf:
                    out.append("<ul>" + "".join(f"<li>{inline_md(x)}</li>" for x in list_buf) + "</ul>")
                    list_buf.clear()
                level = len(m.group(1))
                out.append(f"<h{level}>" + inline_md(m.group(2)) + f"</h{level}>")
                continue
            if line.startswith("- "):
                flush_paragraph(para, out)
                list_buf.append(line[2:].strip())
                continue
            para.append(line.strip())
        flush_paragraph(para, out)
        if list_buf:
            out.append("<ul>" + "".join(f"<li>{inline_md(x)}</li>" for x in list_buf) + "</ul>")
        return "\n".join(out)


nav_links = "\n".join(
    f'<a href="{out}">{html.escape(title)}</a>' for title, _src, out, _desc in DOC_ORDER
)

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
    md = (REF / src).read_text(encoding="utf-8")
    body = render_markdown(md)
    (PAGES / out).write_text(
        PAGE_TEMPLATE.format(title=html.escape(title), nav=nav_links, body=body),
        encoding="utf-8",
    )


public_docs = [
    ("Product Spec", "pages/product-spec.html", "What Callsign does now and why the alpha exists."),
    ("Architecture", "pages/architecture.html", "The current UI shell and the future background service."),
    ("Roadmap", "pages/roadmap.html", "What comes after the visible alpha."),
    ("Test Plan", "pages/test-plan.html", "How we prove the alpha works safely."),
]

public_cards = "\n".join(
    f'''<article class="card" data-doc-card>
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
  <title>Callsign | Open Source AI for Your Desktop</title>
  <meta name="description" content="Callsign is an open source Windows and Linux desktop assistant with paid Home and Advanced tiers. The free version wakes on your callsign, confirms identity, and launches installed apps in a visible way." />
  <link rel="stylesheet" href="assets/styles.css" />
</head>
<body>
  <header class="site-header">
    <a class="brand" href="index.html">Callsign</a>
    <nav>
      <a href="#how-it-works">How it works</a>
      <a href="#open-source">Open source</a>
      <a href="#docs">Docs</a>
      <a href="#roadmap">Roadmap</a>
    </nav>
  </header>

  <main>
    <section class="hero">
      <div class="eyebrow">Windows and Linux MVP - open source core - paid Home and Advanced tiers</div>
      <h1>Say Callsign. Launch the app. Stay in control.</h1>
      <p>Callsign is a voice-driven desktop assistant for Windows and Linux. The free version focuses on the visible workflow: create an account, train your voice, say your callsign, and launch installed apps through the Start menu. Home and Advanced unlock deeper control of the system.</p>
      <div class="actions">
        <a class="button primary" href="#how-it-works">See how it works</a>
        <a class="button" href="#docs">Read the docs</a>
      </div>
    </section>

    <section class="section" id="how-it-works">
      <h2>How It Works</h2>
      <div class="grid">
        <article class="card"><h3>Create an account</h3><p>Set up a profile with your callsign and the details Callsign needs to recognize you.</p></article>
        <article class="card"><h3>Train your voice</h3><p>Record a few samples so the assistant knows when the right person is speaking.</p></article>
        <article class="card"><h3>Say the wake word</h3><p>Speak <strong>Callsign</strong>, then say your callsign to unlock the command window.</p></article>
        <article class="card"><h3>Launch the app</h3><p>Ask for an installed app by name and Callsign opens it through a visible Start menu flow.</p></article>
      </div>
    </section>

    <section class="section" id="trust">
      <h2>Magic With Manners</h2>
      <div class="grid">
        <article class="card"><h3>Visible by design</h3><p>Nothing starts unless the user sees what is happening and can stop it.</p></article>
        <article class="card"><h3>Local first</h3><p>The alpha keeps profile data and enrollment state on the device by default.</p></article>
        <article class="card"><h3>Easy to explain</h3><p>The product promise is simple: say the word, confirm identity, launch the app.</p></article>
        <article class="card"><h3>Open source core</h3><p>The base desktop experience stays free, with paid tiers reserved for deeper control.</p></article>
      </div>
    </section>

    <section class="section" id="open-source">
      <h2>Open Source and Paid Tiers</h2>
      <div class="card">
        <p>Callsign is being built as an open source desktop assistant with paid Home and Advanced tiers. The free version stays useful on its own as the Start menu launcher, while paid tiers should help cover costs, support the team, and unlock deeper control of the system without hiding the core workflow behind a subscription wall.</p>
      </div>
    </section>

    <section class="section" id="roadmap">
      <h2>Current Alpha</h2>
      <div class="grid">
        <article class="card"><h3>Free tier</h3><p>Start menu launching, account setup, and voice identity.</p></article>
        <article class="card"><h3>Home tier</h3><p>More guided desktop control and richer everyday workflows.</p></article>
        <article class="card"><h3>Advanced tier</h3><p>Deeper system control and power-user automation.</p></article>
        <article class="card"><h3>Core stays free</h3><p>The base desktop experience stays open source and free to use across Windows and Linux.</p></article>
      </div>
    </section>

    <section class="section" id="docs">
      <h2>Contributor Docs</h2>
      <div class="grid">
        {public_cards}
      </div>
    </section>
  </main>

  <footer class="footer">Callsign - open source desktop AI with Home and Advanced tiers.</footer>
  <script src="assets/site.js"></script>
</body>
</html>
"""

(DOCS / "index.html").write_text(index, encoding="utf-8")
print(f"Generated {len(DOC_ORDER)} pages and docs/index.html")
