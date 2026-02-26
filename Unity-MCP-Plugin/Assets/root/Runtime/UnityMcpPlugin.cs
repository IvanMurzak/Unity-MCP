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
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP
{
    using LogLevel = Runtime.Utils.LogLevel;

    public partial class UnityMcpPlugin : IDisposable
    {
        public const string Version = "0.50.1";

        private static int _singletonCount = 0;
        public static bool HasAnyInstance => _singletonCount > 0;
        protected static void IncrementSingletonCount() => Interlocked.Increment(ref _singletonCount);
        protected static void DecrementSingletonCount() => Interlocked.Decrement(ref _singletonCount);

        private static LogLevel _configuredLogLevel = LogLevel.Warning;
        // Uses direct enum comparison: configured threshold <= requested level means enabled
        public static bool IsLogEnabled(LogLevel level) => _configuredLogLevel <= level;
        public static void ApplyLogLevel(LogLevel level) => _configuredLogLevel = level;

        protected readonly CompositeDisposable _disposables = new();
        protected readonly McpPluginSlot _plugin = new();

        // Tracks only the latest plugin's ConnectionState subscription.
        // Replaced (old one disposed) each time BuildMcpPlugin creates a new IMcpPlugin instance.
        private IDisposable? _pluginConnectionSubscription;

        public IMcpPlugin? McpPluginInstance => _plugin.Instance;
        public bool HasMcpPluginInstance => _plugin.HasInstance;

        public ILogger Logger => McpPluginInstance?.Logger ?? _logger;
        public Reflector? Reflector => McpPluginInstance?.McpManager.Reflector;
        public IToolManager? Tools => McpPluginInstance?.McpManager.ToolManager;
        public IPromptManager? Prompts => McpPluginInstance?.McpManager.PromptManager;
        public IResourceManager? Resources => McpPluginInstance?.McpManager.ResourceManager;

        public UnityLogCollector? LogCollector { get; protected set; } = null;

        // --- Connection state ---

        protected readonly ReactiveProperty<HubConnectionState> _connectionState
            = new(HubConnectionState.Disconnected);

        public ReadOnlyReactiveProperty<HubConnectionState> ConnectionState => _connectionState;

        public ReadOnlyReactiveProperty<bool> IsConnected => _connectionState
            .Select(x => x == HubConnectionState.Connected)
            .ToReadOnlyReactiveProperty(false);

        // --- Constructor / Dispose ---

        protected UnityMcpPlugin()
        {
            _disposables.Add(_connectionState);
        }

        public virtual void Dispose()
        {
            _pluginConnectionSubscription?.Dispose();
            _pluginConnectionSubscription = null;
            _disposables.Dispose();
            // LogCollector and _connectionState are disposed by _disposables
            LogCollector = null;
        }

        // --- Log collector ---

        public void AddUnityLogCollector(ILogStorage logStorage)
        {
            if (logStorage == null)
                throw new ArgumentNullException(nameof(logStorage));

            if (LogCollector != null)
                throw new InvalidOperationException($"{nameof(UnityLogCollector)} is already added.");

            LogCollector = new UnityLogCollector(logStorage);
            _disposables.Add(LogCollector);
        }

        public void AddUnityLogCollectorIfNeeded(Func<ILogStorage> logStorageProvider)
        {
            if (LogCollector != null)
                return;

            AddUnityLogCollector(logStorageProvider());
        }

        public void DisposeLogCollector()
        {
            LogCollector?.Save();
            LogCollector?.Dispose();
            LogCollector = null;
        }

        // --- Connection methods ---

        public Task<bool> ConnectIfNeeded()
        {
            if (!unityConnectionConfig.KeepConnected)
                return Task.FromResult(false);
            return Connect();
        }

        public async Task<bool> Connect()
        {
            _logger.LogTrace("{method} called.", nameof(Connect));
            try
            {
                var mcpPlugin = McpPluginInstance;
                if (mcpPlugin == null)
                {
                    _logger.LogError("{method}: McpPlugin instance is null.", nameof(Connect));
                    return false;
                }
                return await mcpPlugin.Connect();
            }
            finally
            {
                _logger.LogTrace("{method} completed.", nameof(Connect));
            }
        }

        public async Task Disconnect()
        {
            _logger.LogTrace("{method} called.", nameof(Disconnect));
            try
            {
                var mcpPlugin = McpPluginInstance;
                if (mcpPlugin == null)
                {
                    _logger.LogWarning("{method}: McpPlugin instance is null, nothing to disconnect, ignoring.",
                        nameof(Disconnect));
                    return;
                }
                try
                {
                    _logger.LogDebug("{method}: Disconnecting McpPlugin instance.", nameof(Disconnect));
                    await mcpPlugin.Disconnect();
                }
                catch (Exception e)
                {
                    _logger.LogError("{method}: Exception during disconnecting: {exception}",
                        nameof(Disconnect), e);
                }
            }
            finally
            {
                _logger.LogTrace("{method} completed.", nameof(Disconnect));
            }
        }

        public void DisconnectImmediate()
        {
            _logger.LogTrace("{method} called.", nameof(DisconnectImmediate));
            try
            {
                var mcpPlugin = McpPluginInstance;
                if (mcpPlugin == null)
                {
                    _logger.LogWarning("{method}: McpPlugin instance is null, nothing to disconnect, ignoring.",
                        nameof(DisconnectImmediate));
                    return;
                }
                try
                {
                    _logger.LogDebug("{method}: Disconnecting McpPlugin instance.", nameof(DisconnectImmediate));
                    mcpPlugin.DisconnectImmediate();
                }
                catch (Exception e)
                {
                    _logger.LogError("{method}: Exception during disconnecting: {exception}",
                        nameof(DisconnectImmediate), e);
                }
            }
            finally
            {
                _logger.LogTrace("{method} completed.", nameof(DisconnectImmediate));
            }
        }

        public async Task NotifyToolRequestCompleted(RequestToolCompletedData request, CancellationToken cancellationToken = default)
        {
            var mcpPlugin = McpPluginInstance
                ?? throw new InvalidOperationException($"{nameof(McpPluginInstance)} is null");

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

            if (mcpPlugin.McpManagerHub == null)
            {
                _logger.LogCritical("{method}: {instance} is null",
                    nameof(NotifyToolRequestCompleted), nameof(mcpPlugin.McpManagerHub));
                return;
            }

            await mcpPlugin.McpManagerHub.NotifyToolRequestCompleted(request);
        }

        // --- Token / Port utilities ---

        /// <summary>
        /// Generates a cryptographically random URL-safe token.
        /// Used by <see cref="UnityConnectionConfig.SetDefault"/> for initial token generation.
        /// </summary>
        public static string GenerateToken()
        {
            var bytes = new byte[32];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        /// <summary>
        /// Generate a deterministic TCP port based on current directory.
        /// Uses SHA256 hash for better distribution and less collisions.
        /// Port range: 50000-59999 (less commonly used dynamic ports).
        /// </summary>
        public static int GeneratePortFromDirectory()
        {
            const int MinPort = 50000; // Higher range to avoid common dynamic ports
            const int MaxPort = 59999;
            const int PortRange = MaxPort - MinPort + 1;

            var currentDir = Environment.CurrentDirectory.ToLowerInvariant();

            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(currentDir));

                // Use first 4 bytes as an unsigned integer to avoid Math.Abs(int.MinValue) overflow
                var hash = (uint)BitConverter.ToInt32(hashBytes, 0);

                // Map to port range
                var port = MinPort + (int)(hash % PortRange);

                return port;
            }
        }
    }
}
