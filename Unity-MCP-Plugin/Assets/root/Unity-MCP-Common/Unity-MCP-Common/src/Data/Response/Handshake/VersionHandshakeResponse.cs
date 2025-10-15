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
using System.Text.Json.Serialization;

namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public class VersionHandshakeResponse
    {
        [JsonPropertyName("apiVersion")]
        public string ApiVersion { get; set; } = string.Empty;

        [JsonPropertyName("serverVersion")]
        public string ServerVersion { get; set; } = string.Empty;

        [JsonPropertyName("compatible")]
        public bool Compatible { get; set; } = false;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}