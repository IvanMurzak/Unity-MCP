<div align="center" width="100%">
  <h1>Unity MCP — <i>CLI</i></h1>

[![npm](https://img.shields.io/npm/v/unity-mcp-cli?label=npm&labelColor=333A41 'paquete npm')](https://www.npmjs.com/package/unity-mcp-cli)
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

<b>[中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.es.md)</b>

Herramienta CLI multiplataforma para **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** — crea proyectos, instala plugins, configura herramientas MCP e inicia Unity con conexiones MCP activas. Todo desde una sola linea de comandos.

## ![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-features.es.svg?raw=true)

- :white_check_mark: **Crear proyectos** — crea nuevos proyectos de Unity mediante el Editor de Unity
- :white_check_mark: **Instalar editores** — instala cualquier version del Editor de Unity desde la linea de comandos
- :white_check_mark: **Instalar plugin** — agrega el plugin Unity-MCP a `manifest.json` con todos los registros de ambito requeridos
- :white_check_mark: **Eliminar plugin** — elimina el plugin Unity-MCP de `manifest.json`
- :white_check_mark: **Configurar** — activa/desactiva herramientas, prompts y recursos MCP
- :white_check_mark: **Verificar estado** — visualiza el proceso de Unity, servidor local y conexion al servidor en la nube de un vistazo
- :white_check_mark: **Ejecutar herramientas** — ejecuta herramientas MCP directamente desde la linea de comandos
- :white_check_mark: **Configurar MCP** — escribe archivos de configuracion MCP para agentes de IA en cualquiera de los 14 agentes soportados
- :white_check_mark: **Configurar habilidades** — genera archivos de habilidades para agentes de IA a traves del servidor MCP
- :white_check_mark: **Esperar disponibilidad** — sondea hasta que Unity Editor y el servidor MCP esten conectados y acepten llamadas de herramientas
- :white_check_mark: **Abrir y conectar** — inicia Unity con variables de entorno MCP opcionales para la conexion automatica al servidor
- :white_check_mark: **Multiplataforma** — Windows, macOS y Linux
- :white_check_mark: **Compatible con CI** — detecta automaticamente terminales no interactivas y desactiva spinners/colores
- :white_check_mark: **Modo detallado** — usa `--verbose` en cualquier comando para obtener salida de diagnostico detallada
- :white_check_mark: **Control de versiones** — nunca degrada versiones del plugin, resuelve la ultima version desde OpenUPM

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Inicio rapido

Instala globalmente y ejecuta:

```bash
# 1.1 Install unity-mcp-cli                                #  ┌────────────────────┐
npm install -g unity-mcp-cli                               #  │ Available AI agent │
                                                           #  ├────────────────────┤
# 1.2 (Optional) Install Unity                             #  │ antigravity        │
unity-mcp-cli install-unity                                #  │ claude-code        │
                                                           #  │ claude-desktop     │
# 1.3 (Optional) Create Unity project                      #  │ cline              │
unity-mcp-cli create-project ./MyUnityProject              #  │ codex              │
                                                           #  │ cursor             │
# 2. Install "AI Game Developer" in Unity project          #  │ gemini             │
unity-mcp-cli install-plugin ./MyUnityProject              #  │ github-copilot-cli │
                                                           #  │ kilo-code          │
# 3. Login to cloud server                                 #  │ open-code          │
unity-mcp-cli login ./MyUnityProject                       #  │ rider-junie        │
                                                           #  │ unity-ai           │
# 4. Open Unity project (auto-connects and generates skills)  │ vs-copilot         │
unity-mcp-cli open ./MyUnityProject                        #  │ vscode-copilot     │
                                                           #  └────────────────────┘
# 5. Wait for Unity Editor to be ready
unity-mcp-cli wait-for-ready ./MyUnityProject
```

O ejecuta cualquier comando al instante con `npx` — sin necesidad de instalacion global:

```bash
npx unity-mcp-cli install-plugin /path/to/unity/project
```

> **Requisitos:** [Node.js](https://nodejs.org/) ^20.19.0 || >=22.12.0. [Unity Hub](https://unity.com/download) se instala automaticamente si no se encuentra.

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Contenidos

- [Inicio rapido](#inicio-rapido)
- [Contenidos](#contenidos)
- [Comandos](#comandos)
  - [`configure`](#configure)
  - [`create-project`](#create-project)
  - [`install-plugin`](#install-plugin)
  - [`install-unity`](#install-unity)
  - [`open`](#open)
  - [`run-tool`](#run-tool)
  - [`wait-for-ready`](#wait-for-ready)
  - [`setup-mcp`](#setup-mcp)
  - [`setup-skills`](#setup-skills)
  - [`remove-plugin`](#remove-plugin)
  - [`status`](#status)
  - [Opciones globales](#opciones-globales)
- [Ejemplo de automatizacion completa](#ejemplo-de-automatizacion-completa)
- [Como funciona](#como-funciona)
    - [Puerto determinista](#puerto-determinista)
    - [Instalacion del plugin](#instalacion-del-plugin)
    - [Archivo de configuracion](#archivo-de-configuracion)
    - [Integracion con Unity Hub](#integracion-con-unity-hub)

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Comandos

## `configure`

Configura herramientas, prompts y recursos MCP en `UserSettings/AI-Game-Developer-Config.json`.

```bash
unity-mcp-cli configure ./MyGame --list
```

| Opcion | Requerido | Descripcion |
|---|---|---|
| `[path]` | Si | Ruta al proyecto de Unity (posicional o `--path`) |
| `--list` | No | Muestra la configuracion actual y termina |
| `--enable-tools <names>` | No | Activa herramientas especificas (separadas por comas) |
| `--disable-tools <names>` | No | Desactiva herramientas especificas (separadas por comas) |
| `--enable-all-tools` | No | Activa todas las herramientas |
| `--disable-all-tools` | No | Desactiva todas las herramientas |
| `--enable-prompts <names>` | No | Activa prompts especificos (separados por comas) |
| `--disable-prompts <names>` | No | Desactiva prompts especificos (separados por comas) |
| `--enable-all-prompts` | No | Activa todos los prompts |
| `--disable-all-prompts` | No | Desactiva todos los prompts |
| `--enable-resources <names>` | No | Activa recursos especificos (separados por comas) |
| `--disable-resources <names>` | No | Desactiva recursos especificos (separados por comas) |
| `--enable-all-resources` | No | Activa todos los recursos |
| `--disable-all-resources` | No | Desactiva todos los recursos |

**Ejemplo — activar herramientas especificas y desactivar todos los prompts:**

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

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `create-project`

Crea un nuevo proyecto de Unity utilizando el Editor de Unity.

```bash
unity-mcp-cli create-project /path/to/new/project
```

| Opcion | Requerido | Descripcion |
|---|---|---|
| `[path]` | Si | Ruta donde se creara el proyecto (posicional o `--path`) |
| `--unity <version>` | No | Version del Editor de Unity a utilizar (por defecto, la mas alta instalada) |

**Ejemplo — crear un proyecto con una version especifica del editor:**

```bash
unity-mcp-cli create-project ./MyGame --unity 2022.3.62f1
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-plugin`

Instala el plugin Unity-MCP en el archivo `Packages/manifest.json` de un proyecto de Unity.

```bash
unity-mcp-cli install-plugin ./MyGame
```

| Opcion | Requerido | Descripcion |
|---|---|---|
| `[path]` | Si | Ruta al proyecto de Unity (posicional o `--path`) |
| `--plugin-version <version>` | No | Version del plugin a instalar (por defecto, la ultima desde [OpenUPM](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)) |

Este comando:
1. Agrega el **registro de ambito de OpenUPM** con todos los ambitos requeridos
2. Agrega `com.ivanmurzak.unity.mcp` a `dependencies`
3. **Nunca degrada** — si ya hay instalada una version superior, se conserva

**Ejemplo — instalar una version especifica del plugin:**

```bash
unity-mcp-cli install-plugin ./MyGame --plugin-version 0.51.6
```

> Despues de ejecutar este comando, abre el proyecto en el Editor de Unity para completar la instalacion del paquete.

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-unity`

Instala una version del Editor de Unity mediante la CLI de Unity Hub.

```bash
unity-mcp-cli install-unity 6000.3.1f1
```

| Argumento / Opcion | Requerido | Descripcion |
|---|---|---|
| `[version]` | No | Version del Editor de Unity a instalar (ej. `6000.3.1f1`) |
| `--path <path>` | No | Lee la version requerida desde un proyecto existente |

Si no se proporciona ningun argumento ni opcion, el comando instala la ultima version estable desde la lista de lanzamientos de Unity Hub.

**Ejemplo — instalar la version del editor que necesita un proyecto:**

```bash
unity-mcp-cli install-unity --path ./MyGame
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `open`

Abre un proyecto de Unity en el Editor de Unity. Por defecto, establece variables de entorno de conexion MCP si se proporcionan opciones de conexion. Usa `--no-connect` para abrir sin conexion MCP.

```bash
unity-mcp-cli open ./MyGame
```

| Opcion | Variable de entorno | Requerido | Descripcion |
|---|---|---|---|
| `[path]` | — | Si | Ruta al proyecto de Unity (posicional o `--path`) |
| `--unity <version>` | — | No | Version especifica del Editor de Unity a utilizar (por defecto, la version de la configuracion del proyecto; si no esta disponible, la mas alta instalada) |
| `--no-connect` | — | No | Abrir sin variables de entorno de conexion MCP |
| `--url <url>` | `UNITY_MCP_HOST` | No | URL del servidor MCP al que conectarse |
| `--keep-connected` | `UNITY_MCP_KEEP_CONNECTED` | No | Fuerza mantener la conexion activa |
| `--token <token>` | `UNITY_MCP_TOKEN` | No | Token de autenticacion |
| `--auth <option>` | `UNITY_MCP_AUTH_OPTION` | No | Modo de autenticacion: `none` o `required` |
| `--tools <names>` | `UNITY_MCP_TOOLS` | No | Lista de herramientas a activar, separadas por comas |
| `--transport <method>` | `UNITY_MCP_TRANSPORT` | No | Metodo de transporte: `streamableHttp` o `stdio` |
| `--start-server <value>` | `UNITY_MCP_START_SERVER` | No | Establece `true` o `false` para controlar el inicio automatico del servidor MCP |

El proceso del editor se lanza en modo desacoplado — la CLI regresa inmediatamente.

**Ejemplo — abrir con conexion MCP:**

```bash
unity-mcp-cli open ./MyGame \
  --url http://localhost:8080 \
  --keep-connected
```

**Ejemplo — abrir sin conexion MCP (apertura simple):**

```bash
unity-mcp-cli open ./MyGame --no-connect
```

**Ejemplo — abrir con autenticacion y herramientas especificas:**

```bash
unity-mcp-cli open ./MyGame \
  --url http://my-server:8080 \
  --token my-secret-token \
  --auth required \
  --tools gameobject-create,gameobject-find
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `run-tool`

Ejecuta una herramienta MCP directamente a traves de la API HTTP. La URL del servidor y el token de autorizacion se **resuelven automaticamente** desde el archivo de configuracion del proyecto (`UserSettings/AI-Game-Developer-Config.json`), basandose en el modo de conexion actual (Custom o Cloud).

```bash
unity-mcp-cli run-tool gameobject-create ./MyGame --input '{"name":"Cube"}'
```

| Opcion | Requerido | Descripcion |
|---|---|---|
| `<tool-name>` | Si | Nombre de la herramienta MCP a ejecutar |
| `[path]` | No | Ruta al proyecto de Unity (posicional o `--path`) — se usa para leer la configuracion y detectar el puerto |
| `--url <url>` | No | URL directa del servidor (omite la configuracion) |
| `--token <token>` | No | Token Bearer (omite la configuracion) |
| `--input <json>` | No | Cadena JSON con los argumentos de la herramienta (por defecto `{}`) |
| `--input-file <file>` | No | Lee los argumentos JSON desde un archivo |
| `--raw` | No | Salida JSON sin formato (sin formato visual, sin spinner) |
| `--timeout <ms>` | No | Tiempo de espera de la solicitud en milisegundos (por defecto: 60000) |

**Prioridad de resolucion de URL:**
1. `--url` → se usa directamente
2. Archivo de configuracion → `host` (modo Custom) o URL de nube predefinida (modo Cloud)
3. Puerto determinista a partir de la ruta del proyecto

**La autorizacion** se lee automaticamente desde la configuracion del proyecto (`token` en modo Custom, `cloudToken` en modo Cloud). Usa `--token` para reemplazar explicitamente el token derivado de la configuracion.

**Ejemplo — llamar a una herramienta (URL y autenticacion desde la configuracion):**

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

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `wait-for-ready`

Espera hasta que Unity Editor y el servidor MCP esten conectados y listos para aceptar llamadas de herramientas. Sondea el servidor a un intervalo configurable hasta que responda correctamente o se alcance el tiempo limite. Util para scripts de automatizacion y orquestacion de agentes de IA donde `open` inicia Unity pero el agente necesita saber cuando puede empezar a llamar herramientas.

```bash
unity-mcp-cli wait-for-ready ./MyGame
```

| Opcion | Requerido | Descripcion |
|---|---|---|
| `[path]` | No | Ruta al proyecto de Unity (posicional o `--path`) — se usa para leer la configuracion y detectar el puerto |
| `--url <url>` | No | URL directa del servidor (omite la configuracion) |
| `--token <token>` | No | Token Bearer (omite la configuracion) |
| `--timeout <ms>` | No | Tiempo maximo de espera en milisegundos (por defecto: 120000) |
| `--interval <ms>` | No | Intervalo de sondeo en milisegundos (por defecto: 3000) |

**Ejemplo — esperar con timeout por defecto (120s):**

```bash
unity-mcp-cli open ./MyGame
unity-mcp-cli wait-for-ready ./MyGame
unity-mcp-cli run-tool tests-run ./MyGame --input '{"testMode":"EditMode"}'
```

**Ejemplo — timeout mas corto para CI:**

```bash
unity-mcp-cli wait-for-ready ./MyGame --timeout 60000 --interval 2000
```

**Ejemplo — URL explicita del servidor:**

```bash
unity-mcp-cli wait-for-ready --url http://localhost:8080 --timeout 30000
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `setup-mcp`

Escribe archivos de configuracion MCP para agentes de IA, permitiendo la configuracion headless/CI sin la interfaz del Editor de Unity. Soporta los 14 agentes (Claude Code, Cursor, Gemini, Codex, etc.).

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame
```

| Opcion | Requerido | Descripcion |
|---|---|---|
| `[agent-id]` | Si | Agente a configurar (usa `--list` para ver todos) |
| `[path]` | No | Ruta al proyecto de Unity (por defecto, el directorio actual) |
| `--transport <transport>` | No | Metodo de transporte: `stdio` o `http` (por defecto: `http`) |
| `--url <url>` | No | URL del servidor (para transporte http) |
| `--token <token>` | No | Token de autenticacion |
| `--list` | No | Lista todos los IDs de agentes disponibles |

**Ejemplo — listar todos los agentes soportados:**

```bash
unity-mcp-cli setup-mcp --list
```

**Ejemplo — configurar Cursor con transporte stdio:**

```bash
unity-mcp-cli setup-mcp cursor ./MyGame --transport stdio
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `setup-skills`

Genera archivos de habilidades para un agente de IA llamando a la API de herramientas del sistema del servidor MCP. Requiere que el Editor de Unity este en ejecucion con el plugin MCP instalado.

```bash
unity-mcp-cli setup-skills claude-code ./MyGame
```

| Opcion | Requerido | Descripcion |
|---|---|---|
| `[agent-id]` | Si | Agente para el que generar habilidades (usa `--list` para ver todos) |
| `[path]` | No | Ruta al proyecto de Unity (por defecto, el directorio actual) |
| `--url <url>` | No | URL del servidor |
| `--token <token>` | No | Token de autenticacion |
| `--list` | No | Lista todos los agentes con el estado de soporte de habilidades |
| `--timeout <ms>` | No | Tiempo de espera de la solicitud en milisegundos (por defecto: 60000) |

**Ejemplo — listar agentes con soporte de habilidades:**

```bash
unity-mcp-cli setup-skills --list
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `remove-plugin`

Elimina el plugin Unity-MCP del archivo `Packages/manifest.json` de un proyecto de Unity.

```bash
unity-mcp-cli remove-plugin ./MyGame
```

| Opcion | Requerido | Descripcion |
|---|---|---|
| `[path]` | Si | Ruta al proyecto de Unity (posicional o `--path`) |

Este comando:
1. Elimina `com.ivanmurzak.unity.mcp` de `dependencies`
2. **Conserva los registros de ambito y sus ambitos** — otros paquetes pueden depender de ellos
3. **No realiza ninguna accion** si el plugin no esta instalado

> Despues de ejecutar este comando, abre el proyecto en el Editor de Unity para aplicar el cambio.

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `status`

Verifica el estado de conexion del Unity Editor y el servidor MCP. Muestra si Unity esta en ejecucion, si el servidor MCP local esta accesible y si el servidor configurado (ej. nube) esta accesible.

```bash
unity-mcp-cli status ./MyGame
```

| Opcion | Requerido | Descripcion |
|---|---|---|
| `[path]` | No | Ruta al proyecto de Unity (posicional o `--path`) |
| `--url <url>` | No | URL directa del servidor (omite la configuracion) |
| `--token <token>` | No | Token Bearer (omite la configuracion) |
| `--timeout <ms>` | No | Tiempo de espera del sondeo en milisegundos (por defecto: 5000) |

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## Opciones globales

Estas opciones estan disponibles en todos los comandos:

| Opcion | Descripcion |
|---|---|
| `-v, --verbose` | Activa la salida de diagnostico detallada para resolucion de problemas |
| `--version` | Muestra la version de la CLI |
| `--help` | Muestra la ayuda del comando |

**Ejemplo — ejecutar cualquier comando con salida detallada:**

```bash
unity-mcp-cli install-plugin ./MyGame --verbose
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Ejemplo de automatizacion completa

Configura un proyecto Unity MCP completo desde cero con un solo script:

```bash
# 1. Create a new Unity project
unity-mcp-cli create-project ./MyAIGame --unity 6000.3.1f1

# 2. Install the Unity-MCP plugin
unity-mcp-cli install-plugin ./MyAIGame

# 3. Enable all MCP tools
unity-mcp-cli configure ./MyAIGame --enable-all-tools

# 4. Login to cloud server (authenticates and saves token)
unity-mcp-cli login ./MyAIGame

# 5. Open the project (auto-connects and generates skills for claude-code)
unity-mcp-cli open ./MyAIGame

# 6. Wait for Unity Editor and MCP server to be ready
unity-mcp-cli wait-for-ready ./MyAIGame

# 7. Run tests to verify everything works
unity-mcp-cli run-tool tests-run ./MyAIGame --input '{"testMode":"EditMode"}'
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Como funciona

### Puerto determinista

La CLI genera un **puerto determinista** para cada proyecto de Unity basandose en la ruta de su directorio (hash SHA256 mapeado al rango de puertos 20000–29999). Esto coincide con la generacion de puertos del plugin de Unity, garantizando que el servidor y el plugin acuerden automaticamente el mismo puerto sin necesidad de configuracion manual.

### Instalacion del plugin

El comando `install-plugin` modifica `Packages/manifest.json` directamente:
- Agrega el registro de ambito de [OpenUPM](https://openupm.com/) (`package.openupm.com`)
- Registra todos los ambitos requeridos (`com.ivanmurzak`, `extensions.unity`, `org.nuget.*`)
- Agrega la dependencia `com.ivanmurzak.unity.mcp` con actualizaciones que respetan la version (nunca degrada)

### Archivo de configuracion

El comando `configure` lee y escribe `UserSettings/AI-Game-Developer-Config.json`, que controla:
- **Tools** — herramientas MCP disponibles para los agentes de IA
- **Prompts** — prompts predefinidos inyectados en las conversaciones con el LLM
- **Resources** — datos de solo lectura expuestos a los agentes de IA
- **Connection settings** — URL del host, token de autenticacion, metodo de transporte, tiempos de espera

### Integracion con Unity Hub

Los comandos que gestionan editores o crean proyectos usan la **CLI de Unity Hub** (modo `--headless`). Si Unity Hub no esta instalado, la CLI **lo descarga e instala automaticamente**:
- **Windows** — instalacion silenciosa mediante `UnityHubSetup.exe /S` (puede requerir privilegios de administrador)
- **macOS** — descarga el DMG, lo monta y copia `Unity Hub.app` en `/Applications`
- **Linux** — descarga `UnityHub.AppImage` en `~/Applications/`

> Para la documentacion completa del proyecto Unity-MCP, consulta el [README principal](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md).

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)
