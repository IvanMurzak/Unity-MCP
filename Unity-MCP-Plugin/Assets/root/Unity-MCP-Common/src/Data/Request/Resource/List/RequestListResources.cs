/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System;

namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public class RequestListResources : IRequestListResources
    {
        public string RequestID { get; set; } = Guid.NewGuid().ToString();
        public string? Cursor { get; set; }

        public RequestListResources() { }
        public RequestListResources(string? cursor = null)
            : this(Guid.NewGuid().ToString(), cursor) { }
        public RequestListResources(string requestId, string? cursor = null)
        {
            RequestID = requestId ?? throw new ArgumentNullException(nameof(requestId));
            Cursor = cursor;
        }

        public virtual void Dispose()
        {

        }
        ~RequestListResources() => Dispose();
    }
}
