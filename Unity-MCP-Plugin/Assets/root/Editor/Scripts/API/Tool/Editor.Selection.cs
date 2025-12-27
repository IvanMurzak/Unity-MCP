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
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Editor_Selection
    {
        public static class Error
        {
            public static string ScriptPathIsEmpty()
                => "[Error] Script path is empty. Please provide a valid path. Sample: \"Assets/Scripts/MyScript.cs\".";
        }

        public class SelectionData
        {
            [Description("Returns the actual game object selection. Includes Prefabs, non-modifiable objects.")]
            public GameObjectRef[]? gameObjects;
            [Description("Returns the top level selection, excluding Prefabs.")]
            public ComponentRef[]? transforms;
            [Description("The actual unfiltered selection from the Scene returned as instance ids instead of objects.")]
            public int[]? instanceIDs;
            [Description("Returns the guids of the selected assets.")]
            public string[]? assetGUIDs;
            [Description("Returns the active game object. (The one shown in the inspector).")]
            public GameObjectRef? activeGameObject;
            [Description("Returns the instanceID of the actual object selection. Includes Prefabs, non-modifiable objects")]
            public int activeInstanceID;
            [Description("Returns the actual object selection. Includes Prefabs, non-modifiable objects.")]
            public ObjectRef? activeObject;
            [Description("Returns the active transform. (The one shown in the inspector).")]
            public ComponentRef? activeTransform;

            public static SelectionData FromSelection()
            {
                return new SelectionData
                {
                    gameObjects = Selection.gameObjects?.Select(go => new GameObjectRef(go)).ToArray(),
                    transforms = Selection.transforms?.Select(t => new ComponentRef(t)).ToArray(),
#if UNITY_6000_3_OR_NEWER
                    instanceIDs = Selection.entityIds.Cast<int>().ToArray(),
#else
                    instanceIDs = Selection.instanceIDs,
#endif
                    assetGUIDs = Selection.assetGUIDs,
                    activeGameObject = new GameObjectRef(Selection.activeGameObject),
#if UNITY_6000_3_OR_NEWER
                    activeInstanceID = Selection.activeEntityId,
#else
                    activeInstanceID = Selection.activeInstanceID,
#endif
                    activeObject = new ObjectRef(Selection.activeObject),
                    activeTransform = new ComponentRef(Selection.activeTransform)
                };
            }
        }
    }
}
