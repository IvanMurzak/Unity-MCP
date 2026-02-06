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
            Title = "Object / Modify"
        )]
        [Description("Modify the specified Unity Object. " +
            "Allows direct modification of object fields and properties. " +
            "Use '" + ObjectGetDataToolId + "' first to inspect the object structure before modifying.")]
        public ModifyObjectResponse Modify
        (
            ObjectRef objectRef,
            [Description("The object data to apply. Should contain '" + nameof(SerializedMember.fields) + "' and/or '" + nameof(SerializedMember.props) + "' with the values to modify.\n" +
                "Only include the fields/properties you want to change.\n" +
                "Any unknown or invalid fields and properties will be reported in the response.")]
            SerializedMember objectDiff
        )
        {
            if (objectRef == null)
                throw new ArgumentNullException(nameof(objectRef));

            if (!objectRef.IsValid(out var error))
                throw new ArgumentException(error, nameof(objectRef));

            if (objectDiff == null)
                throw new ArgumentNullException(nameof(objectDiff), "No object data provided to modify.");

            return MainThread.Instance.Run(() =>
            {
                var obj = objectRef.FindObject();
                if (obj == null)
                    throw new Exception($"Not found UnityEngine.Object with provided data for reference: {objectRef}.");

                var logs = new Logs();
                var objToModify = (object)obj;

                var success = McpPlugin.McpPlugin.Instance!.McpManager.Reflector.TryPopulate(
                    ref objToModify,
                    data: objectDiff,
                    logs: logs,
                    logger: UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_Object>());

                if (success)
                {
                    UnityEditor.EditorUtility.SetDirty(obj);
                }

                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                // Return updated object data
                var data = McpPlugin.McpPlugin.Instance!.McpManager.Reflector.Serialize(
                    obj,
                    name: obj.name,
                    recursive: true,
                    logger: UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_Object>()
                );

                return new ModifyObjectResponse(success, logs)
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
