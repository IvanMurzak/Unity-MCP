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
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_GameObject
    {
        [McpPluginTool
        (
            "GameObject_Modify",
            Title = "GameObject / Modify"
        )]
        [Description(@"Modify GameObjects and/or attached component's field and properties in opened Prefab or in a Scene.
You can modify multiple GameObjects at once. Just provide the same number of GameObject references and SerializedMember objects.")]
        public string Modify
        (
            GameObjectRefList gameObjectRefs,
            [Description("Each item in the array represents a GameObject modification of the 'gameObjectRefs' at the same index.\n" +
                "Usually a GameObject is a container for components. Each component may have fields and properties for modification.\n" +
                "If you need to modify components of a gameObject, please use '" + nameof(SerializedMember.fields) + "' to wrap a component into it. " +
                "Each component needs to have '" + nameof(SerializedMember.typeName) + "' and '" + nameof(SerializedMember.name) + "' or 'value." + ObjectRef.ObjectRefProperty.InstanceID + "' fields to identify the exact modification target.\n" +
                "Ignore values that should not be modified.\n" +
                "Any unknown or wrong located fields and properties will be ignored.\n" +
                "Check the result of this command to see what was changed. The ignored fields and properties will be listed.")]
            SerializedMemberList gameObjectDiffs
        )
        {
            return MainThread.Instance.Run(() =>
            {
                if (gameObjectRefs.Count == 0)
                    return "[Error] No GameObject references provided. Please provide at least one GameObject reference.";

                if (gameObjectDiffs.Count != gameObjectRefs.Count)
                    return $"[Error] The number of {nameof(gameObjectDiffs)} and {nameof(gameObjectRefs)} should be the same. " +
                        $"{nameof(gameObjectDiffs)}: {gameObjectDiffs.Count}, {nameof(gameObjectRefs)}: {gameObjectRefs.Count}";

                var stringBuilder = new StringBuilder();

                for (int i = 0; i < gameObjectRefs.Count; i++)
                {

                    var go = gameObjectRefs[i].FindGameObject(out var error);
                    if (error != null)
                    {
                        stringBuilder.AppendLine($"[Error] {error}");
                        continue;
                    }
                    if (go == null)
                    {
                        stringBuilder.AppendLine($"[Error] GameObject by {nameof(gameObjectRefs)}[{i}] not found.");
                        continue;
                    }
                    var objToModify = (object)go;

                    // LLM may mistakenly provide "typeName" as a Component type when it should be a GameObject.
                    // It is fine, lets handle it gracefully.
                    var type = TypeUtils.GetType(gameObjectDiffs[i].typeName);
                    if (type != null && typeof(UnityEngine.Component).IsAssignableFrom(type))
                    {
                        var component = go.GetComponent(type);
                        if (component == null)
                        {
                            stringBuilder.AppendLine($"[Error] Component '{type.GetTypeName(pretty: false)}' not found on GameObject '{go.name.ValueOrNull()}'.");
                            continue;
                        }
                        // Switch to the component type for modification.
                        objToModify = component;
                    }

                    var success = McpPlugin.McpPlugin.Instance!.McpManager.Reflector.TryPopulate(
                        ref objToModify,
                        data: gameObjectDiffs[i],
                        stringBuilder: stringBuilder,
                        logger: McpPlugin.McpPlugin.Instance.Logger);

                    if (success)
                        UnityEditor.EditorUtility.SetDirty(go);
                }

                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                var result = stringBuilder.ToString();
                return string.IsNullOrEmpty(result)
                    ? "[Success] No modifications were made."
                    : result;
            });
        }
    }
}
