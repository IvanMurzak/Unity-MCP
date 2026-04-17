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
using System.Collections.Generic;
using com.IvanMurzak.McpPlugin.Common.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    public static class EnvironmentUtils
    {
        static readonly ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger(nameof(EnvironmentUtils));
        // Environment variable names for MCP connection overrides.
        // These override values loaded from the JSON config file. On first run (when the config
        // is newly created), overrides are applied before Save() so they are persisted to disk.
        // On subsequent runs the config already exists, so overrides are applied at runtime only.
        // Exception: UNITY_MCP_TOOLS uses [JsonIgnore] and is never persisted.
        public const string EnvHost = "UNITY_MCP_HOST";
        public const string EnvKeepConnected = "UNITY_MCP_KEEP_CONNECTED";
        public const string EnvAuthOption = "UNITY_MCP_AUTH_OPTION";
        public const string EnvToken = "UNITY_MCP_TOKEN";
        public const string EnvTools = "UNITY_MCP_TOOLS";
        public const string EnvStartServer = "UNITY_MCP_START_SERVER";
        public const string EnvTransport = "UNITY_MCP_TRANSPORT";
        public const string EnvCloudUrl = "UNITY_MCP_CLOUD_URL";

        /// <summary>
        /// Checks if the current environment is a CI environment.
        /// </summary>
        public static bool IsCi()
        {
            var commandLineArgs = ArgsUtils.ParseCommandLineArguments();

            var ci = commandLineArgs.GetValueOrDefault("CI") ?? Environment.GetEnvironmentVariable("CI");
            var gha = commandLineArgs.GetValueOrDefault("GITHUB_ACTIONS") ?? Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
            var az = commandLineArgs.GetValueOrDefault("TF_BUILD") ?? Environment.GetEnvironmentVariable("TF_BUILD"); // Azure Pipelines

            return string.Equals(ci?.Trim()?.Trim('"'), "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(gha?.Trim()?.Trim('"'), "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(az?.Trim()?.Trim('"'), "true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Applies environment variable (or command-line argument) overrides to the given config.
        /// Checks command-line args first, then falls back to process environment variables.
        /// Invalid or missing values are silently ignored, leaving the config field unchanged.
        /// On first run (when the config is newly created), overrides are applied before Save()
        /// so they are persisted to disk. On subsequent runs they act as runtime-only overrides.
        /// Exception: <see cref="EnvTools"/> (<c>UNITY_MCP_TOOLS</c>) targets a
        /// <c>[JsonIgnore]</c> property and is never persisted regardless of timing.
        /// </summary>
        public static void ApplyEnvironmentOverrides(UnityMcpPlugin.UnityConnectionConfig config)
        {
            var args = ArgsUtils.ParseCommandLineArguments();

            // Host URL override
            var host = args.GetValueOrDefault(EnvHost) ?? Environment.GetEnvironmentVariable(EnvHost);
            if (!string.IsNullOrWhiteSpace(host))
            {
                config.Host = host.Trim().Trim('"');
                _logger.LogInformation("[MCP] Env override: {Key}={Value}", EnvHost, config.Host);
            }

            // KeepConnected (active connection) override
            var keepConnected = args.GetValueOrDefault(EnvKeepConnected) ?? Environment.GetEnvironmentVariable(EnvKeepConnected);
            if (!string.IsNullOrWhiteSpace(keepConnected)
                && bool.TryParse(keepConnected.Trim().Trim('"'), out var kc))
            {
                config.KeepConnected = kc;
                _logger.LogInformation("[MCP] Env override: {Key}={Value}", EnvKeepConnected, config.KeepConnected);
            }

            // AuthOption override (none / required)
            var authOption = args.GetValueOrDefault(EnvAuthOption) ?? Environment.GetEnvironmentVariable(EnvAuthOption);
            if (!string.IsNullOrWhiteSpace(authOption)
                && Enum.TryParse<AuthOption>(authOption.Trim().Trim('"'), ignoreCase: true, out var ao))
            {
                config.AuthOption = ao;
                _logger.LogInformation("[MCP] Env override: {Key}={Value}", EnvAuthOption, config.AuthOption);
            }

            // Auth token override
            var token = args.GetValueOrDefault(EnvToken) ?? Environment.GetEnvironmentVariable(EnvToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                config.Token = token.Trim().Trim('"');
                _logger.LogInformation("[MCP] Env override: {Key}=***", EnvToken);
            }

            // Transport method override (streamableHttp / stdio)
            var transport = args.GetValueOrDefault(EnvTransport) ?? Environment.GetEnvironmentVariable(EnvTransport);
            if (!string.IsNullOrWhiteSpace(transport)
                && Enum.TryParse<TransportMethod>(transport.Trim().Trim('"'), ignoreCase: true, out var tm))
            {
                config.TransportMethod = tm;
                _logger.LogInformation("[MCP] Env override: {Key}={Value}", EnvTransport, config.TransportMethod);
            }

            // Start server override — controls whether MCP server auto-starts in streamableHttp mode
            var startServer = args.GetValueOrDefault(EnvStartServer) ?? Environment.GetEnvironmentVariable(EnvStartServer);
            if (!string.IsNullOrWhiteSpace(startServer)
                && bool.TryParse(startServer.Trim().Trim('"'), out var ss))
            {
                config.KeepServerRunning = ss;
                _logger.LogInformation("[MCP] Env override: {Key}={Value}", EnvStartServer, config.KeepServerRunning);
            }

            // Enabled tools override — comma-separated tool IDs
            var tools = args.GetValueOrDefault(EnvTools) ?? Environment.GetEnvironmentVariable(EnvTools);
            if (!string.IsNullOrWhiteSpace(tools))
            {
                var ids = tools.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var trimmed = new List<string>(ids.Length);
                foreach (var id in ids)
                {
                    var value = id.Trim().Trim('"');
                    if (!string.IsNullOrWhiteSpace(value) && seen.Add(value))
                        trimmed.Add(value);
                }
                config.EnabledToolsOverride = trimmed;
                _logger.LogInformation("[MCP] Env override: {Key}={Value}", EnvTools, tools.Trim());
            }
        }
    }
}
