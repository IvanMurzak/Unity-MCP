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
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Player
{
    /// <summary>
    /// Builder API for configuring and initializing UnityMcpPlugin in player (runtime) builds.
    /// Use this instead of config files to programmatically set up MCP at runtime.
    /// <example>
    /// <code>
    /// var plugin = new UnityMcpPluginBuilder()
    ///     .WithHost("http://localhost:50000")
    ///     .WithLogLevel(LogLevel.Warning)
    ///     .WithTransport(TransportMethod.streamableHttp)
    ///     .Build();
    /// </code>
    /// </example>
    /// </summary>
    public class UnityMcpPluginBuilder
    {
        private readonly UnityMcpPlugin.UnityConnectionConfig _config = new();

        public UnityMcpPluginBuilder WithHost(string host)
        {
            _config.Host = host;
            return this;
        }

        public UnityMcpPluginBuilder WithLogLevel(LogLevel logLevel)
        {
            _config.LogLevel = logLevel;
            return this;
        }

        public UnityMcpPluginBuilder WithKeepConnected(bool keepConnected)
        {
            _config.KeepConnected = keepConnected;
            return this;
        }

        public UnityMcpPluginBuilder WithTimeout(int timeoutMs)
        {
            _config.TimeoutMs = timeoutMs;
            return this;
        }

        public UnityMcpPluginBuilder WithTransport(TransportMethod transport)
        {
            _config.TransportMethod = transport;
            return this;
        }

        /// <summary>
        /// Creates and initializes the UnityMcpPlugin singleton with the configured settings.
        /// Builds the MCP plugin instance, making it ready for connection.
        /// Call <see cref="UnityMcpPlugin.Connect"/> afterwards to establish the connection.
        /// </summary>
        public UnityMcpPlugin Build()
        {
            var plugin = UnityMcpPlugin.CreateInstance(_config);
            plugin.BuildMcpPluginIfNeeded();
            return plugin;
        }
    }
}
