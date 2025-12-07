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
using System.Text;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets
    {
        [McpPluginTool
        (
            "Assets_Modify",
            Title = "Modify asset file"
        )]
        [Description(@"Modify asset in the project. Not allowed to modify asset in 'Packages/' folder. Please modify it in 'Assets/' folder.")]
        public string Modify
        (
            AssetObjectRef assetRef,
            [Description("The asset content. It overrides the existing asset content.")]
            SerializedMember content
        )
        => MainThread.Instance.Run(() =>
        {
            if (assetRef?.IsValid == false)
                return $"[Error] Invalid asset reference.";

            if (assetRef?.AssetPath?.StartsWith("Packages/") == true)
                return Error.NotAllowedToModifyAssetInPackages(assetRef.AssetPath);

            var asset = assetRef.FindAssetObject(); // AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
                return $"[Error] Asset not found using the reference:\n{assetRef}";

            // Fixing instanceID - inject expected instance ID into the valueJsonElement
            content.valueJsonElement.SetProperty(ObjectRef.ObjectRefProperty.InstanceID, asset.GetInstanceID());

            var obj = (object)asset;
            var result = new StringBuilder();

            var success = McpPlugin.McpPlugin.Instance!.McpManager.Reflector.TryPopulate(
                ref obj,
                data: content,
                stringBuilder: result,
                logger: McpPlugin.McpPlugin.Instance.Logger);

            if (success)
                EditorUtility.SetDirty(asset);

            // AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            return result.ToString();

            //             var instanceID = asset.GetInstanceID();
            //             return @$"[Success] Loaded asset.
            // # Asset path: {assetPath}
            // # Asset GUID: {assetGuid}
            // # Asset instanceID: {instanceID}";
        });
    }
}
