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
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using AIGD;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Scene
    {
        public const string SceneSetActiveToolId = "scene-set-active";
        [McpPluginTool
        (
            SceneSetActiveToolId,
            Title = "Scene / Set Active",
            IdempotentHint = true
        )]
        [McpPluginSkillDescription("Mark an opened scene as the Editor's active scene (the one new GameObjects are " +
            "added to and that's used as the default for many operations). " +
            "Use '" + SceneListOpenedToolId + "' to enumerate opened scenes first.")]
        [McpPluginSkillBody("Set the specified opened scene as the active scene. " +
            "Use '" + SceneListOpenedToolId + "' tool to get the list of all opened scenes.\n\n" +
            "## Inputs\n\n" +
            "- `sceneRef` — `AssetObjectRef` pointing at a `SceneAsset`. The scene must already be opened.\n\n" +
            "## Behavior\n\n" +
            "Resolves the `SceneAsset`, finds the matching opened scene (by name then by path), and calls " +
            "`EditorSceneManager.SetActiveScene`. No-op if the scene is already active. Returns the post-call " +
            "snapshot of opened scenes.")]
        [Description("Set the specified opened scene as the active scene. " +
            "Use '" + SceneListOpenedToolId + "' tool to get the list of all opened scenes.")]
        public SceneDataShallow[] SetActive(AssetObjectRef sceneRef)
        {
            return MainThread.Instance.Run(() =>
            {
                var sceneAsset = sceneRef.FindAssetObject<UnityEditor.SceneAsset>()
                    ?? throw new System.ArgumentException($"Requested scene is not valid or not found.");

                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneAsset.name);
                if (scene.IsValid() == false)
                {
                    var scenePath = sceneAsset.GetAssetPath();
                    if (string.IsNullOrEmpty(scenePath))
                        throw new System.Exception(Error.ScenePathIsEmpty());

                    scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
                    if (scene.IsValid() == false)
                        throw new System.Exception($"Scene at '{scenePath}' is not opened.");
                }

                // If the scene is already active, just return opened scenes
                if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene() == scene)
                {
                    return OpenedScenes
                        .Select(scene => scene.ToSceneDataShallow())
                        .ToArray();
                }

                var success = UnityEditor.SceneManagement.EditorSceneManager.SetActiveScene(scene);
                if (!success)
                    throw new System.Exception($"Failed to set active scene to '{scene.name}'.");

                EditorUtils.RepaintAllEditorWindows();

                return OpenedScenes
                    .Select(scene => scene.ToSceneDataShallow())
                    .ToArray();
            });
        }
    }
}
