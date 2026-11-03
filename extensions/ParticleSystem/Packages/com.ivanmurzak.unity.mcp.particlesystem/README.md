<h1 align="center"><a href="https://github.com/IvanMurzak/Unity-AI-ParticleSystem?tab=readme-ov-file#unity-ai-particle-system">Unity AI Particle System</a></h1>

<div align="center" width="100%">

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp.particlesystem?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp.particlesystem/)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/workflows/release/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/actions/workflows/release.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-AI-ParticleSystem 'Stars')](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-AI-ParticleSystem?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

</div>

<img width="100%" alt="Promo" src="https://github.com/IvanMurzak/Unity-AI-ParticleSystem/raw/main/docs/img/particle-system-glitch.gif"/>

AI-powered tools for Unity ParticleSystem workflow. Inspect and modify ParticleSystem components directly through natural language commands. Configure emission settings, shape modules, velocity curves, color gradients, and all 24 particle system modules without manual inspector navigation. Ideal for rapid prototyping, procedural effects generation, and streamlining complex particle setups. Built on top of the [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) platform.

### How to use

- [Instructions](https://github.com/IvanMurzak/Unity-MCP?tab=readme-ov-file#step-2-install-mcp-client)
- [Video Tutorial for Visual Studio Code](https://www.youtube.com/watch?v=ZhP7Ju91mOE)
- [Video Tutorial for Visual Studio](https://www.youtube.com/watch?v=RGdak4T69mc)

[![DOWNLOAD INSTALLER](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/button/button_download.svg?raw=true)](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/releases/latest/download/AI-ParticleSystem-Installer.unitypackage)

### Stability status

| Unity Version | Editmode                                                                                                                                                                               | Playmode                                                                                                                                                                               | Standalone                                                                                                                                                                               |
| ------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2022.3.62f3   | [![r](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/workflows/release/badge.svg?job=test-unity-2022-3-62f3-editmode)](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/workflows/release/badge.svg?job=test-unity-2022-3-62f3-playmode)](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/workflows/release/badge.svg?job=test-unity-2022-3-62f3-standalone)](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/actions/workflows/release.yml) |
| 2023.2.22f1   | [![r](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/workflows/release/badge.svg?job=test-unity-2023-2-22f1-editmode)](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/workflows/release/badge.svg?job=test-unity-2023-2-22f1-playmode)](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/workflows/release/badge.svg?job=test-unity-2023-2-22f1-standalone)](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/actions/workflows/release.yml) |
| 6000.3.1f1    | [![r](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/workflows/release/badge.svg?job=test-unity-6000-3-1f1-editmode)](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/workflows/release/badge.svg?job=test-unity-6000-3-1f1-playmode)](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/workflows/release/badge.svg?job=test-unity-6000-3-1f1-standalone)](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/actions/workflows/release.yml)  |

## RQL (no extension tools)

This package adds no MCP tools: it contributes an RQL module, so agents use `rql-query` / `rql-mutate` (the `unity-rql`
skill carries the full "Particle systems" guide with examples). `P` below stands for
`objects | get components | where $ is ParticleSystem`.

| Former tool | RQL |
| --- | --- |
| `particle-system-get` | `P \| select main.duration main.startLifetime emission.rateOverTime shape.shapeType` (any module member) |
| `particle-system-modify` | `P \| set main.startLifetime = 3, emission.rateOverTime = [5, 25], main.startColor = "#ff8800"` |

Module structs (`main`, `emission`, `shape` …) are written through; `MinMaxCurve` takes a number or `[min, max]`,
`MinMaxGradient` a color, `[color, color]` or gradient keys. Playback: `P \| call Play()` (also `Pause`, `Stop`, `Clear`,
`Simulate(seconds)`).

Supported modules (24 total):

| Module | Description |
| ------ | ----------- |
| Main | Duration, looping, start lifetime, speed, size, rotation, color |
| Emission | Rate over time/distance, bursts |
| Shape | Emitter shape (sphere, cone, box, mesh, etc.) |
| Velocity Over Lifetime | Velocity changes over particle lifetime |
| Limit Velocity Over Lifetime | Speed limits and damping |
| Inherit Velocity | Velocity inheritance from emitter movement |
| Lifetime By Emitter Speed | Particle lifetime based on emitter speed |
| Force Over Lifetime | External forces applied to particles |
| Color Over Lifetime | Color gradient over particle lifetime |
| Color By Speed | Color based on particle speed |
| Size Over Lifetime | Size curve over particle lifetime |
| Size By Speed | Size based on particle speed |
| Rotation Over Lifetime | Angular velocity over lifetime |
| Rotation By Speed | Rotation based on particle speed |
| External Forces | Wind zone and force field influence |
| Noise | Turbulence and noise-based movement |
| Collision | World and plane collision |
| Trigger | Trigger zone interactions |
| Sub Emitters | Child particle systems on events |
| Texture Sheet Animation | Sprite sheet animation |
| Lights | Real-time lights attached to particles |
| Trails | Particle trail rendering |
| Custom Data | Custom per-particle data streams |
| Renderer | Material, render mode, sorting, shadows |

## Installation

### Option 1 - Installer

- **[Download Installer](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/releases/latest/download/AI-ParticleSystem-Installer.unitypackage)**
- **Import installer into Unity project**
  > - You can double-click on the file - Unity will open it automatically
  > - OR: Open Unity Editor first, then click on `Assets/Import Package/Custom Package`, and choose the file

### Option 2 - OpenUPM-CLI

- [Install OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- Open the command line in your Unity project folder

```bash
openupm add com.ivanmurzak.unity.mcp.particlesystem
```
