[English](installation.md) | [中文](installation.zh-CN.md) | [日本語](installation.ja.md) | <b>Español</b> | [فارسی](installation.fa.md) | [हिन्दी](installation.hi.md) | [العربية](installation.ar.md)

# Guía de instalación

Instala el plugin **AI Game Developer** en un proyecto de Unity, conecta un agente de IA y desinstálalo de nuevo si lo necesitas. Volver al [README](README.es.md).

## Requisitos

- **Unity 2022.3** o posterior en **Windows, macOS o Linux** (los tres están probados y son compatibles).
- Una ruta de proyecto **sin espacios**. El plugin registra un error al iniciarse cuando la ruta contiene alguno.
  - ✅ `C:/MyProjects/MyProject`
  - ❌ `C:/My Projects/MyProject`
- Para la CLI: [Node.js](https://nodejs.org/) 22.14 o posterior.

## 1. Instalar el plugin

### Opción 1: Instalador

1. Descarga **[AI-Game-Dev-Installer.unitypackage](https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage)**.
2. Haz doble clic sobre él con el proyecto abierto, o usa **Assets ▸ Import Package ▸ Custom Package** en Unity.

### Opción 2: OpenUPM

```bash
openupm add com.ivanmurzak.unity.mcp
```

### Opción 3: CLI

[`unity-mcp-cli`](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) edita `Packages/manifest.json` por ti, así que el Editor no necesita estar abierto:

```bash
npm install -g unity-mcp-cli

unity-mcp-cli install-unity                      # optional: install a Unity Editor
unity-mcp-cli create-project ./MyUnityProject    # optional: create a project
unity-mcp-cli install-plugin ./MyUnityProject    # add the plugin
unity-mcp-cli login                              # sign in to ai-game.dev (browser device flow)
unity-mcp-cli open ./MyUnityProject              # open Unity; it connects and generates skills
unity-mcp-cli wait-for-ready ./MyUnityProject    # wait until tools can be called
```

`npx unity-mcp-cli <command>` funciona sin instalación global.

**Inicio de sesión y fijación del proyecto.** `login` guarda la credencial en `~/.ai-game-dev/credentials.json`; no hay ningún token que pegar. De forma predeterminada, la configuración del agente que escribe `setup-mcp` apunta a un endpoint **fijado** por proyecto (`https://ai-game.dev/mcp/p/<pin>`); pasa `--no-pin` para usar el compartido `https://ai-game.dev/mcp`. Los equipos pueden compartir el acceso con un código de inscripción: `unity-mcp-cli install-plugin --enroll <code>`.

## 2. Elegir un agente de IA

Necesitas un agente; es la ventana de chat en la que trabajas. Debe admitir Skills o herramientas MCP que se actualicen mientras se ejecuta. Se recomienda Claude Code. La ventana de AI Game Developer y `unity-mcp-cli setup-mcp` escriben la configuración por ti para Claude Code, Claude Desktop, Codex, Cursor, Gemini, GitHub Copilot (CLI, VS Code, Visual Studio), Antigravity, Cline, Open Code, Rider (Junie), Unity AI y otros; `unity-mcp-cli setup-mcp --list` muestra la lista completa. Cualquier otro cliente MCP funciona con la configuración JSON de la ventana.

## 3. Conectar el agente

### Automático

1. Abre el proyecto en Unity.
2. Abre **Window ▸ AI Game Dev**.
3. Haz clic en **Auto-generate** Skills (recomendado), o en **Configure** MCP para tu agente.

![AI Game Developer window](img/ai-connector-window.gif)

Lo mismo desde la terminal:

```bash
unity-mcp-cli setup-skills claude-code ./MyUnityProject
unity-mcp-cli setup-mcp claude-code ./MyUnityProject             # http (default)
unity-mcp-cli setup-mcp cursor ./MyUnityProject --transport stdio
```

### Manual

Si tu agente no está en la lista, copia el JSON que se muestra en la ventana de AI Game Developer en su configuración MCP.

Para un agente que añade servidores desde su propia línea de comandos, construye el `<command>` que inicia el servidor local (modo Custom). Sustituye `<unityProjectPath>` por la ruta completa del proyecto y `<port>` por el puerto de la ventana de AI Game Developer:

| Plataforma | `<command>` |
| --- | --- |
| Windows x64 | `"<unityProjectPath>/Library/mcp-server/win-x64/gamedev-mcp-server.exe" port=<port> client-transport=stdio` |
| Windows x86 | `"<unityProjectPath>/Library/mcp-server/win-x86/gamedev-mcp-server.exe" port=<port> client-transport=stdio` |
| Windows arm64 | `"<unityProjectPath>/Library/mcp-server/win-arm64/gamedev-mcp-server.exe" port=<port> client-transport=stdio` |
| macOS Apple Silicon | `"<unityProjectPath>/Library/mcp-server/osx-arm64/gamedev-mcp-server" port=<port> client-transport=stdio` |
| macOS Intel | `"<unityProjectPath>/Library/mcp-server/osx-x64/gamedev-mcp-server" port=<port> client-transport=stdio` |
| Linux x64 | `"<unityProjectPath>/Library/mcp-server/linux-x64/gamedev-mcp-server" port=<port> client-transport=stdio` |
| Linux arm64 | `"<unityProjectPath>/Library/mcp-server/linux-arm64/gamedev-mcp-server" port=<port> client-transport=stdio` |

Después añádelo:

```bash
claude mcp add ai-game-developer <command>    # Claude Code
gemini mcp add ai-game-developer <command>    # Gemini CLI
```

GitHub Copilot CLI: ejecuta `copilot` y después `/mcp add` con el nombre de servidor `ai-game-developer`, el tipo `local` y el `<command>` anterior.

> En VS Code con Copilot, asegúrate de que el modo **Agent** esté activado.

## Configuración del equipo: notificaciones de actualización

El plugin muestra una ventana emergente de actualización al iniciar el Editor cuando OpenUPM tiene una versión más reciente. *"Do not show again"* es una opción por usuario. Para desactivar la ventana emergente para **todo el equipo**, abre **Edit ▸ Project Settings ▸ AI Game Developer** y activa *"Disable update notifications for the entire team"*. La opción se guarda en `ProjectSettings/AI-Game-Developer-UpdateSettings.asset`; haz commit de ese archivo. El mismo interruptor está en **Tools ▸ AI Game Developer ▸ Updates ▸ Disable Update Notifications (Team)**.

## Desinstalar

1. Abre **Window ▸ Package Manager**, selecciona **AI Game Developer — MCP** y haz clic en **Remove** (o ejecuta `unity-mcp-cli remove-plugin ./MyUnityProject`).
2. Cierra Unity y después elimina la carpeta `Assets/Plugins/NuGet` y su archivo `Assets/Plugins/NuGet.meta`.

## Siguiente

- [Herramientas MCP predeterminadas](default-mcp-tools.es.md)
- [Herramientas personalizadas y runtime](custom-tools.es.md)
- [Servidor MCP y conexión](mcp-server.es.md)
- [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki)
