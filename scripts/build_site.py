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
  <title>Callsign · Windows Voice Agent MCP Server</title>
  <meta name=\"description\" content=\"Callsign is a local-first Windows 11 voice agent runtime built around a safety-gated MCP automation server.\" />
  <link rel=\"stylesheet\" href=\"assets/styles.css\" />
</head>
<body>
  <header class=\"site-header\">
    <a class=\"brand\" href=\"index.html\">Callsign</a>
    <nav>
      <a href=\"pages/product-spec.html\">Product</a>
      <a href=\"pages/architecture.html\">Architecture</a>
      <a href=\"pages/security-model.html\">Security</a>
      <a href=\"pages/mcp-tools.html\">MCP Tools</a>
      <a href=\"pages/burndown.html\">Burndown</a>
    </nav>
  </header>

  <main>
    <section class=\"hero\">
      <div class=\"eyebrow\">Local-first · Windows 11 · MCP · Voice</div>
      <h1>Voice control for Windows, without haunted cursor chaos.</h1>
      <p>Callsign is a design starter for a Windows automation MCP server and voice/model host. The model plans, the MCP server exposes typed desktop capabilities, the policy engine gates risky actions, and the audit log records what happened.</p>
      <div class=\"actions\">
        <a class=\"button primary\" href=\"pages/architecture.html\">Read the architecture</a>
        <a class=\"button\" href=\"pages/security-model.html\">Review the safety model</a>
        <a class=\"button\" href=\"reference/BURNDOWN.md\">Open burndown</a>
      </div>
    </section>

    <section class=\"section\">
      <h2>Core idea</h2>
      <div class=\"flow\"><pre><code>User voice/text
  → Voice/model host
  → LLM planner
  → MCP tool call
  → Policy engine
  → Windows UI Automation / native API / fallback input
  → Verification
  → Audit log</code></pre></div>
    </section>

    <section class=\"section\">
      <h2>Design principles</h2>
      <div class=\"grid\">
        <article class=\"card\"><h3>Semantic first</h3><p>Prefer native APIs and Windows UI Automation over coordinate clicks and raw keystrokes.</p></article>
        <article class=\"card\"><h3>Policy outside the model</h3><p>The LLM can propose actions. It cannot authorize risky ones.</p></article>
        <article class=\"card\"><h3>Visible and reversible</h3><p>Actions should happen on visible targets, include verification, and support handoff or undo where possible.</p></article>
        <article class=\"card\"><h3>Local by default</h3><p>Screenshots, clipboard, UI text, selectors, recipes, and audit logs stay local unless explicitly configured.</p></article>
      </div>
    </section>

    <section class=\"section\">
      <h2>Documentation</h2>
      <input class=\"search\" data-doc-search placeholder=\"Filter docs…\" />
      <div class=\"grid\">{cards}</div>
    </section>

    <section class=\"section\">
      <h2>Publish this site</h2>
      <div class=\"card\">
        <p>This site is ready to run from the repository <code>/docs</code> folder. In GitHub, open repository settings, go to Pages, choose <strong>Deploy from a branch</strong>, select your branch, and choose <code>/docs</code> as the folder.</p>
        <p>For local preview, run:</p>
        <pre><code>python -m http.server 8000 -d docs</code></pre>
      </div>
    </section>
  </main>

  <footer class=\"footer\">Callsign docs starter · Static site, no external assets.</footer>
  <script src=\"assets/site.js\"></script>
</body>
</html>
"""
(DOCS / "index.html").write_text(index, encoding="utf-8")
print(f"Generated {len(DOC_ORDER)} pages and docs/index.html")
