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

namespace com.IvanMurzak.Unity.MCP.Runtime.Data
{
    [Description("Specifies what to include for tool input arguments.")]
    public enum InputRequest
    {
        [Description("Do not include input arguments.")]
        None = 0,

        [Description("Include input argument names only.")]
        Inputs = 1,

        [Description("Include input argument names and descriptions.")]
        InputsWithDescription = 2
    }
}
