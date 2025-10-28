/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

using R3;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public class EventAppPromptsChange : Subject<EventAppPromptsChange.EventData>
    {
        public class EventData
        {
            public string ConnectionId { get; set; } = string.Empty;
            public string Data { get; set; } = string.Empty;
        }
    }
}
