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

namespace com.IvanMurzak.Unity.MCP.Common
{
    public class RunResource : IRunResource
    {
        public bool Enabled { get; set; } = true;
        public string Route { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? MimeType { get; set; }

        [JsonIgnore]
        public IRunResourceContent RunGetContent { get; set; }

        [JsonIgnore]
        public IRunResourceContext RunListContext { get; set; }

        public RunResource(string route, string name, IRunResourceContent runnerGetContent, IRunResourceContext runnerListContext, string? description = null, string? mimeType = null)
        {
            Route = route;
            Name = name;
            RunGetContent = runnerGetContent;
            RunListContext = runnerListContext;
            Description = description;
            MimeType = mimeType;
        }
    }
}
