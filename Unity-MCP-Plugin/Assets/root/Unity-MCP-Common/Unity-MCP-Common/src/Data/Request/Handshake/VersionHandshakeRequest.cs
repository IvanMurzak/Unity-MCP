/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System.Text.Json.Serialization;

namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public class VersionHandshakeRequest : IRequestID
    {
        [JsonPropertyName("requestId")]
        public string RequestID { get; set; } = string.Empty;

        [JsonPropertyName("apiVersion")]
        public string ApiVersion { get; set; } = string.Empty;

        [JsonPropertyName("pluginVersion")]
        public string PluginVersion { get; set; } = string.Empty;

        [JsonPropertyName("unityVersion")]
        public string UnityVersion { get; set; } = string.Empty;
    }
}