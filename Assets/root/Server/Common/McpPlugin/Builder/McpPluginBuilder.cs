#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common.Data;
using com.IvanMurzak.Unity.MCP.Common.Reflection;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public class McpPluginBuilder : IMcpPluginBuilder
    {
        readonly ILogger? _logger;
        readonly IServiceCollection _services;

        readonly List<ToolMethodData> _toolMethods = new();
        readonly Dictionary<string, IRunTool> _toolRunners = new();

        readonly List<ResourceMethodData> _resourceMethods = new();
        readonly Dictionary<string, IRunResource> _resourceRunners = new();

        bool isBuilt = false;

        public IServiceCollection Services => _services;
        public ServiceProvider? ServiceProvider { get; private set; }

        public McpPluginBuilder(ILogger? logger = null, IServiceCollection? services = null)
        {
            _logger = logger;
            _services = services ?? new ServiceCollection();

            _services.AddTransient<IConnectionManager, ConnectionManager>();
            _services.AddSingleton<IMcpPlugin, McpPlugin>();

            Func<string, Task<HubConnection>> hubConnectionBuilder = (string endpoint) =>
            {
                if (ServiceProvider == null)
                    throw new InvalidOperationException("ServiceProvider is not initialized. Call Build() before using this method.");

                var connectionConfig = ServiceProvider.GetRequiredService<IOptions<ConnectionConfig>>().Value;
                var hubConnection = new HubConnectionBuilder()
                    .WithUrl(connectionConfig.Endpoint + endpoint)
                    .WithAutomaticReconnect(new FixedRetryPolicy(TimeSpan.FromSeconds(1)))
                    .WithServerTimeout(TimeSpan.FromSeconds(3))
                    .AddJsonProtocol(options =>
                    {
                        // options.PayloadSerializerOptions.PropertyNamingPolicy = null;
                        // options.PayloadSerializerOptions.DictionaryKeyPolicy = null;
                    })
                    .ConfigureLogging(logging =>
                    {
                        // logging.AddNLog();
                        logging.SetMinimumLevel(LogLevel.Trace);
                    })
                    .Build();

                return Task.FromResult(hubConnection);
            };
            _services.AddSingleton(hubConnectionBuilder);
        }

        public IMcpPluginBuilder WithTool(string name, Type classType, MethodInfo method)
        {
            if (isBuilt)
                throw new InvalidOperationException("The builder has already been built.");

            var attribute = method.GetCustomAttribute<McpPluginToolAttribute>();
            if (attribute == null)
            {
                _logger?.LogWarning($"Method {classType.FullName}{method.Name} does not have a '{nameof(McpPluginToolAttribute)}'.");
                return this;
            }

            if (string.IsNullOrEmpty(attribute.Name))
                throw new ArgumentException($"Tool name cannot be null or empty. Type: {classType.Name}, Method: {method.Name}");

            _toolMethods.Add(new ToolMethodData
            (
                classType: classType,
                methodInfo: method,
                attribute: attribute
            ));
            return this;
        }

        public IMcpPluginBuilder AddTool(string name, IRunTool runner)
        {
            if (isBuilt)
                throw new InvalidOperationException("The builder has already been built.");

            if (_toolRunners.ContainsKey(name))
                throw new ArgumentException($"Tool with name '{name}' already exists.");

            _toolRunners.Add(name, runner);
            return this;
        }

        public IMcpPluginBuilder WithResource(Type classType, MethodInfo getContentMethod)
        {
            if (isBuilt)
                throw new InvalidOperationException("The builder has already been built.");

            var attribute = getContentMethod.GetCustomAttribute<McpPluginResourceAttribute>();
            if (attribute == null)
            {
                _logger?.LogWarning($"Method {classType.FullName}{getContentMethod.Name} does not have a '{nameof(McpPluginResourceAttribute)}'.");
                return this;
            }

            var listResourcesMethodName = attribute.ListResources ?? throw new InvalidOperationException($"Method {getContentMethod.Name} does not have a 'ListResources'.");
            var listResourcesMethod = classType.GetMethod(listResourcesMethodName);
            if (listResourcesMethod == null)
                throw new InvalidOperationException($"Method {classType.FullName}{listResourcesMethodName} not found in type {classType.Name}.");

            if (!getContentMethod.ReturnType.IsArray ||
                !typeof(ResponseResourceContent).IsAssignableFrom(getContentMethod.ReturnType.GetElementType()))
                throw new InvalidOperationException($"Method {classType.FullName}{getContentMethod.Name} must return {nameof(ResponseResourceContent)} array.");

            if (!listResourcesMethod.ReturnType.IsArray ||
                !typeof(ResponseListResource).IsAssignableFrom(listResourcesMethod.ReturnType.GetElementType()))
                throw new InvalidOperationException($"Method {classType.FullName}{listResourcesMethod.Name} must return {nameof(ResponseListResource)} array.");

            _resourceMethods.Add(new ResourceMethodData
            (
                classType: classType,
                attribute: attribute,
                getContentMethod: getContentMethod,
                listResourcesMethod: listResourcesMethod
            ));

            return this;
        }

        public IMcpPluginBuilder AddResource(IRunResource resourceParams)
        {
            if (isBuilt)
                throw new InvalidOperationException("The builder has already been built.");

            if (_resourceRunners == null)
                throw new ArgumentNullException(nameof(_resourceRunners));
            if (resourceParams == null)
                throw new ArgumentNullException(nameof(resourceParams));

            if (_resourceRunners.ContainsKey(resourceParams.Route))
                throw new ArgumentException($"Resource with routing '{resourceParams.Route}' already exists.");

            _resourceRunners.Add(resourceParams.Route, resourceParams);
            return this;
        }

        public IMcpPluginBuilder AddLogging(Action<ILoggingBuilder> loggingBuilder)
        {
            if (isBuilt)
                throw new InvalidOperationException("The builder has already been built.");

            _services.AddLogging(loggingBuilder);
            return this;
        }

        public IMcpPluginBuilder WithConfig(Action<ConnectionConfig> config)
        {
            if (isBuilt)
                throw new InvalidOperationException("The builder has already been built.");

            _services.Configure(config);
            return this;
        }

        public IMcpPlugin Build(Reflector reflector)
        {
            if (isBuilt)
                throw new InvalidOperationException("The builder has already been built.");

            _services.AddSingleton(new ToolRunnerCollection(reflector, _logger)
                .Add(_toolMethods)
                .Add(_toolRunners));

            _services.AddSingleton(new ResourceRunnerCollection(reflector, _logger)
                .Add(_resourceMethods)
                .Add(_resourceRunners));

            ServiceProvider = _services.BuildServiceProvider();
            isBuilt = true;

            return ServiceProvider.GetRequiredService<IMcpPlugin>();
        }
    }
}