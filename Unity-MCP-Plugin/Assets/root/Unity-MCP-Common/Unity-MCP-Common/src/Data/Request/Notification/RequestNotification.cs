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

namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public class RequestNotification : IRequestNotification
    {
        public string RequestID { get; set; } = string.Empty;
        public string? Path { get; set; }
        public string? Name { get; set; }
        public IDictionary<string, object?>? Parameters { get; set; } = new Dictionary<string, object?>();

        public RequestNotification() { }
        public RequestNotification(string path, string name)
        : this(Guid.NewGuid().ToString(), path, name) { }
        public RequestNotification(string requestId, string path, string name) : this()
        {
            RequestID = requestId ?? throw new ArgumentNullException(nameof(requestId));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public virtual void Dispose()
        {
            Parameters?.Clear();
        }
        ~RequestNotification() => Dispose();
    }
}
