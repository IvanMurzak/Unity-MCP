<div align="center" width="100%">
  <h1>✨ Desarrollador de Juegos IA — <i>Unity MCP</i></h1>

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![Unity Runtime](https://img.shields.io/badge/Runtime-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Runtime supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![OpenUPM](https://img.shields.io/badge/dynamic/json?labelColor=333A41&label=Downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.ivanmurzak.unity.mcp)](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

  <img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/promo/ai-developer-banner.jpg" alt="AI work" title="Level building" width="100%">

  <b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ja.md)</b>

</div>

`Unity MCP` es un asistente de desarrollo de juegos impulsado por IA **para Editor y Runtime**. Conecta **Claude**, **Cursor** y **Windsurf** a Unity vía MCP. Automatiza flujos de trabajo, genera código y **habilita IA dentro de tus juegos**.

A diferencia de otras herramientas, este plugin funciona **dentro de tu juego compilado**, permitiendo depuración IA en tiempo real e interacción jugador-IA.

> **[💬 Únete a nuestro servidor de Discord](https://discord.gg/cfbdMZX99G)** - ¡Haz preguntas, muestra tu trabajo y conéctate con otros desarrolladores!

## Características

- ✔️ **Agentes IA** - Usa los mejores agentes de **Anthropic**, **OpenAI**, **Microsoft** o cualquier otro proveedor sin límites
- ✔️ **HERRAMIENTAS** - Amplio rango de [Herramientas MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md) por defecto para operar en Unity Editor
- ✔️ **HABILIDADES** - Genera automáticamente habilidades para cada herramienta MCP, permitiéndote usar herramientas sin desperdiciar tokens en el registro de herramientas MCP
- ✔️ **Código y pruebas** - Pídele a la IA que escriba código y ejecute pruebas
- ✔️ **Runtime (en juego)** - Usa LLMs directamente dentro de tu juego compilado para comportamiento dinámico de NPCs o depuración
- ✔️ **Soporte de depuración** - Pídele a la IA que obtenga registros y corrija errores
- ✔️ **Conversación natural** - Chatea con la IA como lo harías con un humano
- ✔️ **Despliegue flexible** - Funciona localmente (stdio) y remotamente (http) por configuración
- ✔️ **Extensible** - Crea [herramientas MCP personalizadas en el código de tu proyecto](#agregar-herramienta-mcp-personalizada)

[![DESCARGAR INSTALADOR](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/button/button_download_es.svg?raw=true)](https://github.com/IvanMurzak/Unity-MCP/releases/download/0.50.1/AI-Game-Dev-Installer.unitypackage)

![Desarrollador de juegos con IA Ventanas](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/editor/ai-game-developer-windows.png?raw=true)

### Herramientas

<details>
  <summary>Assets</summary>

- `copy` - Copia el asset en la ruta y lo almacena en newPath
- `create-folder` - Crea una nueva carpeta en la carpeta padre especificada
- `delete` - Elimina los assets en las rutas del proyecto
- `find` - Busca en la base de datos de assets usando la cadena de filtro de búsqueda
- `get-data` - Obtiene los datos del asset del archivo de asset, incluyendo todos los campos y propiedades serializables
- `modify` - Modifica el archivo de asset en el proyecto
- `move` - Mueve los assets en las rutas del proyecto (también se usa para renombrar)
- `refresh` - Refresca el AssetDatabase

</details>

<details>
  <summary>Assets / Material</summary>

- `create` - Crea un nuevo asset de material con parámetros por defecto

</details>

<details>
  <summary>Assets / Shader</summary>

- `list-all` - Lista todos los shaders disponibles en los assets y paquetes del proyecto

</details>

<details>
  <summary>Assets / Prefab</summary>

- `create` - Crea un prefab a partir de un GameObject en la escena activa actual
- `instantiate` - Instancia un prefab en la escena activa actual
- `open` - Abre el modo de edición de prefab para un GameObject específico
- `close` - Cierra el prefab actualmente abierto
- `save` - Guarda un prefab en modo de edición de prefab

</details>

<details>
  <summary>GameObject</summary>

- `create` - Crea un nuevo GameObject en un Prefab abierto o en una Escena
- `destroy` - Destruye el GameObject y todos los GameObjects anidados recursivamente
- `duplicate` - Duplica GameObjects en un Prefab abierto o en una Escena
- `find` - Encuentra un GameObject específico mediante la información proporcionada
- `modify` - Modifica GameObjects y/o campos y propiedades de los componentes adjuntos
- `set-parent` - Establece el GameObject padre para una lista de GameObjects

</details>

<details>
  <summary>GameObject / Component</summary>

- `add` - Agrega un Componente al GameObject
- `destroy` - Destruye uno o varios componentes del GameObject objetivo
- `get` - Obtiene información detallada sobre un Componente específico en un GameObject
- `modify` - Modifica un Componente específico en un GameObject

</details>

<details>
  <summary>Component</summary>

- `list` - Lista los nombres de las clases C# extendidas de UnityEngine.Component

</details>

<details>
  <summary>Scene</summary>

- `create` - Crea una nueva escena en los assets del proyecto
- `get-data` - Recupera la lista de GameObjects raíz en la escena especificada
- `list-opened` - Devuelve la lista de escenas actualmente abiertas en el Editor de Unity
- `open` - Abre una escena desde el archivo de asset del proyecto
- `save` - Guarda la escena abierta en el archivo de asset
- `set-active` - Establece la escena abierta especificada como la escena activa
- `unload` - Descarga la escena de las escenas abiertas en el Editor de Unity

</details>

<details>
  <summary>Script</summary>

- `delete` - Elimina los archivos de script
- `execute` - Compila y ejecuta código C# dinámicamente usando Roslyn
- `read` - Lee el contenido de un archivo de script
- `update-or-create` - Actualiza o crea un archivo de script con el código C# proporcionado

</details>

<details>
  <summary>Console</summary>

- `get-logs` - Recupera los registros del Editor de Unity con opciones de filtrado

</details>

<details>
  <summary>Editor / Application</summary>

- `get-state` - Devuelve información sobre el estado de la aplicación del Editor de Unity (modo juego, pausado, compilación)
- `set-state` - Controla el estado de la aplicación del Editor de Unity (iniciar/detener/pausar modo juego)

</details>

<details>
  <summary>Editor / Selection</summary>

- `get` - Obtiene información sobre la Selección actual en el Editor de Unity
- `set` - Establece la Selección actual en el Editor de Unity

</details>

<details>
  <summary>Reflection</summary>

- `method-find` - Encuentra un método en el proyecto usando Reflexión de C# (incluso métodos privados)
- `method-call` - Llama a cualquier método C# con parámetros de entrada y devuelve los resultados

</details>

<details>
  <summary>Tests</summary>

- `run` - Ejecuta pruebas de Unity (EditMode/PlayMode) con filtrado y resultados detallados

</details>

<details>
  <summary>Package Manager (Gestor de Paquetes)</summary>

- `add` - Instala un paquete desde el registro de Unity Package Manager, URL de Git o ruta local
- `remove` - Elimina (desinstala) un paquete del proyecto Unity
- `list` - Lista todos los paquetes instalados en el proyecto Unity (paquetes UPM)
- `search` - Busca paquetes tanto en el registro de Unity Package Manager como en los paquetes instalados

</details>

#### Herramientas adicionales

- [Animation](https://github.com/IvanMurzak/Unity-AI-Animation/)
- [ParticleSystem](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/)
- [ProBuilder](https://github.com/IvanMurzak/Unity-AI-ProBuilder/)

### Estado de estabilidad

| Versión de Unity | Modo Editor                                                                                                                                                                            | Modo Juego                                                                                                                                                                             | Independiente                                                                                                                                                                            |
| ---------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2022.3.62f3      | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2022-3-62f3-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2022-3-62f3-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2022-3-62f3-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) |
| 2023.2.22f1      | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2023-2-22f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2023-2-22f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2023-2-22f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) |
| 6000.3.1f1       | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-6000-3-1f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-6000-3-1f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-6000-3-1f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)  |

## Contenido

- [Instalación](#instalación)
  - [Paso 1: Instalar el `Plugin Unity MCP`](#paso-1-instalar-el-plugin-unity-mcp)
    - [Opción 1 - Instalador](#opción-1---instalador)
    - [Opción 2 - OpenUPM-CLI](#opción-2---openupm-cli)
  - [Paso 2: Instalar el `Cliente MCP`](#paso-2-instalar-el-cliente-mcp)
  - [Paso 3: Configurar el `Cliente MCP`](#paso-3-configurar-el-cliente-mcp)
    - [Configuración automática](#configuración-automática)
    - [Configuración manual](#configuración-manual)
    - [Configuración por línea de comandos](#configuración-por-línea-de-comandos)
- [Ejemplos de Flujo de Trabajo IA: Claude y Gemini](#ejemplos-de-flujo-de-trabajo-ia-claude-y-gemini)
  - [Características avanzadas para LLM](#características-avanzadas-para-llm)
    - [Capacidades principales](#capacidades-principales)
    - [Características de reflexión](#características-de-reflexión)
- [Personalizar MCP](#personalizar-mcp)
  - [Agregar `Herramienta MCP` personalizada](#agregar-herramienta-mcp-personalizada)
  - [Agregar `Prompt MCP` personalizado](#agregar-prompt-mcp-personalizado)
- [Uso en tiempo de ejecución (en juego)](#uso-en-tiempo-de-ejecución-en-juego)
  - [Ejemplo: Bot de ajedrez impulsado por IA](#ejemplo-bot-de-ajedrez-impulsado-por-ia)
  - [¿Por qué se necesita el uso en tiempo de ejecución?](#por-qué-se-necesita-el-uso-en-tiempo-de-ejecución)
- [Configuración del `Servidor MCP` Unity](#configuración-del-servidor-mcp-unity)
  - [Variables](#variables)
  - [Docker 📦](#docker-)
    - [Transporte `streamableHttp`](#transporte-streamablehttp)
    - [Transporte `stdio`](#transporte-stdio)
    - [`Puerto` personalizado](#puerto-personalizado)
  - [Ejecutable binario](#ejecutable-binario)
- [Cómo funciona](#cómo-funciona)
  - [¿Qué es `MCP`?](#qué-es-mcp)
  - [¿Qué es un `Cliente MCP`?](#qué-es-un-cliente-mcp)
  - [¿Qué es un `Servidor MCP`?](#qué-es-un-servidor-mcp)
  - [¿Qué es una `Herramienta MCP`?](#qué-es-una-herramienta-mcp)
    - [¿Cuándo usar una `Herramienta MCP`?](#cuándo-usar-una-herramienta-mcp)
  - [¿Qué es un `Recurso MCP`?](#qué-es-un-recurso-mcp)
    - [¿Cuándo usar un `Recurso MCP`?](#cuándo-usar-un-recurso-mcp)
  - [¿Qué es un `Prompt MCP`?](#qué-es-un-prompt-mcp)
    - [¿Cuándo usar un `Prompt MCP`?](#cuándo-usar-un-prompt-mcp)
- [Contribución 💙💛](#contribución-)

# Instalación

## Paso 1: Instalar el `Plugin Unity MCP`

<details>
  <summary><b>⚠️ Requisitos (clic)</b></summary>

> [!IMPORTANTE]
> **La ruta del proyecto no puede contener espacios**
>
> - ✅ `C:/MyProjects/Project`
> - ❌ `C:/My Projects/Project`

</details>

### Opción 1 - Instalador

- **[⬇️ Descargar Instalador](https://github.com/IvanMurzak/Unity-MCP/releases/download/0.50.1/AI-Game-Dev-Installer.unitypackage)**
- **📂 Importar instalador al proyecto Unity**
  > - Puedes hacer doble clic en el archivo - Unity lo abrirá automáticamente
  > - O: Abre Unity Editor primero, luego haz clic en `Assets/Import Package/Custom Package`, y elige el archivo

### Opción 2 - OpenUPM-CLI

- [⬇️ Instalar OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- 📟 Abrir la línea de comandos en la carpeta de tu proyecto Unity

```bash
openupm add com.ivanmurzak.unity.mcp
```

## Paso 2: Instalar el `Cliente MCP`

Elige un solo `Cliente MCP` que prefieras - no necesitas instalar todos. Esta será tu ventana de chat principal para comunicarte con el LLM.

- [Claude Code](https://github.com/anthropics/claude-code) (altamente recomendado)
- [Claude Desktop](https://claude.ai/download)
- [GitHub Copilot in VS Code](https://code.visualstudio.com/docs/copilot/overview)
- [Antigravity](https://antigravity.google/)
- [Cursor](https://www.cursor.com/)
- [Windsurf](https://windsurf.com)
- Cualquier otro compatible

> El protocolo MCP es bastante universal, por lo que puedes usar cualquier cliente MCP que prefieras - funcionará tan suavemente como cualquier otro. El único requisito importante es que el cliente MCP debe soportar actualizaciones dinámicas de Herramientas MCP.

## Paso 3: Configurar el `Cliente MCP`

### Configuración automática

- Abrir proyecto Unity
- Abrir `Window/AI Game Developer (Unity-MCP)`
- Hacer clic en `Configure` en tu cliente MCP

![Unity_AI](https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/ai-connector-window.gif)

> Si tu cliente MCP no está en la lista, usa el JSON crudo mostrado en la ventana para inyectarlo en tu cliente MCP. Lee las instrucciones para tu cliente MCP específico sobre cómo hacer esto.

### Configuración manual

Si la configuración automática no funciona por alguna razón, usa el JSON de la ventana `AI Game Developer (Unity-MCP)` para configurar cualquier `Cliente MCP` manualmente.

### Configuración por línea de comandos

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

**2. Reemplaza `<unityProjectPath>` con la ruta completa al proyecto Unity**
**3. Reemplaza `<port>` con tu puerto de la configuración de AI Game Developer**
**4. Agrega el servidor MCP usando la línea de comandos**

</details>

<details>
  <summary><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/mcp-clients/gemini-64.png?raw=true" width="16" height="16" alt="Gemini"> Gemini</summary>

  ```bash
  gemini mcp add ai-game-developer <command>
  ```

  > Reemplaza `<command>` de la tabla anterior
</details>

<details>
  <summary><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/mcp-clients/claude-64.png?raw=true" width="16" height="16" alt="Gemini"> Claude Code</summary>

  ```bash
  claude mcp add ai-game-developer <command>
  ```

  > Reemplaza `<command>` de la tabla anterior
</details>

<details>
  <summary><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/mcp-clients/github-copilot-64.png?raw=true" width="16" height="16" alt="Gemini"> GitHub Copilot CLI</summary>

  ```bash
  copilot
  ```

  ```bash
  /mcp add
  ```

  Nombre del servidor: `ai-game-developer`
  Tipo de servidor: `local`
  Comando: `<command>`
  > Reemplaza `<command>` de la tabla anterior
</details>

---

# Ejemplos de Flujo de Trabajo IA: Claude y Gemini

Comunícate con la IA (LLM) en tu `Cliente MCP`. Pídele que haga cualquier cosa que quieras. Mientras mejor describas tu tarea o idea, mejor será su rendimiento.

Algunos `Clientes MCP` te permiten elegir diferentes modelos LLM. Presta atención a esta característica, ya que algunos modelos pueden funcionar mucho mejor que otros.

**Comandos de ejemplo:**

```text
Explica la jerarquía de mi escena
```

```text
Crea 3 cubos en círculo con radio 2
```

```text
Crea material dorado metálico y adjúntalo a un objeto esfera
```

> Asegúrate de que el modo `Agent` esté activado en tu cliente MCP

## Características avanzadas para LLM

Unity MCP proporciona herramientas avanzadas que permiten al LLM trabajar más rápido y efectivamente, evitando errores y auto-corrigiéndose cuando ocurren errores. Todo está diseñado para lograr tus objetivos de manera eficiente.

### Capacidades principales

- ✔️ **Herramientas listas para agentes** - Encuentra cualquier cosa que necesites en 1-2 pasos
- ✔️ **Compilación instantánea** - Compilación y ejecución de código C# usando `Roslyn` para iteración más rápida
- ✔️ **Acceso completo a assets** - Acceso de lectura/escritura a assets y scripts C#
- ✔️ **Retroalimentación inteligente** - Retroalimentación positiva y negativa bien descrita para comprensión adecuada de problemas

### Características de reflexión

- ✔️ **Referencias de objetos** - Proporciona referencias a objetos existentes para código C# instantáneo
- ✔️ **Acceso a datos del proyecto** - Obtén acceso completo a todos los datos del proyecto en formato legible
- ✔️ **Modificaciones granulares** - Llena y modifica cualquier pieza de datos en el proyecto
- ✔️ **Descubrimiento de métodos** - Encuentra cualquier método en toda la base de código, incluyendo archivos DLL compilados
- ✔️ **Ejecución de métodos** - Llama cualquier método en toda la base de código
- ✔️ **Parámetros avanzados** - Proporciona cualquier propiedad para llamadas de métodos, incluso referencias a objetos existentes en memoria
- ✔️ **API Unity en vivo** - API Unity disponible instantáneamente - incluso cuando Unity cambia, obtienes la API fresca
- ✔️ **Auto-documentación** - Accede a descripciones legibles por humanos de cualquier `class`, `method`, `field` o `property` vía atributos `Description`

---

# Personalizar MCP

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** soporta el desarrollo de `Herramienta MCP`, `Recurso MCP` y `Prompt MCP` personalizados por los propietarios del proyecto. El servidor MCP toma datos del `Plugin Unity MCP` y los expone a un cliente. Cualquiera en la cadena de comunicación MCP recibirá información sobre nuevas características MCP, que el LLM puede decidir usar en algún momento.

## Agregar `Herramienta MCP` personalizada

Para agregar una `Herramienta MCP` personalizada, necesitas:

1. Una clase con el atributo `McpPluginToolType`
2. Un método en la clase con el atributo `McpPluginTool`
3. *Opcional:* Agregar un atributo `Description` a cada argumento del método para ayudar al LLM a entenderlo
4. *Opcional:* Usar propiedades `string? optional = null` con `?` y valores por defecto para marcarlas como `opcional` para el LLM

> Nota que la línea `MainThread.Instance.Run(() =>` te permite ejecutar código en el hilo principal, lo que es requerido para interactuar con la API de Unity. Si no necesitas esto y ejecutar la herramienta en un hilo de fondo es aceptable, evita usar el hilo principal por propósitos de eficiencia.

```csharp
[McpPluginToolType]
public class Tool_GameObject
{
    [McpPluginTool
    (
        "MyCustomTask",
        Title = "Create a new GameObject"
    )]
    [Description("Explica aquí al LLM qué es esto, cuándo debería ser llamado.")]
    public string CustomTask
    (
        [Description("Explica al LLM qué es esto.")]
        string inputData
    )
    {
        // hacer cualquier cosa en hilo de fondo

        return MainThread.Instance.Run(() =>
        {
            // hacer algo en hilo principal si es necesario

            return $"[Éxito] Operación completada.";
        });
    }
}
```

## Agregar `Prompt MCP` personalizado

`Prompt MCP` te permite inyectar prompts predefinidos en la conversación con el LLM. Estos son plantillas inteligentes que pueden proporcionar contexto, instrucciones o conocimiento para guiar el comportamiento de la IA. Los prompts pueden ser texto estático o generados dinámicamente basados en el estado actual de tu proyecto.

```csharp
[McpPluginPromptType]
public static class Prompt_ScriptingCode
{
    [McpPluginPrompt(Name = "add-event-system", Role = Role.User)]
    [Description("Implementar sistema de comunicación basado en UnityEvent entre GameObjects.")]
    public string AddEventSystem()
    {
        return "Crear sistema de eventos usando UnityEvents, UnityActions, o delegados de eventos personalizados para comunicación desacoplada entre sistemas de juego y componentes.";
    }
}
```

---

# Uso en tiempo de ejecución (en juego)

Usa **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** en tu juego/aplicación. Usa Herramientas, Recursos o Prompts. Por defecto no hay herramientas, necesitarías implementar las tuyas personalizadas.

```csharp
// Construir plugin MCP
var mcpPlugin = UnityMcpPluginRuntime.Initialize(builder =>
    {
        builder.WithConfig(config =>
        {
            config.Host = "http://localhost:8080";
            config.Token = "your-token";
        });
        // Registrar automáticamente todas las herramientas del ensamblado actual
        builder.WithToolsFromAssembly(Assembly.GetExecutingAssembly());
    })
    .Build();

await mcpPlugin.Connect(); // Iniciar conexión activa con reintento al Unity-MCP-Server

await mcpPlugin.Disconnect(); // Detener conexión activa y cerrar la conexión existente
```

## Ejemplo: Bot de ajedrez impulsado por IA

Hay un juego de ajedrez clásico. Delegemos la lógica del bot al LLM. El bot debe hacer el turno usando las reglas del juego.

```csharp
[McpPluginToolType]
public static class ChessGameAI
{
    [McpPluginTool("chess-do-turn", Title = "Do the turn")]
    [Description("Do the turn in the chess game. Returns true if the turn was accepted, false otherwise.")]
    public static Task<bool> DoTurn(int figureId, Vector2Int position)
    {
        return MainThread.Instance.RunAsync(() => ChessGameController.Instance.DoTurn(figureId, position));
    }

    [McpPluginTool("chess-get-board", Title = "Get the board")]
    [Description("Get the current state of the chess board.")]
    public static Task<BoardData> GetBoard()
    {
        return MainThread.Instance.RunAsync(() => ChessGameController.Instance.GetBoardData());
    }
}
```

## ¿Por qué se necesita el uso en tiempo de ejecución?

Hay muchos casos de uso, imaginemos que estás trabajando en un juego de ajedrez con bot. Puedes delegar la toma de decisiones del bot al LLM escribiendo unas pocas líneas de código.

---

# Configuración del `Servidor MCP` Unity

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** Server soporta muchas opciones de lanzamiento diferentes y despliegue Docker. Ambos protocolos de transporte son soportados: `streamableHttp` y `stdio`. Si necesitas personalizar o desplegar Unity MCP Server a la nube, esta sección es para ti. [Leer más...](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/mcp-server.md)

## Variables

No importa qué opción de lanzamiento elijas, todas soportan configuración personalizada usando tanto Variables de Entorno como Argumentos de Línea de Comandos. Funcionará con valores por defecto, si solo necesitas lanzarlo, no pierdas tiempo con las variables. Solo asegúrate de que el Plugin Unity también tenga valores por defecto, especialmente el `--port`, deberían ser iguales.

| Variable de Entorno          | Args de Línea de Comandos | Descripción                                                                            |
| ---------------------------- | ------------------------- | -------------------------------------------------------------------------------------- |
| `MCP_PLUGIN_PORT`            | `--port`                  | Puerto de conexión **Cliente** -> **Servidor** <- **Plugin** (por defecto: 8080)       |
| `MCP_PLUGIN_CLIENT_TIMEOUT`   | `--plugin-timeout`        | Tiempo de espera de conexión **Plugin** -> **Servidor** (ms) (por defecto: 10000)      |
| `MCP_PLUGIN_CLIENT_TRANSPORT` | `--client-transport`      | Tipo de transporte **Cliente** -> **Servidor**: `stdio` o `streamableHttp` (por defecto: `streamableHttp`) |

> Los args de línea de comandos también soportan la opción con un solo prefijo `-` (`-port`) y una opción sin prefijo en absoluto (`port`).

## Docker 📦

[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)

Asegúrate de que Docker esté instalado. Y por favor asegúrate de que Docker Desktop esté lanzado si estás en sistema operativo Windows.

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

> Reemplaza `url` con tu endpoint real si está hospedado en la nube

</details>

### Transporte `stdio`

Para usar esta variante, el `Cliente MCP` debería lanzar el `Servidor MCP` en docker. Esto es posible a través de la configuración modificada del `Cliente MCP`.

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

### `Puerto` personalizado

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

> Reemplaza `url` con tu endpoint real si está hospedado en la nube
</details>

## Ejecutable binario

Puedes lanzar Unity `Servidor MCP` directamente desde un archivo binario. Necesitarías tener un binario compilado específicamente para tu arquitectura CPU. Revisa la [Página de Releases de GitHub](https://github.com/IvanMurzak/Unity-MCP/releases), contiene binarios precompilados para todas las arquitecturas CPU.

```bash
./unity-mcp-server --port 8080 --plugin-timeout 10000 --client-transport stdio
```

<details>
  <summary>Configuración del <code>Cliente MCP</code>:</summary>

> Reemplaza `<project>` con la ruta de tu proyecto Unity.

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

---

# Cómo funciona

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** sirve como puente entre LLMs y Unity. Expone y explica las herramientas de Unity al LLM, que luego entiende la interfaz y utiliza las herramientas según las solicitudes del usuario.

Conecta **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** a clientes LLM como [Claude](https://claude.ai/download) o [Cursor](https://www.cursor.com/) usando la ventana integrada `AI Connector`. También se soportan clientes personalizados.

El sistema es altamente extensible - puedes definir `Herramientas MCP`, `Recurso MCP` o `Prompt MCP` personalizados directamente en la base de código de tu proyecto Unity, exponiendo nuevas capacidades a clientes IA o de automatización. Esto hace de Unity MCP una base flexible para construir flujos de trabajo avanzados, prototipado rápido e integración de características impulsadas por IA en tu proceso de desarrollo.

## ¿Qué es `MCP`?

MCP - Protocolo de Contexto de Modelo. En pocas palabras, eso es `USB Type-C` para IA, específicamente para LLM (Modelo de Lenguaje Grande). Enseña al LLM cómo usar características externas. Como Unity Engine en este caso, o incluso tu método C# personalizado en tu código. [Documentación oficial](https://modelcontextprotocol.io/).

## ¿Qué es un `Cliente MCP`?

Es una aplicación con una ventana de chat. Puede tener agentes inteligentes para operar mejor, puede tener Herramientas MCP avanzadas embebidas. En general, un Cliente MCP bien hecho es el 50% del éxito de la IA ejecutando una tarea. Por eso es muy importante elegir el mejor para usar.

## ¿Qué es un `Servidor MCP`?

Es un puente entre el `Cliente MCP` y "algo más", en este caso particular es Unity Engine. Este proyecto incluye el `Servidor MCP`.

## ¿Qué es una `Herramienta MCP`?

Una `Herramienta MCP` es una función o método que el LLM puede llamar para interactuar con Unity. Estas herramientas actúan como puente entre solicitudes de lenguaje natural y operaciones reales de Unity. Cuando le pides a la IA "crear un cubo" o "cambiar color de material," usa Herramientas MCP para ejecutar estas acciones.

**Características clave:**

- **Funciones ejecutables** que realizan operaciones específicas
- **Parámetros tipados** con descripciones para ayudar al LLM a entender qué datos proporcionar
- **Valores de retorno** que dan retroalimentación sobre el éxito o fallo de la operación
- **Consciente de hilos** - puede ejecutarse en hilo principal para llamadas API Unity o hilo de fondo para procesamiento pesado

### ¿Cuándo usar una `Herramienta MCP`?

- **Automatizar tareas repetitivas** - Crear herramientas para operaciones comunes que haces frecuentemente
- **Operaciones complejas** - Agrupar múltiples llamadas API Unity en una sola herramienta fácil de usar
- **Flujos de trabajo específicos del proyecto** - Construir herramientas que entiendan la estructura específica y convenciones de tu proyecto
- **Tareas propensas a errores** - Crear herramientas que incluyan validación y manejo de errores
- **Lógica de juego personalizada** - Exponer los sistemas de tu juego a la IA para creación de contenido dinámico

**Ejemplos:**

- Crear y configurar GameObjects con componentes específicos
- Procesamiento por lotes de assets (texturas, materiales, prefabs)
- Configurar iluminación y efectos de post-procesamiento
- Generar geometría de nivel o colocar objetos proceduralmente
- Configurar ajustes de física o capas de colisión

## ¿Qué es un `Recurso MCP`?

Un `Recurso MCP` proporciona acceso de solo lectura a datos dentro de tu proyecto Unity. A diferencia de las Herramientas MCP que realizan acciones, los Recursos permiten al LLM inspeccionar y entender el estado actual, assets y configuración de tu proyecto. Piensa en ellos como "sensores" que dan contexto sobre tu proyecto a la IA.

**Características clave:**

- **Acceso de solo lectura** a datos del proyecto y objetos Unity
- **Información estructurada** presentada en un formato que el LLM puede entender
- **Datos en tiempo real** que reflejan el estado actual de tu proyecto
- **Conciencia contextual** ayudando a la IA a tomar decisiones informadas

### ¿Cuándo usar un `Recurso MCP`?

- **Análisis de proyecto** - Permitir que la IA entienda la estructura, assets y organización de tu proyecto
- **Asistencia de depuración** - Proporcionar información del estado actual para solución de problemas
- **Sugerencias inteligentes** - Dar contexto a la IA para hacer mejores recomendaciones
- **Generación de documentación** - Crear automáticamente documentación basada en el estado del proyecto
- **Gestión de assets** - Ayudar a la IA a entender qué assets están disponibles y sus propiedades

**Ejemplos:**

- Exponer jerarquía de escena y propiedades de GameObject
- Listar materiales disponibles, texturas y sus configuraciones
- Mostrar dependencias de scripts y relaciones de componentes
- Mostrar configuración de iluminación actual y configuración del pipeline de renderizado
- Proporcionar información sobre fuentes de audio, animaciones y sistemas de partículas

## ¿Qué es un `Prompt MCP`?

Un `Prompt MCP` te permite inyectar prompts predefinidos en la conversación con el LLM. Estos son plantillas inteligentes que pueden proporcionar contexto, instrucciones o conocimiento para guiar el comportamiento de la IA. Los prompts pueden ser texto estático o generados dinámicamente basados en el estado actual de tu proyecto.

**Características clave:**

- **Guía contextual** que influye en cómo responde la IA
- **Basado en roles** - puede simular diferentes personas (solicitudes de Usuario o conocimiento de Asistente)
- **Contenido dinámico** - puede incluir datos del proyecto en tiempo real
- **Plantillas reutilizables** para escenarios y flujos de trabajo comunes

### ¿Cuándo usar un `Prompt MCP`?

- **Proporcionar conocimiento del dominio** - Compartir mejores prácticas y estándares de codificación específicos de tu proyecto
- **Establecer convenciones de codificación** - Establecer convenciones de nomenclatura, patrones de arquitectura y estilo de código
- **Dar contexto sobre estructura del proyecto** - Explicar cómo está organizado tu proyecto y por qué
- **Compartir instrucciones de flujo de trabajo** - Proporcionar procedimientos paso a paso para tareas comunes
- **Inyectar conocimiento especializado** - Agregar información sobre características específicas de Unity, assets de terceros o sistemas personalizados

**Ejemplos:**

- "Siempre usa PascalCase para métodos públicos y camelCase para campos privados"
- "Este proyecto usa un sistema de eventos personalizado ubicado en Scripts/Events/"
- "Al crear elementos UI, siempre agrégalos al Canvas en Scene/UI/MainCanvas"
- "El rendimiento es crítico - prefiere object pooling para objetos instanciados frecuentemente"
- "Este proyecto sigue principios SOLID - explica cualquier decisión de arquitectura"

---

# Contribución 💙💛

Las contribuciones son muy apreciadas. ¡Trae tus ideas y hagamos el desarrollo de juegos más simple que nunca! ¿Tienes una idea para una nueva `Herramienta MCP` o característica, o encontraste un bug y sabes cómo arreglarlo?

**¡Por favor dale una estrella 🌟 a este proyecto si lo encuentras útil!**

1. 👉 [Lee la documentación de desarrollo](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.es.md)
2. 👉 [Haz fork del proyecto](https://github.com/IvanMurzak/Unity-MCP/fork)
3. Clona el fork y abre la carpeta `./Unity-MCP-Plugin` en Unity
4. Implementa nuevas cosas en el proyecto, commit, empújalo a GitHub
5. Crea Pull Request dirigido al repositorio original [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP/compare), rama `main`.
