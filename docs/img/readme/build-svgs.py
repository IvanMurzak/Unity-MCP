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
]:
    K.write(os.path.join(BUTTONS, name), K.button(text, label="Download the AI Game Developer installer", **kw))
