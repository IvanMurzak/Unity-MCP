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

        static string DebugName => $"[{nameof(UnityMcpPlugin)}]";
        static UnityMcpPlugin instance = null!;

        public static bool HasInstance
        {
            get
            {
                lock (_instanceMutex)
                {
                    return instance != null;
                }
            }
        }
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
                        _logger.LogWarning("{method}: ConnectionConfig instance is null",
                            nameof(InitSingletonIfNeeded));
                        return;
                    }
                }
            }
        }

        public static bool IsLogEnabled(LogLevel level) => LogLevel.IsEnabled(level);

        public static LogLevel LogLevel
        {
            get => Instance.unityConnectionConfig.LogLevel;
            set
            {
                Instance.unityConnectionConfig.LogLevel = value;
                NotifyChanged(Instance.unityConnectionConfig);
            }
        }
        public static string Host
        {
            get => Instance.unityConnectionConfig.Host;
            set
            {
                Instance.unityConnectionConfig.Host = value;
                NotifyChanged(Instance.unityConnectionConfig);
            }
        }
        public static bool KeepConnected
        {
            get => Instance.unityConnectionConfig.KeepConnected;
            set
            {
                Instance.unityConnectionConfig.KeepConnected = value;
                NotifyChanged(Instance.unityConnectionConfig);
            }
        }
        public static int TimeoutMs
        {
            get => Instance.unityConnectionConfig.TimeoutMs;
            set
            {
                Instance.unityConnectionConfig.TimeoutMs = value;
                NotifyChanged(Instance.unityConnectionConfig);
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

        static ReactiveProperty<HubConnectionState> _connectionState = new(HubConnectionState.Disconnected);
        public static ReadOnlyReactiveProperty<HubConnectionState> ConnectionState => _connectionState;

        public static ReadOnlyReactiveProperty<bool> IsConnected => _connectionState
            .Select(x => x == HubConnectionState.Connected)
            .ToReadOnlyReactiveProperty(false);

        public static void LogTrace(string message, Type sourceClass, params object?[] args)
        {
            UnityLoggerFactory.LoggerFactory
                .CreateLogger(sourceClass.GetTypeShortName())
                .LogTrace(message, args);
        }
        public static void LogDebug(string message, Type sourceClass, params object?[] args)
        {
            UnityLoggerFactory.LoggerFactory
                .CreateLogger(sourceClass.GetTypeShortName())
                .LogDebug(message, args);
        }
        public static void LogInfo(string message, Type sourceClass, params object?[] args)
        {
            UnityLoggerFactory.LoggerFactory
                .CreateLogger(sourceClass.GetTypeShortName())
                .LogInformation(message, args);
        }
        public static void LogWarn(string message, Type sourceClass, params object?[] args)
        {
            UnityLoggerFactory.LoggerFactory
                .CreateLogger(sourceClass.GetTypeShortName())
                .LogWarning(message, args);
        }
        public static void LogError(string message, Type sourceClass, params object?[] args)
        {
            UnityLoggerFactory.LoggerFactory
                .CreateLogger(sourceClass.GetTypeShortName())
                .LogError(message, args);
        }
        public static void LogException(string message, Type sourceClass, params object?[] args)
        {
            UnityLoggerFactory.LoggerFactory
                .CreateLogger(sourceClass.GetTypeShortName())
                .LogCritical(message, args);
        }

        public static async Task NotifyToolRequestCompleted(RequestToolCompletedData request, CancellationToken cancellationToken = default)
        {
            var mcpPlugin = Instance.McpPluginInstance ?? throw new InvalidOperationException($"{nameof(Instance.McpPluginInstance)} is null");

            // wait when connection will be established
            while (mcpPlugin.ConnectionState.CurrentValue != HubConnectionState.Connected)
            {
                await Task.Delay(100, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("{method}: operation cancelled while waiting for connection.",
                        nameof(NotifyToolRequestCompleted));
                    return;
                }
            }

            if (mcpPlugin.McpManager == null)
            {
                _logger.LogCritical("{method}: {instance} is null",
                    nameof(NotifyToolRequestCompleted), nameof(mcpPlugin.McpManager));
                return;
            }

            if (mcpPlugin.RemoteMcpManagerHub == null)
            {
                _logger.LogCritical("{method}: {instance} is null",
                    nameof(NotifyToolRequestCompleted), nameof(mcpPlugin.RemoteMcpManagerHub));
                return;
            }

            await mcpPlugin.RemoteMcpManagerHub.NotifyToolRequestCompleted(request);
        }

        public static void Validate()
        {
            var changed = false;
            var data = Instance.unityConnectionConfig ??= new UnityConnectionConfig();

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
                Safe.Run(action, Instance.unityConnectionConfig, logLevel: Instance.unityConnectionConfig?.LogLevel ?? LogLevel.Trace);
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
            _logger.Log(MicrosoftLogLevel.Trace, "{method} called.",
                nameof(Connect));

            try
            {
                var mcpPlugin = Instance.McpPluginInstance;
                if (mcpPlugin == null)
                {
                    _logger.LogError("{method} isInitialized set <false>.",
                        nameof(Connect));
                    return false; // ignore
                }
                return await mcpPlugin.Connect();
            }
            finally
            {
                _logger.Log(MicrosoftLogLevel.Trace, "{method} completed.",
                    nameof(Connect));
            }
        }

        public void DisconnectImmediate()
        {
            _logger.Log(MicrosoftLogLevel.Trace, "{method} called.",
                nameof(DisconnectImmediate));

            try
            {
                var mcpPlugin = McpPluginInstance;
                if (mcpPlugin == null)
                {
                    _logger.LogWarning("{method}: McpPlugin instance is null, nothing to disconnect, ignoring.",
                        nameof(DisconnectImmediate));
                    return;
                }
                else
                {
                    try
                    {
                        _logger.LogDebug("{method}: Acquiring connection mutex.",
                            nameof(DisconnectImmediate));
                        _logger.LogDebug("{method}: Disconnecting McpPlugin instance.",
                            nameof(DisconnectImmediate));
                        mcpPlugin.DisconnectImmediate();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("{method}: Exception during disconnecting: {exception}",
                            nameof(DisconnectImmediate), e);
                    }
                    finally
                    {
                        _logger.LogDebug("{method}: Releasing connection mutex.",
                            nameof(DisconnectImmediate));
                    }
                }
            }
            finally
            {
                _logger.Log(MicrosoftLogLevel.Trace, "{method} completed.",
                    nameof(DisconnectImmediate));
            }
        }

        public static void StaticDispose()
        {
            _logger.Log(MicrosoftLogLevel.Trace, "{method} called.",
                nameof(StaticDispose));

            _connectionState.Dispose();

            lock (_instanceMutex)
            {
                instance?.Dispose();
                instance = null!;
            }
        }

        static void NotifyChanged(UnityConnectionConfig data) => Safe.Run(
            action: (x) => _onConfigChanged.OnNext(x),
            value: data,
            logLevel: data?.LogLevel ?? LogLevel.Trace);
    }
}
