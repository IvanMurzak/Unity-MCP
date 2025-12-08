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
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Console
    {
        [McpPluginTool
        (
            "Console_GetLogs",
            Title = "Get Unity Console Logs"
        )]
        [Description("Retrieves the Unity Console log entries. Supports filtering by log type and limiting the number of entries returned.")]
        public async Task<ResponseCallValueTool<LogEntry[]>> GetLogs
        (
            [Description("Maximum number of log entries to return. Default: 100")]
            int maxEntries = 100,
            [Description("Filter by log type. 'null' means All.")]
            LogType? logTypeFilter = null,
            [Description("Include stack traces in the output. Default: false")]
            bool includeStackTrace = false,
            [Description("Return logs from the last N minutes. If 0, returns all available logs. Default: 0")]
            int lastMinutes = 0
        )
        {
            try
            {
                // Validate parameters
                if (maxEntries < 1)
                    return ResponseCallValueTool<LogEntry[]>.Error(Error.InvalidMaxEntries(maxEntries));

                var logCollector = UnityMcpPlugin.Instance.LogCollector;
                if (logCollector == null)
                    return ResponseCallValueTool<LogEntry[]>.Error("[Error] LogCollector is not initialized.");

                // Get all log entries as array to avoid concurrent modification
                var logs = await logCollector.QueryAsync(
                    maxEntries: maxEntries,
                    logTypeFilter: logTypeFilter,
                    includeStackTrace: includeStackTrace,
                    lastMinutes: lastMinutes
                );

                var result = UnityMcpPlugin.Instance.McpPluginInstance!.McpManager.Reflector.JsonSerializer.SerializeToNode(logs);
                var response = ResponseCallValueTool<LogEntry[]>.SuccessStructured(result, result?.ToJsonString());
                return response;
            }
            catch (Exception ex)
            {
                return ResponseCallValueTool<LogEntry[]>.Error($"[Error] Failed to retrieve console logs: {ex.Message}");
            }
        }
    }
}