<h1 align="center"><a href="https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Splines/README.md">Unity AI Splines</a></h1>

<div align="center" width="100%">

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp.splines?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp.splines/)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/badge/License-MIT-green?labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Splines/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

</div>

<img width="100%" alt="Splines" src="https://github.com/IvanMurzak/Unity-MCP/raw/main/extensions/Splines/docs/promo/promo-splines.gif"/>

**AI Splines** is an extension for [AI Game Developer (Unity-MCP)](https://github.com/IvanMurzak/Unity-MCP) that exposes [Unity Splines](https://docs.unity3d.com/Packages/com.unity.splines@2.8/manual/index.html) authoring to AI assistants through the [Model Context Protocol](https://modelcontextprotocol.io/introduction). Create and edit splines, knots, and tangents with natural language.

It wraps `com.unity.splines` **2.8.4** and requires Unity **2022.3** or newer.

## Installation

1. Install [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) in your Unity project.
2. Install this package via OpenUPM:

```bash
openupm add com.ivanmurzak.unity.mcp.splines
```

Or add it through the Unity-MCP plugin window's **Extensions** list.

## RQL (no extension tools)

Since Unity-MCP 1.0.0 this package adds no MCP tools: it contributes an RQL module, so agents use
`rql-query` / `rql-mutate` (the `unity-rql` skill carries the full guide with examples).

| Former tool | RQL |
| --- | --- |
| `splines-container-create` | `splines \| call CreateContainer("Road", "(0, 0, 5)", "(0, 90, 0)", true)` |
| `splines-add-spline` | `… \| where $ is SplineContainer \| call AddSpline()` (`RemoveSpline(i)`) |
| `splines-add-knot` / `-insert-knot` / `-remove-knot` | `call AddKnot("(1, 0, 2)")`, `AddKnots([...])`, `InsertKnot(1, "(2, 0, 1)")`, `RemoveKnot(0)` (optional leading spline index) |
| `splines-set-knot` / `-set-tangent-mode` | `set splines[0].knots[2].position = "(10, 1, 0)"`, `set splines[0].knots[0].tangentMode = "Linear"` |
| `splines-set-closed` | `set splines[0].closed = true` |
| `splines-evaluate` | `call Evaluate(0.5)` (or `Evaluate(splineIndex, t)`) |
| `splines-list` / `-get-knots` / `-get` / `-modify` | `splines \| get containers`, `… \| get splines \| get knots \| select index position tangentMode`, plain `select` / `set` |

## License

MIT © [Ivan Murzak](https://github.com/IvanMurzak)


## Development and releases

This module lives in the Unity-MCP monorepo and retains its own UPM package and version. See [the shared test and release guide](../../docs/dev/extensions.md). Installers and signed UPM archives are attached only to this package's component release; unchanged packages are not rebuilt.
