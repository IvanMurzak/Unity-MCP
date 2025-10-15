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

namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public class ResponseListResource : IResponseListResource
    {
        public string uri { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string? mimeType { get; set; }
        public string? description { get; set; }
        public long? size { get; set; }

        public ResponseListResource() { }
        public ResponseListResource(string uri, string name, string? mimeType = null, string? description = null, long? size = null)
        {
            this.uri = uri;
            this.name = name;
            this.mimeType = mimeType;
            this.description = description;
            this.size = size;
        }
    }
}
