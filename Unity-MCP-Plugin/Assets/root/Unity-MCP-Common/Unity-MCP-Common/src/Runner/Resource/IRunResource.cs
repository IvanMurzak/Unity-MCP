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

namespace com.IvanMurzak.Unity.MCP.Common
{
    public interface IRunResource
    {
        string Route { get; set; }
        string Name { get; set; }
        string? Description { get; set; }
        string? MimeType { get; set; }

        [JsonIgnore]
        IRunResourceContent RunGetContent { get; set; }

        [JsonIgnore]
        IRunResourceContext RunListContext { get; set; }
    }
}
