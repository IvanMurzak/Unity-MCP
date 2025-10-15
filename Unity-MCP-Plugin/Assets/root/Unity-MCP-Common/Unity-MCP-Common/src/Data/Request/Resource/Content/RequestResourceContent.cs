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
    public class RequestResourceContent : IRequestResourceContent
    {
        public string RequestID { get; set; } = string.Empty;
        public string Uri { get; set; } = string.Empty;

        public RequestResourceContent() { }
        public RequestResourceContent(string uri)
            : this(Guid.NewGuid().ToString(), uri) { }
        public RequestResourceContent(string requestId, string uri)
        {
            RequestID = requestId ?? throw new ArgumentNullException(nameof(requestId));
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        }

        public virtual void Dispose()
        {

        }
        ~RequestResourceContent() => Dispose();
    }
}
