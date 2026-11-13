"""svgkit — the one visual style for every README/docs SVG of our repos, taken from the ai-game.dev website.

Import it from a small per-repo build script (copy the script into the repo as docs/assets/build-svgs.py or
similar, together with a copy of this file, so the images can be regenerated):

    import sys; sys.path.insert(0, r"<dir of svgkit.py>")
    import svgkit as K
    K.write("docs/assets/hero.svg", K.hero(title="Unity", accent="MCP", eyebrow="AI GAME DEVELOPER",
            subtitle=["Connect any AI agent", "to the Unity Editor and your game."],
            chips=["Unity 2022.3+", "MCP", "Skills", "CLI"]))
    K.write("docs/assets/terminal.svg", K.terminal(title="~/MyGame", lines=[...]))
    K.write("docs/assets/architecture.svg", K.panel(1200, 520, body, label="..."))   # body from K.card/K.wire/...

Website tokens (cloud/AI-Game-Dev-Server/frontend/src/index.css):
  bg #030609 / #080F17, surface #0D1A25 / #1D2835, text #FAFFFD, muted #8AA6B8, secondary #A8C2D2,
  brand red #FF002A (text tint #FF5A73), blue #6FA8D0 / #3B9BFF, green #22C55E, yellow #EAB308,
  hairline borders rgba(111,168,208,.15), radius 18, red→blue rule, dot grid + red/blue haze,
  fonts Inter (800, tight tracking) and JetBrains Mono. Eyebrows: small red uppercase, letter-spacing .14em.

Rules every image follows (enforced by the helpers):
  * dark panel with its own background — readable on GitHub light and dark themes;
  * canvas 1200 px wide (shown at ~836 px on GitHub ⇒ ×0.7): no text below 16 px in the SVG;
  * fully visible without animation; animation is light (dots on wires, a pulse, typing); reduced-motion respected;
  * no scripts, no external fonts or images.
"""
from xml.sax.saxutils import escape as _esc

BG, BG2, SURF, SURF2 = "#030609", "#080F17", "#0D1A25", "#1D2835"
TEXT, MUTED, SECOND = "#FAFFFD", "#8AA6B8", "#A8C2D2"
RED, RED_TEXT, BLUE, BLUE2 = "#FF002A", "#FF5A73", "#6FA8D0", "#3B9BFF"
GREEN, YELLOW = "#22C55E", "#EAB308"
HAIR = "rgba(111,168,208,0.16)"
HAIR_STRONG = "rgba(111,168,208,0.45)"
SANS = "Inter, 'Segoe UI', system-ui, -apple-system, Helvetica, Arial, sans-serif"
MONO = "'JetBrains Mono', 'Cascadia Code', Consolas, Menlo, monospace"
ACCENTS = {"red": RED_TEXT, "blue": BLUE, "green": GREEN, "yellow": YELLOW, "none": HAIR_STRONG}


def esc(s):
    return _esc(str(s), {'"': "&quot;"})


def _style(extra=""):
    return f"""<style>
    .s {{ font-family: {SANS}; }} .m {{ font-family: {MONO}; white-space: pre; }}
    .pulse {{ animation: pulse 3.2s ease-in-out infinite; transform-box: fill-box; transform-origin: center; }}
    @keyframes pulse {{ 0%,100% {{ opacity: .85; transform: scale(1); }} 50% {{ opacity: 1; transform: scale(1.04); }} }}
    .spin {{ animation: spin 40s linear infinite; transform-box: fill-box; transform-origin: center; }}
    .spin.r {{ animation-duration: 60s; animation-direction: reverse; }}
    @keyframes spin {{ to {{ transform: rotate(360deg); }} }}
    .blink {{ animation: blink 1.1s steps(1) infinite; }} @keyframes blink {{ 50% {{ opacity: 0; }} }}
    {extra}
    @media (prefers-reduced-motion: reduce) {{ * {{ animation: none !important; }} }}
  </style>"""


def panel(w, h, body, label, extra_css="", haze=True):
    """The site 'frame': deep background, red/blue haze, dot grid, hairline border, radius 18, red→blue rule."""
    hz = f"""<rect width="{w}" height="{h}" fill="url(#hzR)"/><rect width="{w}" height="{h}" fill="url(#hzB)"/>""" if haze else ""
    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}" role="img" aria-label="{esc(label)}">
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{BG2}"/><stop offset="1" stop-color="{BG}"/></linearGradient>
    <radialGradient id="hzR" cx="0.18" cy="0" r="0.6"><stop offset="0" stop-color="{RED}" stop-opacity="0.16"/><stop offset="1" stop-color="{RED}" stop-opacity="0"/></radialGradient>
    <radialGradient id="hzB" cx="0.85" cy="0" r="0.6"><stop offset="0" stop-color="{BLUE2}" stop-opacity="0.14"/><stop offset="1" stop-color="{BLUE2}" stop-opacity="0"/></radialGradient>
    <pattern id="dots" width="28" height="28" patternUnits="userSpaceOnUse"><circle cx="1" cy="1" r="1" fill="#A8C2D2" fill-opacity="0.08"/></pattern>
    <linearGradient id="rule" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="{RED}" stop-opacity="0"/><stop offset="0.3" stop-color="#FF2448" stop-opacity="0.6"/><stop offset="0.7" stop-color="{BLUE2}" stop-opacity="0.55"/><stop offset="1" stop-color="{BLUE2}" stop-opacity="0"/></linearGradient>
    <linearGradient id="card" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#0C1620"/><stop offset="1" stop-color="#070D14"/></linearGradient>
    <linearGradient id="logoL" gradientUnits="userSpaceOnUse" x1="512" y1="190" x2="512" y2="785"><stop offset="0" stop-color="#FFFFFF"/><stop offset="1" stop-color="#8395AD"/></linearGradient>
    <linearGradient id="logoR" gradientUnits="userSpaceOnUse" x1="512" y1="190" x2="512" y2="785"><stop offset="0" stop-color="#E1E8F1"/><stop offset="1" stop-color="#455974"/></linearGradient>
    <linearGradient id="logoB" gradientUnits="userSpaceOnUse" x1="512" y1="190" x2="512" y2="785"><stop offset="0" stop-color="#FFFFFF"/><stop offset="1" stop-color="#AABBD0"/></linearGradient>
    <radialGradient id="logoGlow"><stop offset="0" stop-color="#6FA8D0" stop-opacity=".18"/><stop offset="1" stop-color="#6FA8D0" stop-opacity="0"/></radialGradient>
    <linearGradient id="brand" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="{RED_TEXT}"/><stop offset="1" stop-color="{BLUE}"/></linearGradient>
    <clipPath id="frame"><rect width="{w}" height="{h}" rx="18"/></clipPath>
  </defs>
  {_style(extra_css)}
  <g clip-path="url(#frame)">
    <rect width="{w}" height="{h}" fill="url(#bg)"/>{hz}
    <rect width="{w}" height="{h}" fill="url(#dots)"/>
    {body}
    <rect x="0" y="{h - 2}" width="{w}" height="2" fill="url(#rule)"/>
  </g>
  <rect x=".5" y=".5" width="{w - 1}" height="{h - 1}" rx="18" fill="none" stroke="{HAIR}"/>
</svg>
"""


def eyebrow(x, y, text, anchor="start"):
    return f'<text x="{x}" y="{y}" text-anchor="{anchor}" class="s" font-size="16" font-weight="700" letter-spacing="2.4" fill="{RED_TEXT}">{esc(text.upper())}</text>'


def pill(x, y, text, mono=True, color=SECOND, w=None):
    """A chip like the site's: hairline border, surface fill. Width estimated from text when not given."""
    cw = 9.8 if mono else 9.0
    w = w or round(len(text) * cw + 32)
    return (f'<g transform="translate({x} {y})"><rect width="{w}" height="34" rx="17" fill="{SURF}" stroke="{HAIR_STRONG}" stroke-opacity=".6"/>'
            f'<text x="{w / 2}" y="23" text-anchor="middle" class="{"m" if mono else "s"}" font-size="16" fill="{color}">{esc(text)}</text></g>'), w


def chips(x, y, items, gap=10):
    out, cx = [], x
    for t in items:
        s, w = pill(cx, y, t)
        out.append(s); cx += w + gap
    return "".join(out)


def card(x, y, w, h, title, sub=None, accent="none", mono_title=False, sub2=None):
    """A docs-site frame card: gradient fill, hairline border, radius 14, optional accent bar on the left."""
    bar = "" if accent == "none" else f'<rect x="{x}" y="{y + 12}" width="3" height="{h - 24}" rx="1.5" fill="{ACCENTS[accent]}"/>'
    tx = x + (22 if accent != "none" else 18)
    ty = y + (h / 2 + 6 if not sub else h / 2 - 4)
    s = (f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="14" fill="url(#card)" stroke="{HAIR}"/>{bar}'
         f'<text x="{tx}" y="{ty}" class="{"m" if mono_title else "s"}" font-size="{19 if mono_title else 20}" font-weight="700" fill="{TEXT}">{esc(title)}</text>')
    if sub:
        s += f'<text x="{tx}" y="{ty + 26}" class="s" font-size="16" fill="{MUTED}">{esc(sub)}</text>'
    if sub2:
        s += f'<text x="{tx}" y="{ty + 48}" class="s" font-size="16" fill="{MUTED}">{esc(sub2)}</text>'
    return s


_wire_n = [0]


def wire(d, color=BLUE, dur=2.4, begin=0.0, reverse=False):
    """A static dashed connector plus one dot travelling along it (SMIL; a negative begin offsets dots without a start delay)."""
    _wire_n[0] += 1
    pid = f"w{_wire_n[0]}"
    kp = ' keyPoints="1;0" keyTimes="0;1" calcMode="linear"' if reverse else ""
    return (f'<path id="{pid}" d="{d}" fill="none" stroke="{color}" stroke-opacity=".55" stroke-width="1.6" stroke-dasharray="4 6"/>'
            f'<circle r="4" fill="{color}"><animateMotion dur="{dur}s" begin="-{begin}s" repeatCount="indefinite"{kp}><mpath href="#{pid}"/></animateMotion></circle>')


def logo(cx, cy, size=120):
    """The AI Game Dev brand mark (the 'vertex' logo of the desktop app, packages/desktop/src/assets/logo.svg),
    centred at (cx, cy), `size` px tall. Uses the logoL/logoR/logoB gradients defined by panel()."""
    s = size / 650
    tx, ty = cx - 512 * s, cy - 495 * s
    return (f'<g transform="translate({tx:.2f} {ty:.2f}) scale({s:.4f})">'
            '<path d="M512 194 L512 504 L366 785 L185 785 Z" fill="url(#logoL)"/>'
            '<path d="M512 194 L839 785 L658 785 L512 504 Z" fill="url(#logoR)"/>'
            '<path d="M512 194 L512 294 L223 767 L185 785 Z" fill="url(#logoB)"/></g>')


def mark(cx, cy, size=120):
    """Hero visual: the brand logo with slow red/blue orbit rings and a soft glow."""
    r1, r2 = size * 0.95, size * 1.22
    return (f'<circle cx="{cx}" cy="{cy}" r="{size * 0.9}" fill="url(#logoGlow)"/>'
            f'<circle class="spin" cx="{cx}" cy="{cy}" r="{r1}" fill="none" stroke="{RED_TEXT}" stroke-opacity=".35" stroke-dasharray="2 10"/>'
            f'<circle class="spin r" cx="{cx}" cy="{cy}" r="{r2}" fill="none" stroke="{BLUE}" stroke-opacity=".25" stroke-dasharray="36 18"/>'
            f'<g class="pulse">{logo(cx, cy, size * 1.25)}</g>')


def hero(title, accent="", eyebrow_text="", subtitle=(), chip_items=(), right=None, w=1200, h=380, label=None):
    """Page hero: eyebrow, big Inter-800 title (accent words in the site red), subtitle lines, chips; a visual on
    the right (default: the brand-style mark). `right` is SVG markup drawn in the 760..1180 x-range."""
    tsize = 68 if len(title + accent) <= 16 else 56 if len(title + accent) <= 22 else 46
    t = (f'<text x="64" y="176" class="s" font-size="{tsize}" font-weight="800" letter-spacing="-2" fill="{TEXT}">{esc(title)}'
         + (f'<tspan fill="{RED_TEXT}">{esc(accent)}</tspan>' if accent else "") + "</text>")
    subs = "".join(f'<text x="64" y="{226 + i * 32}" class="s" font-size="22" fill="{SECOND}">{esc(line)}</text>' for i, line in enumerate(subtitle))
    cy = 226 + len(subtitle) * 32 + 14
    body = (eyebrow(64, 104, eyebrow_text) if eyebrow_text else "") + t + subs + chips(64, cy, chip_items)
    body += right if right is not None else mark(970, h / 2, 104)
    return panel(w, h, body, label or f"{title}{accent} — {' '.join(subtitle)}")


def terminal(lines, title="~/project", w=1200, label="A terminal session", hold=6.0, note_x=None, open_finished=True):
    """Animated terminal in the site style. lines: list of dicts
        {"cmd": "unity-mcp-cli login"}                       typed command
        {"out": [("muted","Open "),("link","https://..."),("ok","✓ ")], "note": "deviceLogin()"}
    colors: text, muted, link, ok, err, warn, red, key, str, num. Commands are pinned with textLength so the typing
    reveal matches any monospace fallback. The finished screen holds for `hold` seconds; static renderers show it."""
    COL = {"text": TEXT, "muted": MUTED, "link": BLUE, "ok": GREEN, "err": "#EF4444", "warn": YELLOW,
           "red": RED_TEXT, "key": "#A5C8E4", "str": "#F5D08A", "num": "#F59AB0"}
    CW, X0, Y0, LH, STEP = 10.2, 40, 92, 32, 0.042
    note_x = note_x or w - 40
    t, sched = 0.4, []
    for ln in lines:
        start = t + (0.5 if "cmd" in ln else ln.get("pause", 0.35))
        n = len(ln["cmd"]) + 2 if "cmd" in ln else 0
        t = start + (0.15 + (n + 1) * STEP if n else 0.25)
        sched.append((start, n))
    prompt_at = t + 0.5
    if open_finished:
        # Unreal-MCP variant: the loop opens on the finished screen (also the static image), holds it, fades out,
        # replays the session and ends finished again, so the first frame is never blank.
        s0 = hold + 0.8
        sched = [(st + s0, n) for st, n in sched]
        prompt_at += s0
        T = round(prompt_at + 1.5, 2)
        k = lambda x: round(x / T, 4)
        fade = lambda s: (f'<animate attributeName="opacity" dur="{T}s" repeatCount="indefinite" values="1;1;0;0;1;1" '
                          f'keyTimes="0;{k(hold)};{k(hold + .4)};{k(s)};{k(s + .25)};1"/>')
    else:
        fade_out = prompt_at + hold
        T = round(fade_out + 0.8, 2)
        k = lambda x: round(x / T, 4)
        fade = lambda s: (f'<animate attributeName="opacity" dur="{T}s" repeatCount="indefinite" values="0;0;1;1;0;0" '
                          f'keyTimes="0;{k(s)};{k(s + .25)};{k(fade_out)};{k(fade_out + .4)};1"/>')
    body, y = [], Y0
    for ln, (start, n) in zip(lines, sched):
        if "cmd" in ln:
            gone = round((n + 1) * CW + w, 1)
            fr = ([(gone, 0), (0, hold + .4), (0, start)] if open_finished else [(0, 0), (0, start)]) + [(round(i * CW, 1), start + .15 + i * STEP) for i in range(1, n + 1)] + [(gone, start + .15 + (n + 1) * STEP), (gone, T)]
            body.append(f'<g>{fade(start)}<text x="{X0}" y="{y}" class="m" font-size="18" textLength="{round(n * CW, 1)}" lengthAdjust="spacing">'
                        f'<tspan fill="{RED_TEXT}">❯</tspan> <tspan fill="{TEXT}">{esc(ln["cmd"])}</tspan></text>'
                        f'<rect x="{X0 - 2}" y="{y - 22}" width="{w}" height="30" fill="#070D14" transform="translate({w * 2} 0)">'
                        f'<animateTransform attributeName="transform" type="translate" dur="{T}s" repeatCount="indefinite" calcMode="discrete" '
                        f'values="{";".join(f"{x} 0" for x, _ in fr)}" keyTimes="{";".join(str(k(tt)) for _, tt in fr)}"/></rect></g>')
        else:
            spans = "".join(f'<tspan fill="{COL[c]}">{esc(s)}</tspan>' for c, s in ln["out"])
            note = (f'<text x="{note_x}" y="{y}" text-anchor="end" class="m" font-size="16" font-style="italic" fill="{MUTED}" fill-opacity=".8">← {esc(ln["note"])}</text>'
                    if ln.get("note") else "")
            body.append(f'<g>{fade(start)}<text x="{X0}" y="{y}" class="m" font-size="18">{spans}</text>{note}</g>')
        y += LH
    body.append(f'<g>{fade(prompt_at)}<text x="{X0}" y="{y}" class="m" font-size="18" fill="{RED_TEXT}">❯</text>'
                f'<rect class="blink" x="{X0 + 22}" y="{y - 17}" width="10" height="21" fill="{TEXT}"/></g>')
    h = y + 36
    bar = (f'<rect width="{w}" height="50" fill="{SURF}"/><rect y="50" width="{w}" height="1" fill="{HAIR}"/>'
           f'<circle cx="28" cy="25" r="7" fill="#FF5F57"/><circle cx="52" cy="25" r="7" fill="#FEBC2E"/><circle cx="76" cy="25" r="7" fill="#28C840"/>'
           f'<text x="{w / 2}" y="31" text-anchor="middle" class="m" font-size="16" fill="{MUTED}">{esc(title)}</text>')
    inner = f'<rect width="{w}" height="{h}" fill="#070D14"/>' + bar + "".join(body)
    return panel(w, h, inner, label, haze=False)


def write(path, svg):
    import os, xml.dom.minidom
    xml.dom.minidom.parseString(svg.encode("utf-8"))  # must be valid XML
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(svg)
    print("wrote", path)
