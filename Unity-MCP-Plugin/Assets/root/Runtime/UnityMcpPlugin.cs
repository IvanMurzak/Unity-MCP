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
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP
{
    public partial class UnityMcpPlugin : IDisposable
    {
        public const string Version = "0.25.1";

        protected readonly CompositeDisposable _disposables = new();

        public McpPlugin.IToolManager? Tools => McpPluginInstance?.McpManager.ToolManager;
        public McpPlugin.IPromptManager? Prompts => McpPluginInstance?.McpManager.PromptManager;
        public McpPlugin.IResourceManager? Resources => McpPluginInstance?.McpManager.ResourceManager;

        protected UnityMcpPlugin(UnityConnectionConfig? config = null)
        {
            if (config == null)
            {
                config = GetOrCreateConfig(out var wasCreated);
                unityConnectionConfig = config ?? throw new InvalidOperationException($"{nameof(UnityConnectionConfig)} is null");
                if (wasCreated)
                    Save();
            }
            else
            {
                unityConnectionConfig = config ?? throw new InvalidOperationException($"{nameof(UnityConnectionConfig)} is null");
            }
        }

        public void Validate()
        {
            var changed = false;
            var data = unityConnectionConfig ??= new UnityConnectionConfig();

            if (string.IsNullOrEmpty(data.Host))
            {
                data.Host = UnityConnectionConfig.DefaultHost;
                changed = true;
            }

            // Data was changed during validation, need to notify subscribers
            if (changed)
                NotifyChanged(data);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            lock (buildMutex)
            {
                mcpPluginInstance?.Dispose();
                mcpPluginInstance = null;
            }
        }
    }
}
