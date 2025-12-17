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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_GameObject
    {
        [McpPluginTool
        (
            "gameobject-component-get",
            Title = "GameObject / Component / Get"
        )]
        [Description(@"Get detailed information about a specific Component on a GameObject.
Returns component type, enabled state, and optionally serialized fields and properties.
Use this to inspect component data before modifying it.")]
        public GetComponentResponse GetComponent
        (
            GameObjectRef gameObjectRef,
            ComponentRef componentRef,
            [Description("Include serialized fields of the component.")]
            bool includeFields = true,
            [Description("Include serialized properties of the component.")]
            bool includeProperties = true,
            [Description("Performs deep serialization including all nested objects. Otherwise, only serializes top-level members.")]
            bool deepSerialization = false
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var go = gameObjectRef.FindGameObject(out var error);
                if (error != null)
                    throw new Exception(error);

                if (go == null)
                    throw new Exception("GameObject not found.");

                if (!componentRef.IsValid)
                    throw new Exception("ComponentRef is not valid. Provide instanceID, index, or typeName.");

                var allComponents = go.GetComponents<UnityEngine.Component>();
                UnityEngine.Component? targetComponent = null;
                int targetIndex = -1;

                for (int i = 0; i < allComponents.Length; i++)
                {
                    if (componentRef.Matches(allComponents[i], i))
                    {
                        targetComponent = allComponents[i];
                        targetIndex = i;
                        break;
                    }
                }

                if (targetComponent == null)
                    throw new Exception(Error.NotFoundComponent(componentRef.InstanceID, allComponents));

                var response = new GetComponentResponse
                {
                    reference = new ComponentRef(targetComponent),
                    index = targetIndex,
                    component = new ComponentDataShallow(targetComponent)
                };

                var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;
                var logger = McpPlugin.McpPlugin.Instance.Logger;

                if (includeFields || includeProperties)
                {
                    var serialized = reflector.Serialize(
                        obj: targetComponent,
                        name: targetComponent.GetType().GetTypeId(),
                        recursive: deepSerialization,
                        logger: logger
                    );

                    if (includeFields && serialized?.fields != null)
                    {
                        response.fields = serialized.fields
                            .Where(f => f != null)
                            .ToList();
                    }

                    if (includeProperties && serialized?.props != null)
                    {
                        response.properties = serialized.props
                            .Where(p => p != null)
                            .ToList();
                    }
                }

                return response;
            });
        }

        public class GetComponentResponse
        {
            [Description("Reference to the component for future operations.")]
            public ComponentRef? reference;

            [Description("Index of the component in the GameObject's component list.")]
            public int index;

            [Description("Basic component information (type, enabled state).")]
            public ComponentDataShallow? component;

            [Description("Serialized fields of the component.")]
            public List<SerializedMember>? fields;

            [Description("Serialized properties of the component.")]
            public List<SerializedMember>? properties;
        }
    }
}
