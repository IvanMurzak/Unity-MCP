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
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Editor_Selection
    {
        [McpPluginTool
        (
            "Editor_Selection_Set",
            Title = "Set Selection in Unity Editor"
        )]
        [Description(@"'UnityEditor.Selection'. Access to the selection in the editor.
Use it to select Assets or GameObjects in a scene. Set empty array to clear selection.
Selection.instanceIDs - The actual unfiltered selection from the Scene returned as instance ids.
Selection.activeInstanceID -  The 'instanceID' of the actual object selection. Includes Prefabs, non-modifiable objects.")]
        public string Set
        (
            [Description("The 'instanceID' array of the target GameObjects.")]
            int[]? instanceIDs = null,
            [Description("The 'instanceID' of the target Object.")]
            int activeInstanceID = 0
        )
        {
            return MainThread.Instance.Run(() =>
            {
                Selection.instanceIDs = instanceIDs ?? new int[0];
                Selection.activeInstanceID = activeInstanceID;

                return "[Success] " + SelectionPrint;
            });
        }
    }
}
