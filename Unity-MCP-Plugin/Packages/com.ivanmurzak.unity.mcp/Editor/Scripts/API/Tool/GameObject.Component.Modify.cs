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
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using AIGD;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_GameObject
    {
        public const string GameObjectComponentModifyToolId = "gameobject-component-modify";
        [McpPluginTool
        (
            GameObjectComponentModifyToolId,
            Title = "GameObject / Component / Modify",
            IdempotentHint = true
        )]
        [Description("Modify a specific Component on a GameObject in opened Prefab or in a Scene. " +
            "Allows direct modification of component fields and properties without wrapping in GameObject structure. " +
            "Use '" + GameObjectComponentGetToolId + "' first to inspect the component structure before modifying.\n\n" +
            "Three modification surfaces (use whichever fits the task):\n" +
            "  1. '" + "componentDiff" + "' — full SerializedMember diff (legacy, backwards compatible).\n" +
            "  2. '" + "pathPatches" + "' — list of {path, value} pairs routed through Reflector.TryModifyAt; " +
            "atomic per-path modification, multiple entries can target different depths.\n" +
            "  3. '" + "jsonPatch" + "' — a JSON Merge Patch (RFC 7396, extended with [i]/[key] notation) " +
            "routed through Reflector.TryPatch; multiple fields at any depth in a single call.\n" +
            "When more than one is supplied they run in this order: jsonPatch → pathPatches → componentDiff. " +
            "At least one is required.\n" +
            "Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Leading '#/' is stripped.")]
        public ModifyComponentResponse ModifyComponent
        (
            GameObjectRef gameObjectRef,
            ComponentRef componentRef,
            [Description("Optional. The full component data to apply (legacy path). Should contain '" + nameof(SerializedMember.fields) + "' and/or '" + nameof(SerializedMember.props) + "' with the values to modify.\n" +
                "Only include the fields/properties you want to change.\n" +
                "Any unknown or invalid fields and properties will be reported in the response.")]
            SerializedMember? componentDiff = null,
            [Description("Optional. List of path-scoped patches routed through Reflector.TryModifyAt. " +
                "Each entry targets one field/element/entry by path. " +
                "Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'.")]
            List<PathPatch>? pathPatches = null,
            [Description("Optional. JSON Merge Patch (RFC 7396, extended with [i]/[key] keys) routed through " +
                "Reflector.TryPatch. Allows multiple fields at any depth to be updated in a single call. " +
                "Use '$type' for compatible-subtype replacement.")]
            string? jsonPatch = null
        )
        {
            if (!gameObjectRef.IsValid(out var gameObjectValidationError))
                throw new ArgumentException(gameObjectValidationError, nameof(gameObjectRef));

            if (!componentRef.IsValid(out var componentValidationError))
                throw new ArgumentException(componentValidationError, nameof(componentRef));

            var hasDiff = componentDiff != null;
            var hasPathPatches = pathPatches != null && pathPatches.Count > 0;
            var hasJsonPatch = !string.IsNullOrWhiteSpace(jsonPatch);
            if (!hasDiff && !hasPathPatches && !hasJsonPatch)
                throw new ArgumentNullException(paramName: null,
                    $"At least one of '{nameof(componentDiff)}', '{nameof(pathPatches)}', or '{nameof(jsonPatch)}' is required. " +
                    $"Make sure the JSON input uses '{nameof(componentDiff)}' as the key (not 'fields' or 'props' directly), " +
                    $"or supply '{nameof(pathPatches)}' / '{nameof(jsonPatch)}' for path-scoped modifications.");

            return MainThread.Instance.Run(() =>
            {
                var go = gameObjectRef.FindGameObject(out var error);
                if (error != null)
                    throw new Exception(error);

                if (go == null)
                    throw new Exception("GameObject not found.");

                var allComponents = go.GetComponents<UnityEngine.Component>();
                UnityEngine.Component? targetComponent = null;
                int targetIndex = -1;

                for (int i = 0; i < allComponents.Length; i++)
                {
                    if (componentRef.Matches(allComponents[i], i))
                    {
                        targetComponent = allComponents[i];
                        targetIndex = i;
                        break;
                    }
                }

                if (targetComponent == null)
                    throw new Exception(Error.NotFoundComponent(componentRef.InstanceID, allComponents));

                var response = new ModifyComponentResponse
                {
                    Reference = new ComponentRef(targetComponent),
                    Index = targetIndex
                };

                var logs = new Logs();
                var objToModify = (object?)targetComponent;
                var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");
                var logger = UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_GameObject>();

                var anySuccess = false;

                // 1) JSON Patch first — applied as a single Reflector.TryPatch
                if (hasJsonPatch)
                {
                    if (reflector.TryPatch(ref objToModify, jsonPatch!, logs: logs, logger: logger))
                        anySuccess = true;
                }

                // 2) Path-scoped patches — one Reflector.TryModifyAt per entry
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
                        if (reflector.TryModifyAt(ref objToModify, patch.Path, patch.Value, logs: logs, logger: logger))
                            anySuccess = true;
                    }
                }

                // 3) Legacy full SerializedMember diff
                if (hasDiff)
                {
                    if (reflector.TryModify(ref objToModify, data: componentDiff!, logs: logs, logger: logger))
                        anySuccess = true;
                }

                if (anySuccess)
                {
                    UnityEditor.EditorUtility.SetDirty(go);
                    UnityEditor.EditorUtility.SetDirty(targetComponent);
                    response.Success = true;
                }

                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                response.Logs = logs
                    .Select(log => log.ToString())
                    .ToArray();

                // Return updated component data
                response.Component = new ComponentDataShallow(targetComponent);

                return response;
            });
        }

    }
}
