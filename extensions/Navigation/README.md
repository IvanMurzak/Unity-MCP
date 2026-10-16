<h1 align="center"><a href="https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Navigation/README.md">Unity AI Navigation</a></h1>

<div align="center" width="100%">

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp.navigation?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp.navigation/)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

</div>

<img width="100%" alt="Navigation" src="https://github.com/IvanMurzak/Unity-MCP/raw/main/extensions/Navigation/docs/promo/promo-navigation.gif"/>

AI-powered tools for the Unity [AI Navigation](https://docs.unity3d.com/Packages/com.unity.ai.navigation@1.1/manual/index.html) (NavMesh) workflow. Add `NavMeshSurface`s and bake them, configure bake settings (agent radius / height / slope / voxel size), add `NavMeshAgent`s and set their destinations, place `NavMeshModifier` and `NavMeshModifierVolume` components to carve or override areas, connect `NavMeshLink`s, list / get navigation components, and modify any navigation component field directly through natural language commands — no manual Navigation window navigation. Wraps Unity's **AI Navigation** package (`com.unity.ai.navigation`). Ideal for setting up agent pathfinding, runtime NavMesh baking, and procedural level navigation. Built on top of the [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) platform.

### How to use

- [Instructions](https://github.com/IvanMurzak/Unity-MCP?tab=readme-ov-file#step-2-install-mcp-client)
- [Video Tutorial for Visual Studio Code](https://www.youtube.com/watch?v=ZhP7Ju91mOE)
- [Video Tutorial for Visual Studio](https://www.youtube.com/watch?v=RGdak4T69mc)

[![DOWNLOAD INSTALLER](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/button/button_download.svg?raw=true)](https://github.com/IvanMurzak/Unity-MCP/releases/download/com.ivanmurzak.unity.mcp.navigation%2F2.0.1/AI-Navigation-Installer.unitypackage)

### Stability status

| Unity Version | Editmode                                                                                                                                                                                                          | Playmode                                                                                                                                                                                                          | Standalone                                                                                                                                                                                                          |
| ------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2022.3.62f3   | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       |
| 2023.2.22f1   | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       |
| 6000.3.1f1    | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)        | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)        | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)        |

## RQL (no extension tools)

On the RQL branch of Unity-MCP this package adds no MCP tools: it contributes an RQL module, so agents use
`rql-query` / `rql-mutate` (the `unity-rql` skill carries the full guide with examples). `S` below stands for
`objects | get components | where $ is NavMeshSurface`.

| Former tool | RQL |
| --- | --- |
| `navigation-surface-add` / `-agent-add` / `-link-add` / `-modifier-add` / `-modifier-volume-add` | `objects \| where name == "Ground" \| call AddComponent("NavMeshSurface")` (also `NavMeshAgent`, `NavMeshLink`, `NavMeshModifier`, `NavMeshModifierVolume`) |
| `navigation-surface-bake` | `S \| call Bake()` / `Clear()`, `navigation \| call BakeAll()` / `ClearAll()` |
| `navigation-set-bake-settings` | `navigation \| get agentTypes \| where name == "Humanoid" \| set agentRadius = 0.4, agentSlope = 40`; per surface `S \| set overrideVoxelSize = true, voxelSize = 0.1` |
| `navigation-agent-set-destination` | `objects \| get components \| where $ is NavMeshAgent \| call SetDestination("(10, 0, 5)")` (also `Warp`, `ResetPath`; Play Mode) |
| `navigation-list` / `-get` | `S \| select gameObject.name agentType navMeshData buildSettings.agentRadius`, `navigation \| get areas` |
| `navigation-modify` | plain `set`, with names for ids: `set agentType = "Humanoid", areaName = "Jump", areaNames = ["Walkable"], affectedAgents = ["All"]` |

## Installation

### Option 1 - Installer

- **[Download Installer](https://github.com/IvanMurzak/Unity-MCP/releases/download/com.ivanmurzak.unity.mcp.navigation%2F2.0.1/AI-Navigation-Installer.unitypackage)**
- **Import installer into Unity project**
  > - You can double-click on the file - Unity will open it automatically
  > - OR: Open Unity Editor first, then click on `Assets/Import Package/Custom Package`, and choose the file

### Option 2 - OpenUPM-CLI

- [Install OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- Open the command line in your Unity project folder

```bash
openupm add com.ivanmurzak.unity.mcp.navigation
```


## Development and releases

This module lives in the Unity-MCP monorepo and retains its own UPM package and version. See [the shared test and release guide](../../docs/dev/extensions.md). Installers and signed UPM archives are attached only to this package's component release; unchanged packages are not rebuilt.
