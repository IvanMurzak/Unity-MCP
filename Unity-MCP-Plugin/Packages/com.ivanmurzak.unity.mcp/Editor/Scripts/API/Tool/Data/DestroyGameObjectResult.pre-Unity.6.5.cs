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
#if !UNITY_6000_5_OR_NEWER
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Model;
using AIGD;

namespace AIGD
{
    public class DestroyGameObjectResult
    {
        [Description("Name of the destroyed GameObject.")]
        public string? DestroyedName { get; set; }

        [Description("Hierarchy path of the destroyed GameObject.")]
        public string? DestroyedPath { get; set; }

        [Description("Instance ID of the destroyed GameObject.")]
        public int DestroyedInstanceId { get; set; }
    }
}
#endif
