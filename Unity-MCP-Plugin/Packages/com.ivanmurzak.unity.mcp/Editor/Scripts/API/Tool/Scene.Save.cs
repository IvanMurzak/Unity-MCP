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
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Scene
    {
        public const string SceneSaveToolId = "scene-save";
        [AiTool
        (
            SceneSaveToolId,
            Title = "Scene / Save",
            IdempotentHint = true
        )]
        [AiSkillDescription("Save an opened scene back to its asset file (or to a new path when `path` is " +
            "provided). When `openedSceneName` is empty, saves the currently active scene. " +
            "Use '" + SceneListOpenedToolId + "' to find the scene name first.")]
        [AiSkillBody("Save Opened scene to the asset file. " +
            "Use '" + SceneListOpenedToolId + "' tool to get the list of all opened scenes.\n\n" +
            "## Inputs\n\n" +
            "- `openedSceneName` (optional) — name of an opened scene to save. Empty/null = active scene.\n" +
            "- `path` (optional) — destination `.unity` path. Empty/null = save back to the scene's existing path.\n\n" +
            "## Validation\n\n" +
            "Throws if the scene cannot be resolved, has no existing path AND no override path was supplied, or the " +
            "supplied path does not end with `.unity`. On `EditorSceneManager.SaveScene` failure, surfaces an error " +
            "with the current opened-scenes list for diagnosis.")]
        [Description("Save Opened scene to the asset file. " +
            "Use '" + SceneListOpenedToolId + "' tool to get the list of all opened scenes.")]
        public void Save
        (
            [Description("Name of the opened scene that should be saved. Could be empty if need to save the current active scene.")]
            string? openedSceneName = null,
            [Description("Path to the scene file. Should end with \".unity\". If null or empty save to the existed scene asset file.")]
            string? path = null
        )
        {
            MainThread.Instance.Run(() =>
            {
                var scene = string.IsNullOrEmpty(openedSceneName)
                    ? SceneUtils.GetActiveScene()
                    : SceneUtils.GetAllOpenedScenes()
                        .FirstOrDefault(scene => scene.name == openedSceneName);

                if (!scene.IsValid())
                    throw new Exception(Error.NotFoundSceneWithName(openedSceneName));

                if (string.IsNullOrEmpty(path))
                    path = scene.path;

                if (string.IsNullOrEmpty(path))
                    throw new Exception($"Scene '{scene.name}' has no path. Please provide a path to save the scene.");

                if (!path!.EndsWith(".unity"))
                    throw new Exception(Error.FilePathMustEndsWithUnity());

                bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path);
                if (!saved)
                    throw new Exception($"Failed to save scene at '{path}'.\n{OpenedScenesText}");

                EditorUtils.RepaintAllEditorWindows();
            });
        }
    }
}
