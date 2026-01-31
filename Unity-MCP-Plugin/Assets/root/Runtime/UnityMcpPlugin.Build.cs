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
using com.IvanMurzak.ReflectorNet.Utils;
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

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                mcpPluginInstance = BuildMcpPlugin(
                    version: BuildVersion(),
                    reflector: CreateDefaultReflector(),
                    loggerProvider: BuildLoggerProvider()
                );
                stopwatch.Stop();
                _logger.LogDebug("MCP Plugin built in {elapsedMilliseconds} ms.",
                    stopwatch.ElapsedMilliseconds);

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

            var assemblies = AssemblyUtils.AllAssemblies;
            var mcpPluginBuilder = new McpPluginBuilder(version, loggerProvider)
                .WithConfig(config =>
                {
                    _logger.LogInformation("AI Game Developer server host: {host}", Host);
                    config.Host = Host;
                })
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders(); // 👈 Clears the default providers
                    loggingBuilder.SetMinimumLevel(MicrosoftLogLevel.Trace);

                    if (loggerProvider != null)
                        loggingBuilder.AddProvider(loggerProvider);
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
                    "com.IvanMurzak.Unity.MCP.Tests")
                .WithToolsFromAssembly(assemblies)
                .WithPromptsFromAssembly(assemblies)
                .WithResourcesFromAssembly(assemblies);

            var mcpPlugin = mcpPluginBuilder.Build(reflector);

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
                foreach (var tool in toolManager.GetAllTools())
                {
                    var toolFeature = unityConnectionConfig.Tools.FirstOrDefault(t => t.Name == tool.Name!);
                    var isEnabled = toolFeature == null || toolFeature.Enabled;
                    toolManager.SetToolEnabled(tool.Name!, isEnabled);
                    _logger.LogDebug("{method}: Tool '{tool}' enabled: {isEnabled}",
                        nameof(ApplyConfigToMcpPlugin), tool.Name, isEnabled);
                }
            }

            // Enable/Disable prompts based on config
            var promptManager = mcpPlugin.McpManager.PromptManager;
            if (promptManager != null)
            {
                foreach (var prompt in promptManager.GetAllPrompts())
                {
                    var promptFeature = unityConnectionConfig.Prompts.FirstOrDefault(p => p.Name == prompt.Name);
                    var isEnabled = promptFeature == null || promptFeature.Enabled;
                    promptManager.SetPromptEnabled(prompt.Name, isEnabled);
                    _logger.LogDebug("{method}: Prompt '{prompt}' enabled: {isEnabled}",
                        nameof(ApplyConfigToMcpPlugin), prompt.Name, isEnabled);
                }
            }

            // Enable/Disable resources based on config
            var resourceManager = mcpPlugin.McpManager.ResourceManager;
            if (resourceManager != null)
            {
                foreach (var resource in resourceManager.GetAllResources())
                {
                    var resourceFeature = unityConnectionConfig.Resources.FirstOrDefault(r => r.Name == resource.Name);
                    var isEnabled = resourceFeature == null || resourceFeature.Enabled;
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
