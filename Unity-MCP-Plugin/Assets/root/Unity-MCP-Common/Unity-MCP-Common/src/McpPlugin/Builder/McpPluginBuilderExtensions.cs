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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public static partial class McpPluginBuilderExtensions
    {
        public static IMcpPluginBuilder WithAppFeatures(this IServiceCollection services, Version version, ILoggerProvider? loggerProvider = null, Action<IMcpPluginBuilder>? configure = null)
        {
            // Create an instance of McpAppBuilder
            var mcpPluginBuilder = new McpPluginBuilder(version, loggerProvider, services);

            // Allow additional configuration of McpAppBuilder
            configure?.Invoke(mcpPluginBuilder);

            return mcpPluginBuilder;
        }
        public static IMcpPluginBuilder AddMcpPlugin(this IMcpPluginBuilder builder)
        {
            builder.AddMcpRunner();
            builder.Services.AddTransient<IRpcRouter, RpcRouter>();

            // // TODO: Uncomment if any tools or prompts are needed from this assembly
            // // var assembly = typeof(McpAppBuilderExtensions).Assembly;

            // // builder.WithToolsFromAssembly(assembly);
            // // builder.WithPromptsFromAssembly(assembly);
            // // builder.WithResourcesFromAssembly(assembly);

            return builder;
        }

        public static IMcpPluginBuilder AddMcpRunner(this IMcpPluginBuilder builder)
        {
            builder.Services.TryAddSingleton<IMcpRunner, McpRunner>();
            return builder;
        }
    }
}
