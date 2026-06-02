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
#if UNITY_6000_5_OR_NEWER
using AIGD;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Unity 6.5+ variant of the version-specific helpers used by the shared
    /// jsonPatch object-ref tests. On 6.5+ instance IDs are <c>EntityId</c> and the
    /// instanceID JSON wire form is a string (see #759); <c>GetEntityId()</c> is the
    /// accessor and the ref ctors take <c>EntityId</c>.
    /// </summary>
    public partial class TestToolGameObject
    {
        // Builds a single-field merge patch whose value is an object-ref node
        // {"<field>":{"instanceID":"<ulong-as-string>"}} — the 6.5+ wire form.
        static string InstanceIdPatch(string field, UnityEngine.Object obj)
            => $"{{\"{field}\":{{\"instanceID\":\"{UnityEngine.EntityId.ToULong(obj.GetEntityId())}\"}}}}";

        static GameObjectRef GoRef(GameObject go) => new GameObjectRef(go.GetEntityId());

        static ComponentRef CompRef(Component component) => new ComponentRef(component.GetEntityId());

        static bool SameRef(UnityEngine.Object a, UnityEngine.Object b) => a.GetEntityId() == b.GetEntityId();
    }
}
#endif
