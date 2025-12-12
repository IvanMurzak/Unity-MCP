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
using System.Linq;
using System.Text;
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
            "GameObject_DestroyComponents",
            Title = "Destroy Components from a GameObject in opened Prefab or in a Scene"
        )]
        [Description("Destroy one or many components from target GameObject. Can't destroy missed components.")]
        public DestroyComponentsResponse DestroyComponents
        (
            GameObjectRef gameObjectRef,
            ComponentRefList destroyComponentRefs
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var go = gameObjectRef.FindGameObject(out var error);
                if (error != null)
                    throw new System.Exception(error);

                if (go == null)
                    throw new System.Exception($"GameObject by {nameof(gameObjectRef)} not found.");

                var destroyCounter = 0;
                var stringBuilder = new StringBuilder();

                var allComponents = go.GetComponents<UnityEngine.Component>();

                var response = new DestroyComponentsResponse();

                foreach (var component in allComponents)
                {
                    if (destroyComponentRefs.Any(cr => cr.Matches(component)))
                    {
                        if (component == null)
                        {
                            response.Errors ??= new List<string>();
                            response.Errors.Add($"Component instanceID='0' is null. Skipping destruction.");
                            continue; // Skip null components
                        }
                        var destroyedComponentRef = new ComponentRef(component);
                        UnityEngine.Object.DestroyImmediate(component);
                        destroyCounter++;
                        response.DestroyedComponents ??= new ComponentRefList();
                        response.DestroyedComponents.Add(destroyedComponentRef);
                    }
                }

                if (destroyCounter == 0)
                    throw new System.Exception(Error.NotFoundComponents(destroyComponentRefs, allComponents));

                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                return response;
            });
        }

        public class DestroyComponentsResponse
        {
            public ComponentRefList? DestroyedComponents { get; set; }
            public List<string>? Errors { get; set; }
        }
    }
}
