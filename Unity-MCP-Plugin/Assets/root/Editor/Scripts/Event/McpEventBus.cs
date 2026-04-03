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
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    /// <summary>
    /// Thread-safe event bus for MCP event subscription system.
    /// Unity main thread pushes events, MCP tool background threads wait for them.
    /// </summary>
    public static class McpEventBus
    {
        static readonly ConcurrentQueue<McpEventData> _queue = new();
        static readonly SemaphoreSlim _signal = new(0);
        static readonly ConcurrentDictionary<string, string> _registeredCustomTypes = new();

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
            ["asset_imported"]       = "Assets were imported/reimported.",
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

            _queue.Enqueue(new McpEventData
            {
                Type      = type,
                Source    = source,
                Message   = message,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0,
                Payload   = payload
            });
            _signal.Release();

            // Auto-register custom types
            if (!_builtInTypes.ContainsKey(type))
                _registeredCustomTypes.TryAdd(type, $"Custom event: {type}");
        }

        /// <summary>
        /// Wait for a matching event. Blocks until event arrives or timeout.
        /// </summary>
        public static async Task<McpEventSubscribeResult> WaitAsync(
            string? typeFilter, int timeoutMs, bool collectAll, CancellationToken ct)
        {
            var result = new McpEventSubscribeResult();
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            var skipped = new List<McpEventData>();

            try
            {
                while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
                {
                    var remaining = Math.Max(0, (int)(deadline - DateTime.UtcNow).TotalMilliseconds);
                    if (remaining <= 0)
                        break;

                    if (await _signal.WaitAsync(Math.Min(remaining, 500), ct))
                    {
                        if (_queue.TryDequeue(out var evt))
                        {
                            if (string.IsNullOrEmpty(typeFilter) || evt.Type == typeFilter)
                            {
                                result.Events.Add(evt);
                                if (!collectAll)
                                    break; // Got one matching event, return immediately
                            }
                            else
                            {
                                skipped.Add(evt);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelled — fall through with whatever we have
            }

            // Re-enqueue events that didn't match the filter
            foreach (var s in skipped)
            {
                _queue.Enqueue(s);
                _signal.Release();
            }

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

            while (_queue.TryDequeue(out var evt))
            {
                _signal.Wait(0); // Consume the signal for the dequeued item

                if (string.IsNullOrEmpty(typeFilter) || evt.Type == typeFilter)
                    result.Events.Add(evt);
                else
                    skipped.Add(evt);
            }

            // Re-enqueue non-matching events
            foreach (var s in skipped)
            {
                _queue.Enqueue(s);
                _signal.Release();
            }

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
        /// Clear all pending events. Useful for test cleanup.
        /// </summary>
        public static void Clear()
        {
            while (_queue.TryDequeue(out _))
                _signal.Wait(0);
        }
    }
}
