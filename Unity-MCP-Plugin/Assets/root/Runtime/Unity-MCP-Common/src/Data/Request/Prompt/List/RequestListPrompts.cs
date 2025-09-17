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
using System.Collections.Generic;
using System.Text.Json;

namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public class RequestListPrompts : IRequestListPrompts
    {
        public string RequestID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, JsonElement> Arguments { get; set; } = new Dictionary<string, JsonElement>();

        public RequestListPrompts() { }
        public RequestListPrompts(string name, IReadOnlyDictionary<string, JsonElement> arguments)
            : this(Guid.NewGuid().ToString(), name, arguments) { }
        public RequestListPrompts(string requestId, string name, IReadOnlyDictionary<string, JsonElement> arguments)
        {
            RequestID = requestId ?? throw new ArgumentNullException(nameof(requestId));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        public virtual void Dispose()
        {
            // Arguments.Clear();
        }
        ~RequestListPrompts() => Dispose();
    }
}
