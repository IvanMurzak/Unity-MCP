#if !UNITY_5_3_OR_NEWER
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using com.IvanMurzak.Unity.MCP.Common;
using NLog.Extensions.Logging;
using NLog;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using com.IvanMurzak.Unity.MCP.Common.Reflection;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

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
                var builder = WebApplication.CreateBuilder(args);

                // Configure all logs to go to stderr. This is needed for MCP STDIO server to work properly.
                builder.Logging.AddConsole(consoleLogOptions => consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace);

                // Replace default logging with NLog
                // builder.Logging.ClearProviders();
                builder.Logging.AddNLog();

                builder.Services.AddSignalR(configure =>
                {
                    configure.EnableDetailedErrors = true;
                    configure.MaximumReceiveMessageSize = 1024 * 1024 * 256; // 256 MB
                    configure.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                    configure.KeepAliveInterval = TimeSpan.FromSeconds(1);
                    configure.HandshakeTimeout = TimeSpan.FromSeconds(5);
                    configure.JsonSerialize(JsonUtils.JsonSerializerOptions);
                });

                // Setup MCP server ---------------------------------------------------------------
                builder.Services
                    .AddMcpServer(options =>
                    {
                        options.Capabilities ??= new();
                        options.Capabilities.Tools ??= new();
                        options.Capabilities.Tools.ListChanged = true;
                    })
                    .WithStdioServerTransport()
                    //.WithPromptsFromAssembly()
                    .WithToolsFromAssembly()
                    .WithCallToolHandler(ToolRouter.Call)
                    .WithListToolsHandler(ToolRouter.ListAll);
                //.WithReadResourceHandler(ResourceRouter.ReadResource)
                //.WithListResourcesHandler(ResourceRouter.ListResources)
                //.WithListResourceTemplatesHandler(ResourceRouter.ListResourceTemplates);

                // Setup McpApp ----------------------------------------------------------------
                builder.Services.AddMcpPlugin(logger: null, configure =>
                {
                    configure
                        .WithServerFeatures()
                        .AddLogging(logging =>
                        {
                            logging.AddNLog();
                            logging.SetMinimumLevel(LogLevel.Information);
                        });
                }).Build(new Reflector());

                // builder.WebHost.UseUrls(Consts.Hub.DefaultEndpoint);
                builder.WebHost.UseKestrel(options =>
                {
                    options.ListenLocalhost(GetPort(args));
                });

                var app = builder.Build();

                // Middleware ----------------------------------------------------------------
                // ---------------------------------------------------------------------------

                app.UseRouting();
                app.MapHub<RemoteApp>(Consts.Hub.RemoteApp, options =>
                {
                    options.Transports = HttpTransports.All;
                    options.ApplicationMaxBufferSize = 1024 * 1024 * 10; // 10 MB
                    options.TransportMaxBufferSize = 1024 * 1024 * 10; // 10 MB
                });

                if (logger.IsEnabled(NLog.LogLevel.Debug))
                {
                    var endpointDataSource = app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
                    foreach (var endpoint in endpointDataSource.Endpoints)
                        logger.Debug($"Configured endpoint: {endpoint.DisplayName}");

                    app.Use(async (context, next) =>
                    {
                        logger.Debug($"Request: {context.Request.Method} {context.Request.Path}");
                        await next.Invoke();
                        logger.Debug($"Response: {context.Response.StatusCode}");
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
        static int GetPort(string[] args)
        {
            if (args.Length > 0 && int.TryParse(args[0], out var parsedPort))
                return parsedPort;

            var envPort = Environment.GetEnvironmentVariable(Consts.Env.Port);
            if (envPort != null && int.TryParse(envPort, out var parsedEnvPort))
                return parsedEnvPort;

            return Consts.Hub.DefaultPort;
        }
    }
}
#endif