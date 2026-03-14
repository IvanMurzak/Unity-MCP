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
using System.ComponentModel;
using System.Reflection;
using com.IvanMurzak.McpPlugin;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Console
    {
        public const string ConsoleClearLogsToolId = "console-clear-logs";
        [McpPluginTool
        (
            ConsoleClearLogsToolId,
            Title = "Console / Clear Logs",
            DestructiveHint = true,
            IdempotentHint = true
        )]
        [Description("Clears the Unity Editor Console window and optionally the MCP log cache (used by console-get-logs). " +
            "Useful for isolating errors related to a specific action by clearing logs before performing the action.")]
        public string ClearLogs(
            [Description("Whether to also clear the MCP log cache used by console-get-logs. Default: true")]
            bool clearMcpCache = true
        )
        {
            // Clear the Unity Editor Console window via reflection (LogEntries is internal API)
            var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor.CoreModule")
                              ?? Type.GetType("UnityEditorInternal.LogEntries, UnityEditor.CoreModule")
                              ?? Type.GetType("UnityEditor.LogEntries, UnityEditor")
                              ?? Type.GetType("UnityEditorInternal.LogEntries, UnityEditor");
            logEntriesType?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public)
                          ?.Invoke(null, null);

            if (clearMcpCache)
            {
                if (!UnityMcpPluginEditor.HasInstance)
                    throw new InvalidOperationException("UnityMcpPluginEditor is not initialized.");

                var logCollector = UnityMcpPluginEditor.Instance.LogCollector;
                if (logCollector == null)
                    throw new InvalidOperationException("LogCollector is not initialized.");

                logCollector.Clear();
            }

            return clearMcpCache
                ? "[Success] Cleared Unity Console and MCP log cache."
                : "[Success] Cleared Unity Console. MCP log cache preserved.";
        }
    }
}
