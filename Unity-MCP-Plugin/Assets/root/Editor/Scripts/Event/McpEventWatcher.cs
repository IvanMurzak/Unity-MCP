/*
┌──────────────────────────────────────────────────────────────────┐
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    /// <summary>
    /// Automatically collects built-in Unity Editor events and pushes them to McpEventBus.
    /// Lives in Editor folder — excluded from builds.
    /// </summary>
    [InitializeOnLoad]
    static class McpEventWatcher
    {
        static McpEventWatcher()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            Application.logMessageReceived += OnLogMessageReceived;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            McpEventBus.Push(
                type: "play_mode_changed",
                source: "EditorApplication",
                message: state.ToString(),
                payload: new Dictionary<string, object?>
                {
                    ["state"] = state.ToString(),
                    ["isPlaying"] = EditorApplication.isPlaying,
                    ["isPaused"] = EditorApplication.isPaused
                }
            );
        }

        static void OnPauseStateChanged(PauseState state)
        {
            McpEventBus.Push(
                type: "pause_state_changed",
                source: "EditorApplication",
                message: state == PauseState.Paused ? "Paused" : "Unpaused"
            );
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            McpEventBus.Push(
                type: "scene_loaded",
                source: scene.name,
                message: $"Scene '{scene.name}' loaded ({mode}).",
                payload: new Dictionary<string, object?>
                {
                    ["sceneName"] = scene.name,
                    ["scenePath"] = scene.path,
                    ["buildIndex"] = scene.buildIndex,
                    ["loadMode"] = mode.ToString()
                }
            );
        }

        static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            McpEventBus.Push(
                type: "scene_opened",
                source: scene.name,
                message: $"Scene '{scene.name}' opened in Editor ({mode}).",
                payload: new Dictionary<string, object?>
                {
                    ["sceneName"] = scene.name,
                    ["scenePath"] = scene.path,
                    ["openMode"] = mode.ToString()
                }
            );
        }

        static void OnCompilationStarted(object context)
        {
            McpEventBus.Push(
                type: "compilation_started",
                source: "CompilationPipeline",
                message: "Script compilation started."
            );
        }

        static void OnCompilationFinished(object context)
        {
            var hasErrors = EditorUtility.scriptCompilationFailed;
            McpEventBus.Push(
                type: "compilation_finished",
                source: "CompilationPipeline",
                message: hasErrors
                    ? "Script compilation finished with errors."
                    : "Script compilation finished successfully.",
                payload: new Dictionary<string, object?>
                {
                    ["hasErrors"] = hasErrors
                }
            );
        }

        static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type is LogType.Error or LogType.Exception)
            {
                McpEventBus.Push(
                    type: "error_logged",
                    source: "Console",
                    message: condition,
                    payload: new Dictionary<string, object?>
                    {
                        ["logType"] = type.ToString(),
                        ["stackTrace"] = stackTrace
                    }
                );
            }
            else if (type is LogType.Warning)
            {
                McpEventBus.Push(
                    type: "warning_logged",
                    source: "Console",
                    message: condition
                );
            }
        }

        static void OnHierarchyChanged()
        {
            McpEventBus.Push(
                type: "hierarchy_changed",
                source: "EditorApplication",
                message: "Scene hierarchy changed."
            );
        }

        static void OnSelectionChanged()
        {
            var selected = Selection.activeGameObject;
            McpEventBus.Push(
                type: "selection_changed",
                source: "Selection",
                message: selected != null ? $"Selected: {selected.name}" : "Selection cleared.",
                payload: new Dictionary<string, object?>
                {
                    ["selectedName"] = selected != null ? selected.name : null,
                    ["selectedCount"] = Selection.gameObjects.Length
                }
            );
        }
    }
}
