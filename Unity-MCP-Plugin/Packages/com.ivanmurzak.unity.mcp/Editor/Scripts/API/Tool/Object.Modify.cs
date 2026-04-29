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
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Object
    {
        public const string ObjectModifyToolId = "object-modify";
        [McpPluginTool
        (
            ObjectModifyToolId,
            Title = "Object / Modify",
            IdempotentHint = true
        )]
        [Description("Modify the specified Unity Object. " +
            "Allows direct modification of object fields and properties. " +
            "Use '" + ObjectGetDataToolId + "' first to inspect the object structure before modifying.\n\n" +
            "Three modification surfaces (use whichever fits the task):\n" +
            "  1. '" + "objectDiff" + "' — full SerializedMember diff (legacy, backwards compatible).\n" +
            "  2. '" + "pathPatches" + "' — list of {path, value} pairs routed through Reflector.TryModifyAt; " +
            "atomic per-path modification, multiple entries can target different depths.\n" +
            "  3. '" + "jsonPatch" + "' — a JSON Merge Patch (RFC 7396, extended with [i]/[key] notation) " +
            "routed through Reflector.TryPatch; multiple fields at any depth in a single call.\n" +
            "When more than one is supplied they run in this order: jsonPatch → pathPatches → objectDiff. " +
            "At least one is required.\n" +
            "Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Leading '#/' is stripped.")]
        public ModifyObjectResponse Modify
        (
            ObjectRef objectRef,
            [Description("Optional. The full object data to apply (legacy path). Should contain '" + nameof(SerializedMember.fields) + "' and/or '" + nameof(SerializedMember.props) + "' with the values to modify.\n" +
                "Only include the fields/properties you want to change.\n" +
                "Any unknown or invalid fields and properties will be reported in the response.")]
            SerializedMember? objectDiff = null,
            [Description("Optional. List of path-scoped patches routed through Reflector.TryModifyAt.")]
            List<PathPatch>? pathPatches = null,
            [Description("Optional. JSON Merge Patch (RFC 7396, extended with [i]/[key] keys) routed through " +
                "Reflector.TryPatch.")]
            string? jsonPatch = null
        )
        {
            if (objectRef == null)
                throw new ArgumentNullException(nameof(objectRef));

            if (!objectRef.IsValid(out var error))
                throw new ArgumentException(error, nameof(objectRef));

            var hasDiff = objectDiff != null;
            var hasPathPatches = pathPatches != null && pathPatches.Count > 0;
            var hasJsonPatch = !string.IsNullOrWhiteSpace(jsonPatch);
            if (!hasDiff && !hasPathPatches && !hasJsonPatch)
                throw new ArgumentNullException(nameof(objectDiff),
                    "At least one of 'objectDiff', 'pathPatches', or 'jsonPatch' is required. " +
                    "Make sure the JSON input uses 'objectDiff' as the key wrapping the SerializedMember object, " +
                    "or supply 'pathPatches' / 'jsonPatch' for path-scoped modifications.");

            return MainThread.Instance.Run(() =>
            {
                var obj = objectRef.FindObject();
                if (obj == null)
                    throw new Exception($"Not found UnityEngine.Object with provided data for reference: {objectRef}.");

                var logs = new Logs();
                var objToModify = (object?)obj;
                var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");
                var logger = UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_Object>();

                var anySuccess = false;

                if (hasJsonPatch)
                {
                    if (reflector.TryPatch(ref objToModify, jsonPatch!, logs: logs, logger: logger))
                        anySuccess = true;
                }

                if (hasPathPatches)
                {
                    foreach (var patch in pathPatches!)
                    {
                        if (string.IsNullOrEmpty(patch.Path))
                        {
                            logs.Error($"PathPatch with empty path skipped.");
                            continue;
                        }
                        if (reflector.TryModifyAt(ref objToModify, patch.Path, patch.Value, logs: logs, logger: logger))
                            anySuccess = true;
                    }
                }

                if (hasDiff)
                {
                    if (reflector.TryModify(ref objToModify, data: objectDiff!, logs: logs, logger: logger))
                        anySuccess = true;
                }

                if (anySuccess)
                    UnityEditor.EditorUtility.SetDirty(obj);

                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                // Return updated object data
                var data = reflector.Serialize(
                    obj,
                    name: obj.name,
                    recursive: true,
                    logger: logger
                );

                return new ModifyObjectResponse(anySuccess, logs)
                {
                    Reference = objectRef,
                    Data = data
                };
            });
        }

        public class ModifyObjectResponse
        {
            [Description("Whether the modification was successful.")]
            public bool Success { get; set; } = false;

            [Description("Reference to the modified object.")]
            public ObjectRef? Reference { get; set; }

            [Description("Updated object data after modification.")]
            public SerializedMember? Data { get; set; }

            [Description("Log of modifications made and any warnings/errors encountered.")]
            public string[]? Logs { get; set; }

            public ModifyObjectResponse(bool success, Logs logs)
            {
                Success = success;
                Logs = logs
                    .Select(log => log.ToString())
                    .ToArray();
            }
        }
    }
}
