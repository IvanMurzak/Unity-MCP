<div align="center" width="100%">

<img src="img/readme/hero.svg" alt="AI Game Developer para Unity: Skills, herramientas MCP y una CLI que conectan agentes de IA con el Editor de Unity y con tu juego en ejecución" width="100%">

[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&style=for-the-badge&labelColor=0F172A&logoColor=white&color=6D28D9 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Downloads](https://img.shields.io/badge/dynamic/json?label=Downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.ivanmurzak.unity.mcp&style=for-the-badge&labelColor=0F172A&logoColor=white&color=6D28D9 'OpenUPM downloads')](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![CLI](https://img.shields.io/npm/v/unity-mcp-cli?label=CLI&logo=npm&style=for-the-badge&labelColor=0F172A&logoColor=white&color=BE185D 'unity-mcp-cli on npm')](https://www.npmjs.com/package/unity-mcp-cli)
[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-0E7490?logo=unity&style=for-the-badge&labelColor=0F172A&logoColor=white 'Unity 2022.3 or newer')](https://unity.com/releases/editor/archive)
[![release](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/release.yml?label=release&logo=githubactions&style=for-the-badge&labelColor=0F172A&logoColor=white 'Release workflow')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)
[![Discord](https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&style=for-the-badge&labelColor=0F172A&logoColor=white 'Join')](https://discord.gg/cfbdMZX99G)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&style=for-the-badge&labelColor=0F172A&logoColor=white&color=047857 'License')](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

  <p>
    <a href="https://claude.ai/download"><img src="img/mcp-clients/claude-64.png" alt="Claude" title="Claude" height="36"></a>&nbsp;&nbsp;
    <a href="https://openai.com/index/introducing-codex/"><img src="img/mcp-clients/codex-64.png" alt="Codex" title="Codex" height="36"></a>&nbsp;&nbsp;
    <a href="https://www.cursor.com/"><img src="img/mcp-clients/cursor-64.png" alt="Cursor" title="Cursor" height="36"></a>&nbsp;&nbsp;
    <a href="https://code.visualstudio.com/docs/copilot/overview"><img src="img/mcp-clients/github-copilot-64.png" alt="GitHub Copilot" title="GitHub Copilot" height="36"></a>&nbsp;&nbsp;
    <a href="https://gemini.google.com/"><img src="img/mcp-clients/gemini-64.png" alt="Gemini" title="Gemini" height="36"></a>&nbsp;&nbsp;
    <a href="https://antigravity.google/"><img src="img/mcp-clients/antigravity-64.png" alt="Antigravity" title="Antigravity" height="36"></a>&nbsp;&nbsp;
    <a href="https://code.visualstudio.com/"><img src="img/mcp-clients/vs-code-64.png" alt="VS Code" title="VS Code" height="36"></a>&nbsp;&nbsp;
    <a href="https://www.jetbrains.com/rider/"><img src="img/mcp-clients/rider-64.png" alt="Rider" title="Rider" height="36"></a>&nbsp;&nbsp;
    <a href="https://visualstudio.microsoft.com/"><img src="img/mcp-clients/visual-studio-64.png" alt="Visual Studio" title="Visual Studio" height="36"></a>&nbsp;&nbsp;
    <a href="https://opencode.ai/"><img src="img/mcp-clients/open-code-64.png" alt="Open Code" title="Open Code" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/cline/cline"><img src="img/mcp-clients/cline-64.png" alt="Cline" title="Cline" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/Kilo-Org/kilocode"><img src="img/mcp-clients/kilo-code-64.png" alt="Kilo Code" title="Kilo Code" height="36"></a>
  </p>

  <b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ja.md) | [فارسی](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.fa.md)</b>

</div>

**AI Game Developer** (Unity-MCP) conecta tu agente de IA con Unity. Claude Code, Codex, Cursor, GitHub Copilot,
Gemini o cualquier otro cliente [MCP](https://modelcontextprotocol.io/) puede leer y cambiar escenas, prefabs, assets y
código C#, ejecutar tests y tomar capturas en el **Editor de Unity**, y llamar a herramientas que tú escribes **dentro de tu juego compilado**.
Se distribuye como un paquete de Unity gratuito (`com.ivanmurzak.unity.mcp`), una herramienta de línea de comandos (`unity-mcp-cli`) y diez
extensiones opcionales.

## Cómo funciona

<img src="img/readme/architecture.svg" alt="Un agente de IA o unity-mcp-cli habla MCP con un servidor, ai-game.dev Cloud por defecto o un gamedev-mcp-server local, que llama a las herramientas del plugin del Editor de Unity y de tu juego en ejecución" width="100%">

El plugin registra sus herramientas en un servidor MCP; tu agente las llama a través de ese servidor. Por defecto el plugin
usa el servidor **Cloud** en `https://ai-game.dev/mcp`. En modo **Custom** inicia el binario local de
[GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server), o se conecta a uno que tú alojas
(binario o Docker). En lugar de herramientas MCP, el agente también puede usar **Skills** generadas.

## Características

| **RQL** | lee y cambia escenas, objetos, assets y ajustes con un solo lenguaje de consultas |
| **C# y tests** | compila y ejecuta C# con Roslyn; ejecuta tests EditMode y PlayMode |
| **Skills o MCP** | genera Skills con un clic, o conéctate como cliente MCP |
| **Cualquier agente** | Claude Code, Codex, Cursor, Copilot, Gemini y otros clientes MCP |
| **Runtime** | las mismas herramientas en tu juego compilado: NPC, depuración en vivo |
| **Tus herramientas** | cualquier método C# es una herramienta con el atributo `[AiTool]` |
| **CLI** | instala, abre, consulta, compila y prueba desde la terminal o CI |
| **Extensiones** | diez paquetes opcionales de herramientas, de Animation a Timeline |

## Inicio rápido

Requiere **Unity 2022.3** o posterior. Elige una forma de instalar el plugin:

[![DESCARGAR INSTALADOR](img/button/button_download_es.svg)](https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage)

- **Instalador**: descarga el `.unitypackage` de arriba y ábrelo en tu proyecto de Unity.
- **OpenUPM**: `openupm add com.ivanmurzak.unity.mcp`
- **CLI** (Node.js 22.14+), sin necesidad del Editor:

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-plugin ./MyUnityProject   # añade el plugin a Packages/manifest.json
unity-mcp-cli login                             # inicia sesión en ai-game.dev en el navegador
unity-mcp-cli open ./MyUnityProject             # abre Unity; se conecta y genera skills
```

<img src="img/readme/terminal.svg" alt="Sesión de terminal: instalar unity-mcp-cli, añadir el plugin, iniciar sesión, configurar Claude Code y abrir el proyecto" width="100%">

Después conecta tu agente: en Unity abre **Window ▸ AI Game Dev** y pulsa **Auto-generate** Skills (recomendado) o
**Configure** MCP. Desde la terminal es `unity-mcp-cli setup-skills claude-code ./MyUnityProject` o
`unity-mcp-cli setup-mcp claude-code ./MyUnityProject`.

![Ventana de AI Game Developer](img/editor/ai-game-developer-windows.png)

Pide a tu agente *"Crea 3 cubos en un círculo de radio 2"* y mira cómo sucede. Pasos completos, configuración MCP manual
y desinstalación: **[Guía de instalación](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/installation.md)** (en inglés).

https://github.com/user-attachments/assets/228baf4d-4f00-4dce-939d-fb985ebdd8dd

## Uso

**Habla con tu agente.** Describe el resultado que quieres; cuanto más preciso, mejor.

```text
Explica la jerarquía de mi escena
Crea un material dorado metálico y asígnalo a una nueva esfera
Encuentra cada prefab de enemigo sin Rigidbody y añádele uno
```

**Herramientas.** Visibles por defecto: `rql-query`, `rql-mutate`, `rql-schema`, `script-execute`, `tests-run`,
`screenshot-isolated`, las herramientas `package-*` y `assetstore-*` y `unity-tool-list`. Hay más herramientas instaladas pero
ocultas hasta que las muestres. Lista completa: **[Herramientas MCP por defecto](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md)**
o [ai-game.dev/docs/tools](https://ai-game.dev/docs/tools).

**Desde la terminal** (inicia un editor headless si no hay ninguno en ejecución):

```bash
unity-mcp-cli rql 'objects | where name starts-with "Tree" | count'
unity-mcp-cli compile ./MyGame
unity-mcp-cli test ./MyGame --mode PlayMode
```

**Tu propia herramienta.** Basta con una clase con `[AiToolType]` y un método con `[AiTool]`:

```csharp
[AiToolType]
public class Tool_Score
{
    [AiTool("score-reset", Title = "Reset the score")]
    [Description("Sets the player's score to zero.")]
    public string Reset()
    {
        return MainThread.Instance.Run(() =>
        {
            ScoreManager.Instance.Score = 0;
            return "[Success] Score reset.";
        });
    }
}
```

Prompts, herramientas en runtime (en el juego) y un ejemplo completo: **[Herramientas propias y runtime](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/custom-tools.md)**.

## Extensiones

Instálalas desde la sección **Extensions** de la ventana de AI Game Developer, o con
`unity-mcp-cli install-extension <name>`:
[Animation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Animation/README.md) ·
[Cinemachine](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Cinemachine/README.md) ·
[InputSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/InputSystem/README.md) ·
[Navigation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Navigation/README.md) ·
[ParticleSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ParticleSystem/README.md) ·
[ProBuilder](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ProBuilder/README.md) ·
[Splines](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Splines/README.md) ·
[Terrain](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Terrain/README.md) ·
[Tilemap](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Tilemap/README.md) ·
[Timeline](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Timeline/README.md)

## Documentación

| Página | Contenido |
| --- | --- |
| [Guía de instalación](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/installation.md) | Opciones de instalación, conectar un agente, configuración MCP manual, ajustes de equipo, desinstalación |
| [Herramientas MCP por defecto](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md) | Todas las herramientas integradas, fuentes RQL, herramientas eliminadas en 1.0.0 y sus equivalentes RQL |
| [Herramientas propias y runtime](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/custom-tools.md) | Escribe herramientas y prompts; usa MCP dentro de tu juego |
| [Servidor MCP y conexión](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/mcp-server.md) | Modos Cloud y Custom, variables de entorno del plugin, servidor local |
| [Despliegue con Docker](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/DOCKER_DEPLOYMENT.md) | Ejecuta el servidor MCP en un contenedor |
| [Desactivar en CI](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/ci-disable.md) | Mantén el plugin fuera de las ejecuciones de tests normales en CI |
| [Referencia de la CLI](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) | Todos los comandos y opciones de `unity-mcp-cli` |
| [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) | Tutoriales, guías y FAQ |
| [Guía de desarrollo](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.md) | Estructura del repositorio, estilo de código, tests y CI para colaboradores |

Relacionado: [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) (el servidor MCP compartido) ·
[ai-game.dev](https://ai-game.dev) · [Discord](https://discord.gg/cfbdMZX99G)

Las páginas de documentación están en inglés.

## Contribuir

Las ideas, los reportes de errores y los pull requests son bienvenidos. Lee la
[Guía de desarrollo](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.md), haz un fork del repositorio,
abre `./Unity-MCP-Plugin` en Unity y envía un pull request a `main`. Si el proyecto te ayuda, dale una estrella.

## Licencia

[Apache-2.0](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
