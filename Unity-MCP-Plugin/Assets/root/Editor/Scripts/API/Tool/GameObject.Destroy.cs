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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_GameObject
    {
        public const string GameObjectDestroyToolId = "gameobject-destroy";
        [McpPluginTool
        (
            GameObjectDestroyToolId,
            Title = "GameObject / Destroy"
        )]
        [Description("Destroy GameObject and all nested GameObjects recursively in opened Prefab or in a Scene. " +
            "Use '" + GameObjectFindToolId + "' tool to find the target GameObject first.")]
        public DestroyGameObjectResult Destroy(GameObjectRef gameObjectRef)
        {
            if (gameObjectRef == null)
                throw new ArgumentNullException(nameof(gameObjectRef), "No GameObject reference provided.");

            if (!gameObjectRef.IsValid(out var gameObjectValidationError))
                throw new ArgumentException(gameObjectValidationError, nameof(gameObjectRef));

            return MainThread.Instance.Run(() =>
            {
                var logger = UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_GameObject>();

                var go = gameObjectRef.FindGameObject(out var error);
                if (error != null)
                    throw new Exception(error);

                var destroyedName = go!.name;
                var destroyedPath = go.GetPath();
                var destroyedInstanceId = go.GetInstanceID();

                logger.LogInformation("Destroying GameObject '{Name}' (InstanceID: {InstanceId}) at path '{Path}'",
                    destroyedName, destroyedInstanceId, destroyedPath);

                UnityEngine.Object.DestroyImmediate(go);

                logger.LogInformation("Successfully destroyed GameObject '{Name}' (InstanceID: {InstanceId})",
                    destroyedName, destroyedInstanceId);

                EditorUtils.RepaintAllEditorWindows();

                return new DestroyGameObjectResult
                {
                    DestroyedName = destroyedName,
                    DestroyedPath = destroyedPath,
                    DestroyedInstanceId = destroyedInstanceId
                };
            });
        }

        public class DestroyGameObjectResult
        {
            [Description("Name of the destroyed GameObject.")]
            public string? DestroyedName { get; set; }

            [Description("Hierarchy path of the destroyed GameObject.")]
            public string? DestroyedPath { get; set; }

            [Description("Instance ID of the destroyed GameObject.")]
            public int DestroyedInstanceId { get; set; }
        }
    }
}
