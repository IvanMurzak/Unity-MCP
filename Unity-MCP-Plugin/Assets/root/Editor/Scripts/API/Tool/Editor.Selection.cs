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
using System.Linq;
using com.IvanMurzak.McpPlugin.Common;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Editor_Selection
    {
        public static string SelectionPrint => @$"Editor Selection:
Selection.gameObjects: {Selection.gameObjects?.Select(go => go.GetInstanceID()).JoinString(", ")}
Selection.transforms: {Selection.transforms?.Select(t => t.GetInstanceID()).JoinString(", ")}
Selection.instanceIDs: {Selection.instanceIDs?.JoinString(", ")}
Selection.assetGUIDs: {Selection.assetGUIDs?.JoinString(", ")}
Selection.activeGameObject: {Selection.activeGameObject?.GetInstanceID()}
Selection.activeInstanceID: {Selection.activeInstanceID}
Selection.activeObject: {Selection.activeObject?.GetInstanceID()}
Selection.activeTransform: {Selection.activeTransform?.GetInstanceID()}";

        public static class Error
        {
            public static string ScriptPathIsEmpty()
                => "[Error] Script path is empty. Please provide a valid path. Sample: \"Assets/Scripts/MyScript.cs\".";
        }
    }
}
