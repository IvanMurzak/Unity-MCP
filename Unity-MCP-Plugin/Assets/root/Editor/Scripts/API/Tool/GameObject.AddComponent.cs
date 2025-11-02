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
using System.ComponentModel;
using System.Text;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;

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
        public string AddComponent
        (
            [Description("Full name of the Component. It should include full namespace path and the class name.")]
            string[] componentNames,
            GameObjectRef gameObjectRef
        )
        => MainThread.Instance.Run(() =>
        {
            var go = gameObjectRef.FindGameObject(out var error);
            if (error != null)
                return $"[Error] {error}";

            if (go == null)
                return "[Error] GameObject not found.";

            if (componentNames == null)
                return $"[Error] No component names provided.";

            if (componentNames.Length == 0)
                return $"[Error] No component names provided.";

            var stringBuilder = new StringBuilder();

            foreach (var componentName in componentNames)
            {
                var type = TypeUtils.GetType(componentName);
                if (type == null)
                {
                    stringBuilder.AppendLine(Tool_Component.Error.NotFoundComponentType(componentName));
                    continue;
                }

                // Check if type is a subclass of UnityEngine.Component
                if (!typeof(UnityEngine.Component).IsAssignableFrom(type))
                {
                    stringBuilder.AppendLine(Tool_Component.Error.TypeMustBeComponent(componentName));
                    continue;
                }

                var newComponent = go.AddComponent(type);

                if (newComponent == null)
                {
                    stringBuilder.AppendLine($"[Warning] Component '{componentName}' already exists on GameObject or cannot be added.");
                    continue;
                }

                stringBuilder.AppendLine($"[Success] Added component '{componentName}'. Component instanceID='{newComponent.GetInstanceID()}'.");
            }

            stringBuilder.AppendLine(go.Print());
            return stringBuilder.ToString();
        });
    }
}
