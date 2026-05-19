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
#if !UNITY_6000_5_OR_NEWER
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using AIGD;
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

        // Shared skill metadata for `editor-selection-get`. Mirrors the Unity 6.5+ partial
        // (Editor.Selection.cs) so the SKILL.md generation is identical for both builds.
        internal static class GetSkill
        {
            public const string Description =
                "Get information about the current Selection in the Unity Editor — active object, active transform, " +
                "selected GameObjects, transforms, instance IDs, and asset GUIDs (each enrichment is opt-in). " +
                "Pair with 'editor-selection-set' to change the selection.";

            public const string Body =
                "Get information about the current Selection in the Unity Editor. " +
                "Use 'editor-selection-set' tool to set the selection.\n\n" +
                "## Toggles (default off where indicated to keep responses small)\n\n" +
                "- `includeGameObjects` (default `false`) — populate `GameObjects[]`.\n" +
                "- `includeTransforms` (default `false`) — populate `Transforms[]` as `ComponentRef`s.\n" +
                "- `includeInstanceIDs` (default `false`) — populate `InstanceIDs[]`.\n" +
                "- `includeAssetGUIDs` (default `false`) — populate `AssetGUIDs[]` from project-window selection.\n" +
                "- `includeActiveObject` (default `true`) — populate `ActiveObject` as a generic `ObjectRef`.\n" +
                "- `includeActiveTransform` (default `true`) — populate `ActiveTransform` as a `ComponentRef`.\n\n" +
                "`ActiveGameObject` and `ActiveInstanceID` are always populated.";
        }

        // Shared skill metadata for `editor-selection-set`. Mirrors Editor.Selection.cs.
        internal static class SetSkill
        {
            public const string Description =
                "Set the current Selection in the Unity Editor to the provided objects. " +
                "All `ObjectRef`s must resolve to existing Unity objects; otherwise the call throws. " +
                "Use 'editor-selection-get' to inspect the current selection first.";

            public const string Body =
                "Set the current Selection in the Unity Editor to the provided objects. " +
                "Use 'editor-selection-get' tool to get the current selection first.\n\n" +
                "## Inputs\n\n" +
                "- `select` — array of `ObjectRef`. Every entry MUST resolve via `FindObject()`; otherwise the tool throws " +
                "before touching `Selection.objects`.\n\n" +
                "## Behavior\n\n" +
                "Assigns the resolved array to `Selection.objects`, then calls " +
                "`UnityEditorInternal.InternalEditorUtility.RepaintAllViews()` so Hierarchy/Inspector reflect the change. " +
                "Returns the post-change `SelectionData` snapshot.";
        }
    }
}
#endif
