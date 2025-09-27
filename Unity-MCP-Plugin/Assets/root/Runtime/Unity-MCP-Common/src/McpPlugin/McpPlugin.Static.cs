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
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public partial class McpPlugin : IMcpPlugin
    {
        readonly static ReactiveProperty<McpPlugin?> _instance = new(null);

        public static bool HasInstance => _instance.CurrentValue != null;
        public static IMcpPlugin? Instance => _instance.CurrentValue;

        public static IDisposable DoOnce(Action<IMcpPlugin> func) => _instance
            .Where(x => x != null)
            .Take(1)
            .ObserveOnCurrentSynchronizationContext()
            .SubscribeOnCurrentSynchronizationContext()
            .Subscribe(instance =>
            {
                if (instance == null)
                    return;
                if (func == null)
                {
                    instance._logger.LogWarning($"[{nameof(McpPlugin)}] DoOnce() called with null func");
                    return;
                }
                try
                {
                    func(instance);
                }
                catch (Exception e)
                {
                    instance._logger.LogError(e, $"[{nameof(McpPlugin)}] Error in DoOnce()");
                }
            });

        public static IDisposable DoAlways(Action<IMcpPlugin> func) => _instance
            .Where(x => x != null)
            .ObserveOnCurrentSynchronizationContext()
            .SubscribeOnCurrentSynchronizationContext()
            .Subscribe(instance =>
            {
                if (instance == null)
                    return;
                if (func == null)
                {
                    instance._logger.LogWarning($"[{nameof(McpPlugin)}] DoAlways() called with null func");
                    return;
                }
                try
                {
                    func(instance);
                }
                catch (Exception e)
                {
                    instance._logger.LogError(e, $"[{nameof(McpPlugin)}] Error in DoAlways()");
                }
            });

        public static Task StaticDisposeAsync()
        {
            var instance = _instance.CurrentValue;
            if (instance == null)
                return Task.CompletedTask;

            _instance.Value = null;

            return instance.DisposeAsync();
        }
    }
}
