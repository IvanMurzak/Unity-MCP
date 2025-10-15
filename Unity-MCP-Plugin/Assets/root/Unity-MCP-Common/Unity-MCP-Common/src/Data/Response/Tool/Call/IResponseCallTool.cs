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
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public interface IResponseCallTool : IRequestID
    {
        ResponseStatus Status { get; set; }
        List<ContentBlock> Content { get; set; }
        JsonNode? StructuredContent { get; set; }
    }
}
