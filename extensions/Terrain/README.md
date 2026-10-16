<h1 align="center"><a href="https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Terrain/README.md">Unity AI Terrain</a></h1>

<div align="center" width="100%">

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp.terrain?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp.terrain/)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

</div>

<img width="100%" alt="Terrain" src="https://github.com/IvanMurzak/Unity-MCP/raw/main/extensions/Terrain/docs/promo/promo-terrain.gif"/>

AI-powered tools for the Unity [Terrain](https://docs.unity3d.com/Manual/script-Terrain.html) workflow. Create Terrain GameObjects backed by new TerrainData, set heightmap resolution and world size, sculpt and sample heights over a region or the whole terrain, manage TerrainLayers, paint texture layers onto the alphamap, configure detail and tree prototypes, scatter trees, stitch neighbor terrains, and modify any terrain component field directly through natural language commands — no manual inspector navigation. Wraps Unity's built-in **Terrain** modules (`com.unity.modules.terrain` + `com.unity.modules.terrainphysics`). Ideal for rapid greyboxing, procedural landscape generation, and terrain authoring. Built on top of the [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) platform.

### How to use

- [Instructions](https://github.com/IvanMurzak/Unity-MCP?tab=readme-ov-file#step-2-install-mcp-client)
- [Video Tutorial for Visual Studio Code](https://www.youtube.com/watch?v=ZhP7Ju91mOE)
- [Video Tutorial for Visual Studio](https://www.youtube.com/watch?v=RGdak4T69mc)

[![DOWNLOAD INSTALLER](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/button/button_download.svg?raw=true)](https://github.com/IvanMurzak/Unity-MCP/releases/download/com.ivanmurzak.unity.mcp.terrain%2F2.0.1/AI-Terrain-Installer.unitypackage)

### Stability status

| Unity Version | Editmode                                                                                                                                                                                                  | Playmode                                                                                                                                                                                                  | Standalone                                                                                                                                                                                                  |
| ------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2022.3.62f3   | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       |
| 2023.2.22f1   | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       |
| 6000.3.1f1    | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)        | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)        | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)        |

## RQL (no extension tools)

On the RQL branch of Unity-MCP this package adds no MCP tools: it contributes an RQL module, so agents use
`rql-query` / `rql-mutate` (the `unity-rql` skill carries the full guide with examples). `T` below stands for
`objects | get components | where $ is Terrain`.

| Former tool | RQL |
| --- | --- |
| `terrain-create` | `terrain \| call Create("Island", "Assets/Terrain/Island.asset", 257, "(500, 120, 500)")` (name, dataPath, heightmapResolution, size, position) |
| `terrain-list` / `-get` / `-get-component` | `T \| select $ref gameObject.name terrainData.size terrainData.heightmapResolution terrainData.terrainLayers.name` |
| `terrain-set-heightmap-resolution` / `-set-size` | `T \| set terrainData.heightmapResolution = 513, terrainData.size = "(800, 200, 800)"` (validated, not rounded) |
| `terrain-set-heights` | `T \| call SetHeights(0.6, 100, 100, 32, 32)`, `call SetHeightGrid([[0.1, 0.2], [0.3, 0.4]], 10, 20)` |
| `terrain-sample-heights` | `T \| select s=(terrain.heightStats($))`, `terrain.heights($, x, y, w, h)`, `terrain.heightAt($, worldX, worldZ)` |
| `terrain-add-layer` / `-remove-layer` / `-paint-layer` | `terrain \| call CreateLayer("Assets/Terrain/Grass.terrainlayer", unity.asset("….png"))`, `T \| call AddLayer(…)`, `RemoveLayer(0)`, `PaintLayer(1, 1.0, 0, 0, 64, 64)` |
| `terrain-set-detail-prototypes` / `-set-tree-prototypes` | `T \| call AddDetailPrototype(…)`, `AddTreePrototype(unity.asset("….prefab"))`; `set terrainData.treePrototypes[0] = {prefab: …, bendFactor: 0.3}` |
| `terrain-place-trees` | `T \| call PlaceTrees(0, 200, 0.1, 0.1, 0.5, 0.5)`, `PlaceTreesAt(["(0.5, 0.5)"], 0)`, `ClearTrees()` |
| `terrain-set-neighbors` | `T \| call SetNeighbors(left, top, right, bottom)` |
| `terrain-modify-component` | plain `set` on Terrain / TerrainData members |

## Installation

### Option 1 - Installer

- **[Download Installer](https://github.com/IvanMurzak/Unity-MCP/releases/download/com.ivanmurzak.unity.mcp.terrain%2F2.0.1/AI-Terrain-Installer.unitypackage)**
- **Import installer into Unity project**
  > - You can double-click on the file - Unity will open it automatically
  > - OR: Open Unity Editor first, then click on `Assets/Import Package/Custom Package`, and choose the file

### Option 2 - OpenUPM-CLI

- [Install OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- Open the command line in your Unity project folder

```bash
openupm add com.ivanmurzak.unity.mcp.terrain
```


## Development and releases

This module lives in the Unity-MCP monorepo and retains its own UPM package and version. See [the shared test and release guide](../../docs/dev/extensions.md). Installers and signed UPM archives are attached only to this package's component release; unchanged packages are not rebuilt.
