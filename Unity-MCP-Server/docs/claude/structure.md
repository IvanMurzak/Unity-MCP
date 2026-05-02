# Core Components

- **Program.cs**: Main entry point, configures ASP.NET Core web host with MCP server and SignalR hub
- **McpServerService.cs**: Hosted service that manages MCP server lifecycle and tool change notifications
- **Hub/RemoteApp.cs**: SignalR hub for Unity Plugin communication
- **Routing/**: MCP protocol handlers for tools and resources
- **Client/**: Utilities for remote tool and resource execution
- **Extension/**: Builder extensions for MCP server configuration
