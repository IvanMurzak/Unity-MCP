# Configuration Guide

This guide covers all configuration options for Unity-MCP, from basic setup to advanced deployment scenarios. Configure both the Unity Plugin and MCP Server to optimize performance for your specific needs.

## 🎯 Configuration Overview

Unity-MCP has two main components that can be configured:

1. **Unity Plugin** - Configured within Unity Editor
2. **MCP Server** - Configured via environment variables or command-line arguments

Both components need to be aligned for proper communication.

## 🔧 Unity Plugin Configuration

### Basic Settings

Access Unity-MCP settings through **Window → Unity MCP → Settings**.

#### Connection Settings
| Setting | Default | Description |
|---------|---------|-------------|
| **Server Port** | `8080` | Port number for connecting to MCP Server |
| **Auto Connect** | `true` | Automatically connect to server when Unity starts |
| **Connection Timeout** | `10000ms` | How long to wait for server connection |
| **Reconnect Interval** | `5000ms` | Time between reconnection attempts |
| **Max Reconnect Attempts** | `5` | Maximum automatic reconnection tries |

#### Performance Settings
| Setting | Default | Description |
|---------|---------|-------------|
| **Enable Threading** | `true` | Use background threads for non-Unity operations |
| **Batch Size** | `100` | Maximum items to process in single batch |
| **Update Frequency** | `30fps` | How often to check for server messages |
| **Memory Limit** | `256MB` | Maximum memory usage for plugin operations |

#### Debug Settings
| Setting | Default | Description |
|---------|---------|-------------|
| **Verbose Logging** | `false` | Enable detailed debug messages |
| **Log Network Traffic** | `false` | Log all network communication |
| **Performance Monitoring** | `false` | Track and report performance metrics |

### Advanced Unity Configuration

#### Custom Settings File
Create a custom configuration file at `Assets/Unity-MCP/config.json`:

```json
{
  "connection": {
    "serverPort": 8080,
    "autoConnect": true,
    "connectionTimeout": 10000,
    "reconnectInterval": 5000,
    "maxReconnectAttempts": 5
  },
  "performance": {
    "enableThreading": true,
    "batchSize": 100,
    "updateFrequency": 30,
    "memoryLimit": 268435456
  },
  "debug": {
    "verboseLogging": false,
    "logNetworkTraffic": false,
    "performanceMonitoring": false
  },
  "security": {
    "allowRemoteConnections": false,
    "enableSslVerification": true,
    "trustedHosts": ["localhost", "127.0.0.1"]
  }
}
```

#### Programmatic Configuration
Configure settings via C# code:

```csharp
using Unity.MCP.Plugin;

public class CustomMcpConfiguration : MonoBehaviour
{
    void Start()
    {
        var config = McpPluginSettings.Instance;
        
        // Connection settings
        config.ServerPort = 8080;
        config.AutoConnect = true;
        config.ConnectionTimeout = 15000; // 15 seconds
        
        // Performance settings  
        config.EnableThreading = true;
        config.BatchSize = 50; // Smaller batches for real-time games
        
        // Debug settings
        config.VerboseLogging = Application.isEditor;
        
        // Apply configuration
        config.SaveSettings();
    }
}
```

### Environment-Specific Configurations

#### Development Environment
```json
{
  "connection": {
    "serverPort": 8080,
    "autoConnect": true,
    "connectionTimeout": 30000
  },
  "debug": {
    "verboseLogging": true,
    "logNetworkTraffic": true,
    "performanceMonitoring": true
  }
}
```

#### Production/Build Environment
```json
{
  "connection": {
    "serverPort": 8080,
    "autoConnect": false,
    "connectionTimeout": 5000
  },
  "debug": {
    "verboseLogging": false,
    "logNetworkTraffic": false,
    "performanceMonitoring": false
  }
}
```

## ⚙️ MCP Server Configuration

### Environment Variables

Configure the server using environment variables:

| Variable | Default | Description |
|----------|---------|-------------|
| `UNITY_MCP_PORT` | `8080` | Server listening port |
| `UNITY_MCP_CLIENT_TRANSPORT` | `http` | Transport protocol (`http` or `stdio`) |
| `UNITY_MCP_PLUGIN_TIMEOUT` | `10000` | Unity Plugin connection timeout (ms) |
| `UNITY_MCP_MAX_CONNECTIONS` | `10` | Maximum concurrent client connections |
| `UNITY_MCP_LOG_LEVEL` | `info` | Logging level (`debug`, `info`, `warn`, `error`) |
| `UNITY_MCP_ENABLE_CORS` | `true` | Enable Cross-Origin Resource Sharing |
| `UNITY_MCP_SSL_CERT_PATH` | - | Path to SSL certificate (for HTTPS) |
| `UNITY_MCP_SSL_KEY_PATH` | - | Path to SSL private key |

### Command Line Arguments

Alternative to environment variables:

```bash
# Basic usage
unity-mcp-server --port 8080 --client-transport http

# Advanced usage
unity-mcp-server \
  --port 8080 \
  --client-transport http \
  --plugin-timeout 15000 \
  --max-connections 5 \
  --log-level debug \
  --enable-cors true
```

### Server Configuration File

Create `server-config.json` for complex configurations:

```json
{
  "server": {
    "port": 8080,
    "host": "0.0.0.0",
    "clientTransport": "http",
    "maxConnections": 10,
    "enableCors": true,
    "corsOrigins": ["*"]
  },
  "unity": {
    "pluginTimeout": 10000,
    "enableHeartbeat": true,
    "heartbeatInterval": 30000
  },
  "security": {
    "enableSsl": false,
    "sslCertPath": "",
    "sslKeyPath": "",
    "requireAuthentication": false
  },
  "logging": {
    "level": "info",
    "enableFileLogging": false,
    "logFilePath": "./unity-mcp.log",
    "maxLogSize": "10MB"
  },
  "performance": {
    "enableCompression": true,
    "requestTimeout": 30000,
    "maxRequestSize": "10MB"
  }
}
```

Load configuration file:
```bash
unity-mcp-server --config server-config.json
```

## 🐳 Docker Configuration

### Basic Docker Configuration
```bash
# Using environment variables
docker run -p 8080:8080 \
  -e UNITY_MCP_PORT=8080 \
  -e UNITY_MCP_CLIENT_TRANSPORT=http \
  -e UNITY_MCP_LOG_LEVEL=info \
  ivanmurzakdev/unity-mcp-server:latest
```

### Docker Compose Configuration

Create `docker-compose.yml`:
```yaml
version: '3.8'
services:
  unity-mcp-server:
    image: ivanmurzakdev/unity-mcp-server:latest
    ports:
      - "8080:8080"
    environment:
      UNITY_MCP_PORT: 8080
      UNITY_MCP_CLIENT_TRANSPORT: http
      UNITY_MCP_PLUGIN_TIMEOUT: 10000
      UNITY_MCP_LOG_LEVEL: info
      UNITY_MCP_MAX_CONNECTIONS: 10
    volumes:
      - ./config:/app/config
      - ./logs:/app/logs
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
```

### Advanced Docker Configuration
```yaml
version: '3.8'
services:
  unity-mcp-server:
    image: ivanmurzakdev/unity-mcp-server:latest
    ports:
      - "8080:8080"
      - "8443:8443" # HTTPS port
    environment:
      UNITY_MCP_PORT: 8080
      UNITY_MCP_CLIENT_TRANSPORT: http
      UNITY_MCP_ENABLE_CORS: "true"
      UNITY_MCP_LOG_LEVEL: debug
      UNITY_MCP_SSL_CERT_PATH: /app/certs/server.crt
      UNITY_MCP_SSL_KEY_PATH: /app/certs/server.key
    volumes:
      - ./certs:/app/certs:ro
      - ./config:/app/config:ro
      - ./logs:/app/logs
    networks:
      - unity-mcp-network
    deploy:
      resources:
        limits:
          memory: 512M
          cpus: '0.5'
    restart: unless-stopped

networks:
  unity-mcp-network:
    driver: bridge
```

## 🤖 AI Client Configuration

### Claude Desktop Configuration

Configure Claude Desktop to connect to Unity-MCP:

#### STDIO Transport (Recommended)
```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "unity-mcp-server",
      "args": ["--client-transport", "stdio"],
      "env": {
        "UNITY_MCP_PORT": "8080",
        "UNITY_MCP_LOG_LEVEL": "info"
      }
    }
  }
}
```

#### HTTP Transport
```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "curl",
      "args": [
        "-X", "POST",
        "http://localhost:8080/mcp",
        "-H", "Content-Type: application/json"
      ],
      "env": {}
    }
  }
}
```

#### Docker-based Configuration
```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "docker",
      "args": [
        "run", "--rm", "-i",
        "-p", "8080:8080",
        "--name", "unity-mcp-claude",
        "ivanmurzakdev/unity-mcp-server:latest"
      ],
      "env": {
        "UNITY_MCP_CLIENT_TRANSPORT": "stdio"
      }
    }
  }
}
```

### VS Code MCP Extension Configuration

Add to VS Code `settings.json`:
```json
{
  "mcp.servers": {
    "unity-mcp": {
      "command": "unity-mcp-server",
      "args": ["--client-transport", "stdio"],
      "transport": "stdio",
      "env": {
        "UNITY_MCP_PORT": "8080",
        "UNITY_MCP_LOG_LEVEL": "info"
      }
    }
  }
}
```

## 🔒 Security Configuration

### Authentication Setup
```json
{
  "security": {
    "requireAuthentication": true,
    "authMethod": "apikey",
    "apiKeys": [
      {
        "key": "your-api-key-here",
        "name": "development-key",
        "permissions": ["read", "write"]
      }
    ]
  }
}
```

### SSL/TLS Configuration
```bash
# Generate self-signed certificate
openssl req -x509 -newkey rsa:4096 -keyout server.key -out server.crt -days 365 -nodes

# Configure server
unity-mcp-server \
  --enable-ssl \
  --ssl-cert-path ./server.crt \
  --ssl-key-path ./server.key \
  --port 8443
```

### Network Security
```json
{
  "security": {
    "allowedHosts": ["localhost", "127.0.0.1", "192.168.1.100"],
    "enableRateLimiting": true,
    "maxRequestsPerMinute": 100,
    "enableFirewall": true,
    "blockedIPs": []
  }
}
```

## 📊 Performance Configuration

### High-Performance Setup
```json
{
  "performance": {
    "enableCompression": true,
    "compressionLevel": 6,
    "enableCaching": true,
    "cacheSize": "100MB",
    "enableConnectionPooling": true,
    "maxPoolSize": 20,
    "connectionPoolTimeout": 30000
  }
}
```

### Low-Resource Setup
```json
{
  "performance": {
    "maxConnections": 2,
    "requestTimeout": 10000,
    "maxRequestSize": "1MB",
    "enableCompression": false,
    "memoryLimit": "64MB"
  }
}
```

### Production Optimization
```bash
# Docker with resource limits
docker run -p 8080:8080 \
  --memory="256m" \
  --cpus="0.5" \
  -e UNITY_MCP_MAX_CONNECTIONS=5 \
  -e UNITY_MCP_LOG_LEVEL=warn \
  ivanmurzakdev/unity-mcp-server:latest
```

## 🌐 Network Configuration

### Firewall Settings
```bash
# Allow Unity-MCP port through firewall
sudo ufw allow 8080/tcp
sudo ufw reload

# Windows Firewall
netsh advfirewall firewall add rule name="Unity-MCP" dir=in action=allow protocol=TCP localport=8080
```

### Proxy Configuration
```bash
# Behind corporate proxy
export HTTP_PROXY=http://proxy.company.com:8080
export HTTPS_PROXY=https://proxy.company.com:8080
unity-mcp-server --port 8080
```

### Load Balancer Configuration
```yaml
# nginx.conf
upstream unity_mcp_servers {
    server 127.0.0.1:8080;
    server 127.0.0.1:8081;
    server 127.0.0.1:8082;
}

server {
    listen 80;
    location / {
        proxy_pass http://unity_mcp_servers;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

## 🔍 Monitoring & Logging Configuration

### Comprehensive Logging
```json
{
  "logging": {
    "level": "debug",
    "enableFileLogging": true,
    "logFilePath": "./logs/unity-mcp.log",
    "enableRotation": true,
    "maxLogSize": "50MB",
    "maxLogFiles": 10,
    "logFormat": "json",
    "enableMetrics": true,
    "metricsPort": 9090
  }
}
```

### Health Check Configuration
```json
{
  "health": {
    "enableHealthCheck": true,
    "healthCheckPath": "/health",
    "healthCheckInterval": 30000,
    "includeSystemMetrics": true,
    "includeUnityStatus": true
  }
}
```

## 🚨 Troubleshooting Configuration

### Debug Configuration
```json
{
  "debug": {
    "enableDebugMode": true,
    "verboseLogging": true,
    "logNetworkTraffic": true,
    "enableProfiling": true,
    "dumpRequestsToFile": true,
    "debugOutputPath": "./debug"
  }
}
```

### Common Configuration Issues

#### Port Conflicts
```bash
# Check what's using port 8080
netstat -tulpn | grep 8080

# Use alternative port
unity-mcp-server --port 8081
```

#### Connection Timeouts
```json
{
  "connection": {
    "connectionTimeout": 30000,
    "keepAliveTimeout": 60000,
    "socketTimeout": 45000
  }
}
```

## 📋 Configuration Templates

### Development Template
```bash
# Development environment variables
export UNITY_MCP_PORT=8080
export UNITY_MCP_CLIENT_TRANSPORT=http
export UNITY_MCP_LOG_LEVEL=debug
export UNITY_MCP_PLUGIN_TIMEOUT=30000
export UNITY_MCP_MAX_CONNECTIONS=3
```

### Production Template
```bash
# Production environment variables  
export UNITY_MCP_PORT=8080
export UNITY_MCP_CLIENT_TRANSPORT=http
export UNITY_MCP_LOG_LEVEL=warn
export UNITY_MCP_PLUGIN_TIMEOUT=10000
export UNITY_MCP_MAX_CONNECTIONS=10
export UNITY_MCP_ENABLE_CORS=false
```

### Testing Template
```bash
# Testing environment variables
export UNITY_MCP_PORT=8088
export UNITY_MCP_CLIENT_TRANSPORT=stdio
export UNITY_MCP_LOG_LEVEL=debug
export UNITY_MCP_PLUGIN_TIMEOUT=5000
export UNITY_MCP_MAX_CONNECTIONS=1
```

## 📚 Next Steps

### Advanced Configuration
- **[Server Setup](Server-Setup)** - Production deployment strategies
- **[API Reference](API-Reference)** - Technical configuration options
- **[Troubleshooting](Troubleshooting)** - Configuration problem solving

### Integration Guides
- **[Custom Tools Development](Custom-Tools-Development)** - Configure for custom tools
- **[Examples & Tutorials](Examples-and-Tutorials)** - Configuration examples in practice

### Monitoring & Maintenance
- Set up regular configuration reviews
- Monitor performance metrics
- Update configurations as needs change
- Document environment-specific settings

---

**Need help with specific configuration scenarios?** Check our [Troubleshooting guide](Troubleshooting) or explore [Server Setup](Server-Setup) for deployment-specific configurations!