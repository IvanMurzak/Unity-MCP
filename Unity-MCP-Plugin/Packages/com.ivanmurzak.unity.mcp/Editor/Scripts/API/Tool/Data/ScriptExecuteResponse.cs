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
using System.ComponentModel;
using com.IvanMurzak.ReflectorNet.Model;

namespace AIGD
{
    public class ScriptExecuteResponse
    {
        [Description("The serialized return value of the executed script. Null if the script returns void.")]
        public SerializedMember? Result { get; set; }

        [Description("Human-readable message describing the execution result.")]
        public string? Message { get; set; }
    }
}