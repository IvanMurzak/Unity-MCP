/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public class HubEndpointConnectionBuilder : IHubEndpointConnectionBuilder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Reflector _reflector;
        private readonly ILogger _logger;

        public HubEndpointConnectionBuilder(IServiceProvider serviceProvider, Reflector reflector, ILogger<HubConnection> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _reflector = reflector ?? throw new ArgumentNullException(nameof(reflector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<HubConnection> CreateConnectionAsync(string endpoint)
        {
            _logger.LogInformation($"Creating HubConnection to {endpoint}");

            try
            {
                var connectionConfig = _serviceProvider.GetRequiredService<IOptions<ConnectionConfig>>().Value;

                var hubConnectionBuilder = new HubConnectionBuilder()
                    .WithUrl(connectionConfig.Endpoint + endpoint)
                    .WithAutomaticReconnect(new FixedRetryPolicy(TimeSpan.FromSeconds(10)))
                    .WithKeepAliveInterval(TimeSpan.FromSeconds(30))
                    .WithServerTimeout(TimeSpan.FromMinutes(5))
                    .AddJsonProtocol(options => RpcJsonConfiguration.ConfigureJsonSerializer(_reflector, options))
                    .ConfigureLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddProvider(new ForwardLoggerProvider(_logger,
                            additionalErrorMessage: "To stop seeing the error, please <b>Stop</b> the connection to MCP server in <b>AI Game Developer</b> window."));
                        logging.SetMinimumLevel(LogLevel.Trace);
                    });

                var hubConnection = hubConnectionBuilder.Build();

                return Task.FromResult(hubConnection);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to create HubConnection. Exception: {ex.Message}");
                if (ex.InnerException != null)
                    _logger.LogError($"Inner Exception: {ex.InnerException.Message}");
                if (ex is TypeInitializationException tie && tie.InnerException != null)
                    _logger.LogError($"TypeInitializer Inner Exception: {tie.InnerException}");
                throw;
            }
        }
    }
}
