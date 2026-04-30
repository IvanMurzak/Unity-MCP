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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Model;
using AIGD;
using System.Linq;

namespace AIGD
{
    public class ModifyObjectResponse
    {
        [Description("Whether the modification was successful.")]
        public bool Success { get; set; } = false;

        [Description("Reference to the modified object.")]
        public ObjectRef? Reference { get; set; }

        [Description("Updated object data after modification.")]
        public SerializedMember? Data { get; set; }

        [Description("Log of modifications made and any warnings/errors encountered.")]
        public string[]? Logs { get; set; }

        public ModifyObjectResponse(bool success, Logs logs)
        {
            Success = success;
            Logs = logs
                .Select(log => log.ToString())
                .ToArray();
        }
    }
}
