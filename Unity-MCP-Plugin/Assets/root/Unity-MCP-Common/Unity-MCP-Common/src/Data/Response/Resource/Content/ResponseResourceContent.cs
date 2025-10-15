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
    public class ResponseResourceContent : IResponseResourceContent
    {
        public string uri { get; set; } = string.Empty;
        public string? mimeType { get; set; }
        public string? text { get; set; }
        public string? blob { get; set; }

        public ResponseResourceContent() { }
        public ResponseResourceContent(string uri, string? mimeType = null, string? text = null, string? blob = null)
        {
            this.uri = uri;
            this.mimeType = mimeType;
            this.text = text;
            this.blob = blob;
        }

        public static ResponseResourceContent CreateText(string uri, string text, string? mimeType = null)
            => new ResponseResourceContent(uri, mimeType: mimeType, text: text);

        public static ResponseResourceContent CreateBlob(string uri, string blob, string? mimeType = null)
            => new ResponseResourceContent(uri, mimeType: mimeType, blob: blob);
    }
}
