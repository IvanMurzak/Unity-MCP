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
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.McpPlugin.Common.Utils;
using com.IvanMurzak.McpPlugin.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

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

                // Setup MCP Plugin ---------------------------------------------------------------

                builder.Services
                    .WithMcpServer(dataArguments.ClientTransport, logger)
                    .WithMcpPluginServer(dataArguments);

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
                app.UseMcpPluginServer(dataArguments);

                // Setup MCP client -------------------------------------------------
                if (dataArguments.ClientTransport == Consts.MCP.Server.TransportMethod.http)
                {
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

                #region Print Logs
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
                #endregion

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
