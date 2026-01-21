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
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_GameObject
    {
        public const string GameObjectComponentModifyToolId = "gameobject-component-modify";
        [McpPluginTool
        (
            GameObjectComponentModifyToolId,
            Title = "GameObject / Component / Modify"
        )]
        [Description("Modify a specific Component on a GameObject in opened Prefab or in a Scene. " +
            "Allows direct modification of component fields and properties without wrapping in GameObject structure. " +
            "Use '" + GameObjectComponentGetToolId + "' first to inspect the component structure before modifying.")]
        public ModifyComponentResponse ModifyComponent
        (
            GameObjectRef gameObjectRef,
            ComponentRef componentRef,
            [Description("The component data to apply. Should contain '" + nameof(SerializedMember.fields) + "' and/or '" + nameof(SerializedMember.props) + "' with the values to modify.\n" +
                "Only include the fields/properties you want to change.\n" +
                "Any unknown or invalid fields and properties will be reported in the response.")]
            SerializedMember componentDiff
        )
        {
            return MainThread.Instance.Run(() =>
            {
                if (!gameObjectRef.IsValid(out var gameObjectValidationError))
                    throw new ArgumentException(gameObjectValidationError, nameof(gameObjectRef));

                if (!componentRef.IsValid(out var componentValidationError))
                    throw new ArgumentException(componentValidationError, nameof(componentRef));

                var go = gameObjectRef.FindGameObject(out var error);
                if (error != null)
                    throw new Exception(error);

                if (go == null)
                    throw new Exception("GameObject not found.");

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

                var response = new ModifyComponentResponse
                {
                    reference = new ComponentRef(targetComponent),
                    index = targetIndex
                };

                var logs = new Logs();
                var objToModify = (object)targetComponent;

                var success = McpPlugin.McpPlugin.Instance!.McpManager.Reflector.TryPopulate(
                    ref objToModify,
                    data: componentDiff,
                    logs: logs,
                    logger: UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_GameObject>());

                if (success)
                {
                    UnityEditor.EditorUtility.SetDirty(go);
                    UnityEditor.EditorUtility.SetDirty(targetComponent);
                    response.success = true;
                }

                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                response.logs = logs
                    .Select(log => log.ToString())
                    .ToArray();

                // Return updated component data
                response.component = new ComponentDataShallow(targetComponent);

                return response;
            });
        }

        public class ModifyComponentResponse
        {
            [Description("Whether the modification was successful.")]
            public bool success;

            [Description("Reference to the modified component.")]
            public ComponentRef? reference;

            [Description("Index of the component in the GameObject's component list.")]
            public int index;

            [Description("Updated component information after modification.")]
            public ComponentDataShallow? component;

            [Description("Log of modifications made and any warnings/errors encountered.")]
            public string[]? logs;
        }
    }
}
