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
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    /// <summary>
    /// A plain view object whose writes land on scene-level Unity objects that are neither GameObjects nor components, e.g.
    /// the RenderSettings behind <c>lighting | get environment</c>. A mutation of it records <see cref="UndoOwners"/> for Undo
    /// and marks the active scene modified (the objects of other loaded scenes are recorded, not dirtied).
    /// </summary>
    public interface IRqlUndoOwners
    {
        IReadOnlyList<Object> UndoOwners { get; }
    }
}
