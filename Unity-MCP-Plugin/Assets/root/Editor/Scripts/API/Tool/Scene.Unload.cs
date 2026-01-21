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
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Scene
    {
        public const string SceneUnloadToolId = "scene-unload";
        [McpPluginTool
        (
            SceneUnloadToolId,
            Title = "Scene / Unload"
        )]
        [Description("Unload scene from the Opened scenes in Unity Editor. " +
            "Use '" + SceneListOpenedToolId + "' tool to get the list of all opened scenes.")]
        public Task<UnloadSceneResult> Unload
        (
            [Description("Name of the loaded scene.")]
            string name
        )
        {
            return MainThread.Instance.Run(async () =>
            {
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException(Error.SceneNameIsEmpty(), nameof(name));

                var scene = SceneUtils.GetAllOpenedScenes()
                    .FirstOrDefault(scene => scene.name == name);

                if (!scene.IsValid())
                    throw new ArgumentException(Error.NotFoundSceneWithName(name), nameof(name));

                var asyncOperation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);

                while (!asyncOperation.isDone)
                    await Task.Yield();

                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);

                return new UnloadSceneResult
                {
                    Name = name,
                    AssetObjectRef = sceneAsset == null
                        ? null
                        : new AssetObjectRef(sceneAsset)
                };
            });
        }

        public class UnloadSceneResult
        {
            [Description("Name of the unloaded scene.")]
            public string? Name { get; set; }
            [Description("Reference to the unloaded scene asset.")]
            public AssetObjectRef? AssetObjectRef { get; set; } = null!;
        }
    }
}
