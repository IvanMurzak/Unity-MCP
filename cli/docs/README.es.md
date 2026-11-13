<div align="center">

<img src="https://raw.githubusercontent.com/IvanMurzak/Unity-MCP/docs/readme-refresh/docs/img/readme/cli-hero.svg" alt="unity-mcp-cli: instala Unity y el plugin AI Game Developer, conecta agentes de IA, ejecuta herramientas, compila y prueba desde la terminal" width="100%">

<p align="center">
<a href="https://www.npmjs.com/package/unity-mcp-cli"><img src="https://img.shields.io/npm/v/unity-mcp-cli?label=CLI&logo=npm&style=for-the-badge&labelColor=0F172A&logoColor=white&color=BE185D" alt="unity-mcp-cli on npm"></a> <a href="https://nodejs.org/"><img src="https://img.shields.io/badge/Node.js-22.14%2B-0E7490?logo=nodedotjs&style=for-the-badge&labelColor=0F172A&logoColor=white" alt="Node.js 22.14 or newer"></a> <a href="https://unity.com/releases/editor/archive"><img src="https://img.shields.io/badge/Unity-2022.3%2B-0E7490?logo=unity&style=for-the-badge&labelColor=0F172A&logoColor=white" alt="Unity 2022.3 or newer"></a> <a href="https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE"><img src="https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&style=for-the-badge&labelColor=0F172A&logoColor=white&color=047857" alt="License"></a> <a href="https://discord.gg/cfbdMZX99G"><img src="https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&style=for-the-badge&labelColor=0F172A&logoColor=white" alt="Discord"></a>
</p>

<b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.ja.md)</b>

</div>

`unity-mcp-cli` es la herramienta de línea de comandos de **[AI Game Developer](https://github.com/IvanMurzak/Unity-MCP)** (Unity-MCP). Instala editores de Unity, crea proyectos, añade el plugin, conecta agentes de IA, ejecuta herramientas MCP, y compila y prueba proyectos de Unity desde tu terminal o CI.

## Características

✅ **Instalar** — editores de Unity, proyectos nuevos y el plugin, con el registro OpenUPM configurado\
✅ **Conectar** — inicia sesión una vez; configs MCP o Skills para Claude Code, Codex, Cursor…\
✅ **Herramientas** — consultas RQL, cualquier herramienta MCP y código C# en el Editor\
✅ **Compilar y probar** — errores con archivo y línea; tests EditMode y PlayMode\
✅ **Automatizable** — `--format json`, códigos de salida estables, salida apta para CI\
✅ **Biblioteca** — los mismos comandos como API tipada de Node.js\
✅ **Multiplataforma** — Windows, macOS y Linux

## Inicio rápido

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-unity                  # opcional: instala un editor de Unity
unity-mcp-cli create-project ./MyGame        # opcional: crea un proyecto
unity-mcp-cli install-plugin ./MyGame        # añade el plugin
unity-mcp-cli login                          # inicia sesión en ai-game.dev
unity-mcp-cli open ./MyGame                  # abre Unity; se conecta y genera skills
unity-mcp-cli wait-for-ready ./MyGame        # hasta que se puedan usar herramientas
```

Sin instalación global: `npx unity-mcp-cli <command>`. Requiere Node.js 22.14 o posterior; Unity Hub se instala automáticamente si falta.

<img src="https://raw.githubusercontent.com/IvanMurzak/Unity-MCP/docs/readme-refresh/docs/img/readme/terminal.svg" alt="Sesión de terminal: instalar unity-mcp-cli, añadir el plugin, iniciar sesión, configurar Claude Code y abrir el proyecto" width="100%">

## Uso

```bash
unity-mcp-cli setup-skills claude-code ./MyGame                  # Skills para tu agente
unity-mcp-cli rql 'objects | where name starts-with "Tree" | count'
unity-mcp-cli compile ./MyGame --strict --format github
unity-mcp-cli test ./MyGame --mode PlayMode --junit-output junit.xml
unity-mcp-cli install-extension Tilemap ./MyGame
```

`rql` y `compile` inician un editor headless si no hay ninguno; `test` ejecuta un editor en modo batch, como en CI.

## Comandos

| Área | Comandos |
| --- | --- |
| Instalación | `install-unity` · `create-project` · `install-plugin` · `install-extension` · `update` · `remove-plugin` |
| Cuenta | `login` · `unity-account` |
| Agentes de IA | `setup-mcp` · `setup-skills` · `configure` |
| Editor | `open` · `close` · `wait-for-ready` · `status` · `dialog` |
| Herramientas | `tools` · `rql` · `run-tool` · `exec` |
| Compilar y probar | `compile` · `test` |
| Descubrimiento | `commands` |

Todas las opciones, códigos de salida y salida JSON: **[referencia de comandos](https://github.com/IvanMurzak/Unity-MCP/blob/docs/readme-refresh/cli/docs/commands.md)** (en inglés), o `unity-mcp-cli <command> --help`.

## Biblioteca

Los comandos también son una biblioteca tipada sin efectos secundarios; los resultados son `kind: 'success' | 'failure'`.

```ts
import { installPlugin } from 'unity-mcp-cli';

const result = await installPlugin({ unityProjectPath: './MyGame' });
if (result.kind === 'success') console.log(`Installed v${result.installedVersion}`);
```

## Enlaces

[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) · [Guía de instalación](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/installation.md) · [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) · [Changelog](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/CHANGELOG.md) · [Discord](https://discord.gg/cfbdMZX99G)

## Licencia

[Apache-2.0](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)

<div align="center">
  <a href="https://stand-with-ukraine.pp.ua"><img src="https://raw.githubusercontent.com/IvanMurzak/IvanMurzak/main/images/ukraine/flag-rounded.svg" width="28" height="28" alt="Stand with Ukraine" title="Stand with Ukraine"></a>
</div>
