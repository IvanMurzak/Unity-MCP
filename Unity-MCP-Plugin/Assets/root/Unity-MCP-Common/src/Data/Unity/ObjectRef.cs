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
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace com.IvanMurzak.Unity.MCP.Common.Model.Unity
{
    [System.Serializable]
    [Description("Reference to UnityEngine.Object instance. It could be GameObject, Component, Asset, etc. Anything extended from UnityEngine.Object.")]
    public class ObjectRef
    {
        public static partial class ObjectRefProperty
        {
            public const string InstanceID = "instanceID";

            public static IEnumerable<string> All => new[] { InstanceID };
        }
        [JsonInclude, JsonPropertyName(ObjectRefProperty.InstanceID)]
        [Description("instanceID of the UnityEngine.Object. If this is '0', then it will be used as 'null'.")]
        public virtual int InstanceID { get; set; } = 0;

        public ObjectRef() : this(id: 0) { }
        public ObjectRef(int id) => InstanceID = id;

        public override string ToString()
        {
            return $"ObjectRef {ObjectRefProperty.InstanceID}='{InstanceID}'";
        }
    }
}
