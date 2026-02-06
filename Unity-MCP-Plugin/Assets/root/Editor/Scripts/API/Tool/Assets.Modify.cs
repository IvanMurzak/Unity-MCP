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
using System;
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets
    {
        public const string AssetsModifyToolId = "assets-modify";
        [McpPluginTool
        (
            AssetsModifyToolId,
            Title = "Assets / Modify"
        )]
        [Description("Modify asset file in the project. " +
            "Use '" + AssetsGetDataToolId + "' tool first to inspect the asset structure before modifying. " +
            "Not allowed to modify asset file in 'Packages/' folder. Please modify it in 'Assets/' folder.")]
        public string[] Modify
        (
            AssetObjectRef assetRef,
            [Description("The asset content. It overrides the existing asset content.")]
            SerializedMember content
        )
        {
            if (assetRef == null)
                throw new ArgumentNullException(nameof(assetRef));

            if (!assetRef.IsValid(out var assetValidationError))
                throw new ArgumentException(assetValidationError, nameof(assetRef));

            if (assetRef.AssetPath?.StartsWith("Packages/") == true)
                throw new ArgumentException($"Not allowed to modify asset in '/Packages' folder. Please modify it in '/Assets' folder. Path: '{assetRef.AssetPath}'.", nameof(assetRef));

            if (assetRef.AssetPath?.StartsWith(ExtensionsRuntimeObject.UnityEditorBuiltInResourcesPath) == true)
                throw new ArgumentException($"Not allowed to modify built-in asset. Path: '{assetRef.AssetPath}'.", nameof(assetRef));

            return MainThread.Instance.Run(() =>
            {
                var asset = assetRef.FindAssetObject(); // AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (asset == null)
                    throw new Exception($"Asset not found using the reference:\n{assetRef}");

                // Fixing instanceID - inject expected instance ID into the valueJsonElement
                content.valueJsonElement.SetProperty(ObjectRef.ObjectRefProperty.InstanceID, asset.GetInstanceID());

                var obj = (object)asset;
                var logs = new Logs();

                var success = McpPlugin.McpPlugin.Instance!.McpManager.Reflector.TryPopulate(
                    ref obj,
                    data: content,
                    logs: logs,
                    logger: UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_Assets>());

                if (success)
                    EditorUtility.SetDirty(asset);

                // AssetDatabase.CreateAsset(asset, assetPath);
                 AssetDatabase.SaveAssets();
                 AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                 EditorUtils.RepaintAllEditorWindows();

                return logs
                    .Select(log => log.ToString())
                    .ToArray();
            });
        }
    }
}
