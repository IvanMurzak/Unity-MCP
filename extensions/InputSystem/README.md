<h1 align="center"><a href="https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/InputSystem/README.md">Unity AI InputSystem</a></h1>

<div align="center" width="100%">

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp.inputsystem?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp.inputsystem/)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

</div>

<img width="100%" alt="InputSystem" src="https://github.com/IvanMurzak/Unity-MCP/raw/main/extensions/InputSystem/docs/promo/promo-inputsystem.gif"/>

AI-powered tools for the Unity [Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/index.html) authoring workflow. Create `InputActionAsset`s, add and remove action maps, actions, and bindings, build composite bindings (1D/2D axes), define control schemes, save assets, and modify any input asset field directly through natural language commands — no manual Input Actions editor navigation. Wraps Unity's **Input System** package (`com.unity.inputsystem`). Ideal for rapidly bootstrapping player controls, prototyping input layouts, and wiring gameplay actions. Built on top of the [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) platform.

### How to use

- [Instructions](https://github.com/IvanMurzak/Unity-MCP?tab=readme-ov-file#step-2-install-mcp-client)
- [Video Tutorial for Visual Studio Code](https://www.youtube.com/watch?v=ZhP7Ju91mOE)
- [Video Tutorial for Visual Studio](https://www.youtube.com/watch?v=RGdak4T69mc)

[![DOWNLOAD INSTALLER](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/button/button_download.svg?raw=true)](https://github.com/IvanMurzak/Unity-MCP/releases/download/com.ivanmurzak.unity.mcp.inputsystem%2F2.0.1/AI-InputSystem-Installer.unitypackage)

### Stability status

| Unity Version | Editmode                                                                                                                                                                                                          | Playmode                                                                                                                                                                                                          | Standalone                                                                                                                                                                                                          |
| ------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2022.3.62f3   | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       |
| 2023.2.22f1   | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)       |
| 6000.3.1f1    | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)        | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)        | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)        |

## RQL (no extension tools)

On the RQL branch of Unity-MCP this package adds no MCP tools: it contributes an RQL module, so agents use
`rql-query` / `rql-mutate` (the `unity-rql` skill carries the full guide with examples). `A` below stands for
`unity.asset("Assets/Input/Player.inputactions")`; every change is validated, written to the `.inputactions` file and undoable.

| Former tool | RQL |
| --- | --- |
| `inputsystem-asset-create` | `input \| call CreateAsset("Assets/Input/Player.inputactions", "Player")` (path, first map?) |
| `inputsystem-get` | `input \| get assets`, `A \| get maps`, `A \| get actions \| get bindings \| select action path groups`, `A \| get controlSchemes` |
| `inputsystem-save` | not needed: every write is saved to the file at once |
| `inputsystem-actionmap-add` / `-remove` | `A \| call AddMap("UI")`, `A \| get maps \| where name == "UI" \| call Remove()` |
| `inputsystem-action-add` / `-remove` | `A \| get maps \| where name == "Player" \| call AddAction("Move", "Value", "Vector2")`, `… \| get actions \| where name == "Move" \| call Remove()` |
| `inputsystem-binding-add` / `-composite-add` | `A \| get actions \| where name == "Jump" \| call AddBinding("<Keyboard>/space", "Keyboard&Mouse")`, `call AddComposite("2DVector", {up: "<Keyboard>/w", …})` |
| `inputsystem-binding-set` / `-remove` | `A \| get actions \| get bindings \| where path == "<Gamepad>/buttonSouth" \| set path = "<Gamepad>/buttonEast"`, `… \| call Remove()` |
| `inputsystem-controlscheme-add` | `A \| call AddControlScheme("Gamepad", ["<Gamepad>"])`; `… \| get controlSchemes \| set name = …, requiredDevices = […]` |
| `inputsystem-modify` | plain `set` on maps, actions (`name type expectedControlType interactions processors initialStateCheck`), bindings and schemes |

## Installation

### Option 1 - Installer

- **[Download Installer](https://github.com/IvanMurzak/Unity-MCP/releases/download/com.ivanmurzak.unity.mcp.inputsystem%2F2.0.1/AI-InputSystem-Installer.unitypackage)**
- **Import installer into Unity project**
  > - You can double-click on the file - Unity will open it automatically
  > - OR: Open Unity Editor first, then click on `Assets/Import Package/Custom Package`, and choose the file

### Option 2 - OpenUPM-CLI

- [Install OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- Open the command line in your Unity project folder

```bash
openupm add com.ivanmurzak.unity.mcp.inputsystem
```


## Development and releases

This module lives in the Unity-MCP monorepo and retains its own UPM package and version. See [the shared test and release guide](../../docs/dev/extensions.md). Installers and signed UPM archives are attached only to this package's component release; unchanged packages are not rebuilt.
