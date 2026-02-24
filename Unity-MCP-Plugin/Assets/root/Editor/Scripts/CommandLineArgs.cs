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
using System.IO;
using System.Text.Json;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using Microsoft.Extensions.Logging;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    /// <summary>
    /// Carries the outcome of a <see cref="CommandLineArgs.EnforceConnect"/> call.
    /// When <see cref="Success"/> is <c>false</c>, the <see cref="ErrorMessage"/>,
    /// <see cref="ErrorType"/>, and <see cref="StackTrace"/> fields contain
    /// detailed diagnostic information suitable for display in a terminal.
    /// </summary>
    public readonly struct ConnectResult
    {
        /// <summary>Gets a value indicating whether the connection was established successfully.</summary>
        public bool Success { get; }

        /// <summary>Human-readable error description, or <c>null</c> on success.</summary>
        public string? ErrorMessage { get; }

        /// <summary>Full exception type name (e.g. <c>System.InvalidOperationException</c>), or <c>null</c> on success.</summary>
        public string? ErrorType { get; }

        /// <summary>Exception stack trace, or <c>null</c> on success.</summary>
        public string? StackTrace { get; }

        private ConnectResult(bool success, string? errorMessage = null, string? errorType = null, string? stackTrace = null)
        {
            Success = success;
            ErrorMessage = errorMessage;
            ErrorType = errorType;
            StackTrace = stackTrace;
        }

        /// <summary>Creates a successful result.</summary>
        public static ConnectResult Ok() => new ConnectResult(true);

        /// <summary>Creates a failure result with a plain error message.</summary>
        public static ConnectResult Fail(string errorMessage) =>
            new ConnectResult(false, errorMessage);

        /// <summary>Creates a failure result from an exception, capturing type and stack trace.</summary>
        public static ConnectResult Fail(Exception exception) =>
            new ConnectResult(
                false,
                exception.Message,
                exception.GetType().FullName,
                exception.StackTrace);

        /// <inheritdoc/>
        public override string ToString() => Success
            ? "[Success] MCP connection established"
            : $"[Error] {ErrorType}: {ErrorMessage}\n{StackTrace}";
    }

    /// <summary>
    /// Reads custom command-line arguments passed to the Unity Editor at startup
    /// and applies them to the Unity MCP plugin configuration.
    ///
    /// Supported arguments:
    ///   -mcpServerUrl &lt;url&gt;   Enforces an MCP connection to the given URL.
    ///
    /// Example (from the GameDev-cli 'connect' command):
    ///   Unity.exe -projectPath /path/to/project -mcpServerUrl http://localhost:8080
    ///
    /// When called from the command line, the result is written to
    /// <c>Library/mcp-connect-result.json</c> so the CLI can poll for the outcome.
    /// On failure, <see cref="EditorApplication.Exit"/> is called with exit code 1.
    /// </summary>
    [InitializeOnLoad]
    public static partial class CommandLineArgs
    {
        static readonly ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger(nameof(CommandLineArgs));

        /// <summary>
        /// Path to the connect-result file relative to the Unity project root.
        /// Written by <see cref="EnforceConnect"/> when triggered via command-line arg.
        /// The GameDev-cli <c>connect --wait</c> command polls this file.
        /// </summary>
        public const string ConnectResultRelativePath = "Library/mcp-connect-result.json";

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
                    EditorApplication.delayCall += () =>
                    {
                        var result = EnforceConnect(url);
                        WriteConnectResult(result);

                        if (!result.Success)
                        {
                            _logger.LogError(
                                "{class}: EnforceConnect failed — exiting with code 1.\n{result}",
                                nameof(CommandLineArgs), result.ToString());
                            EditorApplication.Exit(1);
                        }
                    };
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
        /// <para>
        /// When called from the command line, the outcome is written to
        /// <c>Library/mcp-connect-result.json</c> in the project root and, on
        /// failure, Unity exits with code 1 so the calling process can detect errors.
        /// </para>
        /// </summary>
        /// <param name="url">
        /// The MCP server URL to connect to (e.g. <c>http://localhost:8080</c>).
        /// </param>
        /// <returns>
        /// A <see cref="ConnectResult"/> indicating success or carrying detailed
        /// error information (message, exception type, stack trace).
        /// </returns>
        public static ConnectResult EnforceConnect(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                var message = "URL must not be null or empty.";
                _logger.LogError("{class}.{method}: {message}",
                    nameof(CommandLineArgs), nameof(EnforceConnect), message);
                return ConnectResult.Fail(message);
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

                _logger.LogInformation("{class}.{method}: Connect() called for {url}",
                    nameof(CommandLineArgs), nameof(EnforceConnect), url);

                return ConnectResult.Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{class}.{method}: Failed to enforce MCP connection to {url}",
                    nameof(CommandLineArgs), nameof(EnforceConnect), url);
                return ConnectResult.Fail(e);
            }
        }

        /// <summary>
        /// Writes the <see cref="ConnectResult"/> to
        /// <c>Library/mcp-connect-result.json</c> inside the current Unity project.
        /// The file is consumed by the GameDev-cli <c>connect --wait</c> command.
        /// </summary>
        private static void WriteConnectResult(ConnectResult result)
        {
            try
            {
                var projectRoot = Path.GetDirectoryName(Application.dataPath)!;
                var resultPath = Path.Combine(projectRoot, ConnectResultRelativePath);

                var payload = new
                {
                    success = result.Success,
                    errorType = result.ErrorType,
                    errorMessage = result.ErrorMessage,
                    stackTrace = result.StackTrace,
                    timestamp = DateTimeOffset.UtcNow.ToString("o"),
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });

                File.WriteAllText(resultPath, json);

                _logger.LogInformation("{class}: Connect result written to {path}",
                    nameof(CommandLineArgs), resultPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{class}: Failed to write connect result file",
                    nameof(CommandLineArgs));
            }
        }
    }
}
