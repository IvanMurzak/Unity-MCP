<h1 align="center"><a href="https://github.com/IvanMurzak/Unity-AI-ProBuilder/?tab=readme-ov-file#unity-ai-probuilder">Unity AI ProBuilder</a></h1>

<div align="center" width="100%">

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp.probuilder?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp.probuilder/)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-AI-ProBuilder 'Stars')](https://github.com/IvanMurzak/Unity-AI-ProBuilder/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-AI-ProBuilder?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

</div>

<img width="100%" alt="Stats" src="https://github.com/IvanMurzak/Unity-AI-ProBuilder/raw/main/docs/img/ai-probuilder-glitch.gif"/>

AI-powered 3D modeling tools for Unity ProBuilder. Enables AI assistants to create and manipulate editable meshes through natural language commands. Create primitive shapes, extrude faces, bevel edges, apply materials, merge objects, and perform advanced mesh operations like bridging and subdivision. Supports semantic face selection by direction (up, down, left, right) for intuitive editing. Perfect for rapid level prototyping and procedural geometry generation. Built on top of the [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) platform.

### How to use

- [Instructions](https://github.com/IvanMurzak/Unity-MCP?tab=readme-ov-file#step-2-install-mcp-client)
- [Video Tutorial for Visual Studio Code](https://www.youtube.com/watch?v=ZhP7Ju91mOE)
- [Video Tutorial for Visual Studio](https://www.youtube.com/watch?v=RGdak4T69mc)

[![DOWNLOAD INSTALLER](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/button/button_download.svg?raw=true)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/releases/latest/download/AI-ProBuilder-Installer.unitypackage)

### Stability status

| Unity Version | Editmode                                                                                                                                                                               | Playmode                                                                                                                                                                               | Standalone                                                                                                                                                                               |
| ------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2022.3.62f3   | [![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg?job=test-unity-2022-3-62f3-editmode)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg?job=test-unity-2022-3-62f3-playmode)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg?job=test-unity-2022-3-62f3-standalone)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml) |
| 2023.2.22f1   | [![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg?job=test-unity-2023-2-22f1-editmode)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg?job=test-unity-2023-2-22f1-playmode)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg?job=test-unity-2023-2-22f1-standalone)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml) |
| 6000.3.1f1    | [![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg?job=test-unity-6000-3-1f1-editmode)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg?job=test-unity-6000-3-1f1-playmode)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg?job=test-unity-6000-3-1f1-standalone)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml)  |

## RQL (no extension tools)

Since Unity-MCP 1.0.0 this package adds no MCP tools: it contributes an RQL module, so agents use
`rql-query` / `rql-mutate` (the `unity-rql` skill carries the full "ProBuilder" guide with examples).
`M` below stands for `probuilder | get meshes | where gameObject.name == "Crate"`.

| Former tool | RQL |
| --- | --- |
| `probuilder-create-shape` | `probuilder \| call CreateShape("Cube", "Crate", "(2, 1, 2)", "(0, 0.5, 0)")` (type, name, size, position, rotation, parent) |
| `probuilder-create-poly-shape` | `probuilder \| call CreatePolyShape(["(0, 0)", "(4, 0)", "(4, 3)"], 0.5, "Floor")` (points, height, name, position, flipNormals, parent) |
| `probuilder-get-mesh-info` | `M \| select faceCount vertexCount edgeCount bounds directions materials`, `M \| get faces \| select index direction center edges`, `probuilder.vertices($, start, count)` |
| `probuilder-extrude` | `M \| call Extrude(probuilder.facing($, "Up"), 0.5)` (faces, distance, method) |
| `probuilder-delete-faces` | `M \| call DeleteFaces([0, 2])` |
| `probuilder-set-face-material` | `M \| call SetFaceMaterial(probuilder.facing($, "Up"), "Assets/Materials/Grass.mat")` |
| `probuilder-flip-normals` | `M \| call FlipNormals([0])` (all faces when omitted) |
| `probuilder-bevel` | `M \| call Bevel(probuilder.edges($, [4]), 0.1)` |
| `probuilder-bridge` | `M \| call Bridge(probuilder.openEdges($)[0], probuilder.openEdges($)[2])` |
| `probuilder-connect-edges` | `M \| call ConnectEdges(probuilder.edges($, [4]))` |
| `probuilder-subdivide-edges` | `M \| call SubdivideEdges([[0, 1]], 2)` |
| `probuilder-set-pivot` | `M \| call SetPivot("Center")` / `SetPivot("FirstVertex")` / `SetPivotAt("(0, 0, 0)")` |
| `probuilder-merge-objects` | `probuilder \| call Merge([ref("ProBuilderMesh#…"), ref("GameObject#…")], true)` |

Faces are index lists (`[0, 1]`), edges vertex index pairs (`[[0, 1]]`); `probuilder.facing($, "Up")` replaces the former
`faceDirection` parameter. Every call validates its input first and is undoable.

## Installation

### Option 1 - Installer

- **[⬇️ Download Installer](https://github.com/IvanMurzak/Unity-AI-ProBuilder/releases/latest/download/AI-ProBuilder-Installer.unitypackage)**
- **📂 Import installer into Unity project**
  > - You can double-click on the file - Unity will open it automatically
  > - OR: Open Unity Editor first, then click on `Assets/Import Package/Custom Package`, and choose the file

### Option 2 - OpenUPM-CLI

- [⬇️ Install OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- 📟 Open the command line in your Unity project folder

```bash
openupm add com.ivanmurzak.unity.mcp.probuilder
```
