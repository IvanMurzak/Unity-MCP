<h1 align="center"><a href="https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Timeline/README.md">Unity AI Timeline</a></h1>

<div align="center" width="100%">

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp.timeline?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp.timeline/)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

</div>

<img width="100%" alt="Timeline" src="https://github.com/IvanMurzak/Unity-MCP/raw/main/extensions/Timeline/docs/promo/promo-timeline.gif"/>

AI-powered tools for the Unity [Timeline](https://docs.unity3d.com/Packages/com.unity.timeline@1.8) cutscene & sequencing workflow. Create `TimelineAsset`s, add Animation / Activation / Audio / Signal / Control tracks, add and time clips (start, duration, blends, eases), place signal markers, bind a `PlayableDirector` and its per-track scene bindings, list and inspect everything, and modify any Timeline object field directly — all through natural language commands, no manual Timeline-window navigation. Wraps `com.unity.timeline` **1.8.12**. Ideal for rapid cutscene blocking, sequence authoring, and procedural timeline rigs. Built on top of the [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) platform.

### How to use

- [Instructions](https://github.com/IvanMurzak/Unity-MCP?tab=readme-ov-file#step-2-install-mcp-client)
- [Video Tutorial for Visual Studio Code](https://www.youtube.com/watch?v=ZhP7Ju91mOE)
- [Video Tutorial for Visual Studio](https://www.youtube.com/watch?v=RGdak4T69mc)

[![DOWNLOAD INSTALLER](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/button/button_download.svg?raw=true)](https://github.com/IvanMurzak/Unity-MCP/releases/download/com.ivanmurzak.unity.mcp.timeline%2F2.0.1/AI-Timeline-Installer.unitypackage)

### Stability status

| Unity Version | Editmode                                                                                                                                                                                                  | Playmode                                                                                                                                                                                                  | Standalone                                                                                                                                                                                                  |
| ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2022.3.62f3   | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2022-3-62f3-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)  |
| 2023.2.22f1   | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-2023-2-22f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)  |
| 6000.3.1f1    | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)   | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)   | [![r](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml/badge.svg?job=test-unity-6000-3-1f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release_extensions.yml)   |

## RQL (no extension tools)

With Unity-MCP 1.0.0 this package adds no MCP tools: it contributes an RQL module, so agents use `rql-query` /
`rql-mutate` (the `unity-rql` skill carries the full guide with examples). `A` below stands for
`unity.asset("Assets/Cutscenes/Intro.playable")`, `D` for `objects | get components | where $ is PlayableDirector`.

| Former tool | RQL |
| --- | --- |
| `timeline-create` | `timeline \| call Create("Assets/Cutscenes/Intro.playable", 30)` (path, frameRate?, fixedDuration?) |
| `timeline-list` | `unity.assets("t:TimelineAsset") \| select $ref name duration frameRate tracks.name` |
| `timeline-get` / `-modify` | plain `select` / `set` on the timeline, its tracks, clips (`… \| get clips \| set asset.removeStartOffset = false`) and markers |
| `timeline-track-add` | `A \| call AddTrack("Animation", "Hero")` (type, name?, group?) |
| `timeline-track-remove` | `A \| get tracks \| where name == "Door" \| call Delete()` |
| `timeline-track-list` | `A \| get tracks \| recurse children \| select $depth name $type muted locked clips.count()` |
| `timeline-clip-add` | `A \| get tracks \| where name == "Hero" \| call AddClip(unity.asset("Assets/Anim/Walk.anim"), 2, 3, "Walk")` |
| `timeline-clip-set-timing` | `… \| get clips \| where displayName == "Walk" \| set duration = 4, clipIn = 0.25, timeScale = 1.5, easeInDuration = 0.5` |
| `timeline-clip-move` | `… \| get clips \| where displayName == "Walk" \| set start = start + 0.5` |
| `timeline-marker-add` | `A \| call AddMarker(3.5)` (timeline marker track) or `A \| get tracks \| where … \| call AddMarker(1, "Signal")` |
| `timeline-director-bind` | `objects \| where name == "Cutscene" \| call AddComponent("PlayableDirector")`, then `D \| set playableAsset = A` |
| `timeline-track-bind` | `D \| set bindings["Hero"] = ref("GameObject#…")` |

## License

[MIT](LICENSE) © [Ivan Murzak](https://github.com/IvanMurzak)


## Development and releases

This module lives in the Unity-MCP monorepo and retains its own UPM package and version. See [the shared test and release guide](../../docs/dev/extensions.md). Installers and signed UPM archives are attached only to this package's component release; unchanged packages are not rebuilt.
