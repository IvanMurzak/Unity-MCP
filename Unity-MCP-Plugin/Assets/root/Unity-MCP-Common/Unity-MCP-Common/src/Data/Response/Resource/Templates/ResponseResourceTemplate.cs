/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public class ResponseResourceTemplate : IResponseResourceTemplate
    {
        public string uriTemplate { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string? mimeType { get; set; }
        public string? description { get; set; }

        public ResponseResourceTemplate() { }
        public ResponseResourceTemplate(string uri, string name, string? mimeType = null, string? description = null)
        {
            this.uriTemplate = uri;
            this.name = name;
            this.mimeType = mimeType;
            this.description = description;
        }
    }
}
