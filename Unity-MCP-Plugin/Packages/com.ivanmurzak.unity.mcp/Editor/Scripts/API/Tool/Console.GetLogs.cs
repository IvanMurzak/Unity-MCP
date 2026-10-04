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
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Console
    {
        public const string ConsoleGetLogsToolId = "console-get-logs";
        [AiTool
        (
            ConsoleGetLogsToolId,
            Title = "Console / Get Logs",
            ReadOnlyHint = true,
            IdempotentHint = true
        )]
        [AiSkillDescription("Retrieve Unity Editor logs from the MCP plugin's `LogCollector`, " +
            "optionally filtered by log type or time window. Useful for debugging and monitoring Editor activity.")]
        [AiSkillBody("Retrieves Unity Editor logs. " +
            "Useful for debugging and monitoring Unity Editor activity.\n\n" +
            "## Inputs\n\n" +
            "- `maxEntries` (default 100, minimum 1) — caps the size of the returned array.\n" +
            "- `logTypeFilter` — Unity `LogType` filter; `null` returns all severities.\n" +
            "- `includeStackTrace` (default `false`) — include stack-trace strings in each entry.\n" +
            "- `lastMinutes` (default 0) — when non-zero, only logs from the last N minutes are returned.\n" +
            "- `sinceSequence` (default 0) — every entry carries a `sequence` number. When greater than 0, only entries " +
            "with a higher `sequence` are returned, oldest first, up to `maxEntries`.\n\n" +
            "## Polling\n\n" +
            "To read only what is new, pass the highest `sequence` you have received as `sinceSequence`; if the result " +
            "has `maxEntries` entries, call again with the new highest `sequence` to continue. " +
            "If returned sequences are lower than your cursor, the log restarted.")]
        [Description("Retrieves Unity Editor logs. " +
            "Useful for debugging and monitoring Unity Editor activity. " +
            "Every entry has a `sequence` number: to poll, pass the highest `sequence` you received as `sinceSequence` " +
            "to get only newer entries (oldest first; if the result has `maxEntries` entries, call again with the new highest). " +
            "If returned sequences are lower than your cursor, the log restarted.")]
        public LogEntry[] GetLogs
        (
            [Description("Maximum number of log entries to return. Minimum: 1. Default: 100")]
            int maxEntries = 100,
            [Description("Filter by log type. 'null' means All.")]
            LogType? logTypeFilter = null,
            [Description("Include stack traces in the output. Default: false")]
            bool includeStackTrace = false,
            [Description("Return logs from the last N minutes. If 0, returns all available logs. Default: 0")]
            int lastMinutes = 0,
            [Description("Return only entries whose `sequence` is greater than this value, oldest first. " +
                "If 0, returns the most recent entries. Default: 0")]
            long sinceSequence = 0
        )
        {
            // Validate parameters
            if (maxEntries < 1)
                throw new ArgumentException(Error.InvalidMaxEntries(maxEntries));

            if (sinceSequence < 0)
                throw new ArgumentException(Error.InvalidSinceSequence(sinceSequence));

            if (!UnityMcpPluginEditor.HasInstance)
                throw new InvalidOperationException("UnityMcpPluginEditor is not initialized.");

            var logCollector = UnityMcpPluginEditor.Instance.LogCollector;
            if (logCollector == null)
                throw new InvalidOperationException("LogCollector is not initialized.");

            if (sinceSequence > 0)
            {
                return logCollector.QuerySince(
                    sinceSequence: sinceSequence,
                    maxEntries: maxEntries,
                    logTypeFilter: logTypeFilter,
                    includeStackTrace: includeStackTrace,
                    lastMinutes: lastMinutes
                );
            }

            return logCollector.Query(
                maxEntries: maxEntries,
                logTypeFilter: logTypeFilter,
                includeStackTrace: includeStackTrace,
                lastMinutes: lastMinutes
            );
        }
    }
}