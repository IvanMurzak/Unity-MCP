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
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    public static class EnvironmentUtils
    {
        // Environment variable names for MCP connection overrides.
        // These override values loaded from the JSON config file and are never persisted to disk.
        public const string EnvHost = "UNITY_MCP_HOST";
        public const string EnvKeepConnected = "UNITY_MCP_KEEP_CONNECTED";
        public const string EnvAuthOption = "UNITY_MCP_AUTH_OPTION";
        public const string EnvToken = "UNITY_MCP_TOKEN";

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
        /// Overrides are NOT persisted to disk.
        /// </summary>
        public static void ApplyEnvironmentOverrides(UnityMcpPlugin.UnityConnectionConfig config)
        {
            var args = ArgsUtils.ParseCommandLineArguments();

            // Host URL override
            var host = args.GetValueOrDefault(EnvHost) ?? Environment.GetEnvironmentVariable(EnvHost);
            if (!string.IsNullOrWhiteSpace(host))
                config.Host = host.Trim().Trim('"');

            // KeepConnected (active connection) override
            var keepConnected = args.GetValueOrDefault(EnvKeepConnected) ?? Environment.GetEnvironmentVariable(EnvKeepConnected);
            if (!string.IsNullOrWhiteSpace(keepConnected)
                && bool.TryParse(keepConnected.Trim().Trim('"'), out var kc))
                config.KeepConnected = kc;

            // AuthOption override (none / required)
            var authOption = args.GetValueOrDefault(EnvAuthOption) ?? Environment.GetEnvironmentVariable(EnvAuthOption);
            if (!string.IsNullOrWhiteSpace(authOption)
                && Enum.TryParse<AuthOption>(authOption.Trim().Trim('"'), ignoreCase: true, out var ao))
                config.AuthOption = ao;

            // Auth token override
            var token = args.GetValueOrDefault(EnvToken) ?? Environment.GetEnvironmentVariable(EnvToken);
            if (!string.IsNullOrWhiteSpace(token))
                config.Token = token.Trim().Trim('"');
        }
    }
}
