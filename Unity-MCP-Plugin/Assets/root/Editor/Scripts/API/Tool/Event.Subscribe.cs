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

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Event
    {
        public const string EventSubscribeToolId = "event-subscribe";

        [McpPluginTool
        (
            EventSubscribeToolId,
            Title = "Event / Subscribe"
        )]
        [Description(
            "Waits for a Unity event and returns it. BLOCKING — holds response until event fires or timeout.\n\n" +
            "IMPORTANT: This is a BLOCKING call. Use PARALLEL tool calls to subscribe and trigger simultaneously:\n" +
            "  [parallel] event-subscribe(type='my_event', timeoutMs=15000) + script-execute(trigger action)\n" +
            "  The subscribe waits while the action executes; when the event fires, subscribe returns immediately.\n\n" +
            "== BUILT-IN EVENTS (no code changes needed) ==\n" +
            "- play_mode_changed: Play/Pause/Stop transitions\n" +
            "- scene_loaded / scene_opened: Scene loading\n" +
            "- compilation_started / compilation_finished: Script compilation (hasErrors flag)\n" +
            "- error_logged / warning_logged: Console messages\n" +
            "- pause_state_changed: Editor pause toggle\n" +
            "- hierarchy_changed: GO created/destroyed/reparented (NOTE: fires on ANY hierarchy change, not just yours)\n" +
            "- selection_changed: Editor selection\n\n" +
            "== CUSTOM EVENTS (for game logic — RECOMMENDED) ==\n" +
            "Built-in events CANNOT detect game-specific moments like 'server callback done' or 'popup closed'.\n" +
            "Do NOT use hierarchy_changed as a proxy for server responses — it fires on unrelated changes.\n\n" +
            "BEST: Dynamic hook via script-execute (NO source code modification, auto-cleanup on play mode exit):\n" +
            "  Step 1 — Hook game event to McpEventBus (run once before test):\n" +
            "    script-execute (IMPORTANT: add 'using com.IvanMurzak.Unity.MCP.Editor.API;' for McpEventBus):\n" +
            "      SomeManager.Instance.OnDataLoaded += () => McpEventBus.Push(\"data_loaded\");\n" +
            "      SomePopup.OnClosed += () => McpEventBus.Push(\"popup_closed\");\n" +
            "  Step 2 — Trigger action + subscribe in parallel:\n" +
            "    [parallel] event-subscribe(type='data_loaded') + script-execute(trigger action)\n" +
            "  Step 3 — Hooks die automatically when play mode stops. No cleanup needed.\n\n" +
            "ALTERNATIVE: Add McpEventBus.Push() directly in game code (persistent, survives sessions):\n" +
            "  #if UNITY_EDITOR\n" +
            "  McpEventBus.Push(\"friend_data_loaded\", source: \"FriendManager\");\n" +
            "  #endif\n\n" +
            "Custom events appear in event-list(filter='custom') after first Push.\n\n" +
            "== USAGE PATTERNS ==\n" +
            "1. Dynamic hook + parallel subscribe (RECOMMENDED for playtesting):\n" +
            "   script-execute(hook game events to McpEventBus)\n" +
            "   [parallel] event-subscribe(type='my_event') + script-execute(trigger action)\n" +
            "2. Parallel subscribe + trigger (for built-in events):\n" +
            "   [parallel] event-subscribe(type='compilation_finished') + script-update-or-create(...)\n" +
            "3. Sequential drain (when event already happened):\n" +
            "   script-execute(action) → event-subscribe(type='x', timeoutMs=0) to drain pending\n" +
            "4. Background monitoring (non-blocking): use event-watch instead"
        )]
        public async Task<McpEventSubscribeResult> Subscribe
        (
            [Description(
                "Event type to filter for. " +
                "Use a specific type like 'error_logged' or 'compilation_finished'. " +
                "Empty string or null matches ANY event type. " +
                "Use 'event-list' tool to see all available types."
            )]
            string? type = null,

            [Description(
                "Maximum wait time in milliseconds. " +
                "Range: 0-120000. Default: 30000 (30 seconds). " +
                "Set to 0 to drain pending events without waiting."
            )]
            int timeoutMs = 30000,

            [Description(
                "If true, collects ALL matching events until timeout. " +
                "If false (default), returns immediately after the FIRST matching event."
            )]
            bool collectAll = false
        )
        {
            // Special case: timeoutMs=0 means drain pending, no wait
            if (timeoutMs == 0)
                return McpEventBus.DrainPending(type);

            if (timeoutMs < 1000 || timeoutMs > 120000)
                throw new ArgumentException(Error.InvalidTimeout(timeoutMs));

            using var cts = new CancellationTokenSource(timeoutMs + 1000); // Grace period
            return await McpEventBus.WaitAsync(type, timeoutMs, collectAll, cts.Token);
        }
    }
}
