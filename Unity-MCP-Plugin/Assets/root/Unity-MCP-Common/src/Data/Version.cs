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
namespace com.IvanMurzak.Unity.MCP.Common
{
    public class Version
    {
        public string Api { get; set; } = "1.0.0";
        public string Plugin { get; set; } = "1.0.0";
        public string UnityVersion { get; set; } = string.Empty;
    }
}