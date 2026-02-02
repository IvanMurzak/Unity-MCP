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
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Editor_Selection
    {
        public const string EditorSelectionSetToolId = "editor-selection-set";
        [McpPluginTool
        (
            EditorSelectionSetToolId,
            Title = "Editor / Selection / Set"
        )]
        [Description("Set the current Selection in the Unity Editor to the provided objects. " +
            "Use '" + EditorSelectionGetToolId + "' tool to get the current selection first.")]
        public SelectionData Set(ObjectRef[] select)
        {
            return MainThread.Instance.Run(() =>
            {
                var objects = select.Select(o => o.FindObject()).ToArray();
                if (objects.Any(o => o == null))
                    throw new System.Exception("One or more objects could not be found. Please ensure all provided ObjectRefs are valid.");

                Selection.objects = objects;

                UnityMcpPlugin.NotifyToolExecuted();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                return SelectionData.FromSelection();
            });
        }
    }
}
