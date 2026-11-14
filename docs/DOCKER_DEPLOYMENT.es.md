[English](DOCKER_DEPLOYMENT.md) | [中文](DOCKER_DEPLOYMENT.zh-CN.md) | [日本語](DOCKER_DEPLOYMENT.ja.md) | <b>Español</b> | [فارسی](DOCKER_DEPLOYMENT.fa.md) | [हिन्दी](DOCKER_DEPLOYMENT.hi.md) | [العربية](DOCKER_DEPLOYMENT.ar.md)

# Despliegue con Docker

Ejecuta el servidor MCP en un contenedor, para alojamiento en la nube o un entorno aislado. Volver al [README](README.es.md) · [Servidor MCP y conexión](mcp-server.es.md).

- **Imagen:** [`aigamedeveloper/mcp-server`](https://hub.docker.com/r/aigamedeveloper/mcp-server), compilada a partir del repositorio compartido [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server)
- **Etiquetas:** `latest` y versiones de lanzamiento `X.Y.Z`
- **Puerto:** `8080` (expuesto)

Sigues necesitando el plugin [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP#quick-start) en tu proyecto de Unity, configurado en modo **Custom** con la URL del servidor (consulta los [modos de conexión](mcp-server.es.md#modos-de-conexión)).

![Docker launch](img/editor/docker-launch.gif)

## HTTP (autoalojado, accesible desde fuera del contenedor)

El servidor se enlaza a loopback de forma predeterminada, que no es accesible a través de un puerto publicado. Define `MCP_BIND=any`:

```bash
docker run -p 8080:8080 \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp \
  -e MCP_PLUGIN_PORT=8080 \
  -e MCP_BIND=any \
  aigamedeveloper/mcp-server
```

Configuración del agente:

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "url": "http://localhost:8080"
    }
  }
}
```

Sustituye la URL por tu endpoint real cuando el servidor se ejecute en otro lugar.

### Con un token compartido

El modo `token` exige un mismo secreto bearer tanto del agente como del plugin de Unity (`UNITY_MCP_TOKEN`, o el campo de token en la ventana de AI Game Developer). El secreto viaja por HTTP sin cifrar a menos que TLS termine delante del servidor.

```bash
docker run -p 8080:8080 \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp -e MCP_PLUGIN_PORT=8080 -e MCP_BIND=any \
  -e MCP_AUTH=token -e MCP_PLUGIN_TOKEN=<secret> \
  aigamedeveloper/mcp-server
```

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "url": "http://localhost:8080",
      "headers": { "Authorization": "Bearer <secret>" }
    }
  }
}
```

### Puerto personalizado

Publica el mismo puerto que definiste en `MCP_PLUGIN_PORT`:

```bash
docker run -p 9090:9090 \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp -e MCP_PLUGIN_PORT=9090 -e MCP_BIND=any \
  aigamedeveloper/mcp-server
```

## stdio (el agente inicia el contenedor)

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "command": "docker",
      "args": [
        "run", "-i", "--rm",
        "-e", "MCP_PLUGIN_CLIENT_TRANSPORT=stdio",
        "-e", "MCP_BIND=any",
        "-p", "8080:8080",
        "aigamedeveloper/mcp-server"
      ]
    }
  }
}
```

El agente se comunica con el servidor a través de stdin y stdout del contenedor; el puerto publicado es para el plugin de Unity.

## Configuración

Todas las opciones del servidor son variables de entorno (`-e NAME=value`). Las más comunes se enumeran en [Servidor MCP y conexión](mcp-server.es.md#opciones-comunes-del-servidor); la lista completa, incluidos el modo OAuth y las sesiones respaldadas por Redis, está en el [README de GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server#configuration).
