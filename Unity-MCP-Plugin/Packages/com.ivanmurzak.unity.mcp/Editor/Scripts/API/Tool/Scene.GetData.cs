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
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Scene
    {
        public const string SceneGetDataToolId = "scene-get-data";
        [McpPluginTool
        (
            SceneGetDataToolId,
            Title = "Scene / Get Data",
            ReadOnlyHint = true,
            IdempotentHint = true
        )]
        [Description("This tool retrieves the list of root GameObjects in the specified scene. " +
            "Use '" + SceneListOpenedToolId + "' tool to get the list of all opened scenes.\n\n" +
            "Path-scoped reads (token-saving): supply '" + "paths" + "' (a list of paths) to read only the listed " +
            "fields/elements from the scene's root-GameObjects array via Reflector.TryReadAt, or '" + "viewQuery" +
            "' (a ViewQuery) to navigate/filter the same array via Reflector.View. The result populates 'Data' on the " +
            "returned SceneData. These two parameters are mutually exclusive.\n" +
            "Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Leading '#/' is stripped. " +
            "Example: paths=['[0]/name'] reads the name of the first root GameObject.")]
        public SceneData GetData
        (
            [Description("Name of the opened scene. If empty or null, the active scene will be used.")]
            string? openedSceneName = null,
            [Description("If true, includes root GameObjects in the scene data.")]
            bool includeRootGameObjects = false,
            [Description("Determines the depth of the hierarchy to include.")]
            int includeChildrenDepth = 3,
            [Description("If true, includes bounding box information for GameObjects.")]
            bool includeBounds = false,
            [Description("If true, includes component data for GameObjects.")]
            bool includeData = false,
            [Description("Optional. List of paths to read individually via Reflector.TryReadAt against the scene's " +
                "root-GameObjects array. Path syntax: 'fieldName', '[i]/field', '[i]/component/[j]/property'. " +
                "Mutually exclusive with '" + "viewQuery" + "'.")]
            List<string>? paths = null,
            [Description("Optional. View-query filter routed through Reflector.View on the scene's root-GameObjects " +
                "array. Mutually exclusive with '" + "paths" + "'.")]
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
                var scene = string.IsNullOrEmpty(openedSceneName)
                    ? UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                    : UnityEngine.SceneManagement.SceneManager.GetSceneByName(openedSceneName);

                if (!scene.IsValid())
                    throw new ArgumentException(Error.NotFoundSceneWithName(openedSceneName));

                var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");
                var logger = UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_Scene>();

                var sceneData = new SceneData(
                    scene: scene,
                    reflector: reflector,
                    includeRootGameObjects: includeRootGameObjects,
                    includeChildrenDepth: includeChildrenDepth,
                    includeBounds: includeBounds,
                    includeData: includeData,
                    logger: logger
                );

                if (hasPaths || hasViewQuery)
                {
                    var rootGos = scene.GetRootGameObjects();
                    if (hasPaths)
                        sceneData.Data = PathReadHelper.BuildPathReadAggregate(
                            reflector, rootGos, scene.name, paths!, logger);
                    else
                        sceneData.Data = reflector.View(rootGos, viewQuery, logs: null, logger: logger)
                            ?? new SerializedMember { name = scene.name, typeName = rootGos.GetType().FullName ?? string.Empty };
                }

                return sceneData;
            });
        }
    }
}
