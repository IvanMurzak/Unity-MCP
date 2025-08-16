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
            var connectionConfig = _serviceProvider.GetRequiredService<IOptions<ConnectionConfig>>().Value;
            var hubConnection = new HubConnectionBuilder()
                .WithUrl(connectionConfig.Endpoint + endpoint)
                .WithAutomaticReconnect(new FixedRetryPolicy(TimeSpan.FromSeconds(1)))
                .WithServerTimeout(TimeSpan.FromSeconds(5))
                .AddJsonProtocol(options =>
                {
                    var jsonSerializerOptions = _reflector.JsonSerializer.JsonSerializerOptions;

                    options.PayloadSerializerOptions.DefaultIgnoreCondition = jsonSerializerOptions.DefaultIgnoreCondition;
                    options.PayloadSerializerOptions.PropertyNamingPolicy = jsonSerializerOptions.PropertyNamingPolicy;
                    options.PayloadSerializerOptions.WriteIndented = jsonSerializerOptions.WriteIndented;

                    foreach (var converter in jsonSerializerOptions.Converters)
                        options.PayloadSerializerOptions.Converters.Add(converter);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(new ForwardLoggerProvider(_logger,
                        additionalErrorMessage: "To stop seeing the error, please <b>Stop</b> the connection to MCP server in <b>AI Connector</b> window."));
                    logging.SetMinimumLevel(LogLevel.Trace);
                })
                .Build();

            return Task.FromResult(hubConnection);
        }
    }
}