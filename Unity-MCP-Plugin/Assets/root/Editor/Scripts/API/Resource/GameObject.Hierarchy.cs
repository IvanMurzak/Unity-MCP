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
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    using Consts = McpPlugin.Common.Consts;

    [McpPluginResourceType]
    public partial class Resource_GameObject
    {
        [McpPluginResource
        (
            Route = "gameObject://currentScene/{path}",
            MimeType = Consts.MimeType.TextJson,
            ListResources = nameof(CurrentSceneAll),
            Name = "gameobject-current-scene",
            Description = "Get gameObject's components and the values of each explicit property."
        )]
        public ResponseResourceContent[] CurrentScene(string uri, string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new System.Exception("[Error] Path to the GameObject is empty.");

            return MainThread.Instance.Run(() =>
            {
                var go = GameObject.Find(path);
                if (go == null)
                    throw new System.Exception($"[Error] GameObject '{path}' not found.");

                var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

                return ResponseResourceContent.CreateText(
                    uri,
                    reflector.Serialize(
                        go,
                        logger: UnityLoggerFactory.LoggerFactory.CreateLogger<Resource_GameObject>()
                    ).ToJson(reflector),
                    Consts.MimeType.TextJson
                ).MakeArray();
            });
        }

        public ResponseListResource[] CurrentSceneAll() => MainThread.Instance.Run(()
            => EditorSceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => GameObjectUtils.GetAllRecursively(root))
                .Select(kvp => new ResponseListResource(
                    uri: $"gameObject://currentScene/{kvp.Key}",
                    name: kvp.Value.name,
                    enabled: true,
                    mimeType: Consts.MimeType.TextJson))
                .ToArray());
    }
}