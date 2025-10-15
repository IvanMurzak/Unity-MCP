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
using System.Text.Json;

namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public class ResponseListTool : IResponseListTool
    {
        public string Name { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Description { get; set; }
        public JsonElement InputSchema { get; set; }
        public JsonElement? OutputSchema { get; set; }

        public ResponseListTool() { }
    }
}
