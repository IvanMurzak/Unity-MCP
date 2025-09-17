/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

using System;
using com.IvanMurzak.Unity.MCP.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using NLog;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public static class ExtensionsMcpServer
    {
        public static IMcpServerBuilder? WithMcpServer(this IServiceCollection services, DataArguments dataArguments, Logger logger)
        {
            var mcpBuilder = services
                .AddMcpServer(options =>
                {
                    options.Capabilities ??= new();
                    options.Capabilities.Tools ??= new();
                    options.Capabilities.Tools.ListChanged = true;
                    options.Capabilities.Tools.CallToolHandler = ToolRouter.Call;
                    options.Capabilities.Tools.ListToolsHandler = ToolRouter.ListAll;

                    options.Capabilities.Resources ??= new();
                    options.Capabilities.Resources.ListChanged = true;
                    options.Capabilities.Resources.ReadResourceHandler = ResourceRouter.Read;
                    options.Capabilities.Resources.ListResourcesHandler = ResourceRouter.List;
                    options.Capabilities.Resources.ListResourceTemplatesHandler = ResourceRouter.ListTemplates;

                    options.Capabilities.Prompts ??= new();
                    options.Capabilities.Prompts.ListChanged = true;
                    options.Capabilities.Prompts.GetPromptHandler = PromptRouter.Get;
                    options.Capabilities.Prompts.ListPromptsHandler = PromptRouter.List;
                });

            // // Setup MCP tools
            // mcpBuilder = mcpBuilder
            //     .WithToolsFromAssembly()
            //     .WithCallToolHandler(ToolRouter.Call)
            //     .WithListToolsHandler(ToolRouter.ListAll);

            // // Setup MCP resources
            // mcpBuilder = mcpBuilder
            //     .WithResourcesFromAssembly()
            //     .WithReadResourceHandler(ResourceRouter.Read)
            //     .WithListResourcesHandler(ResourceRouter.List)
            //     .WithListResourceTemplatesHandler(ResourceRouter.ListTemplates);

            // // Setup MCP prompts
            // mcpBuilder = mcpBuilder
            //     .WithPromptsFromAssembly()
            //     .WithGetPromptHandler(PromptRouter.Get)
            //     .WithListPromptsHandler(PromptRouter.List);

            if (dataArguments.ClientTransport == Consts.MCP.Server.TransportMethod.stdio)
            {
                // Configure STDIO transport
                mcpBuilder = mcpBuilder.WithStdioServerTransport();
            }
            else if (dataArguments.ClientTransport == Consts.MCP.Server.TransportMethod.http)
            {
                // Configure HTTP transport
                mcpBuilder = mcpBuilder.WithHttpTransport(options =>
                {
                    logger.Debug($"Http transport configuration.");

                    options.Stateless = false;
                    options.PerSessionExecutionContext = true;
                    options.RunSessionHandler = async (context, server, cancellationToken) =>
                    {
                        var connectionGuid = Guid.NewGuid();
                        try
                        {
                            // This is where you can run logic before a session starts
                            // For example, you can log the session start or initialize resources
                            logger.Debug($"----------\nRunning session handler for HTTP transport. Connection guid: {connectionGuid}");

                            var service = new McpServerService(
                                server.Services!.GetRequiredService<ILogger<McpServerService>>(),
                                server,
                                server.Services!.GetRequiredService<IMcpRunner>(),
                                server.Services!.GetRequiredService<IToolRunner>(),
                                server.Services!.GetRequiredService<IPromptRunner>(),
                                server.Services!.GetRequiredService<IResourceRunner>(),
                                server.Services!.GetRequiredService<EventAppToolsChange>()
                            );

                            try
                            {
                                await service.StartAsync(cancellationToken);
                                await server.RunAsync(cancellationToken);
                            }
                            finally
                            {
                                await service.StopAsync(cancellationToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, $"Error occurred while processing HTTP transport session. Connection guid: {connectionGuid}.");
                        }
                        finally
                        {
                            logger.Debug($"Session handler for HTTP transport completed. Connection guid: {connectionGuid}\n----------");
                        }
                    };
                });
            }
            else
            {
                throw new ArgumentException($"Unsupported transport method: {dataArguments.ClientTransport}. " +
                    $"Supported methods are: {Consts.MCP.Server.TransportMethod.stdio}, {Consts.MCP.Server.TransportMethod.http}");
            }
            return mcpBuilder;
        }
    }
}
