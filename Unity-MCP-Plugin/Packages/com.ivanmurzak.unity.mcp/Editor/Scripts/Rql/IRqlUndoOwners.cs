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
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    /// <summary>
    /// A plain view object whose writes land on Unity objects that are neither GameObjects nor components, e.g. the
    /// RenderSettings behind <c>lighting | get environment</c> or the TimeManager behind <c>settings | get time</c>. A
    /// mutation of it records <see cref="UndoOwners"/> for Undo; scene-level owners mark the active scene modified (the
    /// objects of other loaded scenes are recorded, not dirtied), persistent ones (ProjectSettings/*.asset) are dirtied and
    /// saved with the assets.
    /// </summary>
    public interface IRqlUndoOwners
    {
        IReadOnlyList<Object> UndoOwners { get; }
    }

    /// <summary>Range checks for the setters of settings views (<see cref="IRqlUndoOwners"/>).</summary>
    public static class RqlValueGuard
    {
        /// <summary>
        /// Checked before the assignment: a rejected value changes nothing (the engine rolls back the earlier writes of the set).
        /// </summary>
        public static void Require<T>(T value, bool ok, string range)
        {
            if (!ok) throw new ArgumentOutOfRangeException(nameof(value), value, "must be " + range);
        }
    }
}
