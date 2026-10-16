<h1 align="center"><a href="https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Animation/README.md">Unity AI Animation</a></h1>

<div align="center" width="100%">

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp.animation?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp.animation/)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/badge/License-MIT-green?labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Animation/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

</div>

<img width="100%" alt="Stats" src="https://github.com/IvanMurzak/Unity-MCP/raw/main/extensions/Animation/docs/img/ai-animation-glitch.gif"/>

AI-powered tools for Unity animation workflow. Create and modify AnimationClips and AnimatorControllers directly through natural language commands. Automate repetitive animation tasks like setting up state machines, configuring transitions, and adding keyframes. Ideal for rapid prototyping, procedural animation generation, and streamlining complex animator setups. Built on top of the [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) platform.

### How to use

- [Instructions](https://github.com/IvanMurzak/Unity-MCP?tab=readme-ov-file#step-2-install-mcp-client)
- [Video Tutorial for Visual Studio Code](https://www.youtube.com/watch?v=ZhP7Ju91mOE)
- [Video Tutorial for Visual Studio](https://www.youtube.com/watch?v=RGdak4T69mc)

[![DOWNLOAD INSTALLER](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/button/button_download.svg?raw=true)](https://github.com/IvanMurzak/Unity-MCP/releases/download/com.ivanmurzak.unity.mcp.animation%2F2.0.1/AI-Animation-Installer.unitypackage)

### Stability status

| Unity Version | Editmode                                                                                                                                                                               | Playmode                                                                                                                                                                               | Standalone                                                                                                                                                                               |
| ------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2022.3.62f3   | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml) |
| 2023.2.22f1   | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml) |
| 6000.3.1f1    | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)  |

## RQL (no extension tools)

With Unity-MCP 1.0.0 this package adds no MCP tools: it contributes an RQL module, so agents use
`rql-query` / `rql-mutate` (the `unity-rql` skill carries the full guide with examples).

| Former tool | RQL |
| --- | --- |
| `animation-create` | `animation \| call CreateClip("Assets/Animations/Walk.anim", 30, true)` (path, frameRate, loop) |
| `animation-get-data` | `unity.asset("….anim") \| select name length frameRate isLooping`, `… \| get curves \| select path type property keys`, `… \| get events` |
| `animation-modify` | `call SetCurve("", "Transform", "localPosition.y", [[0, 0], [1, 2]])`, `RemoveCurve`, `ClearCurves`, `AddEvent(0.5, "Footstep")`, `RemoveEvent`, `ClearEvents`; `set frameRate = 30, wrapMode = "Loop", settings.loopTime = true`, `set curves[0].keys[1].value = 2`, `set events[0].time = 0.25` |
| `animator-create` | `animation \| call CreateController("Assets/Animations/Player.controller")` |
| `animator-get-data` | `unity.asset("….controller") \| get layers \| get states \| select name speed motion.name transitions.destinationState.name`, `… \| get parameters` |
| `animator-modify` | `call AddParameter("Speed", "Float")`, `RemoveParameter`, `AddLayer`, `RemoveLayer`, `AddState("Walk", unity.asset("….anim"))`, `RemoveState`, `SetDefaultState`, `AddTransition("Idle", "Walk", "Speed", "Greater", 0.1)`, `AddAnyStateTransition`, `RemoveTransition`; `… \| get states \| where name == "Walk" \| set speed = 1.5` |

## Installation

### Option 1 - Installer

- **[⬇️ Download Installer](https://github.com/IvanMurzak/Unity-MCP/releases/download/com.ivanmurzak.unity.mcp.animation%2F2.0.1/AI-Animation-Installer.unitypackage)**
- **📂 Import installer into Unity project**
  > - You can double-click on the file - Unity will open it automatically
  > - OR: Open Unity Editor first, then click on `Assets/Import Package/Custom Package`, and choose the file

### Option 2 - OpenUPM-CLI

- [⬇️ Install OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- 📟 Open the command line in your Unity project folder

```bash
openupm add com.ivanmurzak.unity.mcp.animation
```


## Development and releases

This module lives in the Unity-MCP monorepo and retains its own UPM package and version. See [the shared test and release guide](../../docs/dev/extensions.md). Installers and signed UPM archives are attached only to this package's component release; unchanged packages are not rebuilt.
