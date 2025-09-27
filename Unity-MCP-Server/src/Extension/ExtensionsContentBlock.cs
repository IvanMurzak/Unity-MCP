/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

using ModelContextProtocol.Protocol;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public static class ExtensionsContentBlock
    {
        public static ContentBlock ToTextContent(this Common.Model.ContentBlock response) => new TextContentBlock()
        {
            Type = response.Type,
            Text = response.Text ?? string.Empty
        };
    }
}
