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
            Name = "GameObject from Current Scene by Path",
            Route = "gameobject://currentScene/{path}",
            MimeType = Consts.MimeType.TextJson,
            ListResources = nameof(CurrentSceneAll),
            Description = "Get gameObject's components and the values of each explicit property."
        )]
        public ResponseResourceContent[] CurrentScene(string uri, string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new System.Exception("[Error] Path to the GameObject is empty.");

            return MainThread.Instance.Run(() =>
            {
                var go = GameObjectUtils.FindByPath(path)
                    ?? throw new System.Exception($"[Error] GameObject by path '{path}' not found.");

                var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

                return ResponseResourceContent.CreateText(
                    uri: uri,
                    mimeType: Consts.MimeType.TextJson,
                    text: reflector.Serialize(
                        obj: go,
                        fallbackType: typeof(GameObject),
                        recursive: false,
                        logger: UnityLoggerFactory.LoggerFactory.CreateLogger<Resource_GameObject>()
                    ).ToJson(reflector) ?? string.Empty
                ).MakeArray();
            });
        }

        public ResponseListResource[] CurrentSceneAll() => MainThread.Instance.Run(() =>
        {
            return EditorSceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => GameObjectUtils.GetAllRecursively(root))
                .Select(kvp => new ResponseListResource(
                    uri: $"gameobject://currentScene/{kvp.Key}",
                    name: kvp.Value.name,
                    enabled: true,
                    mimeType: Consts.MimeType.TextJson))
                .ToArray();
        });
    }
}