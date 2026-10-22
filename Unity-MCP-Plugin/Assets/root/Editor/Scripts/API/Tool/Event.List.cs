/*
┌──────────────────────────────────────────────────────────────────┐
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Event
    {
        public const string EventListToolId = "event-list";

        [McpPluginTool
        (
            EventListToolId,
            Title = "Event / List",
            ReadOnlyHint = true,
            IdempotentHint = true
        )]
        [Description(
            "Lists all event types available for event-subscribe.\n\n" +
            "Shows built-in Unity Editor events AND custom game events.\n" +
            "Custom events are registered automatically when McpEventBus.Push() is called from game code,\n" +
            "or manually via script-execute: McpEventBus.RegisterCustomType(\"event_name\", \"description\").\n\n" +
            "If only built-in events appear, the project may need custom events added to game code:\n" +
            "  #if UNITY_EDITOR\n" +
            "  McpEventBus.Push(\"server_response_done\", source: \"NetworkHelper\");\n" +
            "  #endif\n" +
            "See event-subscribe tool description for full custom event guide."
        )]
        public McpEventTypeInfo[] ListEvents
        (
            [Description(
                "Filter event types. " +
                "'all' = show everything (default), " +
                "'builtin' = only built-in Unity events, " +
                "'custom' = only user-registered custom events."
            )]
            string? filter = "all"
        )
        {
            var allTypes = McpEventBus.GetAllEventTypes();

            return (filter?.ToLowerInvariant()) switch
            {
                "builtin" => allTypes.Where(t => t.IsBuiltIn).ToArray(),
                "custom"  => allTypes.Where(t => !t.IsBuiltIn).ToArray(),
                _         => allTypes
            };
        }
    }
}
