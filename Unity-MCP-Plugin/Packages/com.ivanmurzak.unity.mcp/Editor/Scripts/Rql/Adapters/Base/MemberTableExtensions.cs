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
using System.Linq;
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters
{
    /// <summary>
    /// For members that exist only in some Unity versions (e.g. MonoBehaviour.didStart). Members present in every supported
    /// version use the strict MemberTable calls, which reject unknown names so typos fail loudly.
    /// </summary>
    public static class MemberTableExtensions
    {
        public static MemberTable HideExisting(this MemberTable table, params string[] names) =>
            table.Hide(names.Where(table.Contains).ToArray());
    }
}
