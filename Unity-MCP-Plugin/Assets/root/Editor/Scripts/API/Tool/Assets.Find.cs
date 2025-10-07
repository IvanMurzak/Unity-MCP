/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System.ComponentModel;
using System.Text;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Common;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    using Consts = Common.Consts;
    public partial class Tool_Assets
    {
        [McpPluginTool
        (
            "Assets_Find",
            Title = "Find assets in the project"
        )]
        [Description(@"Search the asset database using the search filter string.
Available types:
t:AnimationClip
t:AudioClip
t:AudioMixer
t:ComputeShader
t:Font
t:GUISkin
t:Material
t:Mesh
t:Model
t:PhysicMaterial
t:Prefab
t:Scene
t:Script
t:Shader
t:Sprite
t:Texture
t:VideoClip
t:VisualEffectAsset
t:VisualEffectSubgraph")]
        public string Search
        (
// <ref>https://docs.unity3d.com/ScriptReference/AssetDatabase.FindAssets.html</ref>
[Description(@"Searching filter. Could be empty.
Name: Filter assets by their filename (without extension). Words separated by whitespace are treated as separate name searches. For example, 'test asset' is a name of an Asset which will be searched for. Note that the name can be used to identify an asset. Further, the name used in the filter string can be specified as a subsection. For example, the 'test asset' example above can be matched using 'test'.
Labels (l:): Assets can have labels attached to them. Assets with particular labels can be found using the keyword 'l:' before each label. This indicates that the string is searching for labels.
Types (t:): Find assets based on explicitly identified types. The keyword 't:' is used as a way to specify that typed assets are being looked for. If more than one type is included in the filter string, then assets that match one class will be returned. Types can either be built-in types such as Texture2D or user-created script classes. User-created classes are assets created from a ScriptableObject class in the project. If all assets are wanted, use Object as all assets derive from Object. Specifying one or more folders using the searchInFolders argument will limit the searching to these folders and their child folders. This is faster than searching all assets in all folders.
AssetBundles (b:): Find assets which are part of an Asset bundle. The keyword 'b:' is used to determine that Asset bundle names should be part of the query.
Area (a:): Find assets in a specific area of a project. Valid values are 'all', 'assets', and 'packages'. Use this to make your query more specific using the 'a:' keyword followed by the area name to speed up searching.
Globbing (glob:): Use globbing to match specific rules. The keyword 'glob:' is followed by the query. For example, 'glob:Editor' will find all Editor folders in a project, 'glob:(Editor|Resources)' will find all Editor and Resources folders in a project, 'glob:Editor/*' will return all Assets inside Editor folders in a project, while 'glob:Editor/**' will return all Assets within Editor folders recursively.

Note:
Searching is case insensitive.")]
            string? filter = null,
            [Description("The folders where the search will start. If null, the search will be performed in all folders.")]
            string[]? searchInFolders = null
        )
        => MainThread.Instance.Run(() =>
        {
            var assetGuids = (searchInFolders?.Length ?? 0) == 0
                ? AssetDatabase.FindAssets(filter ?? string.Empty)
                : AssetDatabase.FindAssets(filter ?? string.Empty, searchInFolders);

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("instanceID | assetGuid                            | assetPath");
            stringBuilder.AppendLine("-----------+--------------------------------------+---------------------------------");
            //                       " -12345    | 8e09c738-7b14-4d83-9740-2b396bd4cfc9 | Assets/Editor/Image.png");

            for (var i = 0; i < assetGuids.Length; i++)
            {
                if (i >= Consts.MCP.Plugin.LinesLimit)
                {
                    stringBuilder.AppendLine($"... and {assetGuids.Length - i} more assets. Use {nameof(searchInFolders)} parameter to specify request.");
                    break;
                }
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                var assetObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (assetObject == null) continue;
                var instanceID = assetObject.GetInstanceID();
                stringBuilder.AppendLine($"{instanceID,-10} | {assetGuids[i],-36} | {assetPath}");
            }

            return $"[Success] Assets found: {assetGuids.Length}.\n{stringBuilder}";
        });
    }
}