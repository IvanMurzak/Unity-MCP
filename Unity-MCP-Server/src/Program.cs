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
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Json;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Configure NLog
            LogManager.Setup().LoadConfigurationFromFile("NLog.config");

            var dataArguments = new DataArguments(args);

            // In STDIO mode, redirect console logs to stderr to avoid polluting stdout with non-JSON content
            if (dataArguments.ClientTransport == Consts.MCP.Server.TransportMethod.stdio)
            {
                var consoleTarget = LogManager.Configuration?.FindTargetByName("console") as NLog.Targets.ColoredConsoleTarget;
                if (consoleTarget != null)
                {
                    consoleTarget.StdErr = true;
                }
                LogManager.ReconfigExistingLoggers();
            }

            var logger = LogManager.GetCurrentClassLogger();
            try
            {

                // TODO: remove usage of static ConnectionConfig, replace it with instance with DI injection.
                // Set the runtime configurable timeout
                ConnectionConfig.TimeoutMs = dataArguments.PluginTimeoutMs;

                var consoleWriteLine = dataArguments.ClientTransport switch
                {
                    Consts.MCP.Server.TransportMethod.stdio => (Action<string>)(message => Console.Error.WriteLine(message)),
                    Consts.MCP.Server.TransportMethod.http => (Action<string>)(message => Console.WriteLine(message)),
                    _ => throw new ArgumentException($"Unsupported transport method: {dataArguments.ClientTransport}. " +
                        $"Supported methods are: {Consts.MCP.Server.TransportMethod.stdio}, {Consts.MCP.Server.TransportMethod.http}")
                };

                consoleWriteLine("Location: " + Environment.CurrentDirectory);
                consoleWriteLine($"Launch arguments: {string.Join(" ", args)}");
                consoleWriteLine($"Parsed arguments: {JsonSerializer.Serialize(dataArguments, JsonOptions.Pretty)}");

                var builder = WebApplication.CreateBuilder(args);

                // Replace default logging with NLog
                builder.Logging.ClearProviders();
                builder.Logging.AddNLog();

                var reflector = new Reflector();

                // Setup SignalR ---------------------------------------------------------------
                builder.Services.AddSignalR(configure =>
                {
                    configure.EnableDetailedErrors = false;
                    configure.MaximumReceiveMessageSize = 1024 * 1024 * 256; // 256 MB
                    configure.ClientTimeoutInterval = TimeSpan.FromMinutes(5);
                    configure.KeepAliveInterval = TimeSpan.FromSeconds(30);
                    configure.HandshakeTimeout = TimeSpan.FromMinutes(2);
                })
                .AddJsonProtocol(options => RpcJsonConfiguration.ConfigureJsonSerializer(reflector, options));

                // Setup MCP Plugin ---------------------------------------------------------------
                var version = new Common.Version
                {
                    Api = Consts.ApiVersion,
                    Plugin = Consts.ApiVersion
                };
                builder.Services.WithAppFeatures(version, new NLogLoggerProvider(), configure =>
                {
                    configure.WithServerFeatures(dataArguments);
                }).Build(reflector);

                // Setup MCP Server ---------------------------------------------------------------

                var mcpBuilder = builder.Services
                    .AddMcpServer(options =>
                    {
                        options.Capabilities ??= new();
                        options.Capabilities.Tools ??= new();
                        options.Capabilities.Tools.ListChanged = true;
                    })
                    .WithToolsFromAssembly()
                    .WithCallToolHandler(ToolRouter.Call)
                    .WithListToolsHandler(ToolRouter.ListAll);

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

                // builder.WebHost.UseUrls(Consts.Hub.DefaultEndpoint);

                builder.WebHost.UseKestrel(options =>
                {
                    logger.Info($"Start listening on port: {dataArguments.Port}");
                    options.ListenAnyIP(dataArguments.Port);
                });

                var app = builder.Build();

                // Middleware ----------------------------------------------------------------
                // ---------------------------------------------------------------------------

                // Setup SignalR ----------------------------------------------------
                app.UseRouting();
                app.MapHub<RemoteApp>(Consts.Hub.RemoteApp, options =>
                {
                    options.Transports = HttpTransports.All;
                    options.ApplicationMaxBufferSize = 1024 * 1024 * 10; // 10 MB
                    options.TransportMaxBufferSize = 1024 * 1024 * 10; // 10 MB
                });

                // Setup MCP client -------------------------------------------------
                if (dataArguments.ClientTransport == Consts.MCP.Server.TransportMethod.http)
                {
                    // Map MCP endpoint
                    app.MapMcp("/");
                    app.MapMcp("/mcp");

                    // Add a GET /help endpoint for informational message
                    app.MapGet("/help", () =>
                    {
                        var header =
                            "Author: Ivan Murzak (https://github.com/IvanMurzak)\n" +
                            "Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)\n" +
                            "Copyright (c) 2025 Ivan Murzak\n" +
                            "Licensed under the Apache License, Version 2.0.\n" +
                            "See the LICENSE file in the project root for more information.\n" +
                            "\n" +
                            "Use \"/\" endpoint to get connected to MCP server\n";
                        return Results.Text(header, Consts.MimeType.TextPlain);
                    });
                }

                // Print logs -------------------------------------------------------
                if (logger.IsEnabled(NLog.LogLevel.Debug))
                {
                    var endpointDataSource = app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
                    foreach (var endpoint in endpointDataSource.Endpoints)
                        logger.Debug($"Configured endpoint: {endpoint.DisplayName}");

                    app.Use(async (context, next) =>
                    {
                        logger.Debug($"Request: {context.Request.Method} {context.Request.Path}");
                        try
                        {
                            await next.Invoke();
                            logger.Debug($"Response: {context.Response.StatusCode} ({context.Request.Method} {context.Request.Path})");
                        }
                        catch (OperationCanceledException)
                        {
                            // Optionally log as debug or ignore
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, $"Error occurred while processing request: {context.Request.Method} {context.Request.Path}");
                            return;
                        }
                    });
                }

                await app.RunAsync();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Application stopped due to an exception.");
                throw;
            }
            finally
            {
                LogManager.Shutdown();
            }
        }
    }
}
