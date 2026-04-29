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
using System.Collections.Generic;
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_GameObject
    {
        public const string GameObjectModifyToolId = "gameobject-modify";
        [McpPluginTool
        (
            GameObjectModifyToolId,
            Title = "GameObject / Modify",
            IdempotentHint = true
        )]
        [Description("Modify GameObject fields and properties in opened Prefab or in a Scene. " +
            "You can modify multiple GameObjects at once. Just provide the same number of GameObject references and SerializedMember objects.\n\n" +
            "Three modification surfaces (per GameObject — parallel arrays must have the same length as gameObjectRefs):\n" +
            "  1. '" + "gameObjectDiffs" + "' — full SerializedMember diff per GameObject (legacy, backwards compatible).\n" +
            "  2. '" + "pathPatchesPerGameObject" + "' — list of {path, value} patches per GameObject routed " +
            "through Reflector.TryModifyAt; atomic per-path modification.\n" +
            "  3. '" + "jsonPatchesPerGameObject" + "' — JSON Merge Patch per GameObject routed through " +
            "Reflector.TryPatch.\n" +
            "When more than one is supplied for the same GameObject they run in this order: jsonPatch → pathPatches " +
            "→ diff. At least one of the three is required.\n" +
            "Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'.")]
        public Logs? Modify
        (
            GameObjectRefList gameObjectRefs,
            [Description("Optional. Each item in the array represents a GameObject modification of the 'gameObjectRefs' at the same index. " +
                "Usually a GameObject is a container for components. Each component may have fields and properties for modification. " +
                "If you need to modify components of a GameObject, please use '" + GameObjectComponentModifyToolId + "' tool. " +
                "Ignore values that should not be modified. " +
                "Any unknown or wrong located fields and properties will be ignored. " +
                "Check the result of this command to see what was changed. The ignored fields and properties will be listed.")]
            SerializedMemberList? gameObjectDiffs = null,
            [Description("Optional. Per-GameObject list of path-scoped patches routed through Reflector.TryModifyAt. " +
                "Outer index aligns with 'gameObjectRefs'; inner list contains {path, value} entries. " +
                "Pass null or omit for GameObjects that should not receive path patches.")]
            List<List<PathPatch>?>? pathPatchesPerGameObject = null,
            [Description("Optional. Per-GameObject JSON Merge Patch (RFC 7396, extended with [i]/[key] keys) " +
                "routed through Reflector.TryPatch. Outer index aligns with 'gameObjectRefs'. " +
                "Pass null or omit for GameObjects that should not receive a JSON patch.")]
            List<string?>? jsonPatchesPerGameObject = null
        )
        {
            if (gameObjectRefs == null)
                throw new ArgumentNullException(nameof(gameObjectRefs),
                    "The 'gameObjectRefs' parameter is required. Make sure the JSON input uses 'gameObjectRefs' as the key.");

            if (gameObjectRefs.Count == 0)
                throw new ArgumentException("No GameObject references provided. Please provide at least one GameObject reference.", nameof(gameObjectRefs));

            var hasDiffs = gameObjectDiffs != null && gameObjectDiffs.Count > 0;
            var hasPathPatches = pathPatchesPerGameObject != null && pathPatchesPerGameObject.Count > 0;
            var hasJsonPatches = jsonPatchesPerGameObject != null && jsonPatchesPerGameObject.Count > 0;

            if (!hasDiffs && !hasPathPatches && !hasJsonPatches)
                throw new ArgumentException(
                    $"At least one of '{"gameObjectDiffs"}', '{"pathPatchesPerGameObject"}', or '{"jsonPatchesPerGameObject"}' is required.");

            if (hasDiffs && gameObjectDiffs!.Count != gameObjectRefs.Count)
                throw new ArgumentException($"The number of {"gameObjectDiffs"} and {nameof(gameObjectRefs)} should be the same. " +
                    $"{"gameObjectDiffs"}: {gameObjectDiffs.Count}, {nameof(gameObjectRefs)}: {gameObjectRefs.Count}", "gameObjectDiffs");

            if (hasPathPatches && pathPatchesPerGameObject!.Count != gameObjectRefs.Count)
                throw new ArgumentException($"The number of {"pathPatchesPerGameObject"} and {nameof(gameObjectRefs)} should be the same. " +
                    $"{"pathPatchesPerGameObject"}: {pathPatchesPerGameObject.Count}, {nameof(gameObjectRefs)}: {gameObjectRefs.Count}",
                    "pathPatchesPerGameObject");

            if (hasJsonPatches && jsonPatchesPerGameObject!.Count != gameObjectRefs.Count)
                throw new ArgumentException($"The number of {"jsonPatchesPerGameObject"} and {nameof(gameObjectRefs)} should be the same. " +
                    $"{"jsonPatchesPerGameObject"}: {jsonPatchesPerGameObject.Count}, {nameof(gameObjectRefs)}: {gameObjectRefs.Count}",
                    "jsonPatchesPerGameObject");

            return MainThread.Instance.Run(() =>
            {
                var logs = new Logs();
                var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");
                var logger = UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_GameObject>();

                for (int i = 0; i < gameObjectRefs.Count; i++)
                {
                    var go = gameObjectRefs[i].FindGameObject(out var error);
                    if (error != null)
                    {
                        logs.Error(error);
                        continue;
                    }
                    if (go == null)
                    {
                        logs.Error($"GameObject by {nameof(gameObjectRefs)}[{i}] not found.");
                        continue;
                    }

                    var objToModify = (object?)go;
                    var anyChange = false;

                    // 1) JSON Patch
                    if (hasJsonPatches)
                    {
                        var json = jsonPatchesPerGameObject![i];
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            if (reflector.TryPatch(ref objToModify, json!, logs: logs, logger: logger))
                                anyChange = true;
                        }
                    }

                    // 2) Path patches
                    if (hasPathPatches)
                    {
                        var perGo = pathPatchesPerGameObject![i];
                        if (perGo != null)
                        {
                            foreach (var patch in perGo)
                            {
                                if (patch == null || string.IsNullOrEmpty(patch.Path))
                                {
                                    logs.Error($"Empty path patch on {nameof(gameObjectRefs)}[{i}] skipped.");
                                    continue;
                                }
                                if (reflector.TryModifyAt(ref objToModify, patch.Path, patch.Value, logs: logs, logger: logger))
                                    anyChange = true;
                            }
                        }
                    }

                    // 3) Legacy full diff
                    if (hasDiffs)
                    {
                        var diff = gameObjectDiffs![i];
                        if (diff == null)
                        {
                            logs.Error($"'{"gameObjectDiffs"}[{i}]' is null. Each entry in the array must be a valid SerializedMember object.");
                        }
                        else
                        {
                            if (reflector.TryModify(ref objToModify, data: diff, logs: logs, logger: logger))
                                anyChange = true;
                        }
                    }

                    if (anyChange)
                        UnityEditor.EditorUtility.SetDirty(go);
                }

                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                if (logs.Count == 0)
                    logs.Warning("No modifications were made.");

                return logs;
            });
        }
    }
}
