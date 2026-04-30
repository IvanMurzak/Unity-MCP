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
using System;
using System.Collections.Generic;
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
            Title = "Assets / Modify",
            IdempotentHint = true
        )]
        [Description("Modify asset file in the project. " +
            "Use '" + AssetsGetDataToolId + "' tool first to inspect the asset structure before modifying. " +
            "Not allowed to modify asset file in 'Packages/' folder. Please modify it in 'Assets/' folder.\n\n" +
            "Three modification surfaces (use whichever fits the task):\n" +
            "  1. 'content' — full SerializedMember override (legacy, backwards compatible).\n" +
            "  2. 'pathPatches' — list of {path, value} pairs routed through Reflector.TryModifyAt.\n" +
            "  3. 'jsonPatch' — JSON Merge Patch routed through Reflector.TryPatch.\n" +
            "When more than one is supplied they run in this order: jsonPatch → pathPatches → content. " +
            "At least one is required.\n" +
            "Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Leading '#/' is stripped.")]
        public string[] Modify
        (
            AssetObjectRef assetRef,
            [Description("Optional. The asset content. It overrides the existing asset content (legacy path).")]
            SerializedMember? content = null,
            [Description("Optional. List of path-scoped patches routed through Reflector.TryModifyAt.")]
            List<PathPatch>? pathPatches = null,
            [Description("Optional. JSON Merge Patch (RFC 7396, extended with [i]/[key] keys) routed through " +
                "Reflector.TryPatch.")]
            string? jsonPatch = null
        )
        {
            if (assetRef == null)
                throw new ArgumentNullException(nameof(assetRef));

            if (!assetRef.IsValid(out var assetValidationError))
                throw new ArgumentException(assetValidationError, nameof(assetRef));

            if (assetRef.AssetPath?.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase) == true)
                throw new ArgumentException($"Not allowed to modify asset in '/Packages' folder. Please modify it in '/Assets' folder. Path: '{assetRef.AssetPath}'.", nameof(assetRef));

            if (assetRef.AssetPath?.StartsWith(ExtensionsRuntimeObject.UnityEditorBuiltInResourcesPath, StringComparison.OrdinalIgnoreCase) == true)
                throw new ArgumentException($"Not allowed to modify built-in asset. Path: '{assetRef.AssetPath}'.", nameof(assetRef));

            var hasContent = content != null;
            var hasPathPatches = pathPatches != null && pathPatches.Count > 0;
            var hasJsonPatch = !string.IsNullOrWhiteSpace(jsonPatch);
            if (!hasContent && !hasPathPatches && !hasJsonPatch)
                throw new ArgumentNullException(paramName: null,
                    $"At least one of '{nameof(content)}', '{nameof(pathPatches)}', or '{nameof(jsonPatch)}' is required. " +
                    $"Make sure the JSON input uses '{nameof(content)}' as the key wrapping the SerializedMember object, " +
                    $"or supply '{nameof(pathPatches)}' / '{nameof(jsonPatch)}' for path-scoped modifications.");

            return MainThread.Instance.Run(() =>
            {
                var asset = assetRef.FindAssetObject(); // AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (asset == null)
                    throw new Exception($"Asset not found using the reference:\n{assetRef}");

                var obj = (object?)asset;
                var logs = new Logs();
                var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");
                var logger = UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_Assets>();

                var anySuccess = false;

                if (hasJsonPatch)
                {
                    if (reflector.TryPatch(ref obj, jsonPatch!, logs: logs, logger: logger))
                        anySuccess = true;
                }

                if (hasPathPatches)
                {
                    for (int i = 0; i < pathPatches!.Count; i++)
                    {
                        var patch = pathPatches[i];
                        if (patch == null || string.IsNullOrEmpty(patch.Path))
                        {
                            logs.Error($"PathPatch[{i}] with empty path skipped.");
                            continue;
                        }
                        if (reflector.TryModifyAt(ref obj, patch.Path, patch.Value, logs: logs, logger: logger))
                            anySuccess = true;
                    }
                }

                if (hasContent)
                {
                    // Fixing instanceID - inject expected instance ID into the valueJsonElement
                    content!.valueJsonElement.SetProperty(ObjectRef.ObjectRefProperty.InstanceID, asset.GetInstanceID());

                    if (reflector.TryModify(ref obj, data: content!, logs: logs, logger: logger))
                        anySuccess = true;
                }

                if (anySuccess)
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
#endif
