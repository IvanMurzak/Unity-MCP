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
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using Microsoft.Extensions.Logging;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    /// <summary>
    /// Reads custom command-line arguments passed to the Unity Editor at startup
    /// and applies them to the Unity MCP plugin configuration.
    ///
    /// Supported arguments:
    ///   -mcpServerUrl &lt;url&gt;   Enforces an MCP connection to the given URL.
    ///
    /// Example (from the GameDev-cli 'connect' command):
    ///   Unity.exe -projectPath /path/to/project -mcpServerUrl http://localhost:8080
    /// </summary>
    [InitializeOnLoad]
    public static partial class CommandLineArgs
    {
        static readonly ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger(nameof(CommandLineArgs));

        static CommandLineArgs()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "-mcpServerUrl", StringComparison.OrdinalIgnoreCase))
                {
                    var url = args[i + 1];
                    _logger.LogInformation("{class}: Found -mcpServerUrl argument: {url}",
                        nameof(CommandLineArgs), url);

                    // Use delayCall so that the rest of the editor initialisation
                    // (including other [InitializeOnLoad] classes) finishes first.
                    EditorApplication.delayCall += () => EnforceConnect(url);
                    break;
                }
            }
        }

        /// <summary>
        /// Enforces a connection to the MCP server at the specified URL.
        /// <para>
        /// Updates the <c>host</c> field in <c>AI-Game-Developer-Config.json</c>,
        /// saves the file, then disconnects any existing connection and reconnects
        /// to the new URL.
        /// </para>
        /// <para>
        /// This method is triggered automatically when Unity is opened with the
        /// <c>-mcpServerUrl &lt;url&gt;</c> command-line argument (e.g. by the
        /// <c>gamedev connect</c> CLI command). It can also be called directly
        /// from other Editor scripts when a runtime URL override is needed.
        /// </para>
        /// </summary>
        /// <param name="url">
        /// The MCP server URL to connect to (e.g. <c>http://localhost:8080</c>).
        /// </param>
        public static void EnforceConnect(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogError("{class}.{method}: URL must not be null or empty.",
                    nameof(CommandLineArgs), nameof(EnforceConnect));
                return;
            }

            _logger.LogInformation("{class}.{method}: Enforcing MCP connection to {url}",
                nameof(CommandLineArgs), nameof(EnforceConnect), url);

            try
            {
                // Update the in-memory config
                UnityMcpPlugin.Host = url;
                UnityMcpPlugin.KeepConnected = true;

                // Persist the change to AI-Game-Developer-Config.json
                UnityMcpPlugin.Instance.Save();

                _logger.LogInformation("{class}.{method}: Config saved with host = {url}",
                    nameof(CommandLineArgs), nameof(EnforceConnect), url);

                // Rebuild the plugin instance (no-op if already built) and connect
                UnityMcpPlugin.Instance.BuildMcpPluginIfNeeded();
                _ = UnityMcpPlugin.Connect();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{class}.{method}: Failed to enforce MCP connection to {url}",
                    nameof(CommandLineArgs), nameof(EnforceConnect), url);
            }
        }
    }
}
