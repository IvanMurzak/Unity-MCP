<h1 align="center"><a href="https://github.com/IvanMurzak/Unity-AI-Cinemachine?tab=readme-ov-file#unity-ai-cinemachine">Unity AI Cinemachine</a></h1>

<div align="center" width="100%">

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp.cinemachine?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp.cinemachine/)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-AI-Cinemachine/workflows/release/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-AI-Cinemachine/actions/workflows/release.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-AI-Cinemachine 'Stars')](https://github.com/IvanMurzak/Unity-AI-Cinemachine/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-AI-Cinemachine?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-AI-Cinemachine/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

</div>

AI-powered tools for the Unity [Cinemachine](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1) workflow. Create and configure virtual cameras, set Follow / Look At targets, build the camera pipeline (Body / Aim / Noise / Lens), tune priorities and default blends, and modify any Cinemachine component field directly through natural language commands — no manual inspector navigation. Wraps `com.unity.cinemachine` **3.1.6**. Ideal for rapid camera blocking, cutscene setup, and procedural camera rigs. Built on top of the [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) platform.

### How to use

- [Instructions](https://github.com/IvanMurzak/Unity-MCP?tab=readme-ov-file#step-2-install-mcp-client)
- [Video Tutorial for Visual Studio Code](https://www.youtube.com/watch?v=ZhP7Ju91mOE)
- [Video Tutorial for Visual Studio](https://www.youtube.com/watch?v=RGdak4T69mc)

[![DOWNLOAD INSTALLER](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/button/button_download.svg?raw=true)](https://github.com/IvanMurzak/Unity-AI-Cinemachine/releases/latest/download/AI-Cinemachine-Installer.unitypackage)

### Stability status

| Unity Version | Editmode                                                                                                                                                                                                       | Playmode                                                                                                                                                                                                       | Standalone                                                                                                                                                                                                       |
| ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2022.3.62f3   | [![r](https://github.com/IvanMurzak/Unity-AI-Cinemachine/workflows/release/badge.svg?job=test-unity-2022-3-62f3-editmode)](https://github.com/IvanMurzak/Unity-AI-Cinemachine/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-AI-Cinemachine/workflows/release/badge.svg?job=test-unity-2022-3-62f3-playmode)](https://github.com/IvanMurzak/Unity-AI-Cinemachine/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-AI-Cinemachine/workflows/release/badge.svg?job=test-unity-2022-3-62f3-standalone)](https://github.com/IvanMurzak/Unity-AI-Cinemachine/actions/workflows/release.yml)  |
| 2023.2.22f1   | [![r](https://github.com/IvanMurzak/Unity-AI-Cinemachine/workflows/release/badge.svg?job=test-unity-2023-2-22f1-editmode)](https://github.com/IvanMurzak/Unity-AI-Cinemachine/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-AI-Cinemachine/workflows/release/badge.svg?job=test-unity-2023-2-22f1-playmode)](https://github.com/IvanMurzak/Unity-AI-Cinemachine/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-AI-Cinemachine/workflows/release/badge.svg?job=test-unity-2023-2-22f1-standalone)](https://github.com/IvanMurzak/Unity-AI-Cinemachine/actions/workflows/release.yml)  |
| 6000.3.1f1    | [![r](https://github.com/IvanMurzak/Unity-AI-Cinemachine/workflows/release/badge.svg?job=test-unity-6000-3-1f1-editmode)](https://github.com/IvanMurzak/Unity-AI-Cinemachine/actions/workflows/release.yml)   | [![r](https://github.com/IvanMurzak/Unity-AI-Cinemachine/workflows/release/badge.svg?job=test-unity-6000-3-1f1-playmode)](https://github.com/IvanMurzak/Unity-AI-Cinemachine/actions/workflows/release.yml)   | [![r](https://github.com/IvanMurzak/Unity-AI-Cinemachine/workflows/release/badge.svg?job=test-unity-6000-3-1f1-standalone)](https://github.com/IvanMurzak/Unity-AI-Cinemachine/actions/workflows/release.yml)   |

## AI Cinemachine Tools

14 tools, grouped by purpose:

### Camera lifecycle

- `cinemachine-camera-create` - Create a `CinemachineCamera` GameObject in the active scene
- `cinemachine-camera-list` - List all Cinemachine cameras in the scene
- `cinemachine-camera-get` - Get a Cinemachine camera's data (priority, lens, pipeline, targets)
- `cinemachine-brain-ensure` - Ensure a `CinemachineBrain` exists on the main/target Unity `Camera`

### Targets & framing

- `cinemachine-set-targets` - Set the Follow and/or Look At targets on a camera
- `cinemachine-set-priority` - Set a camera's priority (which camera the Brain activates)
- `cinemachine-set-lens` - Set lens settings (FOV / focal length, near/far clip, dutch)

### Pipeline components

- `cinemachine-set-body` - Set the Body (position-control) component (e.g. `CinemachineFollow`)
- `cinemachine-set-aim` - Set the Aim (rotation-control) component (e.g. `CinemachineRotationComposer`)
- `cinemachine-set-noise` - Set the Noise (`CinemachineBasicMultiChannelPerlin`) component
- `cinemachine-add-extension` - Add a Cinemachine extension (e.g. Deoccluder, Confiner)

### Blends & generic

- `cinemachine-set-default-blend` - Set the Brain's default blend (style + time)
- `cinemachine-get` - Generic read: serialize any Cinemachine component on a GameObject
- `cinemachine-modify` - Generic write: apply a `SerializedMember` diff to any Cinemachine component via ReflectorNet (escape hatch for fields not covered by the dedicated tools)

## Installation

### Option 1 - Installer

- **[Download Installer](https://github.com/IvanMurzak/Unity-AI-Cinemachine/releases/latest/download/AI-Cinemachine-Installer.unitypackage)**
- **Import installer into Unity project**
  > - You can double-click on the file - Unity will open it automatically
  > - OR: Open Unity Editor first, then click on `Assets/Import Package/Custom Package`, and choose the file

### Option 2 - OpenUPM-CLI

- [Install OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- Open the command line in your Unity project folder

```bash
openupm add com.ivanmurzak.unity.mcp.cinemachine
```
