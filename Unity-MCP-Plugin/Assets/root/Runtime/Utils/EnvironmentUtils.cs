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

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    public static class EnvironmentUtils
    {
        /// <summary>
        /// Environment variable name used to override the MCP server URL.
        /// When set, Unity Editor will automatically connect to the provided URL on startup.
        /// </summary>
        public const string McpServerUrlEnvVar = "UNITY_MCP_SERVER_URL";

        /// <summary>
        /// Returns the MCP server URL from the <see cref="McpServerUrlEnvVar"/> environment variable
        /// or the matching command-line argument, or <c>null</c> if neither is set.
        /// </summary>
        public static string? GetMcpServerUrl()
        {
            var commandLineArgs = ArgsUtils.ParseCommandLineArguments();
            return commandLineArgs.GetValueOrDefault(McpServerUrlEnvVar)
                ?? Environment.GetEnvironmentVariable(McpServerUrlEnvVar);
        }

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
    }
}
