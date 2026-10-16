<h1 align="center"><a href="https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Cinemachine/README.md">Unity AI Cinemachine</a></h1>

<div align="center" width="100%">

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp.cinemachine?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp.cinemachine/)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

</div>

<img width="100%" alt="Cinemachine" src="https://github.com/IvanMurzak/Unity-MCP/raw/main/extensions/Cinemachine/docs/promo/promo-cinemachine.gif"/>

AI-powered tools for the Unity [Cinemachine](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1) workflow. Create and configure virtual cameras, set Follow / Look At targets, build the camera pipeline (Body / Aim / Noise / Lens), tune priorities and default blends, and modify any Cinemachine component field directly through natural language commands — no manual inspector navigation. Wraps `com.unity.cinemachine` **3.1.6**. Ideal for rapid camera blocking, cutscene setup, and procedural camera rigs. Built on top of the [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) platform.

### How to use

- [Instructions](https://github.com/IvanMurzak/Unity-MCP?tab=readme-ov-file#step-2-install-mcp-client)
- [Video Tutorial for Visual Studio Code](https://www.youtube.com/watch?v=ZhP7Ju91mOE)
- [Video Tutorial for Visual Studio](https://www.youtube.com/watch?v=RGdak4T69mc)

[![DOWNLOAD INSTALLER](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/button/button_download.svg?raw=true)](https://github.com/IvanMurzak/Unity-MCP/releases/download/com.ivanmurzak.unity.mcp.cinemachine%2F2.0.1/AI-Cinemachine-Installer.unitypackage)

### Stability status

| Unity Version | Editmode                                                                                                                                                                                                       | Playmode                                                                                                                                                                                                       | Standalone                                                                                                                                                                                                       |
| ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2022.3.62f3   | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)  |
| 2023.2.22f1   | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)  |
| 6000.3.1f1    | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)   | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)   | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)   |

## RQL (no extension tools)

With Unity-MCP 1.0.0 this package adds no MCP tools: it contributes an RQL module, so agents use
`rql-query` / `rql-mutate` (the `unity-rql` skill carries the full guide with examples). `C` below stands for
`cinemachine | get cameras | where name == "FollowCam"`.

| Former tool | RQL |
| --- | --- |
| `cinemachine-camera-create` | `cinemachine \| call CreateCamera("FollowCam", "Player", null, "(0, 3, -6)", "(20, 0, 0)", 10)` (also ensures a `CinemachineBrain`) |
| `cinemachine-camera-list` / `-camera-get` | `cinemachine \| get cameras \| select name Priority.Value IsLive Follow.name Lens.FieldOfView body.$type aim.$type` |
| `cinemachine-brain-ensure` | `cinemachine \| call EnsureBrain()` (or `EnsureBrain("Camera name")`) |
| `cinemachine-set-targets` | `C \| call SetTargets("Player")` / `SetTargets("Player", "Boss")`, or `set Follow = ref("GameObject#…")` |
| `cinemachine-set-priority` | `C \| set Priority.Value = 20` |
| `cinemachine-set-lens` | `C \| set Lens.FieldOfView = 50, Lens.NearClipPlane = 0.3, Lens.Dutch = 5` |
| `cinemachine-set-body` / `-set-aim` | `C \| call SetBody("Follow")`, `call SetAim("RotationComposer")` (`"None"` removes), then `set body.FollowOffset = "(0, 2, -8)"` |
| `cinemachine-set-noise` | `C \| call SetNoise("Handheld_normal_mild", 1.5, 1)`, `set noise.AmplitudeGain = 0.5` |
| `cinemachine-add-extension` | `objects \| where name == "FollowCam" \| call AddComponent("CinemachineDeoccluder")` |
| `cinemachine-set-default-blend` | `cinemachine \| get brains \| set DefaultBlend.Style = "EaseInOut", DefaultBlend.Time = 1.5` |
| `cinemachine-get` / `-modify` | plain `select` / `set` on any Cinemachine component (`objects \| get components \| where $ is CinemachineFollow \| …`) |

## Installation

### Option 1 - Installer

- **[Download Installer](https://github.com/IvanMurzak/Unity-MCP/releases/download/com.ivanmurzak.unity.mcp.cinemachine%2F2.0.1/AI-Cinemachine-Installer.unitypackage)**
- **Import installer into Unity project**
  > - You can double-click on the file - Unity will open it automatically
  > - OR: Open Unity Editor first, then click on `Assets/Import Package/Custom Package`, and choose the file

### Option 2 - OpenUPM-CLI

- [Install OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- Open the command line in your Unity project folder

```bash
openupm add com.ivanmurzak.unity.mcp.cinemachine
```


## Development and releases

This module lives in the Unity-MCP monorepo and retains its own UPM package and version. See [the shared test and release guide](../../docs/dev/extensions.md). Installers and signed UPM archives are attached only to this package's component release; unchanged packages are not rebuilt.
