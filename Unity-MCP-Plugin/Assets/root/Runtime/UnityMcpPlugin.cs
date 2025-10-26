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
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Model;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP
{
    using ILogger = Microsoft.Extensions.Logging.ILogger;
    using LogLevel = com.IvanMurzak.Unity.MCP.Utils.LogLevel;
    using MicrosoftLogLevel = Microsoft.Extensions.Logging.LogLevel;

    public partial class UnityMcpPlugin
    {
        Data data = new Data();

        static readonly Subject<Data> onConfigChanged = new Subject<Data>();
        static readonly ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger<UnityMcpPlugin>();

        static volatile object instanceMutex = new();
        static UnityMcpPlugin instance = null!;
        static UnityMcpPlugin Instance
        {
            get
            {
                InitSingletonIfNeeded();
                lock (instanceMutex)
                {
                    return instance;
                }
            }
        }
        static string DebugName => $"[{nameof(UnityMcpPlugin)}]";

        public static void InitSingletonIfNeeded()
        {
            lock (instanceMutex)
            {
                if (instance == null)
                {
                    instance = GetOrCreateInstance(out var wasCreated);
                    if (instance == null)
                    {
                        _logger.Log(MicrosoftLogLevel.Warning, "{tag} {class}.{method}: ConnectionConfig instance is null",
                            Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(InitSingletonIfNeeded));
                        return;
                    }
                    else if (wasCreated)
                    {
                        Save();
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
                Instance.data ??= new Data();
                Instance.data.LogLevel = value;
                NotifyChanged(Instance.data);
            }
        }
        public static string Host
        {
            get => Instance.data?.Host ?? Data.DefaultHost;
            set
            {
                Instance.data ??= new Data();
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
                Instance.data ??= new Data();
                Instance.data.KeepConnected = value;
                NotifyChanged(Instance.data);
            }
        }
        public static int TimeoutMs
        {
            get => Instance.data?.TimeoutMs ?? Consts.Hub.DefaultTimeoutMs;
            set
            {
                Instance.data ??= new Data();
                Instance.data.TimeoutMs = value;
                NotifyChanged(Instance.data);
            }
        }
        public static ReadOnlyReactiveProperty<HubConnectionState> ConnectionState
            => McpPlugin.Instance!.ConnectionState;

        public static ReadOnlyReactiveProperty<bool> IsConnected => McpPlugin.Instance!.ConnectionState
            .Select(x => x == HubConnectionState.Connected)
            .ToReadOnlyReactiveProperty(false);

        public static async Task NotifyToolRequestCompleted(ResponseCallTool response, CancellationToken cancellationToken = default)
        {
            // wait when connection will be established
            while (McpPlugin.Instance?.ConnectionState.CurrentValue != HubConnectionState.Connected)
                await Task.Delay(100, cancellationToken);

            if (McpPlugin.Instance?.RpcRouter == null)
            {
                _logger.Log(MicrosoftLogLevel.Warning, "{tag} {class}.{method}: RpcRouter is null",
                    Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(NotifyToolRequestCompleted));

                return;
            }

            await McpPlugin.Instance.RpcRouter.NotifyToolRequestCompleted(response, cancellationToken);
        }

        public static void Validate()
        {
            var changed = false;
            var data = Instance.data ??= new Data();

            if (string.IsNullOrEmpty(data.Host))
            {
                data.Host = Data.DefaultHost;
                changed = true;
            }

            // Data was changed during validation, need to notify subscribers
            if (changed)
                NotifyChanged(data);
        }

        public static IDisposable SubscribeOnChanged(Action<Data> action, bool invokeImmediately = true)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var subscription = onConfigChanged.Subscribe(action);
            if (invokeImmediately)
                Safe.Run(action, Instance.data, logLevel: Instance.data?.LogLevel ?? LogLevel.Trace);
            return subscription;
        }

        public static async Task<bool> Connect(bool initIfNeeded = true)
        {
            _logger.Log(MicrosoftLogLevel.Trace, "{tag} {class}.{method}() called.",
                Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(Connect));

            initializedMutex.WaitOne();
            try
            {
                var instance = McpPlugin.Instance;
                if (instance == null)
                {
                    isInitialized = false;
                    _logger.LogError("{tag} {class}.{method}() isInitialized set <false>.",
                        Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(Connect));
                    return false; // ignore
                }
                return await instance.Connect();
            }
            finally
            {
                _logger.Log(MicrosoftLogLevel.Trace, "{tag} {class}.{method}() completed.",
                    Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(Connect));
                initializedMutex.ReleaseMutex();
            }
        }

        public static async void Disconnect()
        {
            _logger.Log(MicrosoftLogLevel.Trace, "{tag} {class}.{method}() called.",
                Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(Disconnect));

            initializedMutex.WaitOne();
            try
            {
                var instance = McpPlugin.Instance;
                if (instance == null)
                {
                    isInitialized = false;
                    _logger.LogDebug("{tag} {class}.{method}() isInitialized set <false>.",
                        Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(Disconnect));

                    await McpPlugin.StaticDisposeAsync();
                    return; // ignore
                }

                await instance.Disconnect();
            }
            finally
            {
                _logger.Log(MicrosoftLogLevel.Trace, "{tag} {class}.{method}() completed.",
                    Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(Disconnect));
                initializedMutex.ReleaseMutex();
            }
        }

        static void NotifyChanged(Data data) => Safe.Run(
            action: (x) => onConfigChanged.OnNext(x),
            value: data,
            logLevel: data?.LogLevel ?? LogLevel.Trace);
    }
}
