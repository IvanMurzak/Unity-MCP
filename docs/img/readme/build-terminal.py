"""Generate docs/img/readme/terminal.svg: an animated, looping terminal session (SMIL, no JS).

Usage: python docs/img/readme/build-terminal.py docs/img/readme/terminal.svg

Each line starts a short pause after the previous one ends, so editing a command's text retimes the
whole loop. Without animation support the SVG shows the finished session.
"""
import sys
from xml.sax.saxutils import escape

W = 1000            # canvas width
CW = 9.1            # character width the commands are pinned to (textLength), 15px monospace
NOTE_CW = 8.4       # approx character width of the 14px notes
X0, Y0, LH = 36, 84, 27
TYPE_STEP = 0.045   # seconds per typed character
SHOW = 0.25         # fade-in of an output line
PRE = 5.0           # the loop opens on the finished session (also the first frame)
CLEAR = 0.4         # fade-out of the finished session before typing starts
HOLD = 3.0          # finished session at the end of the loop; it wraps into PRE

BG = "#0b1222"
COLORS = {
    "dim": "#94a3b8", "link": "#67e8f9", "code": "#fbbf24", "ok": "#34d399", "txt": "#e2e8f0",
    "punc": "#cbd5e1", "key": "#a5b4fc", "bool": "#f472b6", "str": "#fcd34d",
}

# (kind, content, pause before it, note). kind: "cmd" is typed, "out" appears at once.
LINES = [
    ("cmd", "npm install -g unity-mcp-cli", 0.6, None),
    ("cmd", "unity-mcp-cli install-plugin ./MyGame", 0.8, None),
    ("out", [("ok", "  ✔ "), ("txt", "Done! Open the project in Unity Editor to complete installation.")], 0.6, None),
    ("cmd", "unity-mcp-cli login", 1.0, None),
    ("out", [("ok", "  ✔ "), ("txt", "Signed in. Credential saved to "), ("dim", "~/.ai-game-dev/credentials.json")], 0.9, "browser sign-in"),
    ("cmd", "unity-mcp-cli setup-mcp claude-code ./MyGame", 1.0, None),
    ("out", [("ok", "  ✔ "), ("txt", "Claude Code configured successfully")], 0.6, "or setup-skills"),
    ("cmd", "unity-mcp-cli open ./MyGame", 1.0, None),
    ("out", [("ok", "  ✔ "), ("txt", "Unity Editor located")], 0.7, "connects to the MCP server"),
]
PROMPT_PAUSE = 0.6


def schedule():
    """Absolute start time and typed length of every line, then the final prompt's start."""
    t, out = PRE + CLEAR + 0.3, []
    for kind, content, pause, _ in LINES:
        start = t + pause
        n = len(content) + 2 if kind == "cmd" else 0  # "❯ " + command
        t = start + (0.15 + (n + 1) * TYPE_STEP if kind == "cmd" else SHOW)
        out.append((start, n))
    return out, t + PROMPT_PAUSE


TIMES, PROMPT_AT = schedule()
T = round(PROMPT_AT + HOLD, 2)


def k(t):
    return round(t / T, 4)


def fade(start):
    """Shown at the loop start (finished session), cleared, then shown again from `start`.
    Static renderers show the line."""
    return (f'<animate attributeName="opacity" dur="{T}s" repeatCount="indefinite" '
            f'values="1;1;0;0;1;1" keyTimes="0;{k(PRE)};{k(PRE + CLEAR)};{k(start)};{k(start + SHOW)};1"/>')


def typing_cover(y, start, n):
    """A background-coloured rect that steps right one character at a time, revealing the command."""
    gone = round((n + 1) * CW + W, 1)
    frames = [(gone, 0), (gone, PRE), (0, PRE + CLEAR), (0, start)]
    frames += [(round(i * CW, 1), start + 0.15 + i * TYPE_STEP) for i in range(1, n + 1)]
    frames += [(gone, start + 0.15 + (n + 1) * TYPE_STEP), (gone, T)]
    values = ";".join(f"{x} 0" for x, _ in frames)
    times = ";".join(str(k(t)) for _, t in frames)
    return (f'<rect x="{X0 - 2}" y="{y - 19}" width="{W}" height="26" fill="{BG}" transform="translate({W * 2} 0)">'
            f'<animateTransform attributeName="transform" type="translate" dur="{T}s" repeatCount="indefinite" '
            f'calcMode="discrete" values="{values}" keyTimes="{times}"/></rect>')


body = []
y = Y0
for (kind, content, _, note), (start, n) in zip(LINES, TIMES):
    if kind == "cmd":
        body.append(f'<g>{fade(start)}<text x="{X0}" y="{y}" class="m" textLength="{round(n * CW, 1)}" lengthAdjust="spacing">'
                    f'<tspan fill="{COLORS["ok"]}">❯</tspan> <tspan fill="#f8fafc">{escape(content)}</tspan></text>'
                    f'{typing_cover(y, start, n)}</g>')
    else:
        text = "".join(s for _, s in content)
        if note:
            assert X0 + len(text) * CW + 24 < W - 36 - len(note) * NOTE_CW, f"line overlaps its note: {text}"
        spans = "".join(f'<tspan fill="{COLORS[c]}">{escape(s)}</tspan>' for c, s in content)
        note_svg = f'<text x="{W - 36}" y="{y}" class="m note" text-anchor="end">← {escape(note)}</text>' if note else ""
        body.append(f'<g>{fade(start)}<text x="{X0}" y="{y}" class="m">{spans}</text>{note_svg}</g>')
    y += LH

body.append(f'<g>{fade(PROMPT_AT)}<text x="{X0}" y="{y}" class="m"><tspan fill="{COLORS["ok"]}">❯</tspan></text>'
            f'<rect x="{X0 + 20}" y="{y - 15}" width="9" height="18" fill="{COLORS["txt"]}">'
            f'<animate attributeName="opacity" values="1;0" dur="1.1s" calcMode="discrete" repeatCount="indefinite"/></rect></g>')

H = y + 40
svg = f'''<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}" role="img" aria-label="A terminal session: install unity-mcp-cli, add the plugin to a Unity project, sign in, configure Claude Code and open the project">
  <defs>
    <linearGradient id="bar" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#1e293b"/><stop offset="1" stop-color="#162032"/></linearGradient>
    <clipPath id="frame"><rect width="{W}" height="{H}" rx="14"/></clipPath>
  </defs>
  <style>
    .m {{ font-family: "JetBrains Mono", "Cascadia Code", Consolas, Menlo, monospace; font-size: 15px; white-space: pre; }}
    .note {{ fill: #94a3b8; font-size: 14px; font-style: italic; }}
    @media (prefers-reduced-motion: reduce) {{ * {{ animation: none !important; }} }}
  </style>
  <g clip-path="url(#frame)">
    <rect width="{W}" height="{H}" fill="{BG}"/>
    <rect width="{W}" height="44" fill="url(#bar)"/>
    <circle cx="26" cy="22" r="6.5" fill="#f87171"/><circle cx="48" cy="22" r="6.5" fill="#fbbf24"/><circle cx="70" cy="22" r="6.5" fill="#34d399"/>
    <text x="{W // 2}" y="27" text-anchor="middle" font-family="Segoe UI, Inter, -apple-system, Helvetica, Arial, sans-serif" font-size="13" fill="#94a3b8">~/games — unity-mcp-cli</text>
    {"".join(body)}
  </g>
  <rect x=".5" y=".5" width="{W - 1}" height="{H - 1}" rx="14" fill="none" stroke="#ffffff" stroke-opacity=".1"/>
</svg>
'''
with open(sys.argv[1], "w", encoding="utf-8", newline="\n") as f:
    f.write(svg)
print("wrote", sys.argv[1], f"{W}x{H}, loop {T}s")
