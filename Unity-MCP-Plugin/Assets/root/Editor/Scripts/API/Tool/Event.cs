/*
┌──────────────────────────────────────────────────────────────────┐
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using com.IvanMurzak.McpPlugin;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Event
    {
        public static class Error
        {
            public static string InvalidTimeout(int timeoutMs)
                => $"Invalid timeout value '{timeoutMs}'. Must be between 1000 and 120000 milliseconds.";

            public static string EventBusNotAvailable()
                => "McpEventBus is not available.";
        }
    }
}
