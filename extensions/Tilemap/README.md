<h1 align="center"><a href="https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Tilemap/README.md">Unity AI Tilemap</a></h1>

<div align="center" width="100%">

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp.tilemap?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp.tilemap/)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

</div>

<img width="100%" alt="Tilemap" src="https://github.com/IvanMurzak/Unity-MCP/raw/main/extensions/Tilemap/docs/promo/promo-tilemap.gif"/>

AI-powered tools for the Unity [Tilemap](https://docs.unity3d.com/Manual/class-Tilemap.html) workflow. Create `Grid` + `Tilemap` GameObjects, paint and clear tiles, box-fill regions, read individual tiles, create `Tile` and `RuleTile` assets, set per-cell collider type, color, and transform, configure tilemap orientation and anchor, list / get tilemap components, and modify any tilemap component field directly through natural language commands — no manual tile-palette painting. Wraps Unity's built-in **Tilemap** module (`com.unity.modules.tilemap`) plus the **2D Tilemap Extras** package (`com.unity.2d.tilemap.extras`) for RuleTiles. Ideal for rapidly building 2D levels, prototyping grid layouts, and procedural tilemap generation. Built on top of the [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) platform.

### How to use

- [Instructions](https://github.com/IvanMurzak/Unity-MCP?tab=readme-ov-file#step-2-install-mcp-client)
- [Video Tutorial for Visual Studio Code](https://www.youtube.com/watch?v=ZhP7Ju91mOE)
- [Video Tutorial for Visual Studio](https://www.youtube.com/watch?v=RGdak4T69mc)

[![DOWNLOAD INSTALLER](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/button/button_download.svg?raw=true)](https://github.com/IvanMurzak/Unity-MCP/releases/download/com.ivanmurzak.unity.mcp.tilemap%2F2.0.1/AI-Tilemap-Installer.unitypackage)

### Stability status

| Unity Version | Editmode                                                                                                                                                                                                          | Playmode                                                                                                                                                                                                          | Standalone                                                                                                                                                                                                          |
| ------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2022.3.62f3   | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       |
| 2023.2.22f1   | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       |
| 6000.3.1f1    | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)        | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)        | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)        |

## RQL (no extension tools)

Since 2.0.0 this package adds no MCP tools: it contributes an RQL module to [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP)
1.0.0, so agents use `rql-query` / `rql-mutate` (the `unity-rql` skill carries the full guide with examples).
`T` below stands for `objects | get components | where $ is Tilemap`.

| Former tool | RQL |
| --- | --- |
| `tilemap-create` | `tilemap \| call CreateTilemap("Ground")` (optional parent, cellSize, gridName) |
| `tilemap-list` / `tilemap-get` | `T \| select $ref gameObject.name tileCount usedTiles cellBounds orientation` |
| `tilemap-set-orientation` / `tilemap-modify` | `T \| set orientation = "XZ", tileAnchor = "(0.5, 0.5, 0)"` (any member) |
| `tilemap-set-tile` | `T \| call SetTile("(0, 0, 0)", "Assets/Tiles/Grass.asset")` (`SetTiles([...], tile)`, null erases) |
| `tilemap-box-fill` | `T \| call BoxFill("Assets/Tiles/Grass.asset", -5, -3, 5, 3)` (`ClearBox`, `FloodFill`) |
| `tilemap-clear` | `T \| call ClearAllTiles()` |
| `tilemap-get-tile` | `T \| select c=(tilemap.cell($, "(0, 0, 0)"))` (`tilemap.tileAt`, `tilemap.tiles`, `tilemap.count`, `tilemap.cells`) |
| `tilemap-set-tile-flags` | `T \| call SetColor("(0, 0, 0)", "#FF8080")`, `call SetTransform("(0, 0, 0)", 90)` |
| `tilemap-set-collider-type` | `T \| call SetColliderType("(0, 0, 0)", "None")` |
| `tilemap-create-tile-asset` | `tilemap \| call CreateTile("Assets/Tiles/Grass.asset", "Assets/Art/grass.png")` |
| `tilemap-create-rule-tile` | `tilemap \| call CreateRuleTile("Assets/Tiles/Ground.asset")`, then `unity.asset(…) \| call AddRule(sprite, "NNN/T.T/TTT")` |

## Installation

### Option 1 - Installer

- **[Download Installer](https://github.com/IvanMurzak/Unity-MCP/releases/download/com.ivanmurzak.unity.mcp.tilemap%2F2.0.1/AI-Tilemap-Installer.unitypackage)**
- **Import installer into Unity project**
  > - You can double-click on the file - Unity will open it automatically
  > - OR: Open Unity Editor first, then click on `Assets/Import Package/Custom Package`, and choose the file

### Option 2 - OpenUPM-CLI

- [Install OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- Open the command line in your Unity project folder

```bash
openupm add com.ivanmurzak.unity.mcp.tilemap
```


## Development and releases

This module lives in the Unity-MCP monorepo and retains its own UPM package and version. See [the shared test and release guide](../../docs/dev/extensions.md). Installers and signed UPM archives are attached only to this package's component release; unchanged packages are not rebuilt.
