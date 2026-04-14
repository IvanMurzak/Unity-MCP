/*
┌────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                   │
│  Repository: GitHub (https://github.com/IvanMurzak/MCP-Plugin-dotnet)  │
│  Copyright (c) 2025 Ivan Murzak                                        │
│  Licensed under the Apache License, Version 2.0.                       │
│  See the LICENSE file in the project root for more information.        │
└────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
#if UNITY_6000_5_OR_NEWER
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace com.IvanMurzak.Unity.MCP.Runtime.Data
{
    [System.Serializable]
    [Description("Reference to UnityEngine.Object instance. " +
        "It could be GameObject, Component, Asset, etc. " +
        "Anything extended from UnityEngine.Object.")]
    public class ObjectRef
    {
        public static partial class ObjectRefProperty
        {
            public const string InstanceID = "instanceID";

            public static IEnumerable<string> All => new[] { InstanceID };
        }

        [JsonInclude, JsonPropertyName(ObjectRefProperty.InstanceID)]
        [Description("instanceID of the UnityEngine.Object. If this is '0', then it will be used as 'null'.")]
        public virtual UnityEngine.EntityId InstanceID { get; set; } = UnityEngine.EntityId.None;

        public ObjectRef() : this(entityId: UnityEngine.EntityId.None) { }
        public ObjectRef(UnityEngine.EntityId entityId) => InstanceID = entityId;
        public ObjectRef(UnityEngine.Object? obj)
        {
            InstanceID = obj?.GetEntityId() ?? UnityEngine.EntityId.None;
        }

        public virtual bool IsValid() => IsValid(out var error);
        public virtual bool IsValid(out string? error)
        {
            if (InstanceID != UnityEngine.EntityId.None)
            {
                error = null;
                return true;
            }

            error = $"'{nameof(InstanceID)}' is 'EntityId.None', this is invalid value for any UnityEngine.Object.";
            return false;
        }

        public override string ToString()
        {
            return $"ObjectRef {ObjectRefProperty.InstanceID}='{InstanceID}'";
        }
    }
}
#endif