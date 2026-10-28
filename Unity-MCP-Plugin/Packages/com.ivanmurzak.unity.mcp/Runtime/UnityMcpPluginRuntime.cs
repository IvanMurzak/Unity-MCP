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
        /// Appended to the handshake's <c>environment</c> (the Unity version): the MCP server classifies a connection whose
        /// environment ends in <c>(runtime)</c> as a game, so a running player is kept beside the Editor instead of replacing
        /// it (GameDev-MCP-Server plugin roles). A <c>client_role=runtime</c> hub parameter would say it at connect time, but
        /// McpPlugin does not let a plugin add one yet.
        /// </summary>
        public const string RuntimeEnvironmentSuffix = " (runtime)";

        protected override McpPlugin.Common.Version BuildVersion()
        {
            var version = base.BuildVersion();
            version.Environment += RuntimeEnvironmentSuffix;
            return version;
        }

        internal IMcpPlugin BuildFromBuilder(IMcpPluginBuilder builder)
        {
            _logger.LogTrace("{method} called.", nameof(BuildFromBuilder));

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
