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

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    /// <summary>Process-level opt-out. Set before launching Unity; does not change project settings.</summary>
    public static class McpDisable
    {
        public const string EnvironmentVariable = "UNITY_MCP_DISABLE";

        // No logger, config singleton, Unity API or dependency initialization on this path.
        public static bool IsDisabled => IsEnabledValue(Environment.GetEnvironmentVariable(EnvironmentVariable));

        public static bool IsEnabledValue(string? value)
            => value?.Trim() == "1" || string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }
}
