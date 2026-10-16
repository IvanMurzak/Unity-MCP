/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.ToolFormat
{
    /// <summary>
    /// A tool parameter's description under <see cref="RqlToolBinding"/>, in place of its [Description] — for parameters whose
    /// ReflectorNet description explains the JSON shape (e.g. "each entry needs name and typeName") that RQL does not use.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class RqlArgumentAttribute : Attribute
    {
        public RqlArgumentAttribute(string description) { Description = description; }
        public string Description { get; }
    }
}
