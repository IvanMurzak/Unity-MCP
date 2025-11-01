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
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP
{
    using ILogger = Microsoft.Extensions.Logging.ILogger;
    using LogLevel = com.IvanMurzak.Unity.MCP.Runtime.Utils.LogLevel;
    using MicrosoftLogLevel = Microsoft.Extensions.Logging.LogLevel;

    public partial class UnityMcpPlugin
    {
        static readonly Subject<UnityConnectionConfig> _onConfigChanged = new Subject<UnityConnectionConfig>();
        static readonly ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger<UnityMcpPlugin>();
        static readonly object _instanceMutex = new();

        static string DebugName => $"[{nameof(UnityMcpPlugin)}]";
        static UnityMcpPlugin instance = null!;

        public static UnityMcpPlugin Instance
        {
            get
            {
                InitSingletonIfNeeded();
                lock (_instanceMutex)
                {
                    return instance;
                }
            }
        }

        public static void InitSingletonIfNeeded()
        {
            lock (_instanceMutex)
            {
                if (instance == null)
                {
                    instance = new UnityMcpPlugin();
                    if (instance == null)
                    {
                        _logger.LogWarning("{tag} {class}.{method}: ConnectionConfig instance is null",
                            Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(InitSingletonIfNeeded));
                        return;
                    }
                }
            }
        }

        public static bool IsLogEnabled(LogLevel level) => LogLevel.IsEnabled(level);

        public static LogLevel LogLevel
        {
            get => Instance.data?.LogLevel ?? LogLevel.Trace;
            set
            {
                Instance.data ??= new UnityConnectionConfig();
                Instance.data.LogLevel = value;
                NotifyChanged(Instance.data);
            }
        }
        public static string Host
        {
            get => Instance.data?.Host ?? UnityConnectionConfig.DefaultHost;
            set
            {
                Instance.data ??= new UnityConnectionConfig();
                Instance.data.Host = value;
                NotifyChanged(Instance.data);
            }
        }
        public static int Port
        {
            get
            {
                if (Uri.TryCreate(Host, UriKind.Absolute, out var uri) && uri.Port > 0 && uri.Port <= Consts.Hub.MaxPort)
                    return uri.Port;

                return Consts.Hub.DefaultPort;
            }
        }
        public static bool KeepConnected
        {
            get => Instance.data?.KeepConnected ?? true;
            set
            {
                Instance.data ??= new UnityConnectionConfig();
                Instance.data.KeepConnected = value;
                NotifyChanged(Instance.data);
            }
        }
        public static int TimeoutMs
        {
            get => Instance.data?.TimeoutMs ?? Consts.Hub.DefaultTimeoutMs;
            set
            {
                Instance.data ??= new UnityConnectionConfig();
                Instance.data.TimeoutMs = value;
                NotifyChanged(Instance.data);
            }
        }
        public static ReadOnlyReactiveProperty<HubConnectionState> ConnectionState
            => Instance.McpPluginInstance?.ConnectionState ?? throw new InvalidOperationException($"{nameof(Instance.McpPluginInstance)} is null");

        public static ReadOnlyReactiveProperty<bool> IsConnected => Instance.McpPluginInstance?.ConnectionState
            ?.Select(x => x == HubConnectionState.Connected)
            ?.ToReadOnlyReactiveProperty(false)
            ?? throw new InvalidOperationException($"{nameof(Instance.McpPluginInstance)} is null");

        public static async Task NotifyToolRequestCompleted(ToolRequestCompletedData request, CancellationToken cancellationToken = default)
        {
            var mcpPlugin = Instance.McpPluginInstance ?? throw new InvalidOperationException($"{nameof(Instance.McpPluginInstance)} is null");

            // wait when connection will be established
            while (mcpPlugin.ConnectionState.CurrentValue != HubConnectionState.Connected)
            {
                await Task.Delay(100, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("{tag} {class}.{method}: operation cancelled while waiting for connection.",
                        Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(NotifyToolRequestCompleted));
                    return;
                }
            }

            if (mcpPlugin.McpManager == null)
            {
                _logger.LogCritical("{tag} {class}.{method}: {instance} is null",
                    Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(NotifyToolRequestCompleted), nameof(mcpPlugin.McpManager));
                return;
            }

            if (mcpPlugin.RemoteMcpManagerHub == null)
            {
                _logger.LogCritical("{tag} {class}.{method}: {instance} is null",
                    Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(NotifyToolRequestCompleted), nameof(mcpPlugin.RemoteMcpManagerHub));
                return;
            }

            await mcpPlugin.RemoteMcpManagerHub.NotifyToolRequestCompleted(request, cancellationToken);
        }

        public static void Validate()
        {
            var changed = false;
            var data = Instance.data ??= new UnityConnectionConfig();

            if (string.IsNullOrEmpty(data.Host))
            {
                data.Host = UnityConnectionConfig.DefaultHost;
                changed = true;
            }

            // Data was changed during validation, need to notify subscribers
            if (changed)
                NotifyChanged(data);
        }

        public static IDisposable SubscribeOnChanged(Action<UnityConnectionConfig> action, bool invokeImmediately = true)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var subscription = _onConfigChanged.Subscribe(action);
            if (invokeImmediately)
                Safe.Run(action, Instance.data, logLevel: Instance.data?.LogLevel ?? LogLevel.Trace);
            return subscription;
        }

        public static async Task<bool> Connect(bool initIfNeeded = true)
        {
            _logger.Log(MicrosoftLogLevel.Trace, "{tag} {class}.{method}() called.",
                Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(Connect));

            connectionMutex.WaitOne();
            try
            {
                var mcpPlugin = Instance.McpPluginInstance;
                if (mcpPlugin == null)
                {
                    isInitialized = false;
                    _logger.LogError("{tag} {class}.{method}() isInitialized set <false>.",
                        Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(Connect));
                    return false; // ignore
                }
                return await mcpPlugin.Connect();
            }
            finally
            {
                _logger.Log(MicrosoftLogLevel.Trace, "{tag} {class}.{method}() completed.",
                    Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(Connect));
                connectionMutex.ReleaseMutex();
            }
        }

        public static async void Disconnect()
        {
            _logger.Log(MicrosoftLogLevel.Trace, "{tag} {class}.{method}() called.",
                Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(Disconnect));

            connectionMutex.WaitOne();
            try
            {
                var mcpPlugin = Instance.McpPluginInstance;
                if (mcpPlugin == null)
                {
                    isInitialized = false;
                    _logger.LogDebug("{tag} {class}.{method}() isInitialized set <false>.",
                        Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(Disconnect));

                    await McpPlugin.McpPlugin.StaticDisposeAsync();
                    return; // ignore
                }

                await mcpPlugin.Disconnect();
            }
            finally
            {
                _logger.Log(MicrosoftLogLevel.Trace, "{tag} {class}.{method}() completed.",
                    Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(Disconnect));
                connectionMutex.ReleaseMutex();
            }
        }

        static void NotifyChanged(UnityConnectionConfig data) => Safe.Run(
            action: (x) => _onConfigChanged.OnNext(x),
            value: data,
            logLevel: data?.LogLevel ?? LogLevel.Trace);
    }
}
