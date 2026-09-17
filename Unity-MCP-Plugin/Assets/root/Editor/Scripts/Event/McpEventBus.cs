/*
┌──────────────────────────────────────────────────────────────────┐
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    /// <summary>
    /// Thread-safe event bus for MCP event subscription system.
    /// Unity main thread pushes events, MCP tool background threads consume them.
    /// Uses System.Threading.Channels for lock-free, multi-consumer-safe async I/O.
    /// </summary>
    public static class McpEventBus
    {
        static Channel<McpEventData> _channel = CreateChannel();
        static readonly ConcurrentDictionary<string, string> _registeredCustomTypes = new();

        static Channel<McpEventData> CreateChannel() => Channel.CreateUnbounded<McpEventData>(
            new UnboundedChannelOptions { SingleWriter = false, SingleReader = false });

        // Built-in event type definitions
        static readonly Dictionary<string, string> _builtInTypes = new()
        {
            ["play_mode_changed"]    = "Editor play mode state changed (Playing, Paused, Stopped).",
            ["scene_loaded"]         = "A scene finished loading.",
            ["scene_opened"]         = "A scene was opened in the Editor.",
            ["compilation_started"]  = "Script compilation started.",
            ["compilation_finished"] = "Script compilation finished.",
            ["error_logged"]         = "An error or exception was logged to the console.",
            ["warning_logged"]       = "A warning was logged to the console.",
            ["pause_state_changed"]  = "Editor pause state toggled.",
            ["hierarchy_changed"]    = "The scene hierarchy changed (GameObject created/destroyed/reparented).",
            ["selection_changed"]    = "Editor selection changed.",
        };

        /// <summary>
        /// Push an event onto the bus. Call from Unity main thread or any thread.
        /// </summary>
        public static void Push(string type, string? source = null, string? message = null,
                                Dictionary<string, object?>? payload = null)
        {
            if (string.IsNullOrEmpty(type))
                return;

            _channel.Writer.TryWrite(new McpEventData
            {
                Type      = type,
                Source    = source,
                Message   = message,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0,
                Payload   = payload
            });

            // Auto-register custom types
            if (!_builtInTypes.ContainsKey(type))
                _registeredCustomTypes.TryAdd(type, $"Custom event: {type}");
        }

        /// <summary>
        /// Wait for a matching event. Blocks until event arrives or timeout.
        /// Safe for multiple concurrent consumers (e.g. event-watch + event-subscribe).
        /// </summary>
        public static async Task<McpEventSubscribeResult> WaitAsync(
            string? typeFilter, int timeoutMs, bool collectAll, CancellationToken ct)
        {
            var result = new McpEventSubscribeResult();
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            var skipped = new List<McpEventData>();
            var reader = _channel.Reader;

            try
            {
                while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
                {
                    var remaining = Math.Max(0, (int)(deadline - DateTime.UtcNow).TotalMilliseconds);
                    if (remaining <= 0)
                        break;

                    using var chunkCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    chunkCts.CancelAfter(Math.Min(remaining, 500));

                    try
                    {
                        if (await reader.WaitToReadAsync(chunkCts.Token))
                        {
                            if (reader.TryRead(out var evt))
                            {
                                if (string.IsNullOrEmpty(typeFilter) || evt.Type == typeFilter)
                                {
                                    result.Events.Add(evt);
                                    if (!collectAll)
                                        break;
                                }
                                else
                                {
                                    skipped.Add(evt);
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                    {
                        // 500ms chunk timeout — continue loop to check deadline
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Outer cancellation — fall through
            }

            // Re-enqueue events that didn't match the filter
            foreach (var s in skipped)
                _channel.Writer.TryWrite(s);

            result.TimedOut = result.Events.Count == 0;
            return result;
        }

        /// <summary>
        /// Drain all pending events matching a filter without waiting.
        /// </summary>
        public static McpEventSubscribeResult DrainPending(string? typeFilter)
        {
            var result = new McpEventSubscribeResult();
            var skipped = new List<McpEventData>();

            while (_channel.Reader.TryRead(out var evt))
            {
                if (string.IsNullOrEmpty(typeFilter) || evt.Type == typeFilter)
                    result.Events.Add(evt);
                else
                    skipped.Add(evt);
            }

            // Re-enqueue non-matching events
            foreach (var s in skipped)
                _channel.Writer.TryWrite(s);

            result.TimedOut = result.Events.Count == 0;
            return result;
        }

        public static McpEventTypeInfo[] GetAllEventTypes()
        {
            var list = _builtInTypes.Select(kv => new McpEventTypeInfo
            {
                Type        = kv.Key,
                Description = kv.Value,
                IsBuiltIn   = true
            }).ToList();

            foreach (var kv in _registeredCustomTypes)
            {
                list.Add(new McpEventTypeInfo
                {
                    Type        = kv.Key,
                    Description = kv.Value,
                    IsBuiltIn   = false
                });
            }

            return list.ToArray();
        }

        /// <summary>
        /// Register a custom event type with description (optional, for discoverability).
        /// </summary>
        public static void RegisterCustomType(string type, string description)
        {
            _registeredCustomTypes[type] = description;
        }

        /// <summary>
        /// Clear all pending events by replacing the channel.
        /// </summary>
        public static void Clear()
        {
            var old = _channel;
            _channel = CreateChannel();
            old.Writer.Complete();
        }
    }
}
