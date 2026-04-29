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
        public const string GameObjectFindToolId = "gameobject-find";
        [McpPluginTool
        (
            GameObjectFindToolId,
            Title = "GameObject / Find",
            ReadOnlyHint = true,
            IdempotentHint = true
        )]
        [Description("Finds specific GameObject by provided information in opened Prefab or in a Scene. " +
            "First it looks for the opened Prefab, if any Prefab is opened it looks only there ignoring a scene. " +
            "If no opened Prefab it looks into current active scene. " +
            "Returns GameObject information and its children. " +
            "Also, it returns Components preview just for the target GameObject.\n\n" +
            "Path-scoped reads (token-saving): supply '" + "paths" + "' (a list of paths) to read only the listed " +
            "fields/elements via Reflector.TryReadAt, or '" + "viewQuery" + "' (a ViewQuery) to navigate to a " +
            "subtree and/or filter by name regex / max depth / type via Reflector.View. When either is supplied, " +
            "the result populates 'Data' on the returned GameObjectData and overrides 'includeData' (which would " +
            "otherwise produce a full recursive serialization). " +
            "These two parameters are mutually exclusive — supply at most one.\n" +
            "Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Leading '#/' is stripped.")]
        public GameObjectData? Find
        (
            GameObjectRef gameObjectRef,
            [Description("Include editable GameObject data (tag, layer, etc).")]
            bool includeData = false,
            [Description("Include attached components references.")]
            bool includeComponents = false,
            [Description("Include 3D bounds of the GameObject.")]
            bool includeBounds = false,
            [Description("Include hierarchy metadata.")]
            bool includeHierarchy = false,
            [Description("Determines the depth of the hierarchy to include. 0 - means only the target GameObject. 1 - means to include one layer below.")]
            int hierarchyDepth = 0,
            [Description("Optional. List of paths to read individually via Reflector.TryReadAt. " +
                "When supplied, replaces 'includeData'-style full serialization with a path-scoped aggregate. " +
                "Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. " +
                "Mutually exclusive with '" + "viewQuery" + "'.")]
            List<string>? paths = null,
            [Description("Optional. View-query filter routed through Reflector.View. " +
                "When supplied, replaces 'includeData'-style full serialization with the filtered subtree. " +
                "Mutually exclusive with '" + "paths" + "'.")]
            ViewQuery? viewQuery = null
        )
        {
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
                    return null;

                var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");
                var logger = UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_GameObject>();

                // When path/view-query is supplied, suppress the legacy 'includeData' full serialization
                // (the path-scoped result will replace it on the returned GameObjectData).
                var includeFullData = includeData && !hasPaths && !hasViewQuery;

                var data = go.ToGameObjectData(
                    reflector: reflector,
                    includeData: includeFullData,
                    includeComponents: includeComponents,
                    includeBounds: includeBounds,
                    includeHierarchy: includeHierarchy,
                    hierarchyDepth: hierarchyDepth,
                    logger: logger
                );

                if (hasPaths)
                    data.Data = PathReadHelper.BuildPathReadAggregate(reflector, go, go.name, paths!, logger);
                else if (hasViewQuery)
                    data.Data = reflector.View(go, viewQuery, logs: null, logger: logger)
                        ?? new SerializedMember { name = go.name, typeName = go.GetType().FullName ?? string.Empty };

                return data;
            });
        }
    }
}
