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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP
{
    using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;
    using ILogger = Microsoft.Extensions.Logging.ILogger;
    using LogLevel = com.IvanMurzak.Unity.MCP.Runtime.Utils.LogLevel;

    public partial class UnityMcpPluginEditor
    {
        static readonly Subject<UnityConnectionConfig> _onConfigChanged = new Subject<UnityConnectionConfig>();
        static readonly ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger<UnityMcpPluginEditor>();
        static readonly object _instanceMutex = new();

        static UnityMcpPluginEditor instance = null!;

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
        public static UnityMcpPluginEditor Instance
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
                    instance = new UnityMcpPluginEditor();
                    if (instance == null)
                    {
                        _logger.LogWarning("{method}: UnityMcpPluginEditor instance is null",
                            nameof(InitSingletonIfNeeded));
                        return;
                    }
                }
            }
        }

        // Replaces McpPlugin.McpPlugin static singleton behavior
        private static readonly ReactiveProperty<IMcpPlugin?> _pluginProperty = new(null);
        public static ReadOnlyReactiveProperty<IMcpPlugin?> PluginProperty => _pluginProperty;
        public static IMcpPlugin? CurrentPlugin => _pluginProperty.Value;
        private static void SetCurrentPlugin(IMcpPlugin? plugin) => _pluginProperty.Value = plugin;

        public static LogLevel LogLevel
        {
            get => Instance.unityConnectionConfig.LogLevel;
            set
            {
                Instance.unityConnectionConfig.LogLevel = value;
                ApplyLogLevel(value);
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
        public static bool KeepServerRunning
        {
            get => Instance.unityConnectionConfig.KeepServerRunning;
            set
            {
                Instance.unityConnectionConfig.KeepServerRunning = value;
                NotifyChanged(Instance.unityConnectionConfig);
            }
        }
        public static TransportMethod TransportMethod
        {
            get => Instance.unityConnectionConfig.TransportMethod;
            set
            {
                Instance.unityConnectionConfig.TransportMethod = value;
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

                return GeneratePortFromDirectory();
            }
        }

        public static string? Token
        {
            get => Instance.unityConnectionConfig.Token;
            set
            {
                Instance.unityConnectionConfig.Token = value;
                NotifyChanged(Instance.unityConnectionConfig);
            }
        }
        public static AuthOption AuthOption
        {
            get => Instance.unityConnectionConfig.AuthOption;
            set
            {
                Instance.unityConnectionConfig.AuthOption = value;
                NotifyChanged(Instance.unityConnectionConfig);
            }
        }
        public static bool GenerateSkillFiles
        {
            get => Instance.unityConnectionConfig.GenerateSkillFiles;
            set
            {
                Instance.unityConnectionConfig.GenerateSkillFiles = value;
                NotifyChanged(Instance.unityConnectionConfig);
            }
        }
        public static string SkillsRootFolder
        {
            get => Instance.unityConnectionConfig.SkillsRootFolder;
            set
            {
                Instance.unityConnectionConfig.SkillsRootFolder = value;
                NotifyChanged(Instance.unityConnectionConfig);
            }
        }

        // 'new' is intentional: static dispatch on the subtype, instance logic lives in the base.
        public static new ReadOnlyReactiveProperty<HubConnectionState> ConnectionState
            => ((UnityMcpPlugin)Instance).ConnectionState;
        public static new ReadOnlyReactiveProperty<bool> IsConnected
            => ((UnityMcpPlugin)Instance).IsConnected;

        public static new Task NotifyToolRequestCompleted(RequestToolCompletedData request, CancellationToken cancellationToken = default)
            => ((UnityMcpPlugin)Instance).NotifyToolRequestCompleted(request, cancellationToken);

        public static IDisposable SubscribeOnChanged(Action<UnityConnectionConfig> action, bool invokeImmediately = true)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var subscription = _onConfigChanged.Subscribe(action);
            if (invokeImmediately)
            {
                try { action(Instance.unityConnectionConfig); }
                catch (Exception e) { _logger.LogError(e, "{method}: exception invoking action immediately", nameof(SubscribeOnChanged)); }
            }
            return subscription;
        }

        public static new Task<bool> ConnectIfNeeded() => ((UnityMcpPlugin)Instance).ConnectIfNeeded();

        public static new Task<bool> Connect() => ((UnityMcpPlugin)Instance).Connect();

        // Disconnect() and DisconnectImmediate() are inherited from UnityMcpPlugin base.

        public static void StaticDispose()
        {
            _logger.LogTrace("{method} called.", nameof(StaticDispose));

            lock (_instanceMutex)
            {
                instance?.Dispose();
                instance = null!;
            }
        }

        static void NotifyChanged(UnityConnectionConfig data)
        {
            try { _onConfigChanged.OnNext(data); }
            catch (Exception e) { _logger.LogError(e, "{method}: exception", nameof(NotifyChanged)); }
        }
    }
}
