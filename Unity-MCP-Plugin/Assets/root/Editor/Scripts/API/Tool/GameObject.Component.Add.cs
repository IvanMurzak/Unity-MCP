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
        public const string GameObjectComponentAddToolId = "gameobject-component-add";
        [McpPluginTool
        (
            GameObjectComponentAddToolId,
            Title = "GameObject / Component / Add"
        )]
        [Description("Add Component to GameObject in opened Prefab or in a Scene. " +
            "Use '" + GameObjectFindToolId + "' tool to find the target GameObject first. " +
            "Use '" + ComponentListToolId + "' tool to find the component type names to add.")]
        public AddComponentResponse AddComponent
        (
            [Description("Full name of the Component. It should include full namespace path and the class name.")]
            string[] componentNames,
            GameObjectRef gameObjectRef
        )
        {
            if (gameObjectRef == null)
                throw new ArgumentNullException(nameof(gameObjectRef), "No GameObject reference provided.");

            if (!gameObjectRef.IsValid(out var gameObjectValidationError))
                throw new ArgumentException(gameObjectValidationError, nameof(gameObjectRef));

            return MainThread.Instance.Run(() =>
            {
                var go = gameObjectRef.FindGameObject(out var error);
                if (error != null)
                    throw new Exception(error);

                if (go == null)
                    throw new Exception("GameObject not found.");

                if (componentNames == null)
                    throw new ArgumentNullException(nameof(componentNames), "No component names provided.");

                if (componentNames.Length == 0)
                    throw new ArgumentException("No component names provided.", nameof(componentNames));

                var response = new AddComponentResponse();

                foreach (var componentName in componentNames)
                {
                    var type = TypeUtils.GetType(componentName);
                    if (type == null)
                    {
                        // try to find component with exact class name without namespace
                        type = AllComponentTypes.FirstOrDefault(t => t.Name == componentName);
                        if (type == null)
                        {
                            response.Errors ??= new List<string>();
                            response.Errors.Add($"[Error] Type '{componentName}' not found.");
                            continue;
                        }
                    }

                    // Check if type is a subclass of UnityEngine.Component
                    if (!typeof(UnityEngine.Component).IsAssignableFrom(type))
                    {
                        response.Errors ??= new List<string>();
                        response.Errors.Add($"[Error] Type '{componentName}' is not a subclass of UnityEngine.Component.");
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
            [Description("List of successfully added components.")]
            public List<ComponentDataShallow> AddedComponents { get; set; } = new List<ComponentDataShallow>();

            [Description("List of success messages for added components.")]
            public List<string>? Messages { get; set; }

            [Description("List of errors encountered during component addition.")]
            public List<string>? Errors { get; set; }
        }
    }
}
