<div align="center" width="100%">

<p align="center"><img src="img/readme/hero.svg" alt="AI Game Developer para Unity: Skills, herramientas MCP y una CLI que conectan agentes de IA con el Editor de Unity y con tu juego en ejecución" width="100%"></p>

[![OpenUPM version](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&style=for-the-badge&labelColor=0F172A&logoColor=white&color=2F6F9F)](https://openupm.com/packages/com.ivanmurzak.unity.mcp/) [![OpenUPM downloads per month](https://img.shields.io/badge/dynamic/json?label=Downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.ivanmurzak.unity.mcp&style=for-the-badge&labelColor=0F172A&logoColor=white&color=2F6F9F)](https://openupm.com/packages/com.ivanmurzak.unity.mcp/) [![unity-mcp-cli on npm](https://img.shields.io/npm/v/unity-mcp-cli?label=CLI&logo=npm&style=for-the-badge&labelColor=0F172A&logoColor=white&color=BE185D)](https://www.npmjs.com/package/unity-mcp-cli) [![Unity 2022.3 or newer](https://img.shields.io/badge/Unity-2022.3%2B-0E7490?logo=unity&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://unity.com/releases/editor/archive)\
[![Release workflow status](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/release.yml?label=release&logo=github&color=15803D&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) [![Discord](https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://discord.gg/cfbdMZX99G) [![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&style=for-the-badge&labelColor=0F172A&logoColor=white&color=047857)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE) [![Stand with Ukraine](https://raw.githubusercontent.com/IvanMurzak/IvanMurzak/main/images/ukraine/flag-badge.svg)](https://stand-with-ukraine.pp.ua)

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

  <b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ja.md) | [فارسی](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.fa.md) | [हिन्दी](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.hi.md) | [العربية](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ar.md)</b>

</div>

**AI Game Developer** (Unity-MCP) conecta tu agente de IA con Unity. Claude Code, Codex, Cursor, GitHub Copilot, Gemini o cualquier otro cliente [MCP](https://modelcontextprotocol.io/) puede leer y cambiar escenas, prefabs, assets y código C#, ejecutar tests y tomar capturas en el **Editor de Unity**, y llamar a herramientas que tú escribes **dentro de tu juego compilado**. Se distribuye como un paquete de Unity gratuito (`com.ivanmurzak.unity.mcp`), una herramienta de línea de comandos (`unity-mcp-cli`) y extensiones opcionales.

**Estado:** [última versión](https://github.com/IvanMurzak/Unity-MCP/releases/latest) y [registro de cambios](https://github.com/IvanMurzak/Unity-MCP/blob/main/Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/CHANGELOG.md). El plugin y `unity-mcp-cli` comparten la misma versión. Probado y compatible con **Windows, macOS y Linux**. Requiere Unity **2022.3** o posterior; CI ejecuta tests EditMode, PlayMode y de build standalone en Unity 2022.3, 6000.0, 6000.3 y 6000.6.

## Cómo funciona

<p align="center"><img src="img/readme/architecture.svg" alt="Un agente de IA o unity-mcp-cli habla MCP con un servidor, ai-game.dev Cloud por defecto o un gamedev-mcp-server local, que llama a las herramientas del plugin del Editor de Unity y de tu juego en ejecución" width="100%"></p>

El plugin registra sus herramientas en un servidor MCP; tu agente las llama a través de ese servidor. En lugar de herramientas MCP, el agente también puede usar **Skills** generadas. Tú eliges dónde se ejecuta el servidor:

| | **Local** (modo Custom, sin cuenta) | **Cloud** (ajuste predeterminado) |
| --- | --- | --- |
| Servidor | [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) iniciado por el plugin en `127.0.0.1`, o uno que tú alojas | `https://ai-game.dev/mcp` |
| Cuenta | No hace falta | Inicia sesión en ai-game.dev |
| Datos de tu proyecto | Se quedan en tu máquina | Las llamadas a herramientas pasan por un relay sin estado que no inspecciona ni conserva su contenido; registra metadatos de eventos ([Política de privacidad](https://ai-game.dev/legal/privacy)) |
| Coste | Gratis | Gratis |

**Por qué Cloud es el predeterminado:** no necesita puertos abiertos, evita conflictos de puertos con otros programas y es gratuito. Local es una opción igual de válida para trabajar sin conexión o en entornos aislados (air-gapped). Detalles: [Seguridad y privacidad](#seguridad-y-privacidad).

## Características

✅ **Cubre el Editor** — el agente puede hacer prácticamente cualquier cosa que tú puedas hacer en el Editor de Unity\
✅ **Compacto y eficiente** — herramientas pensadas para agentes: menos llamadas y menos tokens, así el agente trabaja más rápido y más barato ([medido](https://github.com/IvanMurzak/RQL/blob/main/RESULTS.md#33-rql-vs-conventional-reflection-tools))\
✅ **RQL** — lee y cambia escenas, objetos, assets y ajustes con un solo lenguaje de consultas\
✅ **C# y tests** — compila y ejecuta C# con Roslyn; ejecuta tests EditMode y PlayMode\
✅ **Skills o MCP** — genera Skills con un clic, o conéctate como cliente MCP\
✅ **Cualquier agente** — Claude Code, Codex, Cursor, Copilot, Gemini y otros clientes MCP\
✅ **Runtime** — las mismas herramientas en tu juego compilado: lógica de NPC, depuración en vivo\
✅ **Tus herramientas** — cualquier método C# se convierte en herramienta con un solo atributo `[AiTool]`\
✅ **CLI** — instala, abre, consulta, compila y prueba desde la terminal o CI\
✅ **Extensiones** — paquetes opcionales de herramientas, de Animation a Timeline

## Inicio rápido

Requiere **Unity 2022.3** o posterior.

**1. Instala el plugin** (recomendado: el Instalador, no hace falta instalar nada más):

[![DESCARGAR INSTALADOR](img/button/button_download_es.svg)](https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage)

Abre el `.unitypackage` descargado con tu proyecto abierto (haz doble clic en él, o usa **Assets ▸ Import Package ▸ Custom Package**).

<details>
<summary>Otras formas de instalar: OpenUPM / Package Manager, o la CLI para scripts y CI</summary>

- **OpenUPM** — `openupm add com.ivanmurzak.unity.mcp`, o sin ninguna CLI: añade un registro con ámbito en **Edit ▸ Project Settings ▸ Package Manager** (URL `https://package.openupm.com`, ámbitos `com.ivanmurzak` y `extensions.unity`), y después **Window ▸ Package Manager ▸ + ▸ Add package by name** → `com.ivanmurzak.unity.mcp`.
- **unity-mcp-cli** (Node.js 22.14+) — para scripts y CI, sin necesidad del Editor:

  ```bash
  npm install -g unity-mcp-cli
  unity-mcp-cli install-plugin ./MyUnityProject   # añade el plugin a Packages/manifest.json
  unity-mcp-cli open ./MyUnityProject             # abre el proyecto en Unity
  ```

  <p align="center"><img src="img/readme/terminal.svg" alt="Sesión de terminal: instalar unity-mcp-cli, añadir el plugin, iniciar sesión, configurar Claude Code y abrir el proyecto" width="100%"></p>

</details>

**2. Conecta tu agente** — elige una opción; ambas son totalmente compatibles:

| | **Local** (sin cuenta) | **Cloud** (ajuste predeterminado) |
| --- | --- | --- |
| En **Window ▸ AI Game Dev** | conexión **Custom** | conexión **Cloud**, después **Authorize** (inicio de sesión en el navegador) |
| Configuración del agente | **Configure** para tu agente | **Configure** para tu agente |
| Lo mismo desde la CLI | `unity-mcp-cli setup-mcp claude-code ./MyUnityProject --transport stdio` | `unity-mcp-cli login`, después `unity-mcp-cli setup-mcp claude-code ./MyUnityProject` |

**Auto-generate** Skills en la misma ventana es la alternativa a MCP. Otros agentes: [configuración de clientes](#configuración-de-clientes).

**3. Solo en local: elige quién puede llamar a tus herramientas.** El servidor local se inicia con autenticación `none`: solo escucha en `127.0.0.1` y rechaza orígenes de navegador ajenos, pero cualquier programa de tu máquina puede alcanzarlo. Si otras personas o programas no fiables usan la máquina, cambia a `token` en un solo paso: **Window ▸ AI Game Dev** → **Authorization Token** → `token`. El servidor se reinicia con un secreto generado; pulsa **Configure** de nuevo para que tu agente lo envíe.

![Ventana de AI Game Developer](img/editor/ai-game-developer-windows.png)

Pide a tu agente *"Crea 3 cubos en un círculo de radio 2"* y mira cómo sucede. Pasos completos, configuración MCP manual y desinstalación: **[Guía de instalación](installation.es.md)**.

https://github.com/user-attachments/assets/228baf4d-4f00-4dce-939d-fb985ebdd8dd

## Uso

**Habla con tu agente.** Describe el resultado que quieres; cuanto más preciso, mejor.

```text
Explica la jerarquía de mi escena
Crea un material dorado metálico y asígnalo a una nueva esfera
Encuentra cada prefab de enemigo sin Rigidbody y añádele uno
```

**Lo que puede hacer tu agente** (herramientas visibles por defecto):

| Capacidad | Herramienta | Prompt de ejemplo |
| --- | --- | --- |
| Escenas y GameObjects | `rql-query`, `rql-mutate` | *"Lista todos los GameObjects desactivados en la escena abierta"* |
| Assets y prefabs | `rql-query`, `rql-mutate` | *"Crea un material rojo y asígnalo a todos los cubos"* |
| Consola y estado del Editor | `rql-query` | *"¿Qué errores hay en la consola?"* |
| C# en el Editor | `script-execute` | *"Ejecuta C# que coloque 100 árboles en puntos aleatorios del terreno"* |
| Tests | `tests-run` | *"Ejecuta los tests EditMode y arregla lo que falle"* |
| Capturas de pantalla | `screenshot-isolated` | *"Muéstrame el modelo del Player de frente"* |
| Paquetes y Asset Store | `package-*`, `assetstore-*` | *"Instala Cinemachine"* |
| Lista de herramientas | `unity-tool-list` | *"¿Qué herramientas puedes usar aquí?"* |

No necesitas aprender RQL: el agente lo escribe y consulta `rql-schema` para cada fuente, tipo y llamada que necesita. Medido en un mundo de juego de prueba (sandbox): con RQL un agente necesita unas 2–3 llamadas a herramientas y 7–8k tokens por tarea, con un 100% de éxito, frente a 30–37 llamadas y 125–170k tokens, con un 67–91% de éxito, usando herramientas de reflexión simples ([resultados](https://github.com/IvanMurzak/RQL/blob/main/RESULTS.md#33-rql-vs-conventional-reflection-tools)). Hay más herramientas instaladas pero ocultas hasta que las muestres. Lista completa: **[Herramientas MCP por defecto](default-mcp-tools.es.md)** o [ai-game.dev/docs/tools](https://ai-game.dev/docs/tools).

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

Prompts, herramientas en runtime (en el juego) y un ejemplo completo: **[Herramientas propias y runtime](custom-tools.es.md)**.

## Configuración de clientes

El botón **Configure** de la ventana de AI Game Developer, o `unity-mcp-cli setup-mcp <agent> ./MyUnityProject`, escribe estos archivos por ti (Cloud por defecto, local con `--transport stdio`). A mano, para un servidor local: pon el plugin en **Custom**, toma `<port>` de la ventana y usa el binario de tu plataforma desde `<project>/Library/mcp-server/<rid>/` (`win-x64/gamedev-mcp-server.exe`, `osx-arm64/gamedev-mcp-server`, `linux-x64/gamedev-mcp-server`, …).

<details>
<summary><b>Claude Code</b> — <code>.mcp.json</code> en la raíz del proyecto</summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "command": "<project>/Library/mcp-server/win-x64/gamedev-mcp-server.exe",
      "args": ["port=<port>", "client-transport=stdio"]
    }
  }
}
```

</details>

<details>
<summary><b>Cursor</b> — <code>.cursor/mcp.json</code></summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "type": "stdio",
      "command": "<project>/Library/mcp-server/win-x64/gamedev-mcp-server.exe",
      "args": ["port=<port>", "client-transport=stdio"]
    }
  }
}
```

</details>

<details>
<summary><b>Codex</b> — <code>.codex/config.toml</code></summary>

```toml
[mcp_servers.ai-game-developer]
enabled = true
command = "<project>/Library/mcp-server/win-x64/gamedev-mcp-server.exe"
args = ["port=<port>", "client-transport=stdio"]
tool_timeout_sec = 300
```

</details>

Otros agentes y configuración manual: [Guía de instalación](installation.es.md).

## Seguridad y privacidad

**Configuración más privada:** conexión **Custom** (servidor local en `127.0.0.1`, sin cuenta, los datos del proyecto se quedan en tu máquina) con **Authorization Token** en `token` en **Window ▸ AI Game Dev**, y después **Configure** de nuevo para que tu agente envíe el secreto.

✅ **El plugin no abre ningún puerto** — se conecta hacia fuera al servidor MCP.\
✅ **Servidor local en loopback** — `gamedev-mcp-server` se enlaza a `127.0.0.1` salvo que definas `MCP_BIND` para alojarlo.\
✅ **Comprobación de origen** — las solicitudes del navegador con un `Origin` no permitido se rechazan (403) en todos los modos de autenticación.\
✅ **Modos de autenticación** — `none` (predeterminado en local), `token` (un secreto compartido que el agente y el plugin deben enviar), `oauth` (cuenta de ai-game.dev).\
✅ **Relay de Cloud** — un relay sin estado: enruta las llamadas a herramientas entre tu agente y el Editor sin inspeccionar ni conservar su contenido, y no recopila conversaciones, código fuente, archivos del proyecto, assets ni datos de escenas. Registra metadatos de eventos MCP (tipo de evento, tiempos, estado, identificadores de sesión, el nombre de la herramienta, del prompt o del recurso, detalles de errores), que se usan dentro de una ventana móvil de 90 días ([Política de privacidad](https://ai-game.dev/legal/privacy)).\
⚠️ **Sin protección** — en modo `none` cualquier proceso de tu máquina que alcance el puerto puede llamar a herramientas, y las herramientas se ejecutan con los permisos del Editor (`script-execute` ejecuta C#). Usa el modo `token` en máquinas compartidas.

Opciones del servidor y Docker: [Servidor MCP y conexión](mcp-server.es.md).

## Extensiones

Las extensiones son paquetes UPM opcionales que añaden herramientas para funciones concretas de Unity.

```bash
unity-mcp-cli install-extension com.ivanmurzak.unity.mcp.cinemachine ./MyUnityProject
```

| Extensión | Añade | Requiere |
| --- | --- | --- |
| [Animation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Animation/README.md) | AnimationClips y AnimatorControllers |  |
| [Cinemachine](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Cinemachine/README.md) | Cámaras virtuales, objetivos y el pipeline de cámara | `com.unity.cinemachine` 3.1.6+ |
| [InputSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/InputSystem/README.md) | Assets de acciones de entrada, mapas, acciones y bindings | `com.unity.inputsystem` 1.8.2+ |
| [Navigation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Navigation/README.md) | Superficies NavMesh, horneado, agentes y enlaces | `com.unity.ai.navigation` 1.1.7+ |
| [ParticleSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ParticleSystem/README.md) | Emisión, forma, curvas y gradientes de ParticleSystem |  |
| [ProBuilder](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ProBuilder/README.md) | Mallas editables: formas, extrusión, bisel, materiales | `com.unity.probuilder` 5.2.4+ |
| [Splines](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Splines/README.md) | Contenedores de splines, nudos y tangentes | `com.unity.splines` 2.8.4+ |
| [Terrain](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Terrain/README.md) | Terrenos: heightmaps, esculpido y muestreo |  |
| [Tilemap](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Tilemap/README.md) | Grids y tilemaps: pintar, rellenar y leer tiles | `com.unity.2d.tilemap.extras` 3.1.3+ |
| [Timeline](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Timeline/README.md) | Assets de Timeline, pistas, clips y marcadores | `com.unity.timeline` 1.8.12+ |

Los paquetes de Unity necesarios se instalan con la extensión. También puedes instalar extensiones desde la sección **Extensions** de la ventana de AI Game Developer.

## Documentación

| Página | Contenido |
| --- | --- |
| [Guía de instalación](installation.es.md) | Opciones de instalación, conectar un agente, configuración MCP manual, ajustes de equipo, desinstalación |
| [Herramientas MCP por defecto](default-mcp-tools.es.md) | Todas las herramientas integradas, fuentes RQL, herramientas eliminadas en 1.0.0 y sus equivalentes RQL |
| [Herramientas propias y runtime](custom-tools.es.md) | Escribe herramientas y prompts; usa MCP dentro de tu juego |
| [Servidor MCP y conexión](mcp-server.es.md) | Modos Cloud y Custom, variables de entorno del plugin, servidor local |
| [Despliegue con Docker](DOCKER_DEPLOYMENT.es.md) | Ejecuta el servidor MCP en un contenedor |
| [Desactivar en CI](ci-disable.es.md) | Mantén el plugin fuera de las ejecuciones de tests normales en CI |
| [Referencia de la CLI](../cli/docs/README.es.md) | Todos los comandos y opciones de `unity-mcp-cli` |
| [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) | Tutoriales, guías y FAQ |
| [Guía de desarrollo](dev/Development.es.md) | Estructura del repositorio, estilo de código, tests y CI para colaboradores |

Relacionado: [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) (el servidor MCP compartido) · [ai-game.dev](https://ai-game.dev) · [Discord](https://discord.gg/cfbdMZX99G)

## Contribuir

Las ideas, los reportes de errores y los pull requests son bienvenidos. Lee la [Guía de desarrollo](dev/Development.es.md), haz un fork del repositorio, abre `./Unity-MCP-Plugin` en Unity y envía un pull request a `main`. Si el proyecto te ayuda, dale una estrella.

## Licencia

**Gratis para el desarrollo de juegos, incluidos los juegos comerciales.** El plugin usa la licencia [Apache-2.0](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE). Utiliza [RQL](https://github.com/IvanMurzak/RQL) y [Code-Exec](https://github.com/IvanMurzak/Code-Exec), con licencia PolyForm Shield 1.0.0: esa licencia solo restringe a las empresas que crean herramientas de IA competidoras para el desarrollo de juegos. Los desarrolladores de juegos pueden usarlo todo libremente.
