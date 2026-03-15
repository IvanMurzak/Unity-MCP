<div align="center" width="100%">
  <h1>Unity MCP — <i>CLI</i></h1>

[![npm](https://img.shields.io/npm/v/unity-mcp-cli?label=npm&labelColor=333A41 'npm package')](https://www.npmjs.com/package/unity-mcp-cli)
[![Node.js](https://img.shields.io/badge/Node.js-%5E20.19.0%20%7C%7C%20%3E%3D22.12.0-5FA04E?logo=nodedotjs&labelColor=333A41 'Node.js')](https://nodejs.org/)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

  <img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/promo/ai-developer-banner-glitch.gif" alt="AI Game Developer" title="Unity MCP CLI" width="100%">

</div>

<b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.ja.md)</b>

Herramienta CLI multiplataforma para **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** — crea proyectos, instala plugins, configura herramientas MCP e inicia Unity con conexiones MCP activas. Todo desde una sola línea de comandos.

## ![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-features.svg?raw=true)

- :white_check_mark: **Create projects** — crea nuevos proyectos de Unity mediante el Editor de Unity
- :white_check_mark: **Install editors** — instala cualquier versión del Editor de Unity desde la línea de comandos
- :white_check_mark: **Install plugin** — añade el plugin Unity-MCP a `manifest.json` con todos los registros de ámbito requeridos
- :white_check_mark: **Remove plugin** — elimina el plugin Unity-MCP de `manifest.json`
- :white_check_mark: **Configure** — activa/desactiva herramientas, prompts y recursos MCP
- :white_check_mark: **Connect** — inicia Unity con variables de entorno MCP para la conexión automática al servidor
- :white_check_mark: **Cross-platform** — Windows, macOS y Linux
- :white_check_mark: **Version-aware** — nunca degrada versiones del plugin; resuelve la última versión desde OpenUPM

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Inicio Rápido

Ejecuta cualquier comando al instante con `npx` — sin necesidad de instalación:

```bash
npx unity-mcp-cli install-plugin /path/to/unity/project
```

O instala globalmente:

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-plugin /path/to/unity/project
```

> **Requisitos:** [Node.js](https://nodejs.org/) ^20.19.0 || >=22.12.0. [Unity Hub](https://unity.com/download) se instala automáticamente si no se encuentra.

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Contenidos

- [Inicio Rápido](#inicio-rápido)
- [Comandos](#comandos)
  - [`configure`](#configure) — Configurar herramientas, prompts y recursos MCP
  - [`connect`](#connect) — Iniciar Unity con conexión MCP
  - [`create-project`](#create-project) — Crear un nuevo proyecto de Unity
  - [`install-plugin`](#install-plugin) — Instalar el plugin Unity-MCP en un proyecto
  - [`install-unity`](#install-unity) — Instalar el Editor de Unity mediante Unity Hub
  - [`open`](#open) — Abrir un proyecto de Unity en el Editor
  - [`remove-plugin`](#remove-plugin) — Eliminar el plugin Unity-MCP de un proyecto
- [Ejemplo de Automatización Completa](#ejemplo-de-automatización-completa)
- [Cómo Funciona](#cómo-funciona)

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Comandos

## `configure`

Configura herramientas, prompts y recursos MCP en `UserSettings/AI-Game-Developer-Config.json`.

```bash
npx unity-mcp-cli configure ./MyGame --list
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
npx unity-mcp-cli configure ./MyGame \
  --enable-tools gameobject-create,gameobject-find \
  --disable-all-prompts
```

**Ejemplo — activar todo:**

```bash
npx unity-mcp-cli configure ./MyGame \
  --enable-all-tools \
  --enable-all-prompts \
  --enable-all-resources
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `connect`

Abre un proyecto de Unity y lo conecta a un servidor MCP específico mediante variables de entorno. Cada opción se corresponde con una variable de entorno `UNITY_MCP_*` que el plugin de Unity lee al arrancar.

```bash
npx unity-mcp-cli connect \
  --path ./MyGame \
  --url http://localhost:8080
```

| Opción | Variable de Entorno | Requerido | Descripción |
|---|---|---|---|
| `--url <url>` | `UNITY_MCP_HOST` | Sí | URL del servidor MCP al que conectarse |
| `--path <path>` | — | Sí | Ruta al proyecto de Unity |
| `--keep-connected` | `UNITY_MCP_KEEP_CONNECTED` | No | Fuerza mantener la conexión activa |
| `--token <token>` | `UNITY_MCP_TOKEN` | No | Token de autenticación |
| `--auth <option>` | `UNITY_MCP_AUTH_OPTION` | No | Modo de autenticación: `none` o `required` |
| `--tools <names>` | `UNITY_MCP_TOOLS` | No | Lista de herramientas a activar, separadas por comas |
| `--transport <method>` | `UNITY_MCP_TRANSPORT` | No | Método de transporte: `streamableHttp` o `stdio` |
| `--start-server <value>` | `UNITY_MCP_START_SERVER` | No | Establece `true` o `false` para controlar el inicio automático del servidor MCP en el Editor de Unity (solo aplica al transporte `streamableHttp`) |
| `--unity <version>` | — | No | Versión específica del Editor de Unity a utilizar (por defecto, la versión de la configuración del proyecto; si no está disponible, la más alta instalada) |

Este comando inicia el Editor de Unity con las variables de entorno `UNITY_MCP_*` correspondientes para que el plugin las recoja automáticamente al arrancar. Las variables de entorno anulan los valores del archivo de configuración `UserSettings/AI-Game-Developer-Config.json` del proyecto en tiempo de ejecución.

**Ejemplo — conectar con autenticación y herramientas específicas:**

```bash
npx unity-mcp-cli connect \
  --path ./MyGame \
  --url http://my-server:8080 \
  --token my-secret-token \
  --auth required \
  --keep-connected \
  --tools gameobject-create,gameobject-find,script-execute
```

**Ejemplo — conectar con transporte stdio (servidor gestionado por el agente de IA):**

```bash
npx unity-mcp-cli connect \
  --path ./MyGame \
  --url http://localhost:8080 \
  --transport stdio \
  --start-server false
```

**Ejemplo — conectar con streamableHttp e inicio automático del servidor:**

```bash
npx unity-mcp-cli connect \
  --path ./MyGame \
  --url http://localhost:8080 \
  --transport streamableHttp \
  --start-server true \
  --keep-connected
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `create-project`

Crea un nuevo proyecto de Unity utilizando el Editor de Unity.

```bash
npx unity-mcp-cli create-project /path/to/new/project
```

| Opción | Requerido | Descripción |
|---|---|---|
| `[path]` | Sí | Ruta donde se creará el proyecto (posicional o `--path`) |
| `--unity <version>` | No | Versión del Editor de Unity a utilizar (por defecto, la más alta instalada) |

**Ejemplo — crear un proyecto con una versión específica del editor:**

```bash
npx unity-mcp-cli create-project ./MyGame --unity 2022.3.62f1
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-plugin`

Instala el plugin Unity-MCP en el archivo `Packages/manifest.json` de un proyecto de Unity.

```bash
npx unity-mcp-cli install-plugin ./MyGame
```

| Opción | Requerido | Descripción |
|---|---|---|
| `[path]` | Sí | Ruta al proyecto de Unity (posicional o `--path`) |
| `--plugin-version <version>` | No | Versión del plugin a instalar (por defecto, la última desde [OpenUPM](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)) |

Este comando:
1. Añade el **registro de ámbito de OpenUPM** con todos los ámbitos requeridos
2. Añade `com.ivanmurzak.unity.mcp` a `dependencies`
3. **Nunca degrada** — si ya hay instalada una versión superior, se conserva

**Ejemplo — instalar una versión específica del plugin:**

```bash
npx unity-mcp-cli install-plugin ./MyGame --plugin-version 0.52.0
```

> Después de ejecutar este comando, abre el proyecto en el Editor de Unity para completar la instalación del paquete.

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-unity`

Instala una versión del Editor de Unity mediante la CLI de Unity Hub.

```bash
npx unity-mcp-cli install-unity 6000.3.1f1
```

| Argumento / Opción | Requerido | Descripción |
|---|---|---|
| `[version]` | No | Versión del Editor de Unity a instalar (ej. `6000.3.1f1`) |
| `--path <path>` | No | Lee la versión requerida desde un proyecto existente |

Si no se proporciona ningún argumento ni opción, el comando instala la última versión estable desde la lista de lanzamientos de Unity Hub.

**Ejemplo — instalar la versión del editor que necesita un proyecto:**

```bash
npx unity-mcp-cli install-unity --path ./MyGame
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `open`

Abre un proyecto de Unity en el Editor de Unity.

```bash
npx unity-mcp-cli open ./MyGame
```

| Opción | Requerido | Descripción |
|---|---|---|
| `[path]` | Sí | Ruta al proyecto de Unity (posicional o `--path`) |
| `--unity <version>` | No | Versión específica del Editor de Unity a utilizar (por defecto, la versión de la configuración del proyecto; si no está disponible, la más alta instalada) |

El proceso del editor se lanza en modo desacoplado — la CLI regresa inmediatamente.

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `remove-plugin`

Elimina el plugin Unity-MCP del archivo `Packages/manifest.json` de un proyecto de Unity.

```bash
npx unity-mcp-cli remove-plugin ./MyGame
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

# Ejemplo de Automatización Completa

Configura un proyecto Unity MCP completo desde cero con un solo script:

```bash
# 1. Crear un nuevo proyecto de Unity
npx unity-mcp-cli create-project ./MyAIGame --unity 6000.3.1f1

# 2. Instalar el plugin Unity-MCP
npx unity-mcp-cli install-plugin ./MyAIGame

# 3. Activar todas las herramientas MCP
npx unity-mcp-cli configure ./MyAIGame --enable-all-tools

# 4. Abrir el proyecto con conexión MCP
npx unity-mcp-cli connect \
  --path ./MyAIGame \
  --url http://localhost:8080 \
  --keep-connected
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Cómo Funciona

### Puerto Determinista

La CLI genera un **puerto determinista** para cada proyecto de Unity basándose en la ruta de su directorio (hash SHA256 mapeado al rango de puertos 50000–59999). Esto coincide con la generación de puertos del plugin de Unity, garantizando que el servidor y el plugin acuerden automáticamente el mismo puerto sin necesidad de configuración manual.

### Instalación del Plugin

El comando `install-plugin` modifica `Packages/manifest.json` directamente:
- Añade el registro de ámbito de [OpenUPM](https://openupm.com/) (`package.openupm.com`)
- Registra todos los ámbitos requeridos (`com.ivanmurzak`, `extensions.unity`, `org.nuget.*`)
- Añade la dependencia `com.ivanmurzak.unity.mcp` con actualizaciones que respetan la versión (nunca degrada)

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
