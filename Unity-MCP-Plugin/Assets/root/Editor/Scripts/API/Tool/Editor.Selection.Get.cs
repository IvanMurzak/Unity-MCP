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
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Editor_Selection
    {
        [McpPluginTool
        (
            "Editor_Selection_Get",
            Title = "Editor / Selection / Get"
        )]
        [Description(@"Get information about the current Selection in the Unity Editor.")]
        public SelectionData Get(
            bool includeGameObjects = false,
            bool includeTransforms = false,
            bool includeInstanceIDs = false,
            bool includeAssetGUIDs = false,
            bool includeActiveObject = true,
            bool includeActiveTransform = true)
        {
            return MainThread.Instance.Run(() =>
            {
                var response = new SelectionData()
                {
                    activeGameObject = Selection.activeGameObject != null
                        ? new GameObjectRef(Selection.activeGameObject)
                        : null,
                    activeInstanceID = Selection.activeInstanceID
                };

                if (includeGameObjects)
                    response.gameObjects = Selection.gameObjects?.Select(go => new GameObjectRef(go)).ToArray();

                if (includeTransforms)
                    response.transforms = Selection.transforms?.Select(t => new ComponentRef(t)).ToArray();

                if (includeInstanceIDs)
                    response.instanceIDs = Selection.instanceIDs;

                if (includeAssetGUIDs)
                    response.assetGUIDs = Selection.assetGUIDs;

                if (includeActiveObject)
                    response.activeObject = Selection.activeObject != null
                        ? new ObjectRef(Selection.activeObject)
                        : null;

                if (includeActiveTransform)
                    response.activeTransform = Selection.activeTransform != null
                        ? new ComponentRef(Selection.activeTransform)
                        : null;

                return response;
            });
        }
    }
}
