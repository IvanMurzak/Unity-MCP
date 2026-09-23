/*
┌──────────────────────────────────────────────────────────────────┐
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Event
    {
        public const string EventWatchToolId = "event-watch";

        [McpPluginTool
        (
            EventWatchToolId,
            Title = "Event / Watch"
        )]
        [Description(
            "NON-BLOCKING event watcher at MCP server level. Returns 'Processing' status immediately, " +
            "then sends a notification via NotifyToolRequestCompleted when a matching event fires.\n\n" +
            "IMPORTANT LIMITATION: Some MCP clients (including Claude Code) wait for the notification " +
            "before proceeding to the next turn, making this effectively blocking from the AI agent's perspective. " +
            "If your client blocks on Processing responses, use event-subscribe instead.\n\n" +
            "This tool is useful for MCP clients that can handle Processing responses asynchronously, " +
            "or for scenarios where you want the server to notify you without keeping a blocking tool call open.\n\n" +
            "USE CASES:\n" +
            "- Clients that support async Processing: background error monitoring\n" +
            "- Long-running watches where you want server-side timeout management\n\n" +
            "For most use cases, prefer event-subscribe:\n" +
            "  script-execute(trigger async action) → event-subscribe(type='my_event', timeoutMs=30000)\n\n" +
            "CUSTOM EVENTS: Same as event-subscribe — see event-subscribe description for full guide.\n\n" +
            "NOTE: Watch is ephemeral — if Unity domain reloads (e.g. script recompile), the watcher is lost."
        )]
        public ResponseCallTool Watch
        (
            [Description(
                "Event type to watch for. " +
                "Use a specific type like 'error_logged' or 'compilation_finished'. " +
                "Empty string or null matches ANY event type. " +
                "Use 'event-list' tool to see all available types."
            )]
            string? type = null,

            [Description(
                "Maximum watch time in milliseconds. " +
                "Range: 5000-120000. Default: 60000 (60 seconds). " +
                "The watcher is automatically cancelled after this timeout."
            )]
            int timeoutMs = 60000,

            [Description(
                "If true, collects ALL matching events until timeout, then notifies once. " +
                "If false (default), notifies immediately on the FIRST matching event."
            )]
            bool collectAll = false,

            [RequestID]
            string? requestId = null
        )
        {
            if (requestId == null || string.IsNullOrWhiteSpace(requestId))
                return ResponseCallTool.Error("Original request with valid RequestID must be provided.");

            if (timeoutMs < 5000 || timeoutMs > 120000)
                return ResponseCallTool.Error(Error.InvalidWatchTimeout(timeoutMs)).SetRequestID(requestId);

            var watchType = string.IsNullOrEmpty(type) ? "any" : type;

            // Start background watcher — does NOT block the tool response
            _ = Task.Run(async () =>
            {
                try
                {
                    using var cts = new CancellationTokenSource(timeoutMs + 1000);
                    var watchResult = await McpEventBus.WaitAsync(type, timeoutMs, collectAll, cts.Token);

                    var json = System.Text.Json.JsonSerializer.SerializeToNode(watchResult);
                    var response = ResponseCallValueTool<McpEventSubscribeResult>
                        .SuccessStructured(json)
                        .SetRequestID(requestId);

                    await UnityMcpPluginEditor.NotifyToolRequestCompleted(new RequestToolCompletedData
                    {
                        RequestId = requestId,
                        Result = response
                    });
                }
                catch (Exception ex)
                {
                    try
                    {
                        await UnityMcpPluginEditor.NotifyToolRequestCompleted(new RequestToolCompletedData
                        {
                            RequestId = requestId,
                            Result = ResponseCallTool.Error(
                                $"[event-watch] Watcher failed: {ex.Message}"
                            ).SetRequestID(requestId)
                        });
                    }
                    catch
                    {
                        // Connection lost — nothing we can do
                    }
                }
            });

            return ResponseCallTool.Processing(
                $"Watching for '{watchType}' events (timeout: {timeoutMs}ms). " +
                "You will be notified when a matching event occurs. Continue working."
            ).SetRequestID(requestId);
        }
    }
}
