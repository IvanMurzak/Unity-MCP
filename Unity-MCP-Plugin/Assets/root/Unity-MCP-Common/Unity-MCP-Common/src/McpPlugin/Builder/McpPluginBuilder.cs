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
using System.Collections.Generic;
using System.Reflection;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Common.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public class McpPluginBuilder : IMcpPluginBuilder
    {
        readonly ILogger? _logger;
        readonly ILoggerProvider? _loggerProvider;
        readonly IServiceCollection _services;

        readonly List<ToolMethodData> _toolMethods = new();
        readonly Dictionary<string, IRunTool> _toolRunners = new();

        readonly List<PromptMethodData> _promptMethods = new();
        readonly Dictionary<string, IRunPrompt> _promptRunners = new();

        readonly List<ResourceMethodData> _resourceMethods = new();
        readonly Dictionary<string, IRunResource> _resourceRunners = new();

        bool isBuilt = false;

        public IServiceCollection Services => _services;
        public ServiceProvider? ServiceProvider { get; private set; }

        public McpPluginBuilder(Version version, ILoggerProvider? loggerProvider = null, IServiceCollection? services = null)
        {
            _loggerProvider = loggerProvider;
            _logger = loggerProvider?.CreateLogger(nameof(McpPluginBuilder));
            _services = services ?? new ServiceCollection();

            _services.AddSingleton(version);
            _services.AddSingleton<IConnectionManager, ConnectionManager>();
            _services.AddSingleton<IMcpPlugin, McpPlugin>();
            _services.AddSingleton<IHubEndpointConnectionBuilder, HubEndpointConnectionBuilder>();
        }

        #region Tool
        public IMcpPluginBuilder WithTool(Type classType, MethodInfo method)
        {
            if (isBuilt)
                throw new InvalidOperationException("The builder has already been built.");

            var attribute = method.GetCustomAttribute<McpPluginToolAttribute>();
            return WithTool(attribute!, classType, method);
        }
        public IMcpPluginBuilder WithTool(string name, string? title, Type classType, MethodInfo method)
        {
            if (isBuilt)
                throw new InvalidOperationException("The builder has already been built.");

            var attribute = new McpPluginToolAttribute(name, title);
            return WithTool(attribute, classType, method);
        }
        public IMcpPluginBuilder WithTool(McpPluginToolAttribute attribute, Type classType, MethodInfo method)
        {
            if (isBuilt)
                throw new InvalidOperationException("The builder has already been built.");

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
        #endregion

        #region Prompt
        public IMcpPluginBuilder WithPrompt(string name, Type classType, MethodInfo methodInfo)
        {
            if (isBuilt)
                throw new InvalidOperationException("The builder has already been built.");

            var attribute = methodInfo.GetCustomAttribute<McpPluginPromptAttribute>();
            if (attribute == null)
            {
                _logger?.LogWarning($"Method {classType.FullName}{methodInfo.Name} does not have a '{nameof(McpPluginPromptAttribute)}'.");
                return this;
            }

            if (string.IsNullOrEmpty(attribute.Name))
                throw new ArgumentException($"Prompt name cannot be null or empty. Type: {classType.Name}, Method: {methodInfo.Name}");

            _promptMethods.Add(new PromptMethodData
            (
                classType: classType,
                methodInfo: methodInfo,
                attribute: attribute
            ));
            return this;
        }
        public IMcpPluginBuilder AddPrompt(string name, IRunPrompt runner)
        {
            if (isBuilt)
                throw new InvalidOperationException("The builder has already been built.");

            if (_promptRunners.ContainsKey(name))
                throw new ArgumentException($"Prompt with name '{name}' already exists.");

            _promptRunners.Add(name, runner);
            return this;
        }
        #endregion

        #region Resource
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
        #endregion

        #region Other
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

            if (reflector == null)
                throw new ArgumentNullException(nameof(reflector));

            _services.AddSingleton(reflector);

            _services.AddSingleton(new ToolRunnerCollection(reflector, _loggerProvider?.CreateLogger(nameof(ToolRunnerCollection)))
                .Add(_toolMethods)
                .Add(_toolRunners));

            _services.AddSingleton(new PromptRunnerCollection(reflector, _loggerProvider?.CreateLogger(nameof(PromptRunnerCollection)))
                .Add(_promptMethods)
                .Add(_promptRunners));

            _services.AddSingleton(new ResourceRunnerCollection(reflector, _loggerProvider?.CreateLogger(nameof(ResourceRunnerCollection)))
                .Add(_resourceMethods)
                .Add(_resourceRunners));

            ServiceProvider = _services.BuildServiceProvider();
            isBuilt = true;

            return ServiceProvider.GetRequiredService<IMcpPlugin>();
        }
        #endregion
    }
}
