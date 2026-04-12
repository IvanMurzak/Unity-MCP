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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP
{
    using ILogger = Microsoft.Extensions.Logging.ILogger;
    using LogLevel = Runtime.Utils.LogLevel;

    public partial class UnityMcpPluginRuntime
    {
        static readonly Subject<UnityConnectionConfig> _onConfigChanged = new Subject<UnityConnectionConfig>();
        static readonly ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger<UnityMcpPluginRuntime>();
        static readonly object _instanceMutex = new();

        static UnityMcpPluginRuntime _instance = null!;

        public static bool HasInstance
        {
            get { lock (_instanceMutex) { return _instance != null; } }
        }

        public static UnityMcpPluginRuntime Instance
        {
            get { lock (_instanceMutex) { return _instance; } }
        }

        static void InitSingletonIfNeeded()
        {
            lock (_instanceMutex)
            {
                if (_instance == null)
                    _instance = new UnityMcpPluginRuntime();
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
        public static LogLevel LogLevel
        {
            get => _instance.unityConnectionConfig.LogLevel;
            set
            {
                _instance.unityConnectionConfig.LogLevel = value;
                ApplyLogLevel(value);
                NotifyChanged(Instance.unityConnectionConfig);
            }
        }

        /// <summary>
        /// Disposes the runtime plugin instance created by <see cref="Initialize"/>.
        /// Safe to call multiple times; no-op if already disposed.
        /// </summary>
        public static void DisposeInstance()
        {
            lock (_instanceMutex)
            {
                _instance?._plugin.Dispose();
            }
        }

        /// <summary>
        /// Creates a <see cref="UnityMcpPluginBuilder"/> pre-configured with Unity
        /// defaults. Use <see cref="UnityMcpPluginBuilder.McpPlugin"/> to configure
        /// host, token, ignored assemblies, custom tools, etc., then call
        /// <see cref="UnityMcpPluginBuilder.Build"/> to apply and connect.
        /// <para>
        /// Intended for game builds where no JSON config file is available.
        /// In Unity Editor (Edit and Play mode), the editor plugin is automatically
        /// initialized from <c>UserSettings/AI-Game-Developer-Config.json</c>.
        /// </para>
        /// <example>
        /// <code>
        /// [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        /// static void SetupMcp()
        /// {
        ///     var mcpPlugin = UnityMcpPluginRuntime.Initialize(builder =>
        ///         {
        ///             builder.WithConfig(c =>
        ///             {
        ///                 c.Host  = "http://localhost:8080";
        ///                 c.Token = "my-token";
        ///             });
        ///         })
        ///         .Build();
        ///     mcpPlugin.Connect();
        /// }
        /// </code>
        /// </example>
        /// </summary>
        public static UnityMcpPluginBuilder Initialize(Action<IMcpPluginBuilder>? configure = null)
        {
            InitSingletonIfNeeded();
            var runtimeInstance = Instance;
            var version = runtimeInstance.BuildVersion();
            var loggerProvider = runtimeInstance.BuildLoggerProvider();

            var mcpBuilder = new McpPluginBuilder(version, loggerProvider);
            configure?.Invoke(mcpBuilder);

            return new UnityMcpPluginBuilder(mcpBuilder, runtimeInstance, loggerProvider);
        }

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

        static void NotifyChanged(UnityConnectionConfig data)
        {
            try { _onConfigChanged.OnNext(data); }
            catch (Exception e) { _logger.LogError(e, "{method}: exception", nameof(NotifyChanged)); }
        }

        public static void StaticDispose()
        {
            _logger.LogTrace("{method} called.", nameof(StaticDispose));
            lock (_instanceMutex)
            {
                _instance?.Dispose();
                _instance = null!;
            }
        }
    }
}
