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
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_GameObject
    {
        [McpPluginTool
        (
            "GameObject_AddComponent",
            Title = "Add Component to a GameObject in opened Prefab or in a Scene"
        )]
        [Description("Add a component to a GameObject.")]
        public AddComponentResponse AddComponent
        (
            [Description("Full name of the Component. It should include full namespace path and the class name.")]
            string[] componentNames,
            GameObjectRef gameObjectRef
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var go = gameObjectRef.FindGameObject(out var error);
                if (error != null)
                    throw new Exception(error);

                if (go == null)
                    throw new Exception("GameObject not found.");

                if (componentNames == null)
                    throw new Exception("No component names provided.");

                if (componentNames.Length == 0)
                    throw new Exception("No component names provided.");

                var response = new AddComponentResponse();

                foreach (var componentName in componentNames)
                {
                    var type = TypeUtils.GetType(componentName);
                    if (type == null)
                    {
                        // try to find component with exact class name without namespace
                        type = Tool_Component.AllComponentTypes.FirstOrDefault(t => t.Name == componentName);
                        if (type == null)
                        {
                            response.Errors ??= new List<string>();
                            response.Errors.Add(Tool_Component.Error.NotFoundComponentType(componentName));
                            continue;
                        }
                    }

                    // Check if type is a subclass of UnityEngine.Component
                    if (!typeof(UnityEngine.Component).IsAssignableFrom(type))
                    {
                        response.Errors ??= new List<string>();
                        response.Errors.Add(Tool_Component.Error.TypeMustBeComponent(componentName));
                        continue;
                    }

                    var newComponent = go.AddComponent(type);

                    if (newComponent == null)
                    {
                        response.Errors ??= new List<string>();
                        response.Errors.Add($"[Warning] Component '{componentName}' already exists on GameObject or cannot be added.");
                        continue;
                    }

                    response.Messages ??= new List<string>();
                    response.Messages.Add($"[Success] Added component '{componentName}'.");

                    response.AddedComponents.Add(new ComponentDataShallow(newComponent));
                }

                UnityEditor.EditorUtility.SetDirty(go);
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                return response;
            });
        }

        public class AddComponentResponse
        {
            public List<ComponentDataShallow> AddedComponents { get; set; } = new List<ComponentDataShallow>();
            public List<string>? Messages { get; set; }
            public List<string>? Errors { get; set; }
        }
    }
}
