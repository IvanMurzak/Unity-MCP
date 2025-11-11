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
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using R3;
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

                ApplyConfigToMcpPlugin(mcpPluginInstance);

                mcpPluginInstance.ConnectionState
                    .Subscribe(state => _connectionState.Value = state)
                    .AddTo(_disposables);

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
            _logger.LogTrace("{method} called.",
                nameof(BuildMcpPlugin));

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var mcpPlugin = new McpPluginBuilder(version, loggerProvider)
                .AddMcpPlugin()
                .WithConfig(config =>
                {
                    _logger.LogInformation("Unity-MCP server host: {host}", Host);
                    config.Host = Host;
                })
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders(); // 👈 Clears the default providers
                    loggingBuilder.SetMinimumLevel(MicrosoftLogLevel.Trace);

                    if (loggerProvider != null)
                        loggingBuilder.AddProvider(loggerProvider);
                })
                .WithToolsFromAssembly(assemblies)
                .WithPromptsFromAssembly(assemblies)
                .WithResourcesFromAssembly(assemblies)
                .Build(reflector);

            _logger.LogTrace("{method} completed.",
                nameof(BuildMcpPlugin));

            return mcpPlugin;
        }

        protected virtual void ApplyConfigToMcpPlugin(IMcpPlugin mcpPlugin)
        {
            _logger.LogTrace("{method} called.",
                nameof(ApplyConfigToMcpPlugin));

            // Enable/Disable tools based on config
            var toolManager = mcpPlugin.McpManager.ToolManager;
            if (toolManager != null)
            {
                var allEnabled = unityConnectionConfig.EnabledTools.Contains("*");
                foreach (var tool in toolManager.GetAllTools())
                {
                    var isEnabled = allEnabled || unityConnectionConfig.EnabledTools.Contains(tool.Name!);
                    toolManager.SetToolEnabled(tool.Name!, isEnabled);
                    _logger.LogDebug("{method}: Tool '{tool}' enabled: {isEnabled}",
                        nameof(ApplyConfigToMcpPlugin), tool.Name, isEnabled);
                }
            }

            // Enable/Disable prompts based on config
            var promptManager = mcpPlugin.McpManager.PromptManager;
            if (promptManager != null)
            {
                var allEnabled = unityConnectionConfig.EnabledPrompts.Contains("*");
                foreach (var prompt in promptManager.GetAllPrompts())
                {
                    var isEnabled = allEnabled || unityConnectionConfig.EnabledPrompts.Contains(prompt.Name);
                    promptManager.SetPromptEnabled(prompt.Name, isEnabled);
                    _logger.LogDebug("{method}: Prompt '{prompt}' enabled: {isEnabled}",
                        nameof(ApplyConfigToMcpPlugin), prompt.Name, isEnabled);
                }
            }

            // Enable/Disable resources based on config
            var resourceManager = mcpPlugin.McpManager.ResourceManager;
            if (resourceManager != null)
            {
                var allEnabled = unityConnectionConfig.EnabledResources.Contains("*");
                foreach (var resource in resourceManager.GetAllResources())
                {
                    var isEnabled = allEnabled || unityConnectionConfig.EnabledResources.Contains(resource.Name);
                    resourceManager.SetResourceEnabled(resource.Name, isEnabled);
                    _logger.LogDebug("{method}: Resource '{resource}' enabled: {isEnabled}",
                        nameof(ApplyConfigToMcpPlugin), resource.Name, isEnabled);
                }
            }

            _logger.LogTrace("{method} completed.",
                nameof(ApplyConfigToMcpPlugin));
        }
    }
}
