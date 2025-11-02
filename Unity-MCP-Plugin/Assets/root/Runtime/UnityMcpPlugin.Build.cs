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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    using Consts = McpPlugin.Common.Consts;
    using LogLevel = Runtime.Utils.LogLevel;
    using MicrosoftLogLevel = Microsoft.Extensions.Logging.LogLevel;

    public partial class UnityMcpPlugin
    {
        protected readonly object buildMutex = new();

        protected IMcpPlugin? mcpPluginInstance;
        public IMcpPlugin? McpPluginInstance
        {
            get
            {
                lock (buildMutex)
                {
                    return mcpPluginInstance;
                }
            }
            protected set
            {
                lock (buildMutex)
                {
                    mcpPluginInstance = value;
                }
            }
        }
        public bool HasMcpPluginInstance
        {
            get
            {
                lock (buildMutex)
                {
                    return mcpPluginInstance != null;
                }
            }
        }

        public virtual UnityMcpPlugin BuildMcpPluginIfNeeded()
        {
            lock (buildMutex)
            {
                if (mcpPluginInstance != null)
                    return this; // already built

                mcpPluginInstance = BuildMcpPlugin(
                    version: BuildVersion(),
                    reflector: CreateDefaultReflector(),
                    loggerProvider: BuildLoggerProvider()
                );
                return this;
            }
        }

        protected virtual McpPlugin.Common.Version BuildVersion()
        {
            return new McpPlugin.Common.Version
            {
                Api = Consts.ApiVersion,
                Plugin = UnityMcpPlugin.Version,
                Environment = Application.unityVersion
            };
        }

        protected virtual ILoggerProvider? BuildLoggerProvider()
        {
            return new UnityLoggerProvider();
        }

        protected virtual IMcpPlugin BuildMcpPlugin(McpPlugin.Common.Version version, Reflector reflector, ILoggerProvider? loggerProvider = null)
        {
            _logger.Log(MicrosoftLogLevel.Trace, "{tag} {class}.{method}() called.",
                Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(BuildMcpPlugin));

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var mcpPlugin = new McpPluginBuilder(version, loggerProvider)
                .AddMcpPlugin()
                .WithConfig(config =>
                {
                    _logger.Log(MicrosoftLogLevel.Information, "{tag} MCP server address: {host}",
                        Consts.Log.Tag, Host);

                    config.Host = Host;
                })
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders(); // 👈 Clears the default providers

                    if (loggerProvider != null)
                        loggingBuilder.AddProvider(loggerProvider);

                    loggingBuilder.SetMinimumLevel(LogLevel switch
                    {
                        LogLevel.Trace => MicrosoftLogLevel.Trace,
                        LogLevel.Debug => MicrosoftLogLevel.Debug,
                        LogLevel.Info => MicrosoftLogLevel.Information,
                        LogLevel.Warning => MicrosoftLogLevel.Warning,
                        LogLevel.Error => MicrosoftLogLevel.Error,
                        LogLevel.Exception => MicrosoftLogLevel.Critical,
                        _ => MicrosoftLogLevel.Warning
                    });
                })
                .WithToolsFromAssembly(assemblies)
                .WithPromptsFromAssembly(assemblies)
                .WithResourcesFromAssembly(assemblies)
                .Build(reflector);

            _logger.Log(MicrosoftLogLevel.Trace, "{tag} {class}.{method}() completed.",
                Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(BuildMcpPlugin));

            return mcpPlugin;
        }
    }
}
