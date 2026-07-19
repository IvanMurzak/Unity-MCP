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
using com.IvanMurzak.McpPlugin.AgentConfig;
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
        public const string Version = "0.85.0";

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
        /// Generate a deterministic TCP port for the current working directory
        /// (<see cref="Environment.CurrentDirectory"/>). Port range: 20000-29999 (avoids Windows
        /// ephemeral/reserved port ranges).
        /// </summary>
        /// <remarks>
        /// Defect <b>B10</b> fix (auth-fixes d1): the derivation runs the directory through the shared
        /// <see cref="ProjectIdentity"/> <b>v2</b> normalization (trim trailing separators, convert
        /// <c>'\\'</c> to <c>'/'</c>, then <see cref="string.ToLowerInvariant"/>) via
        /// <see cref="ProjectIdentity.DerivePortV2"/> — instead of hashing the raw, untrimmed, un-separator-
        /// normalized <see cref="Environment.CurrentDirectory"/> string it used before. This keeps the
        /// local port in lock-step with the routing pin (also v2-normalized), so a Windows working
        /// directory reported with backslashes hashes identically to its forward-slash form. The port
        /// byte-math (first 4 hash bytes, little-endian, mapped into 20000-29999) is unchanged, so a
        /// path with no backslashes and no trailing separator yields the same port as before.
        /// </remarks>
        public static int GeneratePortFromDirectory()
            => GeneratePortFromDirectory(Environment.CurrentDirectory);

        /// <summary>
        /// Deterministic TCP port for an explicit <paramref name="directory"/>, derived via the shared
        /// <see cref="ProjectIdentity"/> v2 normalization (see <see cref="GeneratePortFromDirectory()"/>).
        /// Exposed so the derivation is unit-testable independent of the process working directory.
        /// </summary>
        public static int GeneratePortFromDirectory(string directory)
            => ProjectIdentity.DerivePortV2(directory);
    }
}
