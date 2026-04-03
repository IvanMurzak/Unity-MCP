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
            "NON-BLOCKING event watcher. Returns immediately with 'Processing' status, " +
            "then sends a notification when a matching event fires. " +
            "Unlike event-subscribe (which blocks), this tool lets you continue working while waiting.\n\n" +
            "USE THIS when you want to be notified about events while doing other work:\n" +
            "  1. event-watch(type='error_logged', timeoutMs=60000) → returns immediately\n" +
            "  2. Continue editing code, running scripts, etc.\n" +
            "  3. If an error occurs → you receive a notification with error details\n\n" +
            "COMPARISON:\n" +
            "- event-subscribe: BLOCKING — use with parallel tool calls for trigger+wait pattern\n" +
            "- event-watch: NON-BLOCKING — use when you want background monitoring\n\n" +
            "COMMON USE CASES:\n" +
            "- Monitor for errors while editing: event-watch(type='error_logged', timeoutMs=120000)\n" +
            "- Wait for compilation after script edit: event-watch(type='compilation_finished', timeoutMs=60000)\n" +
            "- Watch for play mode entry: event-watch(type='play_mode_changed', timeoutMs=30000)\n" +
            "- Watch for custom game event: event-watch(type='stage_cleared', timeoutMs=30000)\n\n" +
            "CUSTOM EVENTS: Same as event-subscribe — add McpEventBus.Push() to game code,\n" +
            "or use script-execute to push events. See event-subscribe description for full guide.\n\n" +
            "NOTE: Watch is ephemeral — if Unity domain reloads (e.g. script recompile), " +
            "the watcher is lost. For compilation events, prefer event-subscribe with parallel calls."
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
                return ResponseCallTool.Error(Error.InvalidTimeout(timeoutMs)).SetRequestID(requestId);

            var watchType = string.IsNullOrEmpty(type) ? "any" : type;

            // Start background watcher — does NOT block the tool response
            _ = Task.Run(async () =>
            {
                try
                {
                    using var cts = new CancellationTokenSource(timeoutMs + 1000);
                    var result = await McpEventBus.WaitAsync(type, timeoutMs, collectAll, cts.Token);

                    ResponseCallTool response;
                    if (result.TimedOut)
                    {
                        response = ResponseCallTool.Success(
                            $"[event-watch] No '{watchType}' events occurred within {timeoutMs}ms timeout."
                        ).SetRequestID(requestId);
                    }
                    else
                    {
                        var eventSummaries = new System.Collections.Generic.List<string>();
                        foreach (var evt in result.Events)
                        {
                            var summary = $"[{evt.Type}] {evt.Message ?? "(no message)"}";
                            if (evt.Source != null)
                                summary += $" (source: {evt.Source})";
                            eventSummaries.Add(summary);
                        }

                        response = ResponseCallTool.Success(
                            $"[event-watch] Caught {result.Events.Count} event(s):\n" +
                            string.Join("\n", eventSummaries)
                        ).SetRequestID(requestId);
                    }

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
