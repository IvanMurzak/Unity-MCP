[English](mcp-server.md) | [中文](mcp-server.zh-CN.md) | [日本語](mcp-server.ja.md) | <b>Español</b> | [فارسی](mcp-server.fa.md) | [हिन्दी](mcp-server.hi.md) | [العربية](mcp-server.ar.md)

# Servidor MCP y conexión

Cómo llega el plugin a un servidor MCP, las variables de entorno que anulan su configuración y cómo ejecutar un servidor por tu cuenta. Volver al [README](README.es.md).

<div align="center">

`AI agent` ↔ **`MCP server`** ↔ `Unity plugin (Editor or game)`

</div>

El servidor no está en este repositorio. Es el **[GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server)** compartido e independiente del motor (binario `gamedev-mcp-server`, imagen Docker `aigamedeveloper/mcp-server`), que también usan Godot-MCP y Unreal-MCP. Su README es la referencia de todas las opciones del servidor. El agente se conecta al servidor mediante `stdio` o `streamableHttp`; el plugin se conecta a él con SignalR.

## Modos de conexión

El modo se elige en la ventana de AI Game Developer (**Window ▸ AI Game Dev**) y se guarda en `UserSettings/AI-Game-Developer-Config.json`.

| Modo | Servidor | Notas |
| --- | --- | --- |
| **Cloud** (predeterminado) | `https://ai-game.dev/mcp` | Inicia sesión con la ventana o con `unity-mcp-cli login`. Nunca inicia un servidor local, aunque `keepServerRunning` sea `true`. |
| **Custom** | `gamedev-mcp-server` local, o cualquier URL que alojes | El campo guardado `host` solo se usa en este modo. Para usar un servidor local, selecciona **Custom** en la ventana, o define `"connectionMode": "Custom"` con Unity cerrado. |

**Por qué Cloud es el predeterminado:** no necesita puertos abiertos, evita conflictos de puertos con otros programas y es gratuito. El relay guarda solo metadatos (IP, hora, nombre de la herramienta, tamaños) durante 90 días y nunca almacena el contenido de las solicitudes ni de las respuestas. **Custom** es una opción igual de válida para trabajar sin conexión o en entornos aislados (air-gapped).

En modo Custom con un servidor local, el plugin descarga la versión del servidor fijada por `ServerVersion` en `McpServerManager.cs` en `Library/mcp-server/<rid>/` y la inicia. **Tools ▸ AI Game Developer ▸ Server** tiene *Download Binaries* y *Open Server Logs*.

## Variables de entorno del plugin

Se leen al iniciar el Editor. Anulan la configuración guardada **solo en memoria** y nunca se escriben de vuelta, así que la ventana sigue mostrando la opción guardada. Los argumentos de línea de comandos tienen prioridad sobre las variables de entorno. Cada variable también se acepta como argumento de línea de comandos `-NAME=value` (por ejemplo `-UNITY_MCP_TRANSPORT=stdio`); `-url`, `-auth` y `-token` son alias cortos. Las filas marcadas con *solo env* no tienen argumento.

| Variable | Valores | Efecto |
| --- | --- | --- |
| `UNITY_MCP_CLOUD_URL` | URL | URL del servidor MCP (`UNITY_MCP_HOST` es un alias heredado) |
| `UNITY_MCP_CONNECTION_MODE` | `Cloud` / `Custom` | Modo de conexión (una URL de loopback implica `Custom`) |
| `UNITY_MCP_KEEP_CONNECTED` | `true` / `false` | Mantener la conexión activa |
| `UNITY_MCP_AUTH_OPTION` | `none` / `oauth` / `token` | Modo de autenticación (el heredado `required` pasa a ser `token`) |
| `UNITY_MCP_TOKEN` | cadena | Token de autenticación |
| `UNITY_MCP_TRANSPORT` | `stdio` / `streamableHttp` | Transporte de cliente que configura el plugin |
| `UNITY_MCP_START_SERVER` | `true` / `false` | Mantener en ejecución un proceso de servidor local |
| `UNITY_MCP_TOOLS` | ids de herramientas separados por comas | Mostrar solo estas herramientas; el resto se ocultan (siguen pudiendo llamarse por nombre). Los ids desconocidos se registran como errores. |
| `UNITY_MCP_CREDENTIALS_DIR` | ruta absoluta de carpeta | *Solo env.* Leer el almacén de credenciales de la máquina desde esta carpeta en lugar de `~/.ai-game-dev` (Editores de desarrollo y de pruebas). Un valor relativo se ignora. |
| `UNITY_MCP_SERVER_PATH` | ruta absoluta a un `gamedev-mcp-server` | *Solo env.* Lanzar este binario en lugar de la versión fijada (ver más abajo) |
| `UNITY_MCP_DISABLE` | `1` / `true` | *Solo env.* Omitir la inicialización del plugin; consulta [Desactivar en CI](ci-disable.es.md) |

Ejemplo, modo batch contra un servidor local con un token:

```bash
Unity.exe -batchmode -nographics \
  -UNITY_MCP_CLOUD_URL=http://localhost:8080 \
  -UNITY_MCP_KEEP_CONNECTED=true \
  -UNITY_MCP_AUTH_OPTION=token \
  -UNITY_MCP_TOKEN=my-secret-token
```

### `UNITY_MCP_SERVER_PATH` (desarrollo del servidor y CI)

El Editor lanza el archivo indicado en lugar de la versión fijada y omite la descarga y la comprobación de versión. Úsalo al desarrollar el servidor o cuando CI compila el servidor desde el código fuente.

- Se lee del entorno del proceso y después de un archivo `.env` en la raíz del proyecto de Unity, para que un Editor iniciado desde la GUI o desde un IDE pueda recogerlo.
- Usa una ruta **absoluta**. Si el archivo no existe, el plugin se comporta en silencio como si la variable no estuviera definida; la línea de consola `Starting MCP server: <path> …` indica el binario que se lanzó realmente.
- La anulación también pasa a ser el `command` en las configuraciones de agente generadas, y *Open Server Logs* la sigue. *Download Binaries* sigue descargando en `Library/mcp-server/<rid>/`.

## Ejecutar un servidor por tu cuenta

Úsalo para una configuración autoalojada o remota. Descarga un zip de versión (`gamedev-mcp-server-<rid>.zip`) desde las [versiones de GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server/releases), o usa [Docker](DOCKER_DEPLOYMENT.es.md).

```bash
# stdio: the agent starts the server itself
./gamedev-mcp-server --port 8080 --client-transport stdio

# HTTP: run it as a standalone process
./gamedev-mcp-server --port 8080 --client-transport streamableHttp
```

Configuración del agente para stdio (sustituye la ruta; en Windows el archivo termina en `.exe`):

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "command": "<project>/Library/mcp-server/win-x64/gamedev-mcp-server.exe",
      "args": ["--port=8080", "--client-transport=stdio"]
    }
  }
}
```

El plugin debe usar el mismo puerto: elige el modo **Custom** y el mismo host y puerto en la ventana de AI Game Developer.

### Opciones comunes del servidor

Los argumentos de línea de comandos anulan las variables de entorno. Los argumentos también funcionan con un solo `-` o sin ninguno (`port=8080`). Valores predeterminados y lista completa: [configuración de GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server#configuration).

| Variable de entorno | Argumento | Significado |
| --- | --- | --- |
| `MCP_PLUGIN_PORT` | `--port` | Puerto tanto para el agente (HTTP) como para el plugin (predeterminado `8080`) |
| `MCP_PLUGIN_CLIENT_TRANSPORT` | `--client-transport` | Transporte del agente: `stdio` o `streamableHttp` |
| `MCP_PLUGIN_CLIENT_TIMEOUT` | `--plugin-timeout` | Tiempo de espera de las solicitudes de catálogo, prompts y recursos al plugin (ms) |
| `MCP_PLUGIN_TOOL_TIMEOUT` | `--tool-timeout` | Cuánto puede durar una llamada a herramienta (ms). Una llamada se envía una vez y nunca se reenvía. |
| `MCP_AUTH` (o `MCP_AUTHORIZATION`) | `--auth` (o `--authorization`) | `none`, `token` u `oauth` |
| `MCP_PLUGIN_TOKEN` | `--token` | Secreto compartido para el modo `token`; el agente y el plugin deben enviarlo ambos |
| `MCP_BIND` | `--bind` | Dirección de enlace: `loopback` (predeterminado), `any` o una IP |
| `MCP_PLUGIN_IDLE_TIMEOUT_SECONDS` | `--idle-timeout-seconds` | Cerrar las sesiones streamableHttp inactivas tras este número de segundos |

Los despliegues alojados también pueden llamar a webhooks en eventos de herramientas, prompts, recursos y conexiones, y autorizar conexiones entrantes: `MCP_PLUGIN_WEBHOOK_TOOL_URL`, `MCP_PLUGIN_WEBHOOK_PROMPT_URL`, `MCP_PLUGIN_WEBHOOK_RESOURCE_URL`, `MCP_PLUGIN_WEBHOOK_CONNECTION_URL`, `MCP_PLUGIN_WEBHOOK_AUTHORIZATION_URL`, `MCP_PLUGIN_WEBHOOK_AUTHORIZATION_FAIL_OPEN`, `MCP_PLUGIN_WEBHOOK_TOKEN`, `MCP_PLUGIN_WEBHOOK_HEADER` y `MCP_PLUGIN_WEBHOOK_TIMEOUT` (todas opcionales, sin definir por defecto).
