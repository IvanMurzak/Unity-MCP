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
    public class ContentBlock
    {
        /// <summary>
        /// The type of content. This determines the structure of the content object. Can be "image", "audio", "text", "resource".
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The text content of the message.
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// The base64-encoded image data.
        /// </summary>
        public string? Data { get; set; }

        /// <summary>
        /// The MIME type of the image.
        /// </summary>
        public string? MimeType { get; set; }

        /// <summary>
        /// The resource content of the message (if embedded).
        /// </summary>
        public ResponseResourceContent? Resource { get; set; }

        public ContentBlock() { }
    }
}
