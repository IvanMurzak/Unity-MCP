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
    public interface IResponseResourceTemplate
    {
        string uriTemplate { get; set; }
        string name { get; set; }
        string? mimeType { get; set; }
        string? description { get; set; }
    }
}
