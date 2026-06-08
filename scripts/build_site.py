from pathlib import Path
import html
import re

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "docs"
REF = DOCS / "reference"
PAGES = DOCS / "pages"
PAGES.mkdir(parents=True, exist_ok=True)

DOC_ORDER = [
    ("Product Spec", "PRODUCT_SPEC.md", "product-spec.html", "Vision, users, MVP, non-goals, and product principles."),
    ("Architecture", "ARCHITECTURE.md", "architecture.html", "Host/server split, runtime loop, transport, resources, and adapters."),
    ("MCP Tools", "MCP_TOOLS.md", "mcp-tools.html", "Tool schemas, risk tiers, result envelopes, and examples."),
    ("Windows Automation", "WINDOWS_AUTOMATION.md", "windows-automation.html", "UI Automation-first strategy with SendInput fallback rules."),
    ("Security Model", "SECURITY_MODEL.md", "security-model.html", "Policy engine, approvals, data handling, and blocked actions."),
    ("Threat Model", "THREAT_MODEL.md", "threat-model.html", "Threat actors, abuse cases, mitigations, and security tests."),
    ("Voice UX", "VOICE_UX.md", "voice-ux.html", "Voice modes, confirmations, interruptions, and recovery."),
    ("Data Model", "DATA_MODEL.md", "data-model.html", "Sessions, audit events, selectors, recipes, and configs."),
    ("Test Plan", "TEST_PLAN.md", "test-plan.html", "Unit, integration, desktop, safety, and manual QA tests."),
    ("Deployment", "DEPLOYMENT.md", "deployment.html", "GitHub Pages setup, local preview, and future app deployment."),
    ("Roadmap", "ROADMAP.md", "roadmap.html", "Milestones from docs starter to voice host and recipes."),
    ("Burndown", "BURNDOWN.md", "burndown.html", "Prioritized implementation backlog and acceptance criteria."),
    ("GitHub Pages", "GITHUB_PAGES.md", "github-pages.html", "How the static site is organized and published from /docs."),
]

try:
    import mistune  # Optional; nicer Markdown rendering when installed.
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
<html lang=\"en\">
<head>
  <meta charset=\"utf-8\" />
  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />
  <title>{title} · Callsign</title>
  <link rel=\"stylesheet\" href=\"../assets/styles.css\" />
</head>
<body>
  <header class=\"site-header\">
    <a class=\"brand\" href=\"../index.html\">Callsign</a>
    <nav>{nav}</nav>
  </header>
  <main class=\"doc-shell\">
    <aside class=\"doc-sidebar\">
      <strong>Docs</strong>
      {nav}
    </aside>
    <article class=\"doc-content\">
      {body}
    </article>
  </main>
  <script src=\"../assets/site.js\"></script>
</body>
</html>
"""

for title, src, out, _desc in DOC_ORDER:
    md = (REF / src).read_text(encoding="utf-8")
    body = render_markdown(md)
    (PAGES / out).write_text(PAGE_TEMPLATE.format(title=html.escape(title), nav=nav_links, body=body), encoding="utf-8")

cards = "\n".join(
    f'''<article class="card" data-doc-card>
      <h3><a href="pages/{out}">{html.escape(title)}</a></h3>
      <p>{html.escape(desc)}</p>
      <p><a href="reference/{src}">Markdown source</a></p>
    </article>''' for title, src, out, desc in DOC_ORDER
)

index = f"""<!doctype html>
<html lang=\"en\">
<head>
  <meta charset=\"utf-8\" />
  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />
  <title>Callsign · Voice-Activated AI for Your Desktop</title>
  <meta name=\"description\" content=\"Callsign is a voice-activated AI desktop assistant that listens for your callsign, understands what you want, and helps your computer move with you.\" />
  <link rel=\"stylesheet\" href=\"assets/styles.css\" />
</head>
<body>
  <header class=\"site-header\">
    <a class=\"brand\" href=\"index.html\">Callsign</a>
    <nav>
      <a href=\"#experience\">Experience</a>
      <a href=\"#trust\">Trust</a>
      <a href=\"#future\">Future</a>
    </nav>
  </header>

  <main>
    <section class=\"hero\">
      <div class=\"eyebrow\">Wake word · Your callsign · AI magic</div>
      <h1>Say the word. Your computer starts listening.</h1>
      <p>Callsign is a voice-activated AI companion for your desktop. It wakes when you say <strong>Callsign</strong>, confirms who you are, listens to what you want, and turns your request into clear, visible action.</p>
      <div class=\"actions\">
        <a class=\"button primary\" href=\"#experience\">See the experience</a>
        <a class=\"button\" href=\"#trust\">Why it feels safe</a>
      </div>
    </section>

    <section class=\"section\" id=\"experience\">
      <h2>The Moment</h2>
      <div class=\"grid\">
        <article class=\"card\"><h3>Call it by name</h3><p>Say <strong>Callsign</strong> and your desktop quietly shifts into listening mode.</p></article>
        <article class=\"card\"><h3>Speak your callsign</h3><p>Your personal callsign confirms the request is coming from you before anything begins.</p></article>
        <article class=\"card\"><h3>Ask naturally</h3><p>Tell it what you want: organize a file, open an app, fill a field, or help move a task forward.</p></article>
        <article class=\"card\"><h3>Watch it happen</h3><p>Callsign shows what it intends to do, then carries out the work where you can see it.</p></article>
      </div>
    </section>

    <section class=\"section\" id=\"trust\">
      <h2>Magic With Manners</h2>
      <div class=\"grid\">
        <article class=\"card\"><h3>You stay in command</h3><p>High-impact actions ask first, and the assistant stays visible while it works.</p></article>
        <article class=\"card\"><h3>No secret handling</h3><p>Passwords, payment details, security settings, and account-ending actions stay out of bounds.</p></article>
        <article class=\"card\"><h3>Easy to stop</h3><p>A clear stop phrase and visible controls keep the experience interruptible.</p></article>
        <article class=\"card\"><h3>Built for everyday work</h3><p>Callsign is for practical desktop help: files, forms, apps, reminders, and repetitive tasks.</p></article>
      </div>
    </section>

    <section class=\"section\" id=\"future\">
      <h2>Windows First, More Coming</h2>
      <div class=\"card\">
        <p>The first alpha is focused on Windows desktop control through voice. Linux support is planned as Callsign grows from a focused assistant into a broader personal computer companion.</p>
      </div>
    </section>

  </main>

  <footer class=\"footer\">Callsign · Your computer, on speaking terms.</footer>
  <script src=\"assets/site.js\"></script>
</body>
</html>
"""
(DOCS / "index.html").write_text(index, encoding="utf-8")
print(f"Generated {len(DOC_ORDER)} pages and docs/index.html")
