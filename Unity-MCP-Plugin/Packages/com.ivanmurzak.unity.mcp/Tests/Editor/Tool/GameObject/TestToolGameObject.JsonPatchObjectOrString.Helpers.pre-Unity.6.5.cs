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
#if !UNITY_6000_5_OR_NEWER
using AIGD;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Pre-Unity-6.5 variant of the version-specific helpers used by the shared
    /// jsonPatch object-ref tests. Before 6.5 instance IDs are <c>int</c> and the
    /// instanceID JSON wire form is a number; <c>GetInstanceID()</c> is the accessor
    /// and the ref ctors take <c>int</c>.
    /// </summary>
    public partial class TestToolGameObject
    {
        // Builds a single-field merge patch whose value is an object-ref node
        // {"<field>":{"instanceID":<int>}} — the pre-6.5 wire form (JSON number).
        static string InstanceIdPatch(string field, UnityEngine.Object obj)
            => $"{{\"{field}\":{{\"instanceID\":{obj.GetInstanceID()}}}}}";

        static GameObjectRef GoRef(GameObject go) => new GameObjectRef(go.GetInstanceID());

        static ComponentRef CompRef(Component component) => new ComponentRef(component.GetInstanceID());

        static bool SameRef(UnityEngine.Object a, UnityEngine.Object b) => a.GetInstanceID() == b.GetInstanceID();
    }
}
#endif
