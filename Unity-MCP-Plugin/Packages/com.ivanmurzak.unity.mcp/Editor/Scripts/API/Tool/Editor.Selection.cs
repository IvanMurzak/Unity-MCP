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
#if UNITY_6000_5_OR_NEWER
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
                => "Script path is empty. Please provide a valid path. Sample: \"Assets/Scripts/MyScript.cs\".";
        }

        public class SelectionData
        {
            [Description("Returns the actual game object selection. Includes Prefabs, non-modifiable objects.")]
            public GameObjectRef[]? GameObjects { get; set; }
            [Description("Returns the top level selection, excluding Prefabs.")]
            public ComponentRef[]? Transforms { get; set; }
            [Description("The actual unfiltered selection from the Scene returned as entity IDs instead of objects.")]
            public UnityEngine.EntityId[]? InstanceIDs { get; set; }
            [Description("Returns the guids of the selected assets.")]
            public string[]? AssetGUIDs { get; set; }
            [Description("Returns the active game object. (The one shown in the inspector).")]
            public GameObjectRef? ActiveGameObject { get; set; }
            [Description("Returns the entity ID of the actual object selection. Includes Prefabs, non-modifiable objects")]
            public UnityEngine.EntityId ActiveInstanceID { get; set; }
            [Description("Returns the actual object selection. Includes Prefabs, non-modifiable objects.")]
            public ObjectRef? ActiveObject { get; set; }
            [Description("Returns the active transform. (The one shown in the inspector).")]
            public ComponentRef? ActiveTransform { get; set; }

            public static SelectionData FromSelection()
            {
                return new SelectionData
                {
                    GameObjects = Selection.gameObjects?.Select(go => new GameObjectRef(go)).ToArray(),
                    Transforms = Selection.transforms?.Select(t => new ComponentRef(t)).ToArray(),
                    InstanceIDs = Selection.entityIds.ToArray(),
                    AssetGUIDs = Selection.assetGUIDs,
                    ActiveGameObject = Selection.activeGameObject != null
                        ? new GameObjectRef(Selection.activeGameObject)
                        : null,
                    ActiveInstanceID = Selection.activeEntityId,
                    ActiveObject = Selection.activeObject != null
                        ? new ObjectRef(Selection.activeObject)
                        : null,
                    ActiveTransform = Selection.activeTransform != null
                        ? new ComponentRef(Selection.activeTransform)
                        : null
                };
            }
        }
    }
}
#endif
