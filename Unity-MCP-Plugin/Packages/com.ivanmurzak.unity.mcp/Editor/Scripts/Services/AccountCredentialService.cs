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
using com.IvanMurzak.McpPlugin.AgentConfig;
using com.IvanMurzak.Unity.MCP.Utils;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP.Editor.Services
{
    /// <summary>
    /// Editor-side owner of the shared machine-credential account credential (mcp-authorize design 06 / D12).
    /// It wraps McpPlugin 7.0's <see cref="PluginCredentialProvider"/> over the machine store
    /// (<c>~/.ai-game-dev/credentials.json</c>) + a <see cref="UnityTokenRefresher"/>, and implements the
    /// zero-button boot behaviour:
    /// <list type="bullet">
    ///   <item><b>Auto-adopt:</b> the provider reads the machine store at construction — a present credential
    ///   means the plugin is <see cref="AuthState.SignedIn"/> with no UI interaction.</item>
    ///   <item><b>Proactive refresh at connect:</b> <see cref="Initialize"/> points
    ///   <see cref="UnityMcpPlugin.UnityConnectionConfig.CloudCredentialProvider"/> at the provider's
    ///   proactively-refreshed access-token callback, so a Cloud-mode SignalR connection presents the
    ///   machine-store JWT automatically (falling back to the mode-routed token when signed out).</item>
    ///   <item><b>On-401 refresh + reconnect:</b> <see cref="AttachTo"/> wires a
    ///   <see cref="ConnectionCredentialCoordinator"/> so a 3-strike authorization rejection refreshes the
    ///   token and reconnects without any UI.</item>
    /// </list>
    /// Credentials NEVER land in a project file / VCS — only in the protected machine store (0600 on POSIX /
    /// DPAPI on Windows), which is shared by every engine plugin, CLI, and the local server on the machine.
    /// </summary>
    public static class AccountCredentialService
    {
        static readonly object _gate = new object();
        static readonly ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger(nameof(AccountCredentialService));

        static PluginCredentialProvider? _provider;
        static ConnectionCredentialCoordinator? _coordinator;
        static bool _wired;

        /// <summary>
        /// The machine-store-backed credential provider (lazily constructed). Reads
        /// <c>~/.ai-game-dev/credentials.json</c> once per domain; a present credential auto-adopts as
        /// <see cref="AuthState.SignedIn"/>.
        /// </summary>
        public static PluginCredentialProvider Provider
        {
            get
            {
                lock (_gate)
                    return _provider ??= CreateDefaultProvider();
            }
        }

        /// <summary>True when the machine store holds a usable credential (zero-button auto-adopt).</summary>
        public static bool IsSignedIn => Provider.IsSignedIn;

        static PluginCredentialProvider CreateDefaultProvider()
        {
            var store = new MachineCredentialStore();
            var refresher = new UnityTokenRefresher(UnityMcpPlugin.UnityConnectionConfig.CloudServerBaseUrl);
            return new PluginCredentialProvider(store, refresher, _logger);
        }

        /// <summary>
        /// Point the connection config's Cloud credential provider at the machine-store-backed, proactively
        /// refreshed access token. Idempotent — the callback always reflects the current provider state, so a
        /// credential adopted later (via <see cref="Adopt"/>) is presented on the next (re)connect without
        /// re-wiring. Safe to call on every editor boot / domain reload.
        /// </summary>
        public static void Initialize()
        {
            lock (_gate)
            {
                if (_wired)
                    return;
                var provider = _provider ??= CreateDefaultProvider();
                UnityMcpPlugin.UnityConnectionConfig.CloudCredentialProvider = provider.AsAccessTokenProvider();
                _wired = true;
            }
        }

        /// <summary>
        /// Attach the auth-rejection → refresh → reconnect coordinator to the freshly-built plugin (design 06,
        /// on-401 recovery). No-op when no machine credential is present (nothing to refresh). Replaces any
        /// previous coordinator so a rebuilt plugin never leaks a stale subscription.
        /// </summary>
        public static void AttachTo(IMcpPlugin? plugin)
        {
            if (plugin == null)
                return;
            Initialize();
            lock (_gate)
            {
                _coordinator?.Dispose();
                _coordinator = null;
                if (_provider != null && _provider.IsSignedIn)
                    _coordinator = new ConnectionCredentialCoordinator(plugin, _provider, _logger);
            }
        }

        /// <summary>
        /// Persist a freshly obtained device-flow credential into the machine store and adopt it
        /// (<see cref="AuthState.SignedIn"/>). Shared once-per-machine across every engine plugin / CLI.
        /// </summary>
        public static void Adopt(MachineCredentials credentials)
        {
            if (credentials == null)
                throw new ArgumentNullException(nameof(credentials));
            Initialize();
            Provider.Adopt(credentials);
        }

        /// <summary>Sign out: delete the stored credential and drop the on-401 coordinator.</summary>
        public static void SignOut()
        {
            lock (_gate)
            {
                _provider?.SignOut();
                _coordinator?.Dispose();
                _coordinator = null;
            }
        }
    }
}
