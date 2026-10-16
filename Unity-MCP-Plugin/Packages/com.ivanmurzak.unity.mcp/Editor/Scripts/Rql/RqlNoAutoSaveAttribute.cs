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

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    /// <summary>
    /// rql-mutate saves scenes, prefabs and assets after a write; a write to an object of a type marked with this attribute
    /// or to a property / field marked with it, or a call of a method marked with it (every overload), changes no project
    /// state, so it does not trigger that save (e.g. <c>profiler | call Start()</c> or <c>editor | set selection = …</c>
    /// must not persist the user's unrelated unsaved scene edits).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
    public sealed class RqlNoAutoSaveAttribute : Attribute { }
}
