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
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_GameObject
    {
        public const string GameObjectFindToolId = "gameobject-find";
        [McpPluginTool
        (
            GameObjectFindToolId,
            Title = "GameObject / Find"
        )]
        [Description("Finds specific GameObject by provided information in opened Prefab or in a Scene. " +
            "First it looks for the opened Prefab, if any Prefab is opened it looks only there ignoring a scene. " +
            "If no opened Prefab it looks into current active scene. " +
            "Returns GameObject information and its children. " +
            "Also, it returns Components preview just for the target GameObject.")]
        public GameObjectData? Find
        (
            GameObjectRef gameObjectRef,
            [Description("Include editable GameObject data (tag, layer, etc).")]
            bool includeData = false,
            [Description("Include attached components references.")]
            bool includeComponents = false,
            [Description("Include 3D bounds of the GameObject.")]
            bool includeBounds = false,
            [Description("Include hierarchy metadata.")]
            bool includeHierarchy = false,
            [Description("Determines the depth of the hierarchy to include. 0 - means only the target GameObject. 1 - means to include one layer below.")]
            int hierarchyDepth = 0
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var go = gameObjectRef.FindGameObject(out var error);
                if (error != null)
                    throw new System.Exception(error);

                if (go == null)
                    return null;

                return go.ToGameObjectData(
                    reflector: McpPlugin.McpPlugin.Instance!.McpManager.Reflector,
                    includeData: includeData,
                    includeComponents: includeComponents,
                    includeBounds: includeBounds,
                    includeHierarchy: includeHierarchy,
                    hierarchyDepth: hierarchyDepth,
                    logger: UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_GameObject>()
                );
            });
        }
    }
}
