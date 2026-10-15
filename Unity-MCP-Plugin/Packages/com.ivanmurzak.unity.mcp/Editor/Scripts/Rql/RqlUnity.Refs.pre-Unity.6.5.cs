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
#if !UNITY_6000_5_OR_NEWER
using System.Globalization;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    public static partial class RqlUnity
    {
        /// <summary>Unity before 6.5: <c>Type#id</c> where id is the instance id; the engine checks the type.</summary>
        private static object? ResolveStableRef(string type, string id)
        {
            if (!int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var instanceId)) return null;
            var o = EditorUtility.InstanceIDToObject(instanceId);
            return o == null ? null : o;
        }
    }
}
#endif
