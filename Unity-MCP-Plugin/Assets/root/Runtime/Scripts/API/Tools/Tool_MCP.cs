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
using com.IvanMurzak.McpPlugin;

namespace com.IvanMurzak.Unity.MCP.Runtime.API
{
    [McpPluginToolType]
    public partial class Tool_MCP
    {
        public enum InputRequest
        {
            None,
            Inputs,
            InputsWithDescription
        }

        public class InputData
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
        }

        public class ToolData
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public InputData[]? Inputs { get; set; }
        }

    }
}
