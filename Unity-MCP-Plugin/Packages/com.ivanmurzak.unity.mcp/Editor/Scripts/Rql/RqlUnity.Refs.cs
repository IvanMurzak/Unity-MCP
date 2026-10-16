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
#if UNITY_6000_5_OR_NEWER
using System.Globalization;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    public static partial class RqlUnity
    {
        /// <summary>Unity 6.5+ (current): <c>Type#id</c> where id is the raw EntityId (or a legacy instance id); the engine checks the type.</summary>
        private static object? ResolveStableRef(string type, string id)
        {
            if (!ulong.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) && !long.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)) return null;
            var o = EditorUtility.EntityIdToObject(com.IvanMurzak.Unity.MCP.Runtime.Utils.EntityIdUtils.FromString(id));
            return o == null ? null : o;
        }
    }
}
#endif
