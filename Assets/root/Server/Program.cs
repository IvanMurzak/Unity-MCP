﻿#if !IGNORE
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using com.IvanMurzak.Unity.MCP.Common;
using NLog.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using NLog;
using System;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.Error.WriteLine("Location: " + Environment.CurrentDirectory);
            // Configure NLog
            var logger = LogManager.Setup().LoadConfigurationFromFile("NLog.config").GetCurrentClassLogger();
            try
            {
                var builder = Host.CreateApplicationBuilder(args);
                // Configure all logs to go to stderr. This is needed for MCP STDIO server to work properly.
                builder.Logging.AddConsole(consoleLogOptions => consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace);

                // Replace default logging with NLog
                // builder.Logging.ClearProviders();
                builder.Logging.AddNLog();



                // Setup MCP server ---------------------------------------------------------------
                builder.Services
                    .AddMcpServer()
                    .WithStdioServerTransport()
                    .WithPromptsFromAssembly()
                    .WithToolsFromAssembly()
                    .WithListResourceTemplatesHandler(ResourceRouter.ListResourceTemplates)
                    .WithListResourcesHandler(ResourceRouter.ListResources)
                    .WithReadResourceHandler(ResourceRouter.ReadResource);

                // Setup SignalR connection builder -----------------------------------------------
                // TODO: Replace raw TCP with SignalR
                // builder.Services.AddSingleton(new HubConnectionBuilder()
                //     .WithUrl(Environment.GetEnvironmentVariable("UNITY_URL") ?? Consts.Remote.URL)
                //     .WithAutomaticReconnect()
                //     .AddJsonProtocol()
                //     .ConfigureLogging(logging =>
                //     {
                //         logging.AddNLog();
                //         logging.SetMinimumLevel(LogLevel.Information);
                //     }));

                // Setup Connector ----------------------------------------------------------------
                builder.Services
                    .AddConnector()
                    .AddLogging(logging =>
                    {
                        logging.AddNLog();
                        logging.SetMinimumLevel(LogLevel.Information);
                    })
                    .WithConfig(config =>
                    {
                        config.ConnectionType = Connector.ConnectionRole.Server;
                    })
                    .Build() // TODO: Build it right now is not the best idea
                    .Connect();

                await builder.Build().RunAsync();
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
#endif