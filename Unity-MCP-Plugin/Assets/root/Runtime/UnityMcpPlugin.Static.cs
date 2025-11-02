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
using com.IvanMurzak.ReflectorNet;
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
        static readonly Mutex _connectionMutex = new();

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

        private static LogLevel _logLevelCache = LogLevel.Trace;
        public static LogLevel LogLevel
        {
            get => _logLevelCache;
            set
            {
                _logLevelCache = value;
                Instance.data ??= new UnityConnectionConfig();
                Instance.data.LogLevel = value;
                NotifyChanged(Instance.data);
            }
        }
        private static string _hostCache = UnityConnectionConfig.DefaultHost;
        public static string Host
        {
            get => _hostCache ?? UnityConnectionConfig.DefaultHost;
            set
            {
                _hostCache = value;
                Instance.data ??= new UnityConnectionConfig();
                Instance.data.Host = value;
                NotifyChanged(Instance.data);
            }
        }
        private static bool _keepConnectedCache = true;
        public static bool KeepConnected
        {
            get => _keepConnectedCache;
            set
            {
                _keepConnectedCache = value;
                Instance.data ??= new UnityConnectionConfig();
                Instance.data.KeepConnected = value;
                NotifyChanged(Instance.data);
            }
        }
        private static int _timeoutMsCache = Consts.Hub.DefaultTimeoutMs;
        public static int TimeoutMs
        {
            get => _timeoutMsCache;
            set
            {
                _timeoutMsCache = value;
                Instance.data ??= new UnityConnectionConfig();
                Instance.data.TimeoutMs = value;
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
        public static ReadOnlyReactiveProperty<HubConnectionState> ConnectionState
            => Instance.McpPluginInstance?.ConnectionState ?? throw new InvalidOperationException($"{nameof(Instance.McpPluginInstance)} is null");

        public static ReadOnlyReactiveProperty<bool> IsConnected => Instance.McpPluginInstance?.ConnectionState
            ?.Select(x => x == HubConnectionState.Connected)
            ?.ToReadOnlyReactiveProperty(false)
            ?? throw new InvalidOperationException($"{nameof(Instance.McpPluginInstance)} is null");

        public static void LogTrace(string message, Type sourceClass, Exception? exception = null)
        {
            UnityLoggerFactory.LoggerFactory
                .CreateLogger(sourceClass.GetTypeShortName())
                .LogTrace(message, exception);
        }
        public static void LogDebug(string message, Type sourceClass, Exception? exception = null)
        {
            UnityLoggerFactory.LoggerFactory
                .CreateLogger(sourceClass.GetTypeShortName())
                .LogDebug(message, exception);
        }
        public static void LogInfo(string message, Type sourceClass, Exception? exception = null)
        {
            UnityLoggerFactory.LoggerFactory
                .CreateLogger(sourceClass.GetTypeShortName())
                .LogInformation(message, exception);
        }
        public static void LogWarn(string message, Type sourceClass, Exception? exception = null)
        {
            UnityLoggerFactory.LoggerFactory
                .CreateLogger(sourceClass.GetTypeShortName())
                .LogWarning(message, exception);
        }
        public static void LogError(string message, Type sourceClass, Exception? exception = null)
        {
            UnityLoggerFactory.LoggerFactory
                .CreateLogger(sourceClass.GetTypeShortName())
                .LogError(message, exception);
        }
        public static void LogException(string message, Type sourceClass, Exception? exception = null)
        {
            UnityLoggerFactory.LoggerFactory
                .CreateLogger(sourceClass.GetTypeShortName())
                .LogCritical(message, exception);
        }

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

        public static Task<bool> ConnectIfNeeded()
        {
            if (KeepConnected == false)
                return Task.FromResult(false);

            return Connect();
        }

        public static async Task<bool> Connect()
        {
            _logger.Log(MicrosoftLogLevel.Trace, "{tag} {class}.{method}() called.",
                Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(Connect));

            _connectionMutex.WaitOne();
            try
            {
                KeepConnected = true;

                var mcpPlugin = Instance.McpPluginInstance;
                if (mcpPlugin == null)
                {
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
                _connectionMutex.ReleaseMutex();
            }
        }

        public static async void Disconnect()
        {
            _logger.Log(MicrosoftLogLevel.Trace, "{tag} {class}.{method}() called.",
                Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(Disconnect));

            _connectionMutex.WaitOne();
            try
            {
                var mcpPlugin = Instance.McpPluginInstance;
                if (mcpPlugin == null)
                {
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
                _connectionMutex.ReleaseMutex();
            }
        }

        static void NotifyChanged(UnityConnectionConfig data) => Safe.Run(
            action: (x) => _onConfigChanged.OnNext(x),
            value: data,
            logLevel: data?.LogLevel ?? LogLevel.Trace);
    }
}
