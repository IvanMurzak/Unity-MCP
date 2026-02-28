<div align="center" width="100%">
  <h1>✨ Desarrollador de Juegos con IA — <i>Unity MCP</i></h1>

[![MCP](https://badge.mcpx.dev 'Servidor MCP')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'Paquete OpenUPM')](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Imagen Docker')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Editor compatible')](https://unity.com/releases/editor/archive)
[![Unity Runtime](https://img.shields.io/badge/Runtime-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Runtime compatible')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg 'Pruebas superadas')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Unirse-7289da?logo=discord&logoColor=white&labelColor=333A41 'Unirse')](https://discord.gg/cfbdMZX99G)
[![OpenUPM](https://img.shields.io/badge/dynamic/json?labelColor=333A41&label=Descargas&query=%24.downloads&suffix=%2Fmes&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.ivanmurzak.unity.mcp)](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Estrellas')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=Licencia&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

  <img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/promo/ai-developer-banner.jpg" alt="IA trabajando" title="Construcción de niveles" width="100%">

  <b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ja.md)</b>

</div>

`Unity MCP` es un asistente de desarrollo de juegos con IA **para Editor y Runtime**. Conecta **Claude**, **Cursor** y **Windsurf** a Unity a través de MCP. Automatiza flujos de trabajo, genera código y **habilita IA dentro de tus juegos**.

A diferencia de otras herramientas, este plugin funciona **dentro de tu juego compilado**, permitiendo la depuración en tiempo real con IA e interacción jugador-IA.

> **[💬 Únete a nuestro servidor de Discord](https://discord.gg/cfbdMZX99G)** - ¡Haz preguntas, muestra tu trabajo y conéctate con otros desarrolladores!

## ![Desarrollador de Juegos con IA — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-features.es.svg?raw=true)

- ✔️ **Agentes de IA** - Usa los mejores agentes de **Anthropic**, **OpenAI**, **Microsoft** u cualquier otro proveedor sin límites
- ✔️ **HERRAMIENTAS** - Una amplia gama de [Herramientas MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md) predeterminadas para operar en el Editor de Unity
- ✔️ **SKILLS** - Genera automáticamente habilidades para cada herramienta MCP
- ✔️ **Código y Pruebas** - Pide a la IA que escriba código y ejecute pruebas
- ✔️ **Runtime (en el juego)** - Usa LLMs directamente dentro de tu juego compilado para comportamiento dinámico de NPCs o depuración
- ✔️ **Soporte de depuración** - Pide a la IA que obtenga registros y corrija errores
- ✔️ **Conversación natural** - Chatea con la IA como lo harías con una persona
- ✔️ **Despliegue flexible** - Funciona localmente (stdio) y remotamente (http) mediante configuración
- ✔️ **Extensible** - Crea [Herramientas MCP personalizadas en el código de tu proyecto](#añadir-herramienta-mcp-personalizada)

[![DESCARGAR INSTALADOR](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/button/button_download.svg?raw=true)](https://github.com/IvanMurzak/Unity-MCP/releases/download/0.51.2/AI-Game-Dev-Installer.unitypackage)

![Ventanas del Desarrollador de Juegos con IA](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/editor/ai-game-developer-windows.png?raw=true)

![Desarrollador de Juegos con IA — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Inicio Rápido

Comienza en tres pasos:

1. **[Instala el Plugin](#paso-1-instalar-el-plugin-unity-mcp)** — descarga el instalador `.unitypackage` o ejecuta `openupm add com.ivanmurzak.unity.mcp`
2. **[Elige un cliente MCP](#paso-2-instalar-cliente-mcp)** — Claude Code, Claude Desktop, GitHub Copilot, Cursor u cualquier otro
3. **[Configura el cliente](#paso-3-configurar-cliente-mcp)** — abre `Window/AI Game Developer - MCP` en Unity y haz clic en **Configure**

Eso es todo. Pídele a tu IA *"Crea 3 cubos en un círculo con radio 2"* y obsérvalo suceder. ✨

---

# Referencia de Herramientas

El plugin viene con más de 50 herramientas integradas en tres categorías. Todas las herramientas están disponibles inmediatamente después de la instalación, sin configuración adicional. Consulta [docs/default-mcp-tools.md](docs/default-mcp-tools.md) para la referencia completa con descripciones detalladas.

<details>
  <summary>Proyecto y Assets</summary>

- `assets-copy` - Copia el asset en la ruta y lo almacena en newPath
- `assets-create-folder` - Crea una nueva carpeta en la carpeta padre especificada
- `assets-delete` - Elimina los assets en las rutas del proyecto
- `assets-find` - Busca en la base de datos de assets usando la cadena de filtro de búsqueda
- `assets-find-built-in` - Busca en los assets integrados del Editor de Unity
- `assets-get-data` - Obtiene datos del asset desde el archivo de asset, incluyendo todos los campos y propiedades serializables
- `assets-material-create` - Crea un nuevo asset de material con parámetros predeterminados
- `assets-modify` - Modifica el archivo de asset en el proyecto
- `assets-move` - Mueve los assets en las rutas del proyecto (también se usa para renombrar)
- `assets-prefab-close` - Cierra el prefab actualmente abierto
- `assets-prefab-create` - Crea un prefab desde un GameObject en la escena activa actual
- `assets-prefab-instantiate` - Instancia un prefab en la escena activa actual
- `assets-prefab-open` - Abre el modo de edición de prefab para un GameObject específico
- `assets-prefab-save` - Guarda un prefab en modo de edición de prefab
- `assets-refresh` - Actualiza la AssetDatabase
- `assets-shader-list-all` - Lista todos los shaders disponibles en los assets del proyecto y paquetes
- `package-add` - Instala un paquete desde el registro del Administrador de Paquetes de Unity, URL de Git o ruta local
- `package-list` - Lista todos los paquetes instalados en el proyecto de Unity (paquetes UPM)
- `package-remove` - Elimina (desinstala) un paquete del proyecto de Unity
- `package-search` - Busca paquetes tanto en el registro del Administrador de Paquetes de Unity como en los paquetes instalados

</details>

<details>
  <summary>Escena y Jerarquía</summary>

- `gameobject-component-add` - Agrega un Componente a un GameObject
- `gameobject-component-destroy` - Destruye uno o varios componentes del GameObject objetivo
- `gameobject-component-get` - Obtiene información detallada sobre un Componente específico de un GameObject
- `gameobject-component-list-all` - Lista los nombres de clases C# extendidas desde UnityEngine.Component
- `gameobject-component-modify` - Modifica un Componente específico de un GameObject
- `gameobject-create` - Crea un nuevo GameObject en el Prefab abierto o en una Escena
- `gameobject-destroy` - Destruye un GameObject y todos los GameObjects anidados de forma recursiva
- `gameobject-duplicate` - Duplica GameObjects en el Prefab abierto o en una Escena
- `gameobject-find` - Encuentra un GameObject específico por la información proporcionada
- `gameobject-modify` - Modifica GameObjects y/o campos y propiedades de componentes adjuntos
- `gameobject-set-parent` - Establece el GameObject padre para una lista de GameObjects
- `object-get-data` - Obtiene datos del Objeto Unity especificado
- `object-modify` - Modifica el Objeto Unity especificado
- `scene-create` - Crea una nueva escena en los assets del proyecto
- `scene-get-data` - Recupera la lista de GameObjects raíz en la escena especificada
- `scene-list-opened` - Devuelve la lista de escenas actualmente abiertas en el Editor de Unity
- `scene-open` - Abre una escena desde el archivo de asset del proyecto
- `scene-save` - Guarda la escena abierta en el archivo de asset
- `scene-set-active` - Establece la escena abierta especificada como la escena activa
- `scene-unload` - Descarga una escena de las escenas abiertas en el Editor de Unity
- `screenshot-camera` - Captura una captura de pantalla desde una cámara y la devuelve como imagen
- `screenshot-game-view` - Captura una captura de pantalla desde la Vista de Juego del Editor de Unity
- `screenshot-scene-view` - Captura una captura de pantalla desde la Vista de Escena del Editor de Unity

</details>

<details>
  <summary>Scripting y Editor</summary>

- `console-get-logs` - Recupera los registros del Editor de Unity con opciones de filtrado
- `editor-application-get-state` - Devuelve información sobre el estado de la aplicación del Editor de Unity (modo de juego, pausado, compilación)
- `editor-application-set-state` - Controla el estado de la aplicación del Editor de Unity (iniciar/detener/pausar el modo de juego)
- `editor-selection-get` - Obtiene información sobre la Selección actual en el Editor de Unity
- `editor-selection-set` - Establece la Selección actual en el Editor de Unity
- `reflection-method-call` - Llama a cualquier método C# con parámetros de entrada y devuelve resultados
- `reflection-method-find` - Encuentra un método en el proyecto usando Reflexión C# (incluso métodos privados)
- `script-delete` - Elimina el/los archivo(s) de script
- `script-execute` - Compila y ejecuta código C# dinámicamente usando Roslyn
- `script-read` - Lee el contenido de un archivo de script
- `script-update-or-create` - Actualiza o crea un archivo de script con el código C# proporcionado
- `tests-run` - Ejecuta pruebas de Unity (EditMode/PlayMode) con filtrado y resultados detallados

</details>

#### Herramientas adicionales

Instala extensiones cuando necesites más herramientas o [crea las tuyas propias](#añadir-herramienta-mcp-personalizada).

- [Animation](https://github.com/IvanMurzak/Unity-AI-Animation/)
- [ParticleSystem](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/)
- [ProBuilder](https://github.com/IvanMurzak/Unity-AI-ProBuilder/)

![Desarrollador de Juegos con IA — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Contenidos

- [Inicio Rápido](#inicio-rápido)
- [Referencia de Herramientas](#referencia-de-herramientas)
      - [Herramientas adicionales](#herramientas-adicionales)
- [Contenidos](#contenidos)
  - [Más Documentación](#más-documentación)
- [Instalación](#instalación)
  - [Paso 1: Instalar el Plugin `Unity MCP`](#paso-1-instalar-el-plugin-unity-mcp)
    - [Opción 1 - Instalador](#opción-1---instalador)
    - [Opción 2 - OpenUPM-CLI](#opción-2---openupm-cli)
  - [Paso 2: Instalar `Cliente MCP`](#paso-2-instalar-cliente-mcp)
  - [Paso 3: Configurar `Cliente MCP`](#paso-3-configurar-cliente-mcp)
    - [Configuración automática](#configuración-automática)
    - [Configuración manual](#configuración-manual)
      - [Configuración por línea de comandos](#configuración-por-línea-de-comandos)
- [Ejemplos de Flujo de Trabajo con IA: Claude y Gemini](#ejemplos-de-flujo-de-trabajo-con-ia-claude-y-gemini)
  - [Características Avanzadas para LLM](#características-avanzadas-para-llm)
    - [Capacidades Principales](#capacidades-principales)
    - [Características basadas en Reflexión](#características-basadas-en-reflexión)
- [Personalizar MCP](#personalizar-mcp)
  - [Añadir Herramienta MCP personalizada](#añadir-herramienta-mcp-personalizada)
  - [Añadir Prompt MCP personalizado](#añadir-prompt-mcp-personalizado)
- [Uso en Runtime (dentro del juego)](#uso-en-runtime-dentro-del-juego)
  - [Ejemplo: Bot de ajedrez con IA](#ejemplo-bot-de-ajedrez-con-ia)
  - [¿Por qué se necesita el uso en runtime?](#por-qué-se-necesita-el-uso-en-runtime)
- [Configuración del `Servidor MCP` de Unity](#configuración-del-servidor-mcp-de-unity)
  - [Variables](#variables)
  - [Docker 📦](#docker-)
    - [Transporte `streamableHttp`](#transporte-streamablehttp)
    - [Transporte `stdio`](#transporte-stdio)
    - [`port` personalizado](#port-personalizado)
  - [Ejecutable binario](#ejecutable-binario)
- [Cómo funciona la Arquitectura de Unity MCP](#cómo-funciona-la-arquitectura-de-unity-mcp)
  - [Qué es `MCP`](#qué-es-mcp)
  - [Qué es un `Cliente MCP`](#qué-es-un-cliente-mcp)
  - [Qué es un `Servidor MCP`](#qué-es-un-servidor-mcp)
  - [Qué es una `Herramienta MCP`](#qué-es-una-herramienta-mcp)
    - [Cuándo usar una `Herramienta MCP`](#cuándo-usar-una-herramienta-mcp)
  - [Qué es un `Recurso MCP`](#qué-es-un-recurso-mcp)
    - [Cuándo usar un `Recurso MCP`](#cuándo-usar-un-recurso-mcp)
  - [Qué es un `Prompt MCP`](#qué-es-un-prompt-mcp)
    - [Cuándo usar un `Prompt MCP`](#cuándo-usar-un-prompt-mcp)
- [Contribución 💙💛](#contribución-)

## Más Documentación

| Documento | Descripción |
| -------- | ----------- |
| [Herramientas MCP Predeterminadas](docs/default-mcp-tools.md) | Referencia completa de todas las herramientas integradas con descripciones |
| [Configuración del Servidor MCP](docs/mcp-server.md) | Configuración del servidor, variables de entorno, alojamiento remoto |
| [Despliegue con Docker](docs/DOCKER_DEPLOYMENT.md) | Guía paso a paso para despliegue con Docker |
| [Guía de Desarrollo](docs/dev/Development.md) | Arquitectura, estilo de código, CI/CD — para colaboradores |
| [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) | Primeros pasos, tutoriales, referencia de API, preguntas frecuentes |

![Desarrollador de Juegos con IA — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Instalación

## Paso 1: Instalar el Plugin `Unity MCP`

<details>
  <summary><b>⚠️ Requisitos (clic para expandir)</b></summary>

> [!IMPORTANT]
> **La ruta del proyecto no puede contener espacios**
>
> - ✅ `C:/MyProjects/Project`
> - ❌ `C:/My Projects/Project`

</details>

### Opción 1 - Instalador

- **[⬇️ Descargar Instalador](https://github.com/IvanMurzak/Unity-MCP/releases/download/0.51.2/AI-Game-Dev-Installer.unitypackage)**
- **📂 Importar el instalador en el proyecto de Unity**
  > - Puedes hacer doble clic en el archivo - Unity lo abrirá automáticamente
  > - O BIEN: Abre el Editor de Unity primero, luego haz clic en `Assets/Import Package/Custom Package` y elige el archivo

### Opción 2 - OpenUPM-CLI

- [⬇️ Instalar OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- 📟 Abre la línea de comandos en la carpeta de tu proyecto de Unity

```bash
openupm add com.ivanmurzak.unity.mcp
```

## Paso 2: Instalar `Cliente MCP`

Elige un único `Cliente MCP` que prefieras; no necesitas instalarlos todos. Esta será tu ventana principal de chat para comunicarte con el LLM.

- [Claude Code](https://github.com/anthropics/claude-code) (muy recomendado)
- [Claude Desktop](https://claude.ai/download)
- [GitHub Copilot en VS Code](https://code.visualstudio.com/docs/copilot/overview)
- [Antigravity](https://antigravity.google/)
- [Cursor](https://www.cursor.com/)
- [Windsurf](https://windsurf.com)
- Cualquier otro compatible

> El protocolo MCP es bastante universal, por lo que puedes usar cualquier cliente MCP que prefieras y funcionará tan bien como cualquier otro. El único requisito importante es que el cliente MCP debe admitir actualizaciones dinámicas de Herramientas MCP.

## Paso 3: Configurar `Cliente MCP`

### Configuración automática

- Abre tu proyecto de Unity
- Abre `Window/AI Game Developer (Unity-MCP)`
- Haz clic en `Configure` en tu cliente MCP

![Unity_AI](https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/ai-connector-window.gif)

> Si tu cliente MCP no está en la lista, usa el JSON sin procesar que se muestra en la ventana para inyectarlo en tu cliente MCP. Lee las instrucciones de tu cliente MCP específico sobre cómo hacer esto.

### Configuración manual

Si la configuración automática no funciona por alguna razón, usa el JSON de la ventana `AI Game Developer (Unity-MCP)` para configurar cualquier `Cliente MCP` manualmente.

#### Configuración por línea de comandos

<details>
  <summary><b>Crear <code>command</code></b></summary>

**1. Elige tu `<command>` para tu entorno**

| Plataforma          | `<command>`                                                                                                 |
| ------------------- | ----------------------------------------------------------------------------------------------------------- |
| Windows x64         | `"<unityProjectPath>/Library/mcp-server/win-x64/unity-mcp-server.exe" port=<port> client-transport=stdio`   |
| Windows x86         | `"<unityProjectPath>/Library/mcp-server/win-x86/unity-mcp-server.exe" port=<port> client-transport=stdio`   |
| Windows arm64       | `"<unityProjectPath>/Library/mcp-server/win-arm64/unity-mcp-server.exe" port=<port> client-transport=stdio` |
| MacOS Apple-Silicon | `"<unityProjectPath>/Library/mcp-server/osx-arm64/unity-mcp-server" port=<port> client-transport=stdio`     |
| MacOS Apple-Intel   | `"<unityProjectPath>/Library/mcp-server/osx-x64/unity-mcp-server" port=<port> client-transport=stdio`       |
| Linux x64           | `"<unityProjectPath>/Library/mcp-server/linux-x64/unity-mcp-server" port=<port> client-transport=stdio`     |
| Linux arm64         | `"<unityProjectPath>/Library/mcp-server/linux-arm64/unity-mcp-server" port=<port> client-transport=stdio`   |

**2. Reemplaza `<unityProjectPath>` con la ruta completa al proyecto de Unity**

**3. Reemplaza `<port>` con tu puerto de la configuración de AI Game Developer**

**4. Agrega el servidor MCP usando la línea de comandos**

</details>

<details>
  <summary><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/mcp-clients/gemini-64.png?raw=true" width="16" height="16" alt="Gemini CLI"> Gemini CLI</summary>

  ```bash
  gemini mcp add ai-game-developer <command>
  ```

  > Reemplaza `<command>` con el comando de la tabla anterior
</details>

<details>
  <summary><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/mcp-clients/claude-64.png?raw=true" width="16" height="16" alt="Claude Code CLI"> Claude Code CLI</summary>

  ```bash
  claude mcp add ai-game-developer <command>
  ```

  > Reemplaza `<command>` con el comando de la tabla anterior
</details>

<details>
  <summary><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/mcp-clients/github-copilot-64.png?raw=true" width="16" height="16" alt="GitHub Copilot CLI"> GitHub Copilot CLI</summary>

  ```bash
  copilot
  ```

  ```bash
  /mcp add
  ```

  Nombre del servidor: `ai-game-developer`
  Tipo de servidor: `local`
  Comando: `<command>`
  > Reemplaza `<command>` con el comando de la tabla anterior
</details>

![Desarrollador de Juegos con IA — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Ejemplos de Flujo de Trabajo con IA: Claude y Gemini

Comunícate con la IA (LLM) en tu `Cliente MCP`. Pídele que haga lo que quieras. Cuanto mejor describas tu tarea o idea, mejor realizará el trabajo.

Algunos `Clientes MCP` te permiten elegir diferentes modelos de LLM. Presta atención a esta función, ya que algunos modelos pueden funcionar mucho mejor que otros.

**Comandos de ejemplo:**

```text
Explica la jerarquía de mi escena
```

```text
Crea 3 cubos en un círculo con radio 2
```

```text
Crea un material dorado metálico y adjúntalo a un gameObject esfera
```

> Asegúrate de que el modo `Agent` esté activado en tu cliente MCP

## Características Avanzadas para LLM

Unity MCP proporciona herramientas avanzadas que permiten al LLM trabajar más rápido y efectivamente, evitando errores y autocorrigiéndose cuando ocurren. Todo está diseñado para alcanzar tus objetivos de manera eficiente.

### Capacidades Principales

- ✔️ **Herramientas listas para agentes** - Encuentra todo lo que necesitas en 1-2 pasos
- ✔️ **Compilación instantánea** - Compilación y ejecución de código C# usando `Roslyn` para iteración más rápida
- ✔️ **Acceso completo a assets** - Acceso de lectura/escritura a assets y scripts C#
- ✔️ **Retroalimentación inteligente** - Retroalimentación positiva y negativa bien descrita para la comprensión adecuada de los problemas

### Características basadas en Reflexión

- ✔️ **Referencias a objetos** - Proporciona referencias a objetos existentes para código C# instantáneo
- ✔️ **Acceso a datos del proyecto** - Obtén acceso completo a todos los datos del proyecto en un formato legible
- ✔️ **Modificaciones granulares** - Rellena y modifica cualquier fragmento de datos en el proyecto
- ✔️ **Descubrimiento de métodos** - Encuentra cualquier método en todo el código base, incluyendo archivos DLL compilados
- ✔️ **Ejecución de métodos** - Llama a cualquier método en todo el código base
- ✔️ **Parámetros avanzados** - Proporciona cualquier propiedad para llamadas a métodos, incluso referencias a objetos existentes en memoria
- ✔️ **API de Unity en vivo** - API de Unity disponible instantáneamente — incluso cuando Unity cambia, obtienes la API actualizada
- ✔️ **Autodocumentada** - Accede a descripciones legibles por humanos de cualquier `clase`, `método` o `propiedad` a través de atributos `Description`

![Desarrollador de Juegos con IA — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Personalizar MCP

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** admite el desarrollo personalizado de `Herramienta MCP`, `Recurso MCP` y `Prompt MCP` por parte de los propietarios del proyecto. El servidor MCP toma datos del `Plugin Unity MCP` y los expone a un cliente. Cualquier persona en la cadena de comunicación MCP recibirá información sobre las nuevas características MCP, que el LLM puede decidir utilizar en algún momento.

## Añadir Herramienta MCP personalizada

Para añadir una `Herramienta MCP` personalizada, necesitas:

1. Una clase con el atributo `McpPluginToolType`
2. Un método en la clase con el atributo `McpPluginTool`
3. *Opcional:* Agrega un atributo `Description` a cada argumento del método para ayudar al LLM a entenderlo
4. *Opcional:* Usa propiedades `string? optional = null` con `?` y valores predeterminados para marcarlas como `opcionales` para el LLM

> Ten en cuenta que la línea `MainThread.Instance.Run(() =>` te permite ejecutar código en el hilo principal, lo cual es necesario para interactuar con la API de Unity. Si no necesitas esto y ejecutar la herramienta en un hilo en segundo plano es aceptable, evita usar el hilo principal por razones de eficiencia.

```csharp
[McpPluginToolType]
public class Tool_GameObject
{
    [McpPluginTool
    (
        "MyCustomTask",
        Title = "Create a new GameObject"
    )]
    [Description("Explica aquí al LLM qué es esto, cuándo debe ser llamado.")]
    public string CustomTask
    (
        [Description("Explica al LLM qué es esto.")]
        string inputData
    )
    {
        // hacer cualquier cosa en el hilo en segundo plano

        return MainThread.Instance.Run(() =>
        {
            // hacer algo en el hilo principal si es necesario

            return $"[Success] Operation completed.";
        });
    }
}
```

## Añadir Prompt MCP personalizado

Un `Prompt MCP` te permite inyectar prompts personalizados en la conversación con el LLM. Admite dos roles de emisor: Usuario y Asistente. Esta es una forma rápida de instruir al LLM para que realice tareas específicas. Puedes generar prompts usando datos personalizados, proporcionando listas o cualquier otra información relevante.

```csharp
[McpPluginPromptType]
public static class Prompt_ScriptingCode
{
    [McpPluginPrompt(Name = "add-event-system", Role = Role.User)]
    [Description("Implementa un sistema de comunicación basado en UnityEvent entre GameObjects.")]
    public string AddEventSystem()
    {
        return "Create event system using UnityEvents, UnityActions, or custom event delegates for decoupled communication between game systems and components.";
    }
}
```

![Desarrollador de Juegos con IA — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Uso en Runtime (dentro del juego)

Usa **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** en tu juego/aplicación. Utiliza Herramientas, Recursos o Prompts. Por defecto no hay herramientas, necesitarías implementar las tuyas propias.

```csharp
// Construir el plugin MCP
var mcpPlugin = UnityMcpPluginRuntime.Initialize(builder =>
    {
        builder.WithConfig(config =>
        {
            config.Host = "http://localhost:8080";
            config.Token = "your-token";
        });
        // Registra automáticamente todas las herramientas del ensamblado actual
        builder.WithToolsFromAssembly(Assembly.GetExecutingAssembly());
    })
    .Build();

await mcpPlugin.Connect(); // Inicia la conexión activa con reintentos al Unity-MCP-Server

await mcpPlugin.Disconnect(); // Detiene la conexión activa y cierra la conexión existente
```

## Ejemplo: Bot de ajedrez con IA

Hay un juego de ajedrez clásico. Vamos a delegar la lógica del bot al LLM. El bot debe hacer su turno siguiendo las reglas del juego.

```csharp
[McpPluginToolType]
public static class ChessGameAI
{
    [McpPluginTool("chess-do-turn", Title = "Do the turn")]
    [Description("Realiza el turno en el juego de ajedrez. Devuelve true si el turno fue aceptado, false en caso contrario.")]
    public static Task<bool> DoTurn(int figureId, Vector2Int position)
    {
        return MainThread.Instance.RunAsync(() => ChessGameController.Instance.DoTurn(figureId, position));
    }

    [McpPluginTool("chess-get-board", Title = "Get the board")]
    [Description("Obtiene el estado actual del tablero de ajedrez.")]
    public static Task<BoardData> GetBoard()
    {
        return MainThread.Instance.RunAsync(() => ChessGameController.Instance.GetBoardData());
    }
}
```

## ¿Por qué se necesita el uso en runtime?

Hay muchos casos de uso; imagina que estás trabajando en un juego de ajedrez con bot. Puedes delegar la toma de decisiones del bot al LLM escribiendo unas pocas líneas de código.

![Desarrollador de Juegos con IA — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Configuración del `Servidor MCP` de Unity

El Servidor de **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** admite muchas opciones de lanzamiento diferentes y despliegue con Docker. Se admiten ambos protocolos de transporte: `streamableHttp` y `stdio`. Si necesitas personalizar o desplegar el Servidor Unity MCP en la nube, esta sección es para ti. [Leer más...](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/mcp-server.md)

## Variables

Sin importar qué opción de lanzamiento elijas, todas admiten configuración personalizada usando tanto Variables de Entorno como Argumentos de Línea de Comandos. Funcionará con valores predeterminados si solo necesitas lanzarlo; no pierdas el tiempo con las variables. Solo asegúrate de que el Plugin de Unity también tenga valores predeterminados, especialmente el `--port` — deben ser iguales.

| Variable de Entorno          | Args de Línea de Comandos | Descripción                                                                  |
| ---------------------------- | ------------------------- | ---------------------------------------------------------------------------- |
| `MCP_PLUGIN_PORT`            | `--port`                  | Puerto de conexión **Cliente** -> **Servidor** <- **Plugin** (predeterminado: 8080) |
| `MCP_PLUGIN_CLIENT_TIMEOUT`   | `--plugin-timeout`        | Tiempo de espera de conexión **Plugin** -> **Servidor** (ms) (predeterminado: 10000) |
| `MCP_PLUGIN_CLIENT_TRANSPORT` | `--client-transport`      | Tipo de transporte **Cliente** -> **Servidor**: `stdio` o `streamableHttp` (predeterminado: `streamableHttp`) |

> Los args de línea de comandos también admiten la opción con un prefijo `-` simple (`-port`) y una opción sin prefijo (`port`).

> **Elegir un transporte:** Usa `stdio` cuando el cliente MCP lanza el binario del servidor directamente (uso local — esta es la configuración más común). Usa `streamableHttp` cuando ejecutes el servidor como un proceso independiente o en Docker/nube, y te conectes a través de HTTP.

## Docker 📦

[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Imagen Docker')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)

Asegúrate de que Docker esté instalado. Y por favor verifica que Docker Desktop esté iniciado si estás en Windows.

[Lee las instrucciones avanzadas de configuración de Docker](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/DOCKER_DEPLOYMENT.md).

### Transporte `streamableHttp`

```bash
docker run -p 8080:8080 ivanmurzakdev/unity-mcp-server
```

<details>
  <summary>Configuración del <code>Cliente MCP</code>:</summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "url": "http://localhost:8080"
    }
  }
}
```

> Reemplaza `url` con tu endpoint real si está alojado en la nube.

</details>

### Transporte `stdio`

Para usar esta variante, el `Cliente MCP` debe lanzar el `Servidor MCP` en Docker. Esto es posible mediante la configuración modificada del `Cliente MCP`.

```bash
docker run -t -e MCP_PLUGIN_CLIENT_TRANSPORT=stdio -p 8080:8080 ivanmurzakdev/unity-mcp-server
```

<details>
  <summary>Configuración del <code>Cliente MCP</code>:</summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "command": "docker",
      "args": [
        "run",
        "-t",
        "-e",
        "MCP_PLUGIN_CLIENT_TRANSPORT=stdio",
        "-p",
        "8080:8080",
        "ivanmurzakdev/unity-mcp-server"
      ]
    }
  }
}
```

</details>

### `port` personalizado

```bash
docker run -e MCP_PLUGIN_PORT=123 -p 123:123 ivanmurzakdev/unity-mcp-server
```

<details>
  <summary>Configuración del <code>Cliente MCP</code>:</summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "url": "http://localhost:123"
    }
  }
}
```

> Reemplaza `url` con tu endpoint real si está alojado en la nube
</details>

## Ejecutable binario

Puedes lanzar el `Servidor MCP` de Unity directamente desde un archivo binario. Necesitarás un binario compilado específicamente para la arquitectura de tu CPU. Consulta la [página de Releases de GitHub](https://github.com/IvanMurzak/Unity-MCP/releases) — contiene binarios precompilados para todas las arquitecturas de CPU.

```bash
./unity-mcp-server --port 8080 --plugin-timeout 10000 --client-transport stdio
```

<details>
  <summary>Configuración del <code>Cliente MCP</code>:</summary>

> Reemplaza `<project>` con la ruta de tu proyecto de Unity.

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "command": "<project>/Library/mcp-server/win-x64/unity-mcp-server.exe",
      "args": [
        "--port=8080",
        "--plugin-timeout=10000",
        "--client-transport=stdio"
      ]
    }
  }
}
```

</details>

![Desarrollador de Juegos con IA — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Cómo funciona la Arquitectura de Unity MCP

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** sirve como puente entre los LLMs y Unity. Expone y explica las herramientas de Unity al LLM, que luego comprende la interfaz y utiliza las herramientas según las solicitudes del usuario.

Conecta **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** a clientes LLM como [Claude](https://claude.ai/download) o [Cursor](https://www.cursor.com/) usando la ventana integrada `AI Connector`. También se admiten clientes personalizados.

El sistema es altamente extensible — puedes definir `Herramientas MCP`, `Recursos MCP` o `Prompts MCP` personalizados directamente en el código de tu proyecto de Unity, exponiendo nuevas capacidades a clientes de IA o automatización. Esto hace de Unity MCP una base flexible para construir flujos de trabajo avanzados, prototipado rápido e integrar funciones impulsadas por IA en tu proceso de desarrollo.

## Qué es `MCP`

MCP - Protocolo de Contexto de Modelo. En pocas palabras, es el `USB Type-C` para la IA, específicamente para LLM (Modelo de Lenguaje Grande). Enseña al LLM cómo usar características externas, como Unity Engine en este caso, o incluso tu método C# personalizado en tu código. [Documentación oficial](https://modelcontextprotocol.io/).

## Qué es un `Cliente MCP`

Es una aplicación con una ventana de chat. Puede tener agentes inteligentes para operar mejor, puede tener Herramientas MCP avanzadas integradas. En general, un buen Cliente MCP es el 50% del éxito de la IA para ejecutar una tarea. Por eso es muy importante elegir el mejor para usar.

## Qué es un `Servidor MCP`

Es un puente entre el `Cliente MCP` y "algo más", en este caso particular es Unity Engine. Este proyecto incluye el `Servidor MCP`.

## Qué es una `Herramienta MCP`

Una `Herramienta MCP` es una función o método que el LLM puede llamar para interactuar con Unity. Estas herramientas actúan como el puente entre las solicitudes en lenguaje natural y las operaciones reales de Unity. Cuando le pides a la IA que "cree un cubo" o "cambie el color del material", usa Herramientas MCP para ejecutar estas acciones.

**Características clave:**

- **Funciones ejecutables** que realizan operaciones específicas
- **Parámetros con tipos** con descripciones para ayudar al LLM a entender qué datos proporcionar
- **Valores de retorno** que dan retroalimentación sobre el éxito o error de la operación
- **Conscientes del hilo** - pueden ejecutarse en el hilo principal para llamadas a la API de Unity o en un hilo en segundo plano para procesamiento pesado

### Cuándo usar una `Herramienta MCP`

- **Automatizar tareas repetitivas** - Crea herramientas para operaciones comunes que realizas frecuentemente
- **Operaciones complejas** - Agrupa múltiples llamadas a la API de Unity en una sola herramienta fácil de usar
- **Flujos de trabajo específicos del proyecto** - Construye herramientas que comprenden la estructura y convenciones específicas de tu proyecto
- **Tareas propensas a errores** - Crea herramientas que incluyen validación y manejo de errores
- **Lógica de juego personalizada** - Expone los sistemas de tu juego a la IA para la creación dinámica de contenido

**Ejemplos:**

- Crear y configurar GameObjects con componentes específicos
- Procesamiento por lotes de assets (texturas, materiales, prefabs)
- Configurar iluminación y efectos de postprocesamiento
- Generar geometría de nivel o colocar objetos proceduralmente
- Configurar ajustes de física o capas de colisión

## Qué es un `Recurso MCP`

Un `Recurso MCP` proporciona acceso de solo lectura a datos dentro de tu proyecto de Unity. A diferencia de las Herramientas MCP que realizan acciones, los Recursos permiten al LLM inspeccionar y entender el estado actual, los assets y la configuración de tu proyecto. Piensa en ellos como "sensores" que dan a la IA contexto sobre tu proyecto.

**Características clave:**

- **Acceso de solo lectura** a datos del proyecto y objetos de Unity
- **Información estructurada** presentada en un formato que el LLM puede entender
- **Datos en tiempo real** que reflejan el estado actual de tu proyecto
- **Conciencia contextual** que ayuda a la IA a tomar decisiones informadas

### Cuándo usar un `Recurso MCP`

- **Análisis del proyecto** - Permite a la IA entender la estructura, los assets y la organización de tu proyecto
- **Asistencia en depuración** - Proporciona información del estado actual para la resolución de problemas
- **Sugerencias inteligentes** - Da contexto a la IA para hacer mejores recomendaciones
- **Generación de documentación** - Crea automáticamente documentación basada en el estado del proyecto
- **Gestión de assets** - Ayuda a la IA a entender qué assets están disponibles y sus propiedades

**Ejemplos:**

- Exponer la jerarquía de escena y las propiedades de GameObject
- Listar materiales, texturas disponibles y su configuración
- Mostrar dependencias de scripts y relaciones de componentes
- Visualizar la configuración de iluminación y el pipeline de renderizado
- Proporcionar información sobre fuentes de audio, animaciones y sistemas de partículas

## Qué es un `Prompt MCP`

Un `Prompt MCP` te permite inyectar prompts predefinidos en la conversación con el LLM. Son plantillas inteligentes que pueden proporcionar contexto, instrucciones o conocimiento para guiar el comportamiento de la IA. Los prompts pueden ser texto estático o generados dinámicamente basándose en el estado actual de tu proyecto.

**Características clave:**

- **Guía contextual** que influye en cómo responde la IA
- **Basados en roles** - pueden simular diferentes personas (solicitudes de Usuario o conocimiento del Asistente)
- **Contenido dinámico** - puede incluir datos del proyecto en tiempo real
- **Plantillas reutilizables** para escenarios y flujos de trabajo comunes

### Cuándo usar un `Prompt MCP`

- **Proporcionar conocimiento de dominio** - Comparte mejores prácticas y estándares de codificación específicos de tu proyecto
- **Establecer convenciones de código** - Define convenciones de nomenclatura, patrones de arquitectura y estilo de código
- **Dar contexto sobre la estructura del proyecto** - Explica cómo está organizado tu proyecto y por qué
- **Compartir instrucciones de flujo de trabajo** - Proporciona procedimientos paso a paso para tareas comunes
- **Inyectar conocimiento especializado** - Añade información sobre características específicas de Unity, assets de terceros o sistemas personalizados

**Ejemplos:**

- "Usar siempre PascalCase para métodos públicos y camelCase para campos privados"
- "Este proyecto usa un sistema de eventos personalizado ubicado en Scripts/Events/"
- "Al crear elementos de UI, agrégalos siempre al Canvas en Scene/UI/MainCanvas"
- "El rendimiento es crítico - preferir object pooling para objetos instanciados frecuentemente"
- "Este proyecto sigue los principios SOLID - explica cualquier decisión de arquitectura"

![Desarrollador de Juegos con IA — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Contribución 💙💛

Las contribuciones son muy apreciadas. ¡Trae tus ideas y hagamos el desarrollo de juegos más simple que nunca! ¿Tienes una idea para una nueva `Herramienta MCP` o función, o encontraste un error y sabes cómo corregirlo?

**¡Por favor dale una estrella 🌟 a este proyecto si lo encuentras útil!**

1. 👉 [Leer la documentación de Desarrollo](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.md)
2. 👉 [Hacer un Fork del proyecto](https://github.com/IvanMurzak/Unity-MCP/fork)
3. Clona el fork y abre la carpeta `./Unity-MCP-Plugin` en Unity
4. Implementa cosas nuevas en el proyecto, haz commit, y empújalas a GitHub
5. Crea un Pull Request apuntando al repositorio original [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP/compare), rama `main`.

![Desarrollador de Juegos con IA — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)
