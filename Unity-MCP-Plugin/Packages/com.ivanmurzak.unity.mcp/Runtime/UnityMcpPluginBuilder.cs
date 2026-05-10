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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP
{
    using MicrosoftLogLevel = Microsoft.Extensions.Logging.LogLevel;

    /// <summary>
    /// Builder for configuring a runtime <see cref="UnityMcpPluginRuntime"/> from C# code.
    /// Intended for game builds where no JSON config file is available.
    /// Obtain an instance via <see cref="UnityMcpPluginRuntime.Initialize()"/>.
    /// <example>
    /// <code>
    /// [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    /// static void SetupMcp()
    /// {
    ///     UnityMcpPluginRuntime.Initialize(builder =>
    ///     {
    ///         builder.WithConfig(c =>
    ///         {
    ///             c.Host  = "http://localhost:8080";
    ///             c.Token = "my-token";
    ///         });
    ///     })
    ///     .Build();
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public sealed class UnityMcpPluginBuilder
    {
        /// <summary>
        /// The underlying <see cref="McpPluginBuilder"/> pre-configured with Unity
        /// defaults (logging, standard ignored assemblies, assembly scanning).
        /// Use this to configure host, token, additional ignored assemblies, custom
        /// tools/prompts/resources, and anything else supported by the MCP plugin.
        /// </summary>
        public IMcpPluginBuilder McpPlugin { get; }

        private readonly UnityMcpPluginRuntime _runtimePlugin;
        private readonly ILogger? _logger;

        internal UnityMcpPluginBuilder(IMcpPluginBuilder mcpBuilder, UnityMcpPluginRuntime runtimePlugin, ILoggerProvider? loggerProvider = null)
        {
            McpPlugin = mcpBuilder;
            _runtimePlugin = runtimePlugin;
            _logger = loggerProvider?.CreateLogger(nameof(UnityMcpPluginBuilder));

            // Apply Unity-specific defaults — the developer does not need to repeat these.
            McpPlugin
                .AddLogging(lb =>
                {
                    lb.ClearProviders();
                    lb.SetMinimumLevel(MicrosoftLogLevel.Trace);
                    if (loggerProvider != null)
                        lb.AddProvider(loggerProvider);
                })
                .IgnoreAssemblies(
                    "mscorlib",
                    "Mono.Security",
                    "netstandard",
                    "nunit.framework",
                    "System",
                    "UnityEngine",
                    "UnityEditor",
                    "Unity.",
                    "Microsoft",
                    "R3",
                    "McpPlugin",
                    "ReflectorNet",
                    "com.IvanMurzak.Unity.MCP.TestFiles",
                    "com.IvanMurzak.Unity.MCP.Editor.Tests",
                    "com.IvanMurzak.Unity.MCP.Tests");
        }

        /// <summary>
        /// Builds and connects a runtime <see cref="UnityMcpPluginRuntime"/> instance alongside the
        /// Editor's existing connection. The Editor's connection is not affected.
        /// Call <see cref="UnityMcpPluginRuntime.DisposeInstance()"/>
        /// to shut it down, or exit Play mode (handled automatically).
        /// </summary>
        public UnityMcpPluginRuntime Build()
        {
            _logger?.LogTrace("{method} called.", nameof(Build));

            _logger?.LogDebug("{method}: Building runtime MCP Plugin from builder...", nameof(Build));
            var built = _runtimePlugin.BuildFromBuilder(McpPlugin);

            _logger?.LogTrace("{method} completed.", nameof(Build));
            return _runtimePlugin;
        }
    }
}
