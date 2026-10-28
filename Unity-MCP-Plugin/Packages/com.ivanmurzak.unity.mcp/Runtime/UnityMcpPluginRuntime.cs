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
using System.Diagnostics;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP
{
    /// <summary>
    /// Runtime singleton for game builds. Provides the developer-facing
    /// <see cref="Initialize"/> API and maintains a separate MCP connection
    /// alongside the Editor's connection.
    /// </summary>
    public partial class UnityMcpPluginRuntime : UnityMcpPlugin
    {
        protected UnityMcpPluginRuntime(UnityConnectionConfig? config = null) : base()
        {
            unityConnectionConfig = config ?? new UnityConnectionConfig();
            IncrementSingletonCount();
        }

        /// <summary>
        /// Declares the connection a <see cref="ClientRole.Runtime"/> (McpPlugin sends it as <c>client_role</c> at connect):
        /// the MCP server keeps a running player beside the Editor instead of letting it replace the Editor
        /// (GameDev-MCP-Server plugin roles). Applied when the plugin is built, after everything the developer configured,
        /// so a game never registers as the Editor.
        /// </summary>
        protected static void DeclareRuntimeRole(IMcpPluginBuilder builder)
            => builder.WithConfig(config => config.ClientRole = ClientRole.Runtime);

        internal IMcpPlugin BuildFromBuilder(IMcpPluginBuilder builder)
        {
            _logger.LogTrace("{method} called.", nameof(BuildFromBuilder));

            // Last, so neither a WithConfig nor a SetConfig the developer made after Initialize can undo it.
            DeclareRuntimeRole(builder);

            var stopwatch = Stopwatch.StartNew();
            var built = _plugin.Set(builder.Build(CreateDefaultReflector()));
            stopwatch.Stop();
            _logger.LogDebug("Runtime MCP Plugin built in {elapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);

            ApplyConfigToMcpPlugin(built);

            built.ConnectionState
                .Subscribe(state => _connectionState.Value = state)
                .AddTo(_disposables);

            _logger.LogTrace("{method} completed.", nameof(BuildFromBuilder));
            return built;
        }

        public override void Dispose()
        {
            DecrementSingletonCount();
            base.Dispose();
            _plugin.Dispose();
        }
    }
}
