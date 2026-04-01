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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Console
    {
        public const string ConsoleClearLogsToolId = "console-clear-logs";
        [McpPluginTool
        (
            ConsoleClearLogsToolId,
            Title = "Console / Clear Logs",
            Enabled = false,
            DestructiveHint = true,
            IdempotentHint = true
        )]
        [Description("Clears the MCP log cache (used by console-get-logs) and the Unity Editor Console window. " +
            "Useful for isolating errors related to a specific action by clearing logs before performing the action.")]
        public void ClearLogs(string? nothing = null)
        {
            MainThread.Instance.Run(() =>
            {
                Debug.ClearDeveloperConsole();

                if (!UnityMcpPluginEditor.HasInstance)
                    throw new InvalidOperationException("UnityMcpPluginEditor is not initialized.");

                var logCollector = UnityMcpPluginEditor.Instance.LogCollector;
                if (logCollector == null)
                    throw new InvalidOperationException("LogCollector is not initialized.");

                logCollector.Clear();
            });
        }
    }
}
