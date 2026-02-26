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

namespace com.IvanMurzak.Unity.MCP
{
    /// <summary>
    /// Editor-only singleton that owns the persistent MCP connection used
    /// by Unity Editor tooling (AI Game Developer window, configurators, etc.).
    /// Lives in the Editor assembly — not accessible from Runtime code.
    /// </summary>
    public partial class UnityMcpPluginEditor : UnityMcpPlugin
    {
        protected UnityMcpPluginEditor() : base()
        {
            var config = GetOrCreateConfig(out var wasCreated);
            unityConnectionConfig = config;
            ApplyLogLevel(unityConnectionConfig.LogLevel);
            if (wasCreated)
                Save();
            IncrementSingletonCount();
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

        public override void Dispose()
        {
            DecrementSingletonCount();
            base.Dispose();
            DisposeMcpPluginInstance();
        }
    }
}
