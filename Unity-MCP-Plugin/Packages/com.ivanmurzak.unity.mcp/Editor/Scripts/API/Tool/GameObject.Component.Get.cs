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
#if UNITY_6000_5_OR_NEWER
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Unity.MCP.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_GameObject
    {
        public const string GameObjectComponentGetToolId = "gameobject-component-get";
        [McpPluginTool
        (
            GameObjectComponentGetToolId,
            Title = "GameObject / Component / Get",
            ReadOnlyHint = true,
            IdempotentHint = true
        )]
        [Description("Get detailed information about a specific Component on a GameObject. " +
            "Returns component type, enabled state, and optionally serialized fields and properties. " +
            "Use this to inspect component data before modifying it. " +
            "Use '" + GameObjectFindToolId + "' tool to get the list of all components on the GameObject.\n\n" +
            "Path-scoped reads (token-saving): supply '" + "paths" + "' (a list of paths) to read only the listed " +
            "fields/elements via Reflector.TryReadAt, or '" + "viewQuery" + "' (a ViewQuery) to navigate to a " +
            "subtree and/or filter by name regex / max depth / type via Reflector.View. The result is returned in the " +
            "'View' field of the response. These two parameters are mutually exclusive — supply at most one.\n" +
            "Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Leading '#/' is stripped.")]
        public GetComponentResponse GetComponent
        (
            GameObjectRef gameObjectRef,
            ComponentRef componentRef,
            [Description("Include serialized fields of the component.")]
            bool includeFields = true,
            [Description("Include serialized properties of the component.")]
            bool includeProperties = true,
            [Description("Performs deep serialization including all nested objects. Otherwise, only serializes top-level members.")]
            bool deepSerialization = false,
            [Description("Optional. List of paths to read individually via Reflector.TryReadAt. " +
                "When supplied, the legacy 'Fields'/'Properties' lists are skipped and the result is returned in 'View'. " +
                "Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. " +
                "Mutually exclusive with '" + "viewQuery" + "'.")]
            List<string>? paths = null,
            [Description("Optional. View-query filter routed through Reflector.View. " +
                "When supplied, the legacy 'Fields'/'Properties' lists are skipped and the filtered subtree is " +
                "returned in 'View'. Mutually exclusive with '" + "paths" + "'.")]
            ViewQuery? viewQuery = null
        )
        {
            if (gameObjectRef == null)
                throw new ArgumentNullException(nameof(gameObjectRef));

            if (componentRef == null)
                throw new ArgumentNullException(nameof(componentRef));

            if (!gameObjectRef.IsValid(out var gameObjectValidationError))
                throw new ArgumentException(gameObjectValidationError, nameof(gameObjectRef));

            if (!componentRef.IsValid(out var componentValidationError))
                throw new ArgumentException(componentValidationError, nameof(componentRef));

            var hasPaths = paths != null && paths.Count > 0;
            var hasViewQuery = viewQuery != null;
            if (hasPaths && hasViewQuery)
                throw new ArgumentException(
                    $"'{nameof(paths)}' and '{nameof(viewQuery)}' are mutually exclusive — supply at most one.");

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

                var response = new GetComponentResponse
                {
                    Reference = new ComponentRef(targetComponent),
                    Index = targetIndex,
                    Component = new ComponentDataShallow(targetComponent)
                };

                var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");
                var logger = UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_GameObject>();

                if (hasPaths)
                {
                    response.View = PathReadHelper.BuildPathReadAggregate(
                        reflector, targetComponent, targetComponent.GetType().GetTypeId(), paths!, logger);
                }
                else if (hasViewQuery)
                {
                    response.View = reflector.View(targetComponent, viewQuery, logs: null, logger: logger)
                        ?? new SerializedMember
                        {
                            name = targetComponent.GetType().GetTypeId(),
                            typeName = targetComponent.GetType().FullName ?? string.Empty
                        };
                }
                else if (includeFields || includeProperties)
                {
                    var serialized = reflector.Serialize(
                        obj: targetComponent,
                        name: targetComponent.GetType().GetTypeId(),
                        recursive: deepSerialization,
                        logger: logger
                    );

                    if (includeFields && serialized?.fields != null)
                    {
                        response.Fields = serialized.fields
                            .Where(f => f != null)
                            .ToList();
                    }

                    if (includeProperties && serialized?.props != null)
                    {
                        response.Properties = serialized.props
                            .Where(p => p != null)
                            .ToList();
                    }
                }

                return response;
            });
        }

        public class GetComponentResponse
        {
            [Description("Reference to the component for future operations.")]
            public ComponentRef? Reference { get; set; }

            [Description("Index of the component in the GameObject's component list.")]
            public int Index { get; set; }

            [Description("Basic component information (type, enabled state).")]
            public ComponentDataShallow? Component { get; set; }

            [Description("Serialized fields of the component. Populated only on the legacy code path " +
                "(no 'paths' / no 'viewQuery').")]
            public List<SerializedMember>? Fields { get; set; }

            [Description("Serialized properties of the component. Populated only on the legacy code path " +
                "(no 'paths' / no 'viewQuery').")]
            public List<SerializedMember>? Properties { get; set; }

            [Description("Path-scoped read or view-query result, populated when 'paths' or 'viewQuery' was supplied. " +
                "Null otherwise.")]
            public SerializedMember? View { get; set; }
        }
    }
}
#endif
