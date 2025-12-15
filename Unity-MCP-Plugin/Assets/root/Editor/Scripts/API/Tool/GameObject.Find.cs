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
            "GameObject_Find",
            Title = "GameObject / Find"
        )]
        [Description(@"Finds specific GameObject by provided information in opened Prefab or in a Scene.
First it looks for the opened Prefab, if any Prefab is opened it looks only there ignoring a scene.
If no opened Prefab it looks into current active scene.
Returns GameObject information and its children.
Also, it returns Components preview just for the target GameObject.")]
        public GameObjectData Find
        (
            GameObjectRef gameObjectRef,
            [Description("Include serialized data of the GameObject and its components.")]
            bool includeData = true,
            [Description("Include bounds of the GameObject.")]
            bool includeBounds = true,
            [Description("Include hierarchy metadata.")]
            bool includeHierarchy = true,
            [Description("Determines the depth of the hierarchy to include. 0 - means only the target GameObject. 1 - means to include one layer below.")]
            int hierarchyDepth = 0,
            [Description("Performs deep serialization including all nested objects. Otherwise, only serializes top-level properties.")]
            bool deepSerialization = true
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var go = gameObjectRef.FindGameObject(out var error);
                if (error != null)
                    throw new System.Exception(error);

                if (go == null)
                    throw new System.Exception("GameObject not found after successful search.");

                return go.ToGameObjectData(
                    includeData: includeData,
                    includeBounds: includeBounds,
                    includeHierarchy: includeHierarchy,
                    hierarchyDepth: hierarchyDepth,
                    deepSerialization: deepSerialization
                );
            });
        }
    }
}
