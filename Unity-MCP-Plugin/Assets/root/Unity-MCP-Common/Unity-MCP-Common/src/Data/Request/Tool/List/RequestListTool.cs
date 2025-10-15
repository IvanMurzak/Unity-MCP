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
    public class RequestListTool : IRequestListTool
    {
        public string RequestID { get; set; } = Guid.NewGuid().ToString();

        // Empty constructor for JSON deserialization
        public RequestListTool() { }

        // Overloaded constructor to set RequestID
        public RequestListTool(string requestId)
        {
            RequestID = requestId ?? throw new ArgumentNullException(nameof(requestId));
        }

        public virtual void Dispose()
        {
        }
        ~RequestListTool() => Dispose();
    }
}
