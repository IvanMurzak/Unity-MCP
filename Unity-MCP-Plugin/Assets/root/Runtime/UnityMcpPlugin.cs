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
using R3;

namespace com.IvanMurzak.Unity.MCP
{
    public partial class UnityMcpPlugin : IDisposable
    {
        public const string Version = "0.26.0";

        protected readonly CompositeDisposable _disposables = new();

        public UnityLogCollector? LogCollector { get; protected set; } = null;

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

        public void AddUnityLogCollector(ILogStorage logStorage)
        {
            if (LogCollector != null)
                throw new InvalidOperationException($"{nameof(UnityLogCollector)} is already added.");

            LogCollector = new UnityLogCollector(logStorage);
            _disposables.Add(LogCollector);
        }

        public void DisposeMcpPluginInstance()
        {
            lock (buildMutex)
            {
                mcpPluginInstance?.Dispose();
                mcpPluginInstance = null;
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
            LogCollector?.Dispose();
            LogCollector = null;
            DisposeMcpPluginInstance();
        }
    }
}
