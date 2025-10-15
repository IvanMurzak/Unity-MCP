/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public interface IRequestListPrompts : IRequestID, IDisposable
    {
        string Name { get; set; }
        IReadOnlyDictionary<string, JsonElement> Arguments { get; set; }
    }
}
