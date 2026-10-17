/*
┌──────────────────────────────────────────────────────────────────┐
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [Description("Represents a Unity event captured by the MCP Event System.")]
    public class McpEventData
    {
        [JsonInclude, JsonPropertyName("type")]
        [Description("Event type identifier (e.g. 'play_mode_changed', 'error_logged', 'scene_loaded').")]
        public string Type { get; set; } = string.Empty;

        [JsonInclude, JsonPropertyName("source")]
        [Description("Origin of the event (e.g. GameObject name, script name, scene name).")]
        public string? Source { get; set; }

        [JsonInclude, JsonPropertyName("message")]
        [Description("Human-readable event description.")]
        public string? Message { get; set; }

        [JsonInclude, JsonPropertyName("timestamp")]
        [Description("UTC epoch seconds when the event was captured.")]
        public double Timestamp { get; set; }

        [JsonInclude, JsonPropertyName("payload")]
        [Description("Additional event-specific data as key-value pairs.")]
        public Dictionary<string, object?>? Payload { get; set; }
    }

    [Description("Describes an available event type that can be subscribed to.")]
    public class McpEventTypeInfo
    {
        [JsonInclude, JsonPropertyName("type")]
        [Description("Event type identifier used in event-subscribe filter.")]
        public string Type { get; set; } = string.Empty;

        [JsonInclude, JsonPropertyName("description")]
        [Description("What triggers this event.")]
        public string? Description { get; set; }

        [JsonInclude, JsonPropertyName("isBuiltIn")]
        [Description("True if this is a built-in event, false if registered by user code.")]
        public bool IsBuiltIn { get; set; }
    }

    [Description("Result of an event subscription wait.")]
    public class McpEventSubscribeResult
    {
        [JsonInclude, JsonPropertyName("timedOut")]
        [Description("True if no matching event occurred within the timeout period.")]
        public bool TimedOut { get; set; }

        [JsonInclude, JsonPropertyName("events")]
        [Description("List of captured events matching the filter. Empty if timed out.")]
        public List<McpEventData> Events { get; set; } = new();
    }
}
