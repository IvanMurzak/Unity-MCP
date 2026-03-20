<div align="center" width="100%">
  <h1>Unity MCP — <i>CLI</i></h1>

[![npm](https://img.shields.io/npm/v/unity-mcp-cli?label=npm&labelColor=333A41 'npm package')](https://www.npmjs.com/package/unity-mcp-cli)
[![Node.js](https://img.shields.io/badge/Node.js-%5E20.19.0%20%7C%7C%20%3E%3D22.12.0-5FA04E?logo=nodedotjs&labelColor=333A41 'Node.js')](https://nodejs.org/)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

  <img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/promo/ai-developer-banner-glitch.gif" alt="AI Game Developer" title="Unity MCP CLI" width="100%">

  <p>
    <a href="https://claude.ai/download"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/claude-64.png" alt="Claude" title="Claude" height="36"></a>&nbsp;&nbsp;
    <a href="https://openai.com/index/introducing-codex/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/codex-64.png" alt="Codex" title="Codex" height="36"></a>&nbsp;&nbsp;
    <a href="https://www.cursor.com/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/cursor-64.png" alt="Cursor" title="Cursor" height="36"></a>&nbsp;&nbsp;
    <a href="https://code.visualstudio.com/docs/copilot/overview"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/github-copilot-64.png" alt="GitHub Copilot" title="GitHub Copilot" height="36"></a>&nbsp;&nbsp;
    <a href="https://gemini.google.com/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/gemini-64.png" alt="Gemini" title="Gemini" height="36"></a>&nbsp;&nbsp;
    <a href="https://antigravity.google/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/antigravity-64.png" alt="Antigravity" title="Antigravity" height="36"></a>&nbsp;&nbsp;
    <a href="https://code.visualstudio.com/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/vs-code-64.png" alt="VS Code" title="VS Code" height="36"></a>&nbsp;&nbsp;
    <a href="https://www.jetbrains.com/rider/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/rider-64.png" alt="Rider" title="Rider" height="36"></a>&nbsp;&nbsp;
    <a href="https://visualstudio.microsoft.com/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/visual-studio-64.png" alt="Visual Studio" title="Visual Studio" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/anthropics/claude-code"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/open-code-64.png" alt="Open Code" title="Open Code" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/cline/cline"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/cline-64.png" alt="Cline" title="Cline" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/Kilo-Org/kilocode"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/kilo-code-64.png" alt="Kilo Code" title="Kilo Code" height="36"></a>
  </p>

</div>

<b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.ja.md)</b>

Herramienta CLI multiplataforma para **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** — crea proyectos, instala plugins, configura herramientas MCP e inicia Unity con conexiones MCP activas. Todo desde una sola línea de comandos.

## ![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-features.svg?raw=true)

- :white_check_mark: **Create projects** — crea nuevos proyectos de Unity mediante el Editor de Unity
- :white_check_mark: **Install editors** — instala cualquier versión del Editor de Unity desde la línea de comandos
- :white_check_mark: **Install plugin** — agrega el plugin Unity-MCP a `manifest.json` con todos los registros de ámbito requeridos
- :white_check_mark: **Remove plugin** — elimina el plugin Unity-MCP de `manifest.json`
- :white_check_mark: **Configure** — activa/desactiva herramientas, prompts y recursos MCP
- :white_check_mark: **Run tools** — ejecuta herramientas MCP directamente desde la línea de comandos
- :white_check_mark: **Setup MCP** — escribe archivos de configuración MCP para agentes de IA en cualquiera de los 14 agentes soportados
- :white_check_mark: **Setup skills** — genera archivos de habilidades para agentes de IA a través del servidor MCP
- :white_check_mark: **Open & Connect** — inicia Unity con variables de entorno MCP opcionales para la conexión automática al servidor
- :white_check_mark: **Cross-platform** — Windows, macOS y Linux
- :white_check_mark: **CI-friendly** — detecta automáticamente terminales no interactivas y desactiva spinners/colores
- :white_check_mark: **Verbose mode** — usa `--verbose` en cualquier comando para obtener salida de diagnóstico detallada
- :white_check_mark: **Version-aware** — nunca degrada versiones del plugin; resuelve la última versión desde OpenUPM

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Inicio Rápido

Instala globalmente y ejecuta:

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-plugin /path/to/unity/project
```

O ejecuta cualquier comando al instante con `npx` — sin necesidad de instalación global:

```bash
npx unity-mcp-cli install-plugin /path/to/unity/project
```

> **Requisitos:** [Node.js](https://nodejs.org/) ^20.19.0 || >=22.12.0. [Unity Hub](https://unity.com/download) se instala automáticamente si no se encuentra.

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Contenidos

- [Inicio Rápido](#inicio-rápido)
- [Contenidos](#contenidos)
- [Comandos](#comandos)
  - [`configure`](#configure)
  - [`create-project`](#create-project)
  - [`install-plugin`](#install-plugin)
  - [`install-unity`](#install-unity)
  - [`open`](#open)
  - [`run-tool`](#run-tool)
  - [`setup-mcp`](#setup-mcp)
  - [`setup-skills`](#setup-skills)
  - [`remove-plugin`](#remove-plugin)
  - [Opciones Globales](#opciones-globales)
- [Ejemplo de Automatización Completa](#ejemplo-de-automatización-completa)
- [Cómo Funciona](#cómo-funciona)
    - [Puerto Determinista](#puerto-determinista)
    - [Instalación del Plugin](#instalación-del-plugin)
    - [Archivo de Configuración](#archivo-de-configuración)
    - [Integración con Unity Hub](#integración-con-unity-hub)

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Comandos

## `configure`

Configura herramientas, prompts y recursos MCP en `UserSettings/AI-Game-Developer-Config.json`.

```bash
unity-mcp-cli configure ./MyGame --list
```

| Opción | Requerido | Descripción |
|---|---|---|
| `[path]` | Sí | Ruta al proyecto de Unity (posicional o `--path`) |
| `--list` | No | Muestra la configuración actual y termina |
| `--enable-tools <names>` | No | Activa herramientas específicas (separadas por comas) |
| `--disable-tools <names>` | No | Desactiva herramientas específicas (separadas por comas) |
| `--enable-all-tools` | No | Activa todas las herramientas |
| `--disable-all-tools` | No | Desactiva todas las herramientas |
| `--enable-prompts <names>` | No | Activa prompts específicos (separados por comas) |
| `--disable-prompts <names>` | No | Desactiva prompts específicos (separados por comas) |
| `--enable-all-prompts` | No | Activa todos los prompts |
| `--disable-all-prompts` | No | Desactiva todos los prompts |
| `--enable-resources <names>` | No | Activa recursos específicos (separados por comas) |
| `--disable-resources <names>` | No | Desactiva recursos específicos (separados por comas) |
| `--enable-all-resources` | No | Activa todos los recursos |
| `--disable-all-resources` | No | Desactiva todos los recursos |

**Ejemplo — activar herramientas específicas y desactivar todos los prompts:**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-tools gameobject-create,gameobject-find \
  --disable-all-prompts
```

**Ejemplo — activar todo:**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-all-tools \
  --enable-all-prompts \
  --enable-all-resources
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `create-project`

Crea un nuevo proyecto de Unity utilizando el Editor de Unity.

```bash
unity-mcp-cli create-project /path/to/new/project
```

| Opción | Requerido | Descripción |
|---|---|---|
| `[path]` | Sí | Ruta donde se creará el proyecto (posicional o `--path`) |
| `--unity <version>` | No | Versión del Editor de Unity a utilizar (por defecto, la más alta instalada) |

**Ejemplo — crear un proyecto con una versión específica del editor:**

```bash
unity-mcp-cli create-project ./MyGame --unity 2022.3.62f1
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-plugin`

Instala el plugin Unity-MCP en el archivo `Packages/manifest.json` de un proyecto de Unity.

```bash
unity-mcp-cli install-plugin ./MyGame
```

| Opción | Requerido | Descripción |
|---|---|---|
| `[path]` | Sí | Ruta al proyecto de Unity (posicional o `--path`) |
| `--plugin-version <version>` | No | Versión del plugin a instalar (por defecto, la última desde [OpenUPM](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)) |

Este comando:
1. Agrega el **registro de ámbito de OpenUPM** con todos los ámbitos requeridos
2. Agrega `com.ivanmurzak.unity.mcp` a `dependencies`
3. **Nunca degrada** — si ya hay instalada una versión superior, se conserva

**Ejemplo — instalar una versión específica del plugin:**

```bash
unity-mcp-cli install-plugin ./MyGame --plugin-version 0.51.6
```

> Después de ejecutar este comando, abre el proyecto en el Editor de Unity para completar la instalación del paquete.

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-unity`

Instala una versión del Editor de Unity mediante la CLI de Unity Hub.

```bash
unity-mcp-cli install-unity 6000.3.1f1
```

| Argumento / Opción | Requerido | Descripción |
|---|---|---|
| `[version]` | No | Versión del Editor de Unity a instalar (ej. `6000.3.1f1`) |
| `--path <path>` | No | Lee la versión requerida desde un proyecto existente |

Si no se proporciona ningún argumento ni opción, el comando instala la última versión estable desde la lista de lanzamientos de Unity Hub.

**Ejemplo — instalar la versión del editor que necesita un proyecto:**

```bash
unity-mcp-cli install-unity --path ./MyGame
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `open`

Abre un proyecto de Unity en el Editor de Unity. Por defecto, establece variables de entorno de conexión MCP si se proporcionan opciones de conexión. Usa `--no-connect` para abrir sin conexión MCP.

```bash
unity-mcp-cli open ./MyGame
```

| Opción | Variable de Entorno | Requerido | Descripción |
|---|---|---|---|
| `[path]` | — | Sí | Ruta al proyecto de Unity (posicional o `--path`) |
| `--unity <version>` | — | No | Versión específica del Editor de Unity a utilizar (por defecto, la versión de la configuración del proyecto; si no está disponible, la más alta instalada) |
| `--no-connect` | — | No | Abrir sin variables de entorno de conexión MCP |
| `--url <url>` | `UNITY_MCP_HOST` | No | URL del servidor MCP al que conectarse |
| `--keep-connected` | `UNITY_MCP_KEEP_CONNECTED` | No | Fuerza mantener la conexión activa |
| `--token <token>` | `UNITY_MCP_TOKEN` | No | Token de autenticación |
| `--auth <option>` | `UNITY_MCP_AUTH_OPTION` | No | Modo de autenticación: `none` o `required` |
| `--tools <names>` | `UNITY_MCP_TOOLS` | No | Lista de herramientas a activar, separadas por comas |
| `--transport <method>` | `UNITY_MCP_TRANSPORT` | No | Método de transporte: `streamableHttp` o `stdio` |
| `--start-server <value>` | `UNITY_MCP_START_SERVER` | No | Establece `true` o `false` para controlar el inicio automático del servidor MCP |

El proceso del editor se lanza en modo desacoplado — la CLI regresa inmediatamente.

**Ejemplo — abrir con conexión MCP:**

```bash
unity-mcp-cli open ./MyGame \
  --url http://localhost:8080 \
  --keep-connected
```

**Ejemplo — abrir sin conexión MCP (apertura simple):**

```bash
unity-mcp-cli open ./MyGame --no-connect
```

**Ejemplo — abrir con autenticación y herramientas específicas:**

```bash
unity-mcp-cli open ./MyGame \
  --url http://my-server:8080 \
  --token my-secret-token \
  --auth required \
  --tools gameobject-create,gameobject-find
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `run-tool`

Ejecuta una herramienta MCP directamente a través de la API HTTP. La URL del servidor y el token de autorización se **resuelven automáticamente** desde el archivo de configuración del proyecto (`UserSettings/AI-Game-Developer-Config.json`), basándose en el modo de conexión actual (Custom o Cloud).

```bash
unity-mcp-cli run-tool gameobject-create ./MyGame --input '{"name":"Cube"}'
```

| Opción | Requerido | Descripción |
|---|---|---|
| `<tool-name>` | Sí | Nombre de la herramienta MCP a ejecutar |
| `[path]` | No | Ruta al proyecto de Unity (posicional o `--path`) — se usa para leer la configuración y detectar el puerto |
| `--url <url>` | No | URL directa del servidor (omite la configuración) |
| `--token <token>` | No | Token Bearer (omite la configuración) |
| `--input <json>` | No | Cadena JSON con los argumentos de la herramienta (por defecto `{}`) |
| `--input-file <file>` | No | Lee los argumentos JSON desde un archivo |
| `--raw` | No | Salida JSON sin formato (sin formato visual, sin spinner) |
| `--timeout <ms>` | No | Tiempo de espera de la solicitud en milisegundos (por defecto: 60000) |

**Prioridad de resolución de URL:**
1. `--url` → se usa directamente
2. Archivo de configuración → `host` (modo Custom) o URL de nube predefinida (modo Cloud)
3. Puerto determinista a partir de la ruta del proyecto

**La autorización** se lee automáticamente desde la configuración del proyecto (`token` en modo Custom, `cloudToken` en modo Cloud). Usa `--token` para reemplazar explícitamente el token derivado de la configuración.

**Ejemplo — llamar a una herramienta (URL y autenticación desde la configuración):**

```bash
unity-mcp-cli run-tool gameobject-find ./MyGame --input '{"query":"Player"}'
```

**Ejemplo — URL explicita:**

```bash
unity-mcp-cli run-tool scene-save --url http://localhost:8080
```

**Ejemplo — redirigir salida JSON sin formato:**

```bash
unity-mcp-cli run-tool assets-list ./MyGame --raw | jq '.results'
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `setup-mcp`

Escribe archivos de configuración MCP para agentes de IA, permitiendo la configuración headless/CI sin la interfaz del Editor de Unity. Soporta los 14 agentes (Claude Code, Cursor, Gemini, Codex, etc.).

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame
```

| Opción | Requerido | Descripción |
|---|---|---|
| `[agent-id]` | Sí | Agente a configurar (usa `--list` para ver todos) |
| `[path]` | No | Ruta al proyecto de Unity (por defecto, el directorio actual) |
| `--transport <transport>` | No | Método de transporte: `stdio` o `http` (por defecto: `http`) |
| `--url <url>` | No | URL del servidor (para transporte http) |
| `--token <token>` | No | Token de autenticación |
| `--list` | No | Lista todos los IDs de agentes disponibles |

**Ejemplo — listar todos los agentes soportados:**

```bash
unity-mcp-cli setup-mcp --list
```

**Ejemplo — configurar Cursor con transporte stdio:**

```bash
unity-mcp-cli setup-mcp cursor ./MyGame --transport stdio
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `setup-skills`

Genera archivos de habilidades para un agente de IA llamando a la API de herramientas del sistema del servidor MCP. Requiere que el Editor de Unity esté en ejecución con el plugin MCP instalado.

```bash
unity-mcp-cli setup-skills claude-code ./MyGame
```

| Opción | Requerido | Descripción |
|---|---|---|
| `[agent-id]` | Sí | Agente para el que generar habilidades (usa `--list` para ver todos) |
| `[path]` | No | Ruta al proyecto de Unity (por defecto, el directorio actual) |
| `--url <url>` | No | URL del servidor |
| `--token <token>` | No | Token de autenticación |
| `--list` | No | Lista todos los agentes con el estado de soporte de habilidades |
| `--timeout <ms>` | No | Tiempo de espera de la solicitud en milisegundos (por defecto: 60000) |

**Ejemplo — listar agentes con soporte de habilidades:**

```bash
unity-mcp-cli setup-skills --list
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `remove-plugin`

Elimina el plugin Unity-MCP del archivo `Packages/manifest.json` de un proyecto de Unity.

```bash
unity-mcp-cli remove-plugin ./MyGame
```

| Opción | Requerido | Descripción |
|---|---|---|
| `[path]` | Sí | Ruta al proyecto de Unity (posicional o `--path`) |

Este comando:
1. Elimina `com.ivanmurzak.unity.mcp` de `dependencies`
2. **Conserva los registros de ámbito y sus ámbitos** — otros paquetes pueden depender de ellos
3. **No realiza ninguna acción** si el plugin no está instalado

> Después de ejecutar este comando, abre el proyecto en el Editor de Unity para aplicar el cambio.

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## Opciones Globales

Estas opciones están disponibles en todos los comandos:

| Opción | Descripción |
|---|---|
| `-v, --verbose` | Activa la salida de diagnóstico detallada para resolución de problemas |
| `--version` | Muestra la versión de la CLI |
| `--help` | Muestra la ayuda del comando |

**Ejemplo — ejecutar cualquier comando con salida detallada:**

```bash
unity-mcp-cli install-plugin ./MyGame --verbose
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Ejemplo de Automatización Completa

Configura un proyecto Unity MCP completo desde cero con un solo script:

```bash
# 1. Crear un nuevo proyecto de Unity
unity-mcp-cli create-project ./MyAIGame --unity 6000.3.1f1

# 2. Instalar el plugin Unity-MCP
unity-mcp-cli install-plugin ./MyAIGame

# 3. Activar todas las herramientas MCP
unity-mcp-cli configure ./MyAIGame --enable-all-tools

# 4. Configurar la integración MCP de Claude Code
unity-mcp-cli setup-mcp claude-code ./MyAIGame

# 5. Abrir el proyecto con conexión MCP
unity-mcp-cli open ./MyAIGame \
  --url http://localhost:8080 \
  --keep-connected
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Cómo Funciona

### Puerto Determinista

La CLI genera un **puerto determinista** para cada proyecto de Unity basándose en la ruta de su directorio (hash SHA256 mapeado al rango de puertos 20000–29999). Esto coincide con la generación de puertos del plugin de Unity, garantizando que el servidor y el plugin acuerden automáticamente el mismo puerto sin necesidad de configuración manual.

### Instalación del Plugin

El comando `install-plugin` modifica `Packages/manifest.json` directamente:
- Agrega el registro de ámbito de [OpenUPM](https://openupm.com/) (`package.openupm.com`)
- Registra todos los ámbitos requeridos (`com.ivanmurzak`, `extensions.unity`, `org.nuget.*`)
- Agrega la dependencia `com.ivanmurzak.unity.mcp` con actualizaciones que respetan la versión (nunca degrada)

### Archivo de Configuración

El comando `configure` lee y escribe `UserSettings/AI-Game-Developer-Config.json`, que controla:
- **Tools** — herramientas MCP disponibles para los agentes de IA
- **Prompts** — prompts predefinidos inyectados en las conversaciones con el LLM
- **Resources** — datos de solo lectura expuestos a los agentes de IA
- **Connection settings** — URL del host, token de autenticación, método de transporte, tiempos de espera

### Integración con Unity Hub

Los comandos que gestionan editores o crean proyectos usan la **CLI de Unity Hub** (modo `--headless`). Si Unity Hub no está instalado, la CLI **lo descarga e instala automáticamente**:
- **Windows** — instalación silenciosa mediante `UnityHubSetup.exe /S` (puede requerir privilegios de administrador)
- **macOS** — descarga el DMG, lo monta y copia `Unity Hub.app` en `/Applications`
- **Linux** — descarga `UnityHub.AppImage` en `~/Applications/`

> Para la documentación completa del proyecto Unity-MCP, consulta el [README principal](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md).

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)
