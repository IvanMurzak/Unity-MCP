"""Regenerate the README images (hero, architecture, terminal) with the shared svgkit style.

Usage: python docs/img/readme/build-svgs.py
svgkit.py next to this file is an unmodified copy of the shared kit.
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import svgkit as K  # noqa: E402


def out(name):
    return os.path.join(HERE, name)


# --- hero -------------------------------------------------------------------------------------------------------
K.write(out("hero.svg"), K.hero(
    title="Unity ", accent="MCP", eyebrow_text="AI Game Developer", accent_fx="flow",
    subtitle=["Let your AI agent build, test and debug", "Unity projects, in the Editor and in your game."],
    chip_items=["Unity 2022.3+", "MCP", "Skills", "CLI", "Runtime"],
    label="AI Game Developer, Unity MCP: Skills, MCP tools and a CLI that connect AI agents to the Unity Editor "
          "and to your running game"))

# --- architecture -----------------------------------------------------------------------------------------------
W, H = 1200, 520
LX, MX, RX, CW = 40, 450, 860, 300      # column x and card width
R1, R2, CH = 110, 270, 120              # row y and card height


def label(x, y, text, anchor="start"):
    return f'<text x="{x}" y="{y}" text-anchor="{anchor}" class="s" font-size="16" fill="{K.SECOND}">{K.esc(text)}</text>'


body = "".join([
    K.eyebrow(LX, 64, "You"), K.eyebrow(430, 64, "MCP server"), K.eyebrow(RX, 64, "Unity"),
    # the server frame: one of two servers
    f'<rect x="430" y="90" width="340" height="320" rx="18" fill="none" stroke="{K.HAIR_STRONG}" stroke-dasharray="6 6"/>',
    K.wire("M340 170 C385 170 385 230 430 230", color=K.RED_TEXT, dur=2.2),
    K.wire("M340 330 C385 330 385 270 430 270", color=K.GREEN, dur=2.2, begin=1.1),
    K.wire("M770 230 C815 230 815 170 860 170", color=K.YELLOW, dur=2.2, begin=0.5),
    K.wire("M770 270 C815 270 815 330 860 330", color=K.BLUE, dur=2.2, begin=1.6),
    K.wire("M190 390 C190 456 1010 456 1010 390", color=K.GREEN, dur=5.0, begin=0.3),
    label(352, 156, "MCP"), label(350, 362, "HTTP"), label(778, 156, "SignalR"), label(778, 362, "SignalR"),
    K.card(LX, R1, CW, CH, "AI agent", "Claude Code, Codex, Cursor,", accent="red", sub2="Copilot, Gemini and more"),
    K.card(LX, R2, CW, CH, "unity-mcp-cli", "install · open · rql · test", accent="green", mono_title=True,
           sub2="scripts and CI"),
    K.card(MX, R1, CW, CH, "Cloud (default)", "ai-game.dev/mcp", accent="none", sub2="sign in: unity-mcp-cli login"),
    K.card(MX, R2, CW, CH, "Custom", "local gamedev-mcp-server", accent="none", sub2="self-hosted or Docker"),
    K.card(RX, R1, CW, CH, "Your game (Runtime)", "your [AiTool] methods", accent="yellow",
           sub2="in a build or in Play mode"),
    K.card(RX, R2, CW, CH, "Unity Editor plugin", "RQL, C#, tests, screenshots", accent="blue",
           sub2="+ extensions and your tools"),
    label(W / 2, 494, "unity-mcp-cli also installs the plugin, opens the project and runs batch-mode tests",
          anchor="middle"),
])
K.write(out("architecture.svg"), K.panel(W, H, body, label=(
    "How it works: an AI agent or unity-mcp-cli talks to an MCP server (ai-game.dev Cloud by default, or a local "
    "gamedev-mcp-server), which calls the tools of your running game and of the Unity Editor plugin; the CLI also "
    "installs the plugin, opens the project and runs batch-mode tests directly")))

# --- terminal (real commands and their real success messages) ---------------------------------------------------
K.write(out("terminal.svg"), K.terminal(title="~/games — unity-mcp-cli", label=(
    "A terminal session: install unity-mcp-cli, add the plugin to a Unity project, sign in, configure Claude Code "
    "and open the project"), lines=[
    {"cmd": "npm install -g unity-mcp-cli"},
    {"cmd": "unity-mcp-cli install-plugin ./MyGame"},
    {"out": [("ok", "  ✔ "), ("text", "Done! Open the project in Unity Editor to complete installation.")]},
    {"cmd": "unity-mcp-cli login"},
    {"out": [("ok", "  ✔ "), ("text", "Signed in. Credential saved to "), ("muted", "~/.ai-game-dev/credentials.json")],
     "note": "browser sign-in"},
    {"cmd": "unity-mcp-cli setup-mcp claude-code ./MyGame"},
    {"out": [("ok", "  ✔ "), ("text", "Claude Code configured successfully")], "note": "or setup-skills"},
    {"cmd": "unity-mcp-cli open ./MyGame"},
    {"out": [("ok", "  ✔ "), ("text", "Unity Editor located")], "note": "connects to the MCP server"},
]))

# --- CLI hero (cli/README.md, shown on npm) ---------------------------------------------------------------------
K.write(out("cli-hero.svg"), K.hero(
    title="unity-mcp-", accent="cli", eyebrow_text="AI Game Developer · Unity", accent_fx="flow",
    subtitle=["Install Unity and the plugin, connect AI agents,", "run tools, compile and test from the terminal."],
    chip_items=["Node.js 22.14+", "Windows", "macOS", "Linux", "CI"],
    label="unity-mcp-cli: install Unity and the AI Game Developer plugin, connect AI agents, run tools, compile and "
          "test Unity projects from the terminal"))

# --- download buttons (README "Download Installer" link; translated READMEs use their own language) --------------
BUTTONS = os.path.join(HERE, "..", "button")
for name, text, kw in [
    ("button_download.svg", "Download Installer", {}),
    ("button_download_es.svg", "Descargar instalador", {"w": 560}),
    ("button_download_ja.svg", "インストーラーをダウンロード", {"w": 520, "icon": False}),
    ("button_download_zh-CN.svg", "下载安装器", {"w": 360, "icon": False}),
    ("button_download_hi.svg", "इंस्टॉलर डाउनलोड करें", {"w": 480, "icon": False}),
    ("button_download_ar.svg", "تنزيل المُثبِّت", {"w": 360, "icon": False}),
    ("button_download_fa.svg", "دانلود نصب‌کننده", {"w": 380, "icon": False}),
]:
    K.write(os.path.join(BUTTONS, name), K.button(text, label="Download the AI Game Developer installer", **kw))

# --- benchmark charts (docs/benchmark.md) --------------------------------------------------------------------------
# Numbers copied from IvanMurzak/RQL: evaluation/reports/unity-mcp-old-vs-rql-branch.md (Unity Editor) and
# RESULTS.md section 3.4 (sandbox game world, holdout2). Labels stay English in every language: they are data names.
EMBER = ('<linearGradient id="emb" gradientUnits="userSpaceOnUse" x1="0" y1="0" x2="700" y2="0" spreadMethod="reflect">'
         '<stop offset="0" stop-color="#FF2448"/><stop offset=".35" stop-color="#FF5A73"/><stop offset=".65" stop-color="#FF8A5B"/>'
         '<stop offset="1" stop-color="#FF4F86"/><animateTransform attributeName="gradientTransform" type="translate" '
         'values="0 0;700 0" dur="9s" repeatCount="indefinite"/></linearGradient>')


def bench_chart(path, eyebrow, before, accent, metrics, names, note, label):
    W, H = 1200, 520
    CX, CWID, GAP, TOP = 40, 360, 20, 150
    parts = [f"<defs>{EMBER}</defs>", K.eyebrow(CX, 62, eyebrow), K.accent_title(CX, 112, before, accent, 40, "ember")]
    # legend
    lx = 1160
    for i, (nm, fill) in enumerate(reversed([(names[0], K.BLUE), (names[1], "url(#emb)")])):
        w = len(nm) * 9 + 40
        lx -= w
        op = ' fill-opacity=".45"' if i == 1 else ""
        parts.append(f'<rect x="{lx}" y="98" width="16" height="16" rx="4" fill="{fill}"{op}/>'
                     f'<text x="{lx + 24}" y="112" class="s" font-size="16" fill="{K.SECOND}">{K.esc(nm)}</text>')
    for i, (title, sub, vals, texts) in enumerate(metrics):
        x = CX + i * (CWID + GAP)
        parts.append(f'<rect x="{x}" y="{TOP}" width="{CWID}" height="300" rx="14" fill="url(#card)" stroke="{K.HAIR}"/>')
        parts.append(f'<text x="{x + 22}" y="{TOP + 40}" class="s" font-size="21" font-weight="700" fill="{K.TEXT}">{K.esc(title)}</text>')
        parts.append(f'<text x="{x + 22}" y="{TOP + 66}" class="s" font-size="16" fill="{K.MUTED}">{K.esc(sub)}</text>')
        top = max(vals)
        for j, (v, t) in enumerate(zip(vals, texts)):
            by = TOP + 100 + j * 92
            bw = max(8, round((CWID - 44) * v / top))
            fill = K.BLUE if j == 0 else "url(#emb)"
            op = ' fill-opacity=".45"' if j == 0 else ""
            parts.append(f'<text x="{x + 22}" y="{by + 18}" class="s" font-size="16" fill="{K.SECOND}">{K.esc(names[j])}</text>')
            parts.append(f'<text x="{x + CWID - 22}" y="{by + 18}" text-anchor="end" class="m" font-size="18" font-weight="700" '
                         f'fill="{K.TEXT}">{K.esc(t)}</text>')
            parts.append(f'<rect x="{x + 22}" y="{by + 32}" width="{CWID - 44}" height="22" rx="11" fill="{K.SURF}"/>')
            parts.append(f'<rect x="{x + 22}" y="{by + 32}" width="{bw}" height="22" rx="11" fill="{fill}"{op}/>')
    parts.append(f'<text x="{W / 2}" y="{H - 30}" text-anchor="middle" class="s" font-size="16" fill="{K.MUTED}">{K.esc(note)}</text>')
    K.write(path, K.panel(W, H, "".join(parts), label=label))


DOCS_ASSETS = os.path.join(HERE, "..", "benchmark")
bench_chart(os.path.join(DOCS_ASSETS, "unity-editor.svg"), "Benchmark · Unity Editor", "Same tasks, ", "fewer tokens",
            [("Prompt tokens per task", "lower is better", [138272, 26013], ["138,272", "26,013"]),
             ("Tool calls per task", "lower is better", [5.5, 3.8], ["5.5", "3.8"]),
             ("Tasks solved", "higher is better", [59, 59], ["59 / 60", "59 / 60"])],
            ["Per-action tools", "RQL tools"],
            "20 tasks × 3 runs in the Unity Editor, one model (DeepSeek Flash), temperature 0",
            "Unity Editor benchmark: prompt tokens per task 138,272 with per-action tools vs 26,013 with RQL tools; "
            "tool calls per task 5.5 vs 3.8; tasks solved 59 of 60 in both")
bench_chart(os.path.join(DOCS_ASSETS, "sandbox.svg"), "Benchmark · sandbox game world", "Fresh tasks, ", "100% solved",
            [("Tokens per task", "lower is better", [169190, 7817], ["169,190", "7,817"]),
             ("Tool calls per task", "lower is better", [36.86, 1.91], ["36.9", "1.9"]),
             ("Tasks solved", "higher is better", [66.7, 100], ["66.7%", "100%"])],
            ["Reflection tools", "RQL"],
            "30 tasks written after tuning was frozen × 3 runs, one model (DeepSeek V4.1-Flash)",
            "Sandbox benchmark: tokens per task 169,190 with plain reflection tools vs 7,817 with RQL; tool calls per "
            "task 36.9 vs 1.9; tasks solved 66.7% vs 100%")
