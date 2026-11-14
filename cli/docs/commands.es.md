[English](commands.md) | [中文](commands.zh-CN.md) | [日本語](commands.ja.md) | <b>Español</b> | [فارسی](commands.fa.md) | [हिन्दी](commands.hi.md) | [العربية](commands.ar.md)

# Referencia de unity-mcp-cli

Todos los comandos y opciones de [`unity-mcp-cli`](../README.md), los códigos de salida, la salida legible por máquina y la API de biblioteca.
Ejecuta `unity-mcp-cli <command> --help` para ver los mismos detalles en la terminal.

**Comandos:** [`configure`](#configure) · [`create-project`](#create-project) · [`update`](#update) · [`install-extension`](#install-extension) · [`install-plugin`](#install-plugin) · [`install-unity`](#install-unity) · [`login`](#login) · [`unity-account`](#unity-account) · [`open`](#open) · [`close`](#close) · [`tools`](#tools) · [`rql`](#rql) · [`run-tool`](#run-tool) · [`exec`](#exec) · [`test`](#test) · [`compile`](#compile) · [`dialog`](#dialog) · [`commands`](#commands) · [`wait-for-ready`](#wait-for-ready) · [`setup-mcp`](#setup-mcp) · [`setup-skills`](#setup-skills) · [`remove-plugin`](#remove-plugin) · [`status`](#status)

**Además:** [Opciones globales](#opciones-globales) · [Códigos de salida y salida legible por máquina](#códigos-de-salida-y-salida-legible-por-máquina) · [Ejemplo completo de automatización](#ejemplo-completo-de-automatización) · [Cómo funciona](#cómo-funciona) · [API de biblioteca](#api-de-biblioteca)

# Comandos

## `configure`

Configura las herramientas, prompts y recursos MCP en `UserSettings/AI-Game-Developer-Config.json`.

```bash
unity-mcp-cli configure ./MyGame --list
```

| Opción | Obligatoria | Descripción |
|---|---|---|
| `[path]` | Sí | Ruta al proyecto de Unity (posicional o `--path`) |
| `--list` | No | Listar la configuración actual y salir |
| `--enable-tools <names>` | No | Mostrar herramientas concretas a los clientes MCP (separadas por comas) |
| `--disable-tools <names>` | No | Ocultar herramientas concretas a los clientes MCP (separadas por comas; una herramienta oculta sigue ejecutándose cuando se llama por su nombre) |
| `--enable-all-tools` | No | Mostrar todas las herramientas |
| `--disable-all-tools` | No | Ocultar todas las herramientas |
| `--enable-prompts <names>` | No | Mostrar prompts concretos a los clientes MCP (separados por comas) |
| `--disable-prompts <names>` | No | Ocultar prompts concretos a los clientes MCP (separados por comas; una herramienta oculta sigue ejecutándose cuando se llama por su nombre) |
| `--enable-all-prompts` | No | Mostrar todos los prompts |
| `--disable-all-prompts` | No | Ocultar todos los prompts |
| `--enable-resources <names>` | No | Mostrar recursos concretos a los clientes MCP (separados por comas) |
| `--disable-resources <names>` | No | Ocultar recursos concretos a los clientes MCP (separados por comas; una herramienta oculta sigue ejecutándose cuando se llama por su nombre) |
| `--enable-all-resources` | No | Mostrar todos los recursos |
| `--disable-all-resources` | No | Ocultar todos los recursos |

**Ejemplo — activar herramientas concretas y desactivar todos los prompts:**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-tools screenshot-scene-view,script-read \
  --disable-all-prompts
```

**Ejemplo — activar todo:**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-all-tools \
  --enable-all-prompts \
  --enable-all-resources
```

## `create-project`

Crea un nuevo proyecto de Unity usando el Editor de Unity.

```bash
unity-mcp-cli create-project /path/to/new/project
```

| Opción | Obligatoria | Descripción |
|---|---|---|
| `[path]` | Sí | Ruta donde se creará el proyecto (posicional o `--path`) |
| `--unity <version>` | No | Versión del Editor de Unity que se usará (por defecto, la más alta instalada) |

**Ejemplo — crear un proyecto con una versión concreta del editor:**

```bash
unity-mcp-cli create-project ./MyGame --unity 2022.3.62f1
```

## `update`

Actualiza todo: el plugin MCP y las extensiones instaladas de un proyecto de Unity a sus últimas versiones y, después, la propia CLI.

```bash
unity-mcp-cli update              # the project around the current folder, then the CLI
unity-mcp-cli update ./MyGame      # a project by path
unity-mcp-cli update --check       # only report; exit code 2 when something is outdated
unity-mcp-cli update --no-self     # leave the CLI as it is
unity-mcp-cli update --no-project  # update only the CLI
unity-mcp-cli update --no-extensions
```

Fuera de un proyecto solo actualiza la CLI. Las instalaciones desde `file:` o git no se tocan y nunca se baja de versión nada.

## `install-extension`

Instala una de las extensiones de Unity-MCP —los paquetes de herramientas opcionales que añaden herramientas MCP para Animation, Cinemachine, Input System, Navigation, Particle System, ProBuilder, Splines, Terrain, Tilemap y Timeline— en el `Packages/manifest.json` de un proyecto de Unity. Es el equivalente en línea de comandos de la sección **Extensions** de la ventana del editor *AI Game Developer*, e instala desde el mismo catálogo compartido.

```bash
# See what is installable (no project and no network required)
unity-mcp-cli install-extension --list

# Run from inside the Unity project folder — the path is optional
cd ./MyGame && unity-mcp-cli install-extension Tilemap
```

| Argumento / Opción | Obligatoria | Descripción |
|---|---|---|
| `[id]` | Sí (salvo con `--list`) | Extensión que se instalará: el id de paquete de OpenUPM (`com.ivanmurzak.unity.mcp.tilemap`) o el nombre del catálogo (`Tilemap`). Sin distinguir mayúsculas. Un id desconocido lista todas las extensiones instalables para que puedas corregirlo. |
| `[path]` | No | Ruta al proyecto de Unity (posicional o `--path`). Por defecto, el directorio actual, verificado igual que en `install-plugin`. |
| `--extension-version <version>` | No | Versión de la extensión que se instalará (por defecto, la última de OpenUPM). **No es `--version`**: esa es la opción global de versión de la propia CLI. |
| `--list` | No | Listar las extensiones instalables y salir. |

Este comando:
1. Añade el **registro con ámbito de OpenUPM** con todos los ámbitos necesarios (el ámbito `com.ivanmurzak` es lo que hace que la extensión se pueda resolver)
2. Añade el id de paquete de la extensión a `dependencies`, resolviendo el `latest` de OpenUPM en el momento de la instalación
3. **Nunca baja de versión** con una versión resuelta automáticamente: si ya hay instalada una versión superior, se conserva y se muestra un aviso. Pasa `--extension-version` para forzar un valor concreto, incluida una bajada de versión.
4. Es **idempotente**: una nueva ejecución que encuentra la extensión ya actualizada lo indica y no escribe nada

**Ejemplo — instalar una versión concreta de una extensión:**

```bash
unity-mcp-cli install-extension Cinemachine ./MyGame --extension-version 1.0.17
```

**Ejemplo — en proceso, desde la biblioteca:**

```ts
import { installExtension, EXTENSIONS_CATALOG } from 'unity-mcp-cli';

const result = await installExtension({
  unityProjectPath: './MyGame',
  extensionId: 'Tilemap',
});
if (result.kind === 'success') {
  console.log(result.outcome, result.fromVersion, '->', result.toVersion);
}
```

> Después de ejecutar este comando, abre (o pon en primer plano) el Editor de Unity para que el Package Manager resuelva la nueva dependencia.

## `install-plugin`

Instala el plugin Unity-MCP en el `Packages/manifest.json` de un proyecto de Unity. Opcionalmente, descarga el binario del servidor MCP correspondiente al RID y canjea un código de inscripción del equipo en la misma ejecución.

```bash
# Run from inside the Unity project folder — the path is optional
cd ./MyGame && unity-mcp-cli install-plugin
```

| Opción | Obligatoria | Descripción |
|---|---|---|
| `[path]` | No | Ruta al proyecto de Unity (posicional o `--path`). Por defecto, el directorio actual; se verifica que el directorio resuelto sea un proyecto de Unity real (marcador `Packages/manifest.json`) y, si no lo es, el error indica exactamente qué se comprobó. |
| `--plugin-version <version>` | No | Versión del plugin que se instalará (por defecto, la última de [OpenUPM](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)) |
| `--with-server` | No | Descargar también el binario de [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) correspondiente al RID en el directorio gestionado por la CLI |
| `--server-version <version>` | No | Versión del servidor que se descargará con `--with-server` (por defecto: la versión de servidor fijada por la CLI) |
| `--server-source <path-or-url>` | No | Anulación para uso sin conexión/CI: instalar el servidor desde una ruta o URL de un zip local (omite la verificación de checksum) |
| `--enroll <code>` | No | Canjear un código de inscripción por una credencial del plugin (se guarda en el almacén compartido de la máquina y se fija a este proyecto) |
| `--enroll-stdin` | No | Leer el código de inscripción desde stdin en lugar de argv (lo mantiene fuera del historial del shell) |

Este comando:
1. Añade el **registro con ámbito de OpenUPM** con todos los ámbitos necesarios
2. Añade `com.ivanmurzak.unity.mcp` a `dependencies`
3. **Nunca baja de versión**: si ya hay instalada una versión superior, se conserva

**Ejemplo — instalar una versión concreta del plugin:**

```bash
unity-mcp-cli install-plugin ./MyGame --plugin-version 0.51.6
```

**Ejemplo — canjear un código de inscripción del equipo (la ruta por defecto es el cwd):**

```bash
cd ./MyGame && unity-mcp-cli install-plugin --enroll <code>
```

> Después de ejecutar este comando, abre el proyecto en el Editor de Unity para completar la instalación del paquete.

## `install-unity`

Instala una versión del Editor de Unity mediante la CLI de Unity Hub.

```bash
unity-mcp-cli install-unity 6000.3.1f1
```

| Argumento / Opción | Obligatoria | Descripción |
|---|---|---|
| `[version]` | No | Versión del Editor de Unity que se instalará (p. ej. `6000.3.1f1`) |
| `--path <path>` | No | Leer la versión necesaria de un proyecto existente |

Si no se indica ni el argumento ni la opción, el comando instala la última versión estable de la lista de versiones de Unity Hub.

**Ejemplo — instalar la versión del editor que necesita un proyecto:**

```bash
unity-mcp-cli install-unity --path ./MyGame
```

## `login`

Inicia sesión en **ai-game.dev** y guarda la credencial en el almacén de credenciales compartido de la máquina (`~/.ai-game-dev/credentials.json`). Ejecuta el **flujo de dispositivo OAuth 2.1** basado en el navegador: no hay ningún token de acceso personal que crear ni pegar. El inicio de sesión autoriza a toda la máquina: emite una credencial de **agente** y deriva de ella una credencial para las herramientas del motor, y todos los consumidores (los comandos de nube de la CLI, el plugin del Editor de Unity, la aplicación de escritorio) la renuevan automáticamente; los tokens rotan en segundo plano, así que inicias sesión una vez por máquina, no una vez por sesión.

```bash
unity-mcp-cli login
```

| Opción | Obligatoria | Descripción |
|---|---|---|
| `--project <path>` | No | Guardar la credencial en un almacén local del proyecto (`<path>/.ai-game-dev/`) en lugar del almacén compartido de la máquina |
| `--force` | No | Volver a autenticarse aunque ya exista una credencial |
| `--tools-only` | No | Autorizar **solo las herramientas del motor** (no se guarda ninguna credencial de agente, así que la aplicación de escritorio no puede recogerla). Pensado para runners de CI / automatización, que aparecen entonces como su propio grupo de dispositivos revocable |
| `--yes` | No | Responder "sí" a las preguntas: necesario para confirmar un **cambio de cuenta** de forma no interactiva (iniciar sesión con una cuenta distinta de la que tiene la máquina) |

El comando muestra un código de usuario corto y una URL de verificación, abre tu navegador y consulta periódicamente hasta que apruebes el inicio de sesión. Si ya existe una credencial, termina antes con *"Already signed in"*; pasa `--force` para sustituirla. Iniciar sesión con una **cuenta distinta** de la que tiene la máquina pide confirmación primero (si la rechazas, todo queda igual); pasa `--yes` para confirmar en scripts.

**Ejemplo — iniciar sesión (almacén compartido de la máquina):**

```bash
unity-mcp-cli login
```

**Ejemplo — runner de CI, solo herramientas del motor:**

```bash
unity-mcp-cli login --tools-only
```

**Ejemplo — guardar la credencial junto a un proyecto concreto:**

```bash
unity-mcp-cli login --project ./MyGame
```

## `unity-account`

Muestra la **cuenta de Unity** con la que ha iniciado sesión el Editor de Unity en ejecución, e inicia su inicio de sesión. El "My Assets"
de Asset Store (la fuente RQL `assetStore`) la necesita; una licencia Personal funciona sin ella, así que un editor puede ejecutarse sin sesión
iniciada. No es la cuenta de ai-game.dev: esa es [`login`](#login).

`--sign-in` abre el inicio de sesión de Unity en ese editor; el **usuario** lo completa allí, mediante Unity Hub. Un editor abierto
directamente (no mediante el Hub, p. ej. por un `open` antiguo) no puede iniciar sesión: solo ofrece "Install Unity Hub", así que la
respuesta indica que se vuelva a abrir el proyecto con [`close`](#close) y [`open`](#open), que lo inician mediante el Hub. Un editor
abierto así toma la cuenta con la que ha iniciado sesión el Hub sin ningún inicio de sesión. Ninguna credencial pasa por la CLI. El comando necesita el proyecto abierto en un editor con ventana
([`open`](#open)) y nunca inicia uno sin interfaz, que no puede mostrar el inicio de sesión. Los agentes pueden ejecutar las mismas consultas con
`rql`: `assetStore | select signedIn userName` y `assetStore | call SignIn()`.

```bash
unity-mcp-cli unity-account ./MyGame            # signedIn|userName
unity-mcp-cli unity-account ./MyGame --sign-in  # opens Unity's sign-in for the user
```

| Opción | Obligatoria | Descripción |
|---|---|---|
| `[path]` | No | Ruta del proyecto de Unity (posicional o `--path`; por defecto: el directorio actual) |
| `--sign-in` | No | Abrir el inicio de sesión de Unity cuando el editor no tiene sesión iniciada (con sesión iniciada: renueva el token de acceso) |
| `--format <format>` | No | `text`, `json` o `ndjson` |

Sale con `0` y la respuesta del editor, y con `7` cuando ningún editor responde. Un plugin demasiado antiguo para este comando recibe la sugerencia de ejecutar
[`update`](#update).

## `open`

Abre un proyecto de Unity en el Editor de Unity. Por defecto, define las variables de entorno de conexión MCP si se proporcionan opciones de conexión. Usa `--no-connect` para abrirlo sin conexión MCP.

Cuando Unity Hub está instalado, el editor se abre a través de él tal como lo hace el propio Hub (`-useHub -hubIPC`, sin credenciales:
el editor pide su sesión al Hub e inicia el Hub en segundo plano si no está en ejecución). El editor toma entonces
la cuenta de Unity con la que ha iniciado sesión el Hub, que necesita el "My Assets" de Asset Store (consulta
[`unity-account`](#unity-account)). Un editor sin interfaz (`--headless`) se abre como antes.

```bash
# Explicit path
unity-mcp-cli open ./MyGame

# From inside the Unity project folder — path defaults to the current directory
cd ./MyGame && unity-mcp-cli open
```

| Opción | Variable de entorno | Obligatoria | Descripción |
|---|---|---|---|
| `[path]` | — | No | Ruta al proyecto de Unity (posicional o `--path`). Por defecto, el directorio de trabajo actual. |
| `--unity <version>` | — | No | Versión concreta del Editor de Unity que se usará (por defecto, la versión de la configuración del proyecto; si no, la más alta instalada) |
| `--editor-path <path>` | — | No | Ruta explícita al ejecutable del Editor de Unity. Omite la detección mediante Unity Hub; útil para ubicaciones de instalación personalizadas. |
| `--no-connect` | — | No | Abrir sin las variables de entorno de conexión MCP |
| `--url <url>` | `UNITY_MCP_CLOUD_URL` | No | URL del servidor MCP al que conectarse (la CLI sigue exportando el alias heredado `UNITY_MCP_HOST`, que el plugin también acepta) |
| `--keep-connected` | `UNITY_MCP_KEEP_CONNECTED` | No | Forzar que la conexión se mantenga activa |
| `--token <token>` | `UNITY_MCP_TOKEN` | No | Token de autenticación |
| `--auth <option>` | `UNITY_MCP_AUTH_OPTION` | No | Modo de autenticación: `none`, `oauth` o `token` |
| `--tools <names>` | `UNITY_MCP_TOOLS` | No | Lista de herramientas que se activarán, separadas por comas |
| `--transport <method>` | `UNITY_MCP_TRANSPORT` | No | Método de transporte: `streamableHttp` o `stdio` |
| `--start-server <value>` | `UNITY_MCP_START_SERVER` | No | `true` o `false` para controlar el inicio automático del servidor MCP |
| `--headless` | — | No | Iniciar el editor sin ventana (`-batchmode`; los gráficos siguen activos, así que las cámaras siguen renderizando) y esperar hasta que su servidor MCP responda. Implica `--keep-connected`. Un editor del proyecto en ejecución se reutiliza; uno iniciado aquí pertenece a la CLI (consulta [`close --owned-only`](#close)), y las llamadas en paralelo inician solo uno. |
| `--idle-timeout <seconds>` | `AIGD_SESSION_IDLE_SECONDS` (headless) / `UNITY_MCP_IDLE_TIMEOUT` (ventana) | No | Cerrar el editor tras este número de segundos sin llamadas a herramientas (primero guarda las escenas y assets modificados) |
| `--ready-timeout <ms>` | — | No | Con `--headless`: cuánto esperar a que el editor esté listo (por defecto: `300000`) |
| `--no-auto-dismiss-launch-errors` | — | No | Desactivar el cierre automático del diálogo "compile errors at launch" del Editor de Unity (por defecto: activado) |
| `--launch-dismiss-timeout-ms <ms>` | — | No | Tiempo de espera total (milisegundos) del bucle de sondeo que cierra automáticamente los errores de inicio (por defecto: `30000`) |
| `--launch-dismiss-poll-interval-ms <ms>` | — | No | Intervalo de sondeo (milisegundos) del bucle que cierra automáticamente los errores de inicio (por defecto: `1500`) |

Sin `--headless`, a un editor sin interfaz que la CLI inició para el proyecto (`exec`, `open --headless`) se le pide primero que guarde y se cierre, para que la ventana pueda abrirse.

El proceso del editor se lanza en modo desacoplado. Por defecto, tras lanzar el editor, `open` busca periódicamente el diálogo "compile errors at launch" de Unity (`"Enter Safe Mode?"` en Unity 2020.2+, `"Hold On" / "Compiler Errors"` en versiones anteriores) y pulsa `Ignore` para que el editor termine de inicializarse; sin esto, cualquier automatización dentro del Editor que deba ejecutarse después de un estado en el que el propio Unity no puede compilar (p. ej. el resolvedor de dependencias NuGet) no puede repararse sola. El diálogo aparece después de que Unity haya arrancado, se haya conectado al Package Manager y haya empezado a compilar —empíricamente unos 6 s en una máquina rápida y más en una lenta—, así que el bucle de sondeo tiene un periodo de gracia tras el cual termina antes si no ha visto ningún diálogo. El periodo de gracia debe cubrir toda la fase de arranque de Unity, o el bucle se rinde antes de que aparezca el diálogo (issue #737); nunca agota todo el `--launch-dismiss-timeout-ms` cuando no hay diálogo. Si se observa el diálogo (y se cierra correctamente), el sondeo continúa hasta el tiempo de espera total para que un diálogo que reaparece (el resolvedor corrige un error → el diálogo vuelve a aparecer con el siguiente) se cierre de nuevo. Quienes llaman en modo biblioteca pueden proporcionar un `AbortSignal` (`launchDismissAbortSignal` en `OpenProjectOptions`) para interrumpir el bucle en cuanto se dispare su propia señal de disponibilidad.

### Requisitos de plataforma del cierre automático

| Plataforma | Requisito | Notas |
|---|---|---|
| **Windows** | Integrado (API Win32) | Usa `EnumWindows` / `EnumChildWindows` / `SendMessageW(BM_CLICK)` controlados desde PowerShell. No requiere configuración adicional. |
| **macOS** | Se debe conceder el **permiso de Accesibilidad** a la terminal (o al binario `unity-mcp-cli`). System Settings → Privacy & Security → Accessibility. | Implementado mediante AppleScript / `osascript`. Sin este permiso, `osascript` informa de un error en cada sondeo y el diálogo no se puede cerrar. |
| **Linux/X11** | `xdotool` en el `PATH` (p. ej. `sudo apt-get install xdotool`). | Wayland **no** está soportado en esta primera versión; sigue las issues upstream sobre el soporte de Wayland. |

Para desactivarlo por completo, pasa `--no-auto-dismiss-launch-errors`.

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

**Ejemplo — abrir con autenticación y herramientas concretas:**

```bash
unity-mcp-cli open ./MyGame \
  --url http://my-server:8080 \
  --token my-secret-token \
  --auth token \
  --tools rql-query,rql-mutate,rql-schema
```

## `close`

Termina de forma ordenada la instancia del Editor de Unity que se ejecuta para una ruta de proyecto dada. Contraparte simétrica de [`open`](#open), para flujos de trabajo con scripts (agentes de CI, ejecutores de pipelines, fixtures de pruebas de integración) que necesitan un cierre limpio sin recurrir a matar procesos a nivel del sistema operativo.

```bash
unity-mcp-cli close ./MyGame
```

| Opción | Obligatoria | Descripción |
|---|---|---|
| `[path]` | No | Ruta al proyecto de Unity (posicional; por defecto, el directorio actual) |
| `--timeout <seconds>` | No | Tiempo de espera del cierre ordenado en segundos (por defecto: 30) |
| `--force` | No | Matar el Editor a la fuerza si no termina dentro de `--timeout` |
| `--owned-only` | No | Cerrar solo un Editor que inició la CLI (`exec`, `open --headless`); un Editor abierto por el usuario nunca se toca |

**Cómo funciona:**

0. Un Editor que inició la CLI se registra en la concesión de sesión del proyecto (`.ai-game-dev/session.json`: PID, hora de inicio del proceso, nonce). Si la concesión nombra un Editor activo, `close` pide a su plugin que guarde y se cierre (la herramienta de sistema `editor-session-quit`, comprobada con el nonce de la concesión, en la URL del servidor que registró la concesión al iniciar el Editor) —esto también llega a un Editor sin interfaz, al que `WM_CLOSE` no llega— y omite los pasos siguientes. Con `--owned-only` se detiene aquí: sin concesión, no se cierra nada. Si ese Editor sigue en ejecución tras `--timeout` (sin `--force`) y hay un diálogo modal abierto, `close` sale con 18 y lo nombra, como en el paso 3.
1. Resuelve el PID del Editor en ejecución leyendo `<project>/Temp/UnityLockfile` (uint32 little-endian de 4 bytes) y lo contrasta con la enumeración de procesos para manejar archivos de bloqueo obsoletos.
2. Envía una señal de cierre ordenado —`SIGTERM` en Linux/macOS, `taskkill` (sin `/F`) en Windows— que deja a Unity terminar el autoguardado / la importación de assets.
3. Consulta cada 250ms hasta que el proceso termina o se agota `--timeout`. Mientras tanto busca un diálogo modal del Editor (Windows, macOS, Linux): un cierre ordenado con cambios sin guardar abre "Scene(s) Have Been Modified" [Save | Don't Save | Cancel], y el Editor espera entonces a una persona. Sin `--force`, `close` se detiene en cuanto dos exploraciones seguidas ven el diálogo (unos 3 s después del cierre) con el código de salida 18 y nombra el diálogo y sus botones (respóndelo en el Editor o con [`dialog --press`](#dialog)); con `--force` sigue esperando a que se agote `--timeout` antes de matarlo a la fuerza.
4. Si el tiempo de espera se agota Y está definido `--force`, recurre a `SIGKILL` / `taskkill /F`.
5. Idempotente: cerrar un Editor ya cerrado (o un proyecto cuyo Editor nunca estuvo en ejecución) sale con 0 y `no running Editor for project at <path>`.
6. Se niega a actuar sobre cualquier ruta que no sea la raíz de un proyecto de Unity (debe existir `ProjectSettings/ProjectVersion.txt`): protege contra invocaciones accidentales que maten todos los Unity del host.

> **Advertencia sobre Windows sin interfaz:** el paso de cierre ordenado usa `taskkill` (sin `/F`), que envía `WM_CLOSE`. Ese mensaje solo llega a procesos que poseen una ventana de nivel superior en el **mismo escritorio/sesión** que la CLI. Si Unity lo lanzó un servicio de Windows en la sesión 0 (o cualquier otro escritorio no interactivo), el cierre ordenado se descartará en silencio, se agotará `--timeout` y `--force` será la única vía para cerrar el Editor. Tenlo en cuenta en los runners de CI sin interfaz. Un Editor iniciado por la CLI (paso 0) no se ve afectado: se cierra mediante su plugin.

**Ejemplo — cerrar y recurrir a forzar tras 60s:**

```bash
unity-mcp-cli close ./MyGame --timeout 60 --force
```

**Ejemplo — cierre limpio al final de un script de automatización:**

```bash
unity-mcp-cli open ./MyGame
unity-mcp-cli wait-for-ready ./MyGame
unity-mcp-cli run-tool tests-run ./MyGame --input '{"testMode":"EditMode"}'
unity-mcp-cli close ./MyGame
```

## `tools`

Lista las herramientas de IA que ofrece el Editor de Unity del proyecto —la escena, los assets, las pruebas, las capturas de pantalla y todo lo que añaden las extensiones
instaladas— con lo que hace cada una y sus entradas, o muestra una herramienta en detalle (descripción completa, entradas, una llamada de
ejemplo, su esquema JSON de entrada). Inicia un editor sin interfaz cuando no hay ninguno en ejecución, como [`exec`](#exec). La lista se lee por MCP
(`tools/list`) y se guarda en el proyecto (ignorada por git), así que `--cached` responde al instante mientras el editor está cerrado.
Las herramientas ocultas (no ofrecidas a los clientes MCP) se listan por nombre; `run-tool` sigue pudiendo llamarlas.

```bash
unity-mcp-cli tools                 # every tool, one line each, with its inputs
unity-mcp-cli tools rql-mutate      # one tool: description, inputs, example call, schema
unity-mcp-cli tools screenshot      # a regex filter over names and descriptions
unity-mcp-cli tools --format json   # full schemas, machine-readable
```

| Opción | Obligatoria | Descripción |
|---|---|---|
| `[name]` | No | Un nombre de herramienta (detalles) o una regex (filtro) |
| `--path <path>` | No | Ruta del proyecto de Unity (por defecto: el directorio actual) |
| `--cached` | No | Responder con la lista guardada sin contactar con el editor |
| `--no-start` | No | No iniciar nunca un editor; usar la lista guardada cuando ninguno responde |
| `--once` | No | Cerrar el editor después si esta llamada lo inició |
| `--ready-timeout <ms>` | No | Esperar a un editor iniciado (por defecto: 300000) |
| `--url <url>` / `--token <token>` | No | Anulaciones del servidor |
| `--format <format>` | No | `text`, `json` o `ndjson` |

## `rql`

RQL desde la línea de comandos, sin escapado JSON: una consulta, o varias separadas por `;`. Una consulta cuya última etapa es `set` o `call` va a
`rql-mutate` (varias: cuando alguna lo es, y entonces todas deben serlo; cada una es su propio cambio de todo o nada, y la primera que falla detiene el resto), cualquier otra a `rql-query`; `--schema [type]` ejecuta `rql-schema`. Inicia un editor sin interfaz como `run-tool`.
Unity guarda un cambio en disco de inmediato, así que tras un `set` / `call` la CLI muestra los archivos de escena y de assets que escribió
(`Saved to disk: Assets/Scenes/Forest.unity`). Un operador escrito como en otro lenguaje (`startsWith`, `like`) recibe
una sugerencia con la forma de RQL. `rql --help` lista recetas.

```bash
unity-mcp-cli rql 'objects | where name starts-with "Tree" | count'
unity-mcp-cli rql 'objects | where name starts-with "Rock" | set transform.localScale = transform.localScale * 1.5'
```

| Opción | Obligatoria | Descripción |
|---|---|---|
| `[query]` | Sí, salvo con `--schema` / `-f` | La consulta RQL (entre comillas simples; las cadenas internas llevan comillas dobles) |
| `-f, --file <file>` | No | Leer la consulta desde un archivo (`-`: stdin), para un shell que elimina las comillas internas |
| `--schema [type]` | No | Listar las fuentes RQL, o los miembros de un tipo |
| `--dry-run` | No | Para `set` / `call`: mostrar qué cambiaría, sin cambiar nada |
| `--max-targets <n>` | No | Para `set` / `call`: más de 200 objetivos (hasta 1000) |
| `--path <path>` | No | Ruta del proyecto de Unity (por defecto: el directorio actual) |
| `--no-start` / `--once` | No | Como en `run-tool` (`--url`, `--token`, `--raw`, `--timeout`, `--ready-timeout` también funcionan) |
| `--format <format>` | No | `text`, `json` o `ndjson` |

## `run-tool`

Ejecuta una herramienta MCP directamente mediante la API HTTP. La URL del servidor y el token de autorización se **resuelven automáticamente** a partir del archivo de configuración del proyecto (`UserSettings/AI-Game-Developer-Config.json`), según el modo de conexión actual (Custom o Cloud).

```bash
unity-mcp-cli run-tool rql-mutate ./MyGame --input '{"query":"editor | call CreateGameObject({name: \"Cube\", primitive: \"Cube\"})"}'
```

| Opción | Obligatoria | Descripción |
|---|---|---|
| `<tool-name>` | Sí | Nombre de la herramienta MCP que se ejecutará |
| `[path]` | No | Ruta del proyecto de Unity (posicional o `--path`): se usa para leer la configuración y detectar el puerto |
| `--url <url>` | No | Anulación directa de la URL del servidor (omite la configuración) |
| `--token <token>` | No | Anulación del token bearer (omite la configuración) |
| `--input <json>` | No | Cadena JSON con los argumentos de la herramienta (por defecto `{}`) |
| `--input-file <file>` | No | Leer los argumentos JSON desde un archivo |
| `--raw` | No | Mostrar JSON sin procesar (sin formato, sin spinner) |
| `--format <format>` | No | `text` (por defecto), `json` o `ndjson`: consulta la [salida legible por máquina](#códigos-de-salida-y-salida-legible-por-máquina) |
| `--timeout <ms>` | No | Tiempo de espera de la solicitud en milisegundos (por defecto: 60000) |
| `--no-start` | No | No iniciar nunca un editor; fallar (salida 7) cuando ninguno responde |
| `--once` | No | Cerrar el editor tras la llamada si esta llamada lo inició (uso puntual en CI) |
| `--ready-timeout <ms>` | No | Cuánto esperar a que un editor iniciado esté listo (por defecto: 300000) |

**Inicio del editor.** Cuando no responde ningún editor del proyecto, `run-tool` inicia uno sin interfaz exactamente igual que
[`exec`](#exec) (concesión de sesión compartida, nunca cierra un editor que no inició), pero solo para un proyecto que indica su
versión de Unity en `ProjectSettings/ProjectVersion.txt`, de modo que nunca adivina un editor. `--no-start` lo desactiva;
`--once` hace que la llamada sea puntual: un editor que inició esta llamada se vuelve a cerrar tras la llamada (y también cuando nunca
llegó a estar listo). `run-system-tool` se comporta igual.

Una herramienta que responde con un error sale con 6; una herramienta desconocida con 2 (`NOT_FOUND`), un tiempo de espera agotado con 14, un editor inaccesible con 7, un editor
ocupado con 17, y una llamada bloqueada por un diálogo modal con 18; la tabla completa, con los estados HTTP, está en
[Códigos de salida](#códigos-de-salida-y-salida-legible-por-máquina).

**Prioridad de resolución de la URL:**
1. `--url` → se usa directamente
2. Archivo de configuración → `host` (modo Custom) o endpoint de nube fijo (modo Cloud)
3. Puerto determinista a partir de la ruta del proyecto

La **autorización** se lee automáticamente de la configuración del proyecto (`token` en modo Custom, `cloudToken` en modo Cloud). Usa `--token` para anular explícitamente el token derivado de la configuración.

**Ejemplo — llamar a una herramienta (URL y autenticación desde la configuración):**

```bash
unity-mcp-cli run-tool rql-query ./MyGame --input '{"query":"objects | where name == \"Player\" | select path name active"}'
```

**Ejemplo — anulación explícita de la URL:**

```bash
unity-mcp-cli run-tool rql-mutate --url http://localhost:8080 --input '{"query":"editor | call SaveScenes()"}'
```

**Ejemplo — canalizar la salida JSON sin procesar:**

```bash
unity-mcp-cli run-tool unity-tool-list ./MyGame --raw | jq .
```

**Ejemplo — llamada puntual en CI (inicia un editor sin interfaz y lo cierra después):**

```bash
unity-mcp-cli run-tool package-list ./MyGame --url http://localhost:28343 --once --format json
```

## `exec`

Ejecuta C# en el Editor de Unity y muestra el resultado. Si el proyecto ya está abierto (con ventana o sin interfaz), `exec` usa ese editor y nunca lo cierra. Si no, inicia un editor sin interfaz que se mantiene en caliente para las siguientes llamadas y se cierra solo tras `--idle` segundos sin llamadas a herramientas (guardando las escenas y assets modificados). El editor que inicia se registra en la concesión de sesión del proyecto (`.ai-game-dev/session.json`) y se inicia bajo un bloqueo de archivo, así que las llamadas `exec` en paralelo comparten un editor; `close --owned-only` lo cierra, y `open` (con ventana) lo cierra antes de abrir la ventana. Estos archivos de sesión son locales de la máquina: `.ai-game-dev/.gitignore` los mantiene fuera de git. Si el proyecto está abierto en un editor que no inició la CLI y su plugin no responde en 60 s (desconectado, o conectado a la nube de ai-game.dev), `exec` se detiene con una sugerencia en lugar de esperar: conecta el plugin, cambia su conexión a Custom (un servidor local) o pasa `--url`.

```bash
# The last expression is the result
unity-mcp-cli exec 'GameObject.FindObjectsOfType<Camera>().Length'

# From a file or stdin, with extra namespaces and helper files
unity-mcp-cli exec -f ./snippet.cs --using MyGame.Gameplay --include Helpers/
echo 'Application.unityVersion' | unity-mcp-cli exec
```

| Opción | Obligatoria | Descripción |
|---|---|---|
| `[code]` | No | Fragmento C#: sentencias simples, la última expresión es el resultado. Alternativamente `-f` o stdin. |
| `--path <path>` | No | Ruta del proyecto de Unity (por defecto: el directorio actual) |
| `-f, --file <file>` | No | Leer el código desde un archivo (`-` para stdin) |
| `--using <namespace>` | No | Namespace adicional, repetible |
| `--include <path>` | No | Archivo `.cs` auxiliar o carpeta (recursiva), relativa a `<Project>/.ai-game-dev/snippets`, repetible |
| `--window` | No | Iniciar el editor con ventana en lugar de sin interfaz (solo cuando aún no está en ejecución) |
| `--idle <seconds>` | No | Cerrar un editor iniciado por `exec` tras este número de segundos inactivo (por defecto: 300) |
| `--ready-timeout <ms>` | No | Cuánto esperar a que el editor esté listo (por defecto: 300000) |
| `--timeout <ms>` | No | Tiempo de espera de la llamada a la herramienta (por defecto: 310000) |
| `--raw` | No | Mostrar el JSON sin procesar de la respuesta de la herramienta |
| `--format <format>` | No | `text` (por defecto), `json` o `ndjson` |
| `--once` | No | Cerrar el editor tras la llamada si esta llamada lo inició |
| `--url <url>` | No | Anulación de la URL del servidor; una URL local también hace que un editor recién iniciado inicie ese servidor |
| `--token <token>` | No | Anulación del token bearer |

`exec` llama a la herramienta `script-execute`, así que los fragmentos, `usings` e `include` necesitan una versión del plugin con soporte de Code-Exec. Un error de compilación o una excepción sale con el código 6 y señala tus propias líneas.

> **¿No has iniciado sesión?** Un proyecto nuevo empieza en el modo de conexión **Cloud**. Sin haber iniciado sesión en ai-game.dev, `exec`,
> `run-tool`, `compile` y `tools` usan en su lugar el servidor MCP **local** del proyecto (lo indican en stderr): a un editor
> que inician se le indica que lo sirva, en memoria; la configuración del proyecto no cambia. Ejecuta `login` para trabajar a través de la nube.
>
> Un editor sin interfaz iniciado por la CLI sigue en ejecución cuando los scripts del proyecto no compilan
> (`-ignorecompilererrors`), para que `compile` pueda informar de los errores; su log es `Logs/unity-mcp-cli-editor.log`.

## `test`

Ejecuta las pruebas de Unity del proyecto como lo hace CI: un **editor en modo batch con el propio `-runTests` de Unity**; no intervienen ni el plugin AI Game
Developer ni el servidor MCP, así que funciona en un proyecto cuyo plugin está roto o falta. El veredicto
procede del informe NUnit que escribe la ejecución (un informe anterior en la misma ruta se elimina primero), nunca solo del
código de salida del editor.

```bash
unity-mcp-cli test ./MyGame                                  # EditMode, report in ./test-results.xml
unity-mcp-cli test ./MyGame --mode PlayMode --filter "MyGame.Tests.Combat" --junit-output junit.xml
unity-mcp-cli test ./MyGame --format github                  # ::error annotations at the failing test's source line
unity-mcp-cli test ./MyGame --format json --timeout 900 -- -nographics
```

| Opción | Obligatoria | Descripción |
|---|---|---|
| `[path]` | No | Ruta del proyecto de Unity (posicional o `--path`; por defecto: el directorio actual) |
| `--mode <mode>` | No | `EditMode` (por defecto) o `PlayMode` |
| `--filter <pattern>` | No | `-testFilter` de Unity: nombres completos de pruebas o una regex; `;` separa varios |
| `--category <names>` | No | `-testCategory` de Unity |
| `--assembly <names>` | No | `-assemblyNames` de Unity |
| `--output <file>` | No | Ruta del informe NUnit (por defecto: `<project>/Logs/test-results.xml`) |
| `--junit-output <file>` | No | Escribir también un informe JUnit |
| `--log-file <file>` | No | Log del editor (por defecto: la ruta del informe con `.log`) |
| `--timeout <seconds>` | No | Matar el editor (y sus procesos hijos) tras este tiempo (por defecto: 3600) |
| `--nographics` | No | Ejecutar sin GPU |
| `--unity <version>` / `--editor-path <path>` | No | Editor que se usará (por defecto: la versión del proyecto) |
| `--format <format>` | No | `text`, `json`, `ndjson` o `github` |
| `-- <args>` | No | Argumentos adicionales del editor; los que gestiona `test` (`-batchmode`, `-runTests`, `-quit`, ...) se rechazan |

**Códigos de salida:** `0` todo superado (una ejecución con cero pruebas se supera con un aviso), `8` pruebas fallidas, `6` sin veredicto (errores de
compilación —listados como `file(line,col): error CS...`—, un problema de licencia, un fallo, o el proyecto **ya está abierto en un
editor**: un segundo editor no puede abrirlo, así que usa `run-tool tests-run` en ese editor), `14` tiempo de espera agotado, `2` argumentos
incorrectos. Ctrl+C o un job de CI cancelado también detienen el editor.

## `compile`

"¿Compila mi C#?" contra el editor del proyecto: refresca los assets para que los scripts modificados se compilen, espera a la compilación
y a su recarga de dominio, e informa de los errores y avisos del compilador de la Console del editor con archivo, línea y
columna. Inicia un editor sin interfaz cuando no hay ninguno en ejecución (se mantiene en caliente para la siguiente llamada, como `exec`); una llamada contra un editor
en caliente tarda unos segundos.

```bash
unity-mcp-cli compile ./MyGame
unity-mcp-cli compile ./MyGame --strict --format github   # warnings fail too
```

| Opción | Obligatoria | Descripción |
|---|---|---|
| `[path]` | No | Ruta del proyecto de Unity (posicional o `--path`) |
| `--strict` | No | Fallar también con avisos del compilador |
| `--timeout <seconds>` | No | Esperar a la compilación y a la recarga de dominio (por defecto: 180) |
| `--ready-timeout <ms>` | No | Esperar a un editor iniciado (por defecto: 300000) |
| `--once` | No | Cerrar el editor después si esta llamada lo inició |
| `--url <url>` / `--token <token>` | No | Anulaciones del servidor, como en `exec` |
| `--format <format>` | No | `text`, `json`, `ndjson` o `github` |

**Códigos de salida:** `0` compila, `6` errores de compilación (o avisos con `--strict`), `7` no se pudo contactar con ningún editor, `14`
tiempo de espera agotado, `18` un diálogo modal bloquea el editor. Los diagnósticos van a stderr con la forma propia del compilador
`file(line,col): error CS0103: ...`.

## `dialog`

Un diálogo modal (una pregunta de guardado, "Enter Safe Mode?", `EditorUtility.DisplayDialog`) bloquea el hilo principal del editor, así que
cada llamada a herramienta espera hasta que alguien lo responde. `dialog` lista los diálogos del editor del proyecto y puede pulsar un
botón. `status`, `run-tool`, `exec`, `compile` y `wait-for-ready` informan de un diálogo así con el código de salida `18`
(`AWAITING_INPUT`) en lugar de un simple tiempo de espera agotado. Windows, macOS y Linux; un editor sin interfaz (`-batchmode`) nunca muestra
diálogos.

| Plataforma | Cómo | Requisito |
|---|---|---|
| **Windows** | Win32 (`EnumWindows`, `BM_CLICK`) mediante PowerShell | Ninguno |
| **macOS** | Scripting de interfaz de System Events mediante `osascript -l JavaScript`: las ventanas de diálogo y hojas del editor, sus textos y botones | **Permiso de Accesibilidad** para la aplicación que ejecuta la CLI (tu terminal o la aplicación de tu agente de IA): System Settings → Privacy & Security → Accessibility. Sin él, `dialog` falla con esa sugerencia y la comprobación de salida 18 no encuentra diálogos. |
| **Linux** | El bus de accesibilidad AT-SPI mediante un auxiliar `python3` (PyGObject): las ventanas de diálogo y alerta del editor (GTK, que usa `EditorUtility.DisplayDialog` de Unity), sus etiquetas y botones, pulsados mediante su acción `click`. Funciona en X11 y Wayland | `python3` con PyGObject y el typelib de AT-SPI (Debian/Ubuntu: `python3-gi gir1.2-atspi-2.0`; Fedora: `python3-gobject at-spi2-core`; Arch: `python-gobject at-spi2-core`) y un bus de accesibilidad en ejecución (`at-spi2-core`, que se inicia bajo demanda en GNOME, KDE y la mayoría de escritorios). Ejecuta la CLI en la sesión de escritorio que ejecuta el editor. Un paquete ausente, un bus inaccesible o la falta de sesión gráfica hacen que `dialog` falle con una sugerencia que indica la solución, y la comprobación de salida 18 no encuentra diálogos. |

Windows también lista las ventanas modales propias de Unity, como "Missing signature", que Unity 6.6 muestra antes de importar un
`.unitypackage` sin firmar (todos los paquetes de Asset Store): Unity dibuja su texto y sus botones por sí mismo, así que la lista muestra solo
su título y un botón, **Cancel**, que `--press Cancel` envía como la tecla Escape. Cualquier otra respuesta (Import Anyway)
corresponde al usuario, en el editor.

El id de un diálogo procede de la lista y no cambia mientras ese diálogo está abierto (Windows: su handle de ventana; macOS y
Linux: un hash de su pid, título, texto y botones). La etiqueta del botón nunca se inserta en un script; solo se pasa como dato.

En una máquina Linux sin interfaz gráfica, ejecuta el editor (sin `-batchmode`) y la CLI bajo `Xvfb` dentro de un mismo
`dbus-run-session`, con `at-spi2-core` instalado. Si el mismo usuario tiene también una sesión de escritorio, da a esa sesión
privada su propio `XDG_RUNTIME_DIR`: de lo contrario, su bus de accesibilidad sustituiría (y al salir eliminaría) el socket
`at-spi` del escritorio.

```bash
unity-mcp-cli dialog ./MyGame                          # list: title and buttons
unity-mcp-cli dialog ./MyGame --press "Don't Save"     # answer it
```

| Opción | Obligatoria | Descripción |
|---|---|---|
| `[path]` | No | Ruta del proyecto de Unity (posicional o `--path`) |
| `--press <button>` | No | Pulsar el botón con esta etiqueta (sin distinguir mayúsculas) |
| `--id <id>` | No | Con `--press`: el diálogo que se responderá (de la lista) |
| `--format <format>` | No | `text`, `json` o `ndjson` |

## `commands`

Todos los comandos con sus argumentos, opciones y valores predeterminados, además de la tabla de códigos de salida, leídos de la propia CLI, así que
nunca se desfasan. Pensado para agentes y herramientas.

```bash
unity-mcp-cli commands --format json
```

## `wait-for-ready`

Espera hasta que el Editor de Unity y el servidor MCP estén conectados y listos para aceptar llamadas a herramientas. Consulta el servidor a un intervalo configurable hasta que responde correctamente o se alcanza el tiempo de espera. Útil para scripts de automatización y la orquestación de agentes de IA, donde `open` lanza Unity pero el agente necesita saber cuándo puede empezar a llamar a herramientas.

```bash
unity-mcp-cli wait-for-ready ./MyGame
```

| Opción | Obligatoria | Descripción |
|---|---|---|
| `[path]` | No | Ruta del proyecto de Unity (posicional o `--path`): se usa para leer la configuración y detectar el puerto |
| `--url <url>` | No | Anulación directa de la URL del servidor (omite la configuración) |
| `--token <token>` | No | Anulación del token bearer (omite la configuración) |
| `--timeout <ms>` | No | Tiempo máximo de espera en milisegundos (por defecto: 120000) |
| `--interval <ms>` | No | Intervalo de sondeo en milisegundos (por defecto: 3000) |
| `--format <format>` | No | `text` (por defecto), `json` o `ndjson` |

Sale con `0` cuando está listo, `14` si se agota el tiempo de espera y `18` cuando un diálogo modal bloquea el editor.

**Ejemplo — esperar con el tiempo de espera predeterminado (120s):**

```bash
unity-mcp-cli open ./MyGame
unity-mcp-cli wait-for-ready ./MyGame
unity-mcp-cli run-tool tests-run ./MyGame --input '{"testMode":"EditMode"}'
```

**Ejemplo — tiempo de espera más corto para CI:**

```bash
unity-mcp-cli wait-for-ready ./MyGame --timeout 60000 --interval 2000
```

**Ejemplo — URL de servidor explícita:**

```bash
unity-mcp-cli wait-for-ready --url http://localhost:8080 --timeout 30000
```

## `setup-mcp`

Escribe archivos de configuración MCP para agentes de IA, lo que permite una configuración sin interfaz/en CI sin la interfaz del Editor de Unity. Admite todos los agentes que muestra `setup-mcp --list` (Claude Code, Cursor, Gemini, Codex y más).

La configuración se **fija** a este proyecto mediante una URL por proyecto (`https://ai-game.dev/mcp/p/<pin>` para el transporte http, o un argumento `project=<pin>` para stdio). Para el transporte http de Cloud también incluye la **clave de proyecto** de este proyecto (`Authorization: Bearer agd_pk_…`) para cada agente: una credencial sin caducidad y revocable, vinculada solo a este proyecto, reutilizada desde `~/.ai-game-dev/project-keys.json` o emitida con la cuenta con la que iniciaste sesión mediante [`login`](#login). Sin haber iniciado sesión (o si no se puede obtener la clave), la configuración solo contiene la URL y el agente inicia sesión con su propio OAuth. Pasa `--oauth` para escribir siempre la configuración solo con URL, `--regenerate-key` para sustituir la clave (las demás configuraciones de agente de este proyecto que llevan la clave anterior se pasan a la nueva y se muestran, y después se revoca la clave anterior; si alguna no se puede reescribir, la clave anterior se deja activa y un aviso la nombra), `--token` para escribir en su lugar un PAT explícito, o `--no-pin` para el endpoint compartido sin fijar. Las configuraciones stdio y de servidor local nunca llevan clave de proyecto. Antigravity lee `~/.gemini/config/mcp_config.json` o `~/.gemini/antigravity/mcp_config.json` según la instalación, así que se escriben ambos archivos y se muestra cada ruta escrita; si alguno no se puede escribir, el comando falla. Esta salida coincide con el botón **Configure** del Editor de Unity.

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame
```

| Opción | Obligatoria | Descripción |
|---|---|---|
| `[agent-id]` | Sí | Agente que se configurará (usa `--list` para verlos todos) |
| `[path]` | No | Ruta del proyecto de Unity (por defecto, el cwd) |
| `--transport <transport>` | No | Método de transporte: `stdio` o `http` (por defecto: `http`) |
| `--url <url>` | No | Anulación de la URL del servidor (para el transporte http) |
| `--token <token>` | No | PAT explícito: se escribe como cabecera `Authorization` en lugar de la clave de proyecto |
| `--oauth` | No | Escribir una configuración de Cloud solo con URL (el agente inicia sesión con su propio OAuth) en lugar de la clave de proyecto |
| `--regenerate-key` | No | Emitir una clave de proyecto nueva, reescribir la configuración y revocar la clave anterior (solo http de Cloud) |
| `--no-pin` | No | Escribir una URL sin fijar / omitir el argumento `project=` (por defecto: fijar a este proyecto mediante `/mcp/p/<pin>`) |
| `--list` | No | Listar todos los ID de agente disponibles |

**Ejemplo — listar todos los agentes compatibles:**

```bash
unity-mcp-cli setup-mcp --list
```

**Ejemplo — configurar Cursor con transporte stdio:**

```bash
unity-mcp-cli setup-mcp cursor ./MyGame --transport stdio
```

**Ejemplo — sustituir la clave de este proyecto (la clave antigua deja de funcionar):**

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame --regenerate-key
```

**Ejemplo — escribir una configuración sin fijar (endpoint compartido `/mcp`):**

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame --no-pin
```

## `setup-skills`

Escribe archivos de skills para un agente de IA. Siempre, sin conexión (sin editor, sin servidor): la skill `unity-mcp-cli`, que explica cómo un agente
usa esta CLI desde un arranque en frío (el primer comando de editor inicia un editor sin interfaz; recetas de `rql`; `compile` / `test`;
`--format json`; códigos de salida y `dialog`), con `references/rql.md` y `references/install.md` para profundizar, además de
`ai-game-dev-app`. Las recetas proceden de la misma fuente que `--help`, así que ambas no pueden desfasarse. Después, cuando el
editor del proyecto responde, también pide al plugin sus skills por herramienta; un editor cerrado no es un error.

```bash
unity-mcp-cli setup-skills claude-code ./MyGame
```

| Opción | Obligatoria | Descripción |
|---|---|---|
| `[agent-id]` | Sí | Agente para el que se generarán las skills (usa `--list` para verlos todos) |
| `[path]` | No | Ruta del proyecto de Unity (por defecto, el cwd) |
| `--url <url>` | No | Anulación de la URL del servidor |
| `--token <token>` | No | Anulación del token de autenticación |
| `--list` | No | Listar todos los agentes con su estado de soporte de skills |
| `--offline` | No | Escribir solo la skill `unity-mcp-cli`; no consultar al editor |
| `--timeout <ms>` | No | Tiempo de espera de la solicitud en milisegundos (por defecto: 60000) |

**Ejemplo — listar los agentes con soporte de skills:**

```bash
unity-mcp-cli setup-skills --list
```

## `remove-plugin`

Elimina el plugin Unity-MCP del `Packages/manifest.json` de un proyecto de Unity.

```bash
unity-mcp-cli remove-plugin ./MyGame
```

| Opción | Obligatoria | Descripción |
|---|---|---|
| `[path]` | Sí | Ruta al proyecto de Unity (posicional o `--path`) |

Este comando:
1. Elimina `com.ivanmurzak.unity.mcp` de `dependencies`
2. **Conserva los registros con ámbito y los ámbitos**: otros paquetes pueden depender de ellos
3. **No hace nada** si el plugin no está instalado

> Después de ejecutar este comando, abre el proyecto en el Editor de Unity para aplicar el cambio.

## `status`

Comprueba el estado de conexión del Editor de Unity y del servidor MCP. Muestra si Unity está en ejecución, si el servidor MCP local es accesible y si el servidor resuelto a partir de la configuración (p. ej., la nube) es accesible.

```bash
unity-mcp-cli status ./MyGame
```

| Opción | Obligatoria | Descripción |
|---|---|---|
| `[path]` | No | Ruta del proyecto de Unity (posicional o `--path`) |
| `--url <url>` | No | Anulación directa de la URL del servidor (omite la configuración) |
| `--token <token>` | No | Anulación del token bearer (omite la configuración) |
| `--timeout <ms>` | No | Tiempo de espera de la comprobación en milisegundos (por defecto: 5000) |
| `--format <format>` | No | `text` (por defecto), `json` o `ndjson` |

Sale con `0` cuando el servidor MCP responde, `7` cuando no responde y `18` cuando un diálogo modal bloquea el editor (listado en
la salida; consulta [`dialog`](#dialog)).

**Ejemplo de salida:**

```
Unity-MCP Status
  Project: /path/to/MyGame
──────────────────────────────────────────────────
Unity Editor Process
✔  Unity is running (PID: 53740)
Local MCP Server
  URL: http://localhost:22958
✖  Not available (connection refused)
Config Server
  URL: https://ai-game.dev/mcp
✔  Connected
──────────────────────────────────────────────────
✔  MCP server is reachable — ready for tool calls
```

## Opciones globales

Estas opciones están disponibles en todos los comandos:

| Opción | Descripción |
|---|---|
| `-v, --verbose` | Activar la salida de diagnóstico detallada para la resolución de problemas |
| `--version` | Mostrar la versión de la CLI |
| `--help` | Mostrar la ayuda del comando |

**Ejemplo — ejecutar cualquier comando con salida detallada:**

```bash
unity-mcp-cli install-plugin ./MyGame --verbose
```

## Códigos de salida y salida legible por máquina

`unity-mcp-cli`, `unreal-mcp-cli` y `godot-cli` comparten una misma tabla de códigos de salida, de modo que un script o un agente puede distinguir "corrige la
línea de comandos" de "el editor falló" y de "las pruebas fallaron" sin leer los mensajes:

| Código | Nombre | Significado |
|---|---|---|
| `0` | `SUCCESS` | El comando se completó correctamente |
| `1` | `ERROR` | Error sin clasificar |
| `2` | `USAGE` | Argumentos incorrectos, una herramienta desconocida; no se ejecutó nada. Corrige la línea de comandos; no la reintentes sin cambios |
| `6` | `COMMAND_FAILED` | El comando se ejecutó y falló (error de herramienta, errores de compilación, ejecución de pruebas sin veredicto, credenciales rechazadas) |
| `7` | `UNREACHABLE` | No se pudo iniciar ni contactar con el editor o el servidor MCP; reintenta cuando el entorno esté corregido |
| `8` | `TESTS_FAILED` | Las pruebas se ejecutaron y algunas fallaron |
| `14` | `TIMEOUT` | Se agotó el tiempo de espera antes de obtener un resultado |
| `17` | `BUSY` | El editor está ocupado y no ejecutó la llamada; reintenta más tarde |
| `18` | `AWAITING_INPUT` | Un diálogo modal bloquea el editor; una persona debe responderlo (consulta [`dialog`](#dialog)) |

`--format json` (en `run-tool`, `run-system-tool`, `exec`, `test`, `compile`, `dialog`, `status`, `wait-for-ready` y
`commands`) imprime exactamente un sobre de resultado en stdout; todo lo demás va a stderr. `--format ndjson` imprime el
mismo sobre como una sola línea y emite líneas de progreso (`{"type":"progress",...}`) en stderr. Un error de línea de comandos con
`--format json` sigue imprimiendo un sobre (código `USAGE`).

```json
{
  "success": false,
  "command": "test",
  "data": { "verdict": "failed", "summary": { "total": 3, "passed": 2, "failed": 1 } },
  "errors": [{ "code": "TESTS_FAILED", "message": "1 test(s) failed." }],
  "warnings": []
}
```

`errors[].code` es un token estable (`USAGE`, `NOT_FOUND`, `NOT_AUTHENTICATED`, `TOOL_FAILED`, `UNREACHABLE`, `TIMEOUT`,
`BUSY`, `AWAITING_INPUT`, `TESTS_FAILED`, `TEST_RUN_ERROR`, `TEST_TIMED_OUT`, `COMPILATION_FAILED`, `PROJECT_OPEN`,
`ERROR`, ...); compara con él, nunca con el mensaje.

**Una llamada a herramienta que no tuvo éxito** (`run-tool`, `run-system-tool`, `exec`, `rql`, `tools`) se asigna al mismo código de salida
y código de error en las tres CLI (la tabla está en `@baizor/gamedev-cli-core`; `commands --format json` la lista como
`errorCodes`). Cuando el servidor MCP respondió, decide su estado HTTP, y el error del sobre lo incluye:
`errors[{code, message, httpStatus}]`.

| Condición | Estado HTTP | Salida | `errors[].code` |
|---|---|---|---|
| Argumentos incorrectos: una opción incorrecta, argumentos de herramienta que la CLI o el servidor rechazaron (cualquier 4xx no listado aquí) | 400 (un 422 `[invalid_params]`) | `2` | `USAGE` |
| No existe ninguna herramienta con ese nombre (lístalas con `tools`), o no hay ningún proyecto en la ruta | 404 | `2` | `NOT_FOUND` |
| No hay credenciales utilizables, o el servidor las rechazó: inicia sesión con `login` | 401, 403 | `6` | `NOT_AUTHENTICATED` |
| La herramienta se ejecutó e informó de un error (`status: "error"`, cualquier 5xx no listado aquí) | 409, 422, 500 | `6` | `TOOL_FAILED` |
| El código del proyecto no compiló, así que no se inició ningún editor | — | `6` | `COMPILATION_FAILED` |
| Conexión rechazada o restablecida, ningún plugin de motor conectado al servidor, plugin perdido después de enviar la llamada | 502 | `7` | `UNREACHABLE` |
| Sin respuesta a tiempo: `--timeout`, o el propio tiempo de espera de herramienta del servidor | 408, 504 | `14` | `TIMEOUT` |
| Ocupado y no ejecutó la llamada (recargando, con límite de tasa, otra llamada de la CLI tiene el editor); reintenta más tarde | 503, 429 | `17` | `BUSY` |
| Un diálogo modal bloquea el editor | — | `18` | `AWAITING_INPUT` |
| Cualquier otra cosa (la llamada se interrumpió, un error inesperado) | — | `1` | `ERROR` |

# Ejemplo completo de automatización

Configura un proyecto Unity MCP completo desde cero en un solo script:

```bash
# 1. Create a new Unity project
unity-mcp-cli create-project ./MyAIGame --unity 6000.3.1f1

# 2. Install the Unity-MCP plugin
unity-mcp-cli install-plugin ./MyAIGame

# 3. Enable all MCP tools
unity-mcp-cli configure ./MyAIGame --enable-all-tools

# 4. Sign in to ai-game.dev (browser OAuth; credential saved to ~/.ai-game-dev)
unity-mcp-cli login

# 5. Open the project (auto-connects and generates skills for claude-code)
unity-mcp-cli open ./MyAIGame

# 6. Wait for Unity Editor and MCP server to be ready
unity-mcp-cli wait-for-ready ./MyAIGame

# 7. Check that the scripts compile, then close the editor
unity-mcp-cli compile ./MyAIGame
unity-mcp-cli close ./MyAIGame

# 8. Run the tests in a batch-mode editor (no plugin needed); JUnit for the CI dashboard
unity-mcp-cli test ./MyAIGame --junit-output junit.xml
```

# Cómo funciona

### Puerto determinista

La CLI genera un **puerto determinista** para cada proyecto de Unity a partir de la ruta de su directorio (hash SHA256 asignado al rango de puertos 20000–29999). Coincide con la generación de puertos del plugin de Unity, de modo que el servidor y el plugin acuerdan automáticamente el mismo puerto sin configuración manual.

### Instalación del plugin

El comando `install-plugin` modifica `Packages/manifest.json` directamente:
- Añade el registro con ámbito de [OpenUPM](https://openupm.com/) (`package.openupm.com`)
- Registra todos los ámbitos necesarios (`com.ivanmurzak`, `extensions.unity`)
- Añade la dependencia `com.ivanmurzak.unity.mcp` con actualizaciones conscientes de la versión (nunca baja de versión)

### Archivo de configuración

El comando `configure` lee y escribe `UserSettings/AI-Game-Developer-Config.json`, que controla:
- **Tools**: herramientas MCP disponibles para los agentes de IA
- **Prompts**: prompts predefinidos que se inyectan en las conversaciones con el LLM
- **Resources**: datos de solo lectura expuestos a los agentes de IA
- **Configuración de conexión**: URL del host, token de autenticación, método de transporte, tiempos de espera

### Integración con Unity Hub

Los comandos que gestionan editores o crean proyectos usan la **CLI de Unity Hub** (modo `--headless`). Si Unity Hub no está instalado, la CLI **lo descarga e instala automáticamente**:
- **Windows**: instalación silenciosa mediante `UnityHubSetup.exe /S` (puede requerir privilegios de administrador)
- **macOS**: descarga el DMG, lo monta y copia `Unity Hub.app` a `/Applications`
- **Linux**: descarga `UnityHub.AppImage` en `~/Applications/`

# API de biblioteca

Además del binario de la CLI, `unity-mcp-cli` expone sus comandos principales como una biblioteca tipada y sin efectos secundarios, para que otras herramientas de Node.js / TypeScript puedan integrar el mismo flujo de instalación / configuración sin invocar el shell.

```ts
import { installPlugin, removePlugin, configure, setupMcp } from 'unity-mcp-cli';

const result = await installPlugin({
  unityProjectPath: './MyUnityProject',
  // version: 'x.y.z',        // optional — defaults to latest from OpenUPM
  onProgress: (event) => {
    // phase is one of: 'start' | 'dependencies-resolved' | 'manifest-patched' | 'done'
    console.log(event.phase, event.message);
  },
});

if (!result.success) {
  console.error('Install failed:', result.error?.message);
  return;
}

console.log(`Installed v${result.installedVersion}`);
for (const warning of result.warnings) console.warn(warning);
for (const step of result.nextSteps) console.log(step);
```

Cada función devuelve un objeto de resultado tipado `{ success, ... }`; los errores nunca se lanzan más allá del límite público. La entrada de la biblioteca no tiene efectos secundarios de nivel superior: `import 'unity-mcp-cli'` nunca analiza argv ni escribe en stdout o stderr.

Consulta [`CHANGELOG.md`](../CHANGELOG.md) para ver la lista completa de funciones y tipos exportados.
