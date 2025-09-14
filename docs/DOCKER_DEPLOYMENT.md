# Docker Deployment

The Unity-MCP-Server is automatically built and deployed as a Docker image when a new release is created.

## Docker Hub Repository

- **Repository**: `ivanmurzakdev/unity-mcp-server`
- **Tags**: 
  - Version-specific: `ivanmurzakdev/unity-mcp-server:X.Y.Z` (e.g., `0.17.1`)
  - Latest: `ivanmurzakdev/unity-mcp-server:latest`

## Deployment Process

The Docker image is built and pushed automatically by the `release.yml` GitHub Actions workflow when:

1. Code is pushed to the `main` branch
2. All Unity tests pass successfully  
3. A new version tag is created based on the `package.json` version

## Multi-Platform Support

The Docker images are built for multiple architectures:
- `linux/amd64` (Intel/AMD 64-bit)
- `linux/arm64` (ARM 64-bit, including Apple Silicon)

## Required Secrets

The deployment requires the following GitHub repository secrets:
- `DOCKER_USERNAME`: Docker Hub username
- `DOCKER_PASSWORD`: Docker Hub password or access token

## Usage

### Pull and Run Latest Version
```bash
docker pull ivanmurzakdev/unity-mcp-server:latest
docker run -p 8080:8080 ivanmurzakdev/unity-mcp-server:latest
```

### Pull and Run Specific Version
```bash
docker pull ivanmurzakdev/unity-mcp-server:0.17.1
docker run -p 8080:8080 ivanmurzakdev/unity-mcp-server:0.17.1
```

## Technical Details

- **Base Image**: `mcr.microsoft.com/dotnet/aspnet:9.0`
- **Build Image**: `mcr.microsoft.com/dotnet/sdk:9.0`
- **Exposed Port**: 8080
- **Framework**: .NET 9.0

The Docker image is built using multi-stage builds to optimize size and security.