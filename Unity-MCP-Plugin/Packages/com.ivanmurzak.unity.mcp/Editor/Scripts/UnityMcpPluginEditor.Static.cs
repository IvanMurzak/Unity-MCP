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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
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
        public static string LocalHost
        {
            get => Instance.unityConnectionConfig.LocalHost;
            set
            {
                Instance.unityConnectionConfig.LocalHost = value;
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
        public static ConnectionMode ConnectionMode
        {
            get => Instance.unityConnectionConfig.ConnectionMode;
            set
            {
                Instance.unityConnectionConfig.ConnectionMode = value;
                NotifyChanged(Instance.unityConnectionConfig);
            }
        }
        public static string CloudServerUrl => UnityMcpPlugin.UnityConnectionConfig.CloudServerUrl;
        public static string? CloudToken
        {
            get => Instance.unityConnectionConfig.CloudToken;
            set
            {
                Instance.unityConnectionConfig.CloudToken = value;
                NotifyChanged(Instance.unityConnectionConfig);
            }
        }

        public static bool IsAutoGenerateSkills(string agentId)
        {
            var dict = Instance.unityConnectionConfig.SkillAutoGenerate;
            return dict.TryGetValue(agentId, out var enabled) && enabled;
        }

        public static void SetAutoGenerateSkills(string agentId, bool enabled)
        {
            Instance.unityConnectionConfig.SkillAutoGenerate[agentId] = enabled;
            NotifyChanged(Instance.unityConnectionConfig);
        }

        public static string SkillsPath
        {
            get => Instance.unityConnectionConfig.SkillsPath;
            set
            {
                Instance.unityConnectionConfig.SkillsPath = NormalizeSkillsPath(value);
                NotifyChanged(Instance.unityConnectionConfig);
            }
        }

        /// <summary>
        /// Normalises a user-supplied or configurator-supplied skills path so the on-disk
        /// `UserSettings/AI-Game-Developer-Config.json` stays portable across machines and
        /// across path-separator conventions. Rules:
        /// <list type="bullet">
        /// <item>null / empty / whitespace → returned verbatim (caller decides what to do).</item>
        /// <item>Absolute path inside <see cref="ProjectRootPath"/> → rewritten to a project-relative
        /// path with forward slashes (e.g. <c>"C:\proj\.claude\skills"</c> → <c>".claude/skills"</c>).
        /// This is the path-portability fix for committing the config file to version control.</item>
        /// <item>Absolute path outside the project → returned unchanged (the user has explicitly
        /// chosen an external location; we do not rewrite it).</item>
        /// <item>Already-relative path → backslashes converted to forward slashes for cross-platform
        /// diff stability; no other change.</item>
        /// </list>
        /// </summary>
        [return: NotNullIfNotNull("value")]
        public static string? NormalizeSkillsPath(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            // Always use forward slashes in the persisted form for cross-platform diff stability.
            // The `!` is required because the Unity Editor C# compiler does not honor
            // `[NotNullWhen(false)]` on `string.IsNullOrWhiteSpace`, so it does not narrow
            // `value` (declared `string?`) to non-null after the early-return guard above and
            // would otherwise emit CS8602 here. The runtime check above already establishes
            // `value` is non-null at this point.
            var normalized = value!.Replace('\\', '/');

            if (!Path.IsPathRooted(normalized))
                return normalized;

            // Absolute path — check whether it lives inside the project root. If yes, make it relative.
            var root = ProjectRootPath.Replace('\\', '/').TrimEnd('/');
            // Compare case-insensitively on Windows because paths there are not case-sensitive.
            // macOS APFS is also case-insensitive by default, but that is an accepted gap here;
            // the typical AI-Game-Developer-Config.json is committed from Windows machines and
            // the legacy values we auto-heal originate from Windows in practice.
            var comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            if (normalized.StartsWith(root + "/", comparison))
                return normalized.Substring(root.Length + 1);

            if (normalized.Equals(root, comparison))
                return string.Empty;

            // Absolute path outside the project root — leave as-is.
            return normalized;
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
