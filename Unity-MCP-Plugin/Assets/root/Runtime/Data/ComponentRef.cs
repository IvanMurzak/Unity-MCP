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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using com.IvanMurzak.ReflectorNet;

namespace com.IvanMurzak.Unity.MCP.Runtime.Data
{
    [Description("Component reference. " +
        "Used to find a Component at GameObject.")]
    public class ComponentRef : ObjectRef
    {
        public static partial class ComponentRefProperty
        {
            public const string Index = "index";
            public const string TypeName = "typeName";

            public static IEnumerable<string> All => ObjectRefProperty.All.Concat(new[]
            {
                Index,
                TypeName
            });
        }
        [JsonInclude, JsonPropertyName(ComponentRefProperty.Index)]
        [Description("Component 'index' attached to a gameObject. The first index is '0' and that is usually Transform or RectTransform. Priority: 2. Default value is -1.")]
        public int Index { get; set; } = -1;

        [JsonInclude, JsonPropertyName(ComponentRefProperty.TypeName)]
        [Description("Component type full name. Sample 'UnityEngine.Transform'. If the gameObject has two components of the same type, the output component is unpredictable. Priority: 3. Default value is null.")]
        public string? TypeName { get; set; } = null;

        public override bool IsValid(out string? error)
        {
            if (InstanceID != 0)
            {
                error = null;
                return true;
            }
            if (Index >= 0)
            {
                error = null;
                return true;
            }
            if (!string.IsNullOrEmpty(TypeName))
            {
                error = null;
                return true;
            }
            error = $"Invalid ComponentRef: '{nameof(InstanceID)}' is 0, '{nameof(Index)}' is less than 0, and '{nameof(TypeName)}' is null or empty.";
            return false;
        }

        public ComponentRef() { }
        public ComponentRef(int instanceID)
        {
            this.InstanceID = instanceID;
        }
        public ComponentRef(UnityEngine.Component? component) : base(component)
        {
            if (component == null)
                return;

            var go = component.gameObject;
            var components = go.GetComponents<UnityEngine.Component>();
            this.Index = System.Array.IndexOf(components, component);
            this.TypeName = component.GetType().GetTypeShortName();
        }

        public override string ToString()
        {
            if (InstanceID != 0)
                return $"Component {ObjectRefProperty.InstanceID}='{InstanceID}'";
            if (Index >= 0)
                return $"Component {ComponentRefProperty.Index}='{Index}'";
            if (!string.IsNullOrEmpty(TypeName))
                return $"Component {ComponentRefProperty.TypeName}='{TypeName}'";
            return "Component unknown";
        }
    }
}
