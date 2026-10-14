/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Linq;
using Rql.Execution;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters
{
    /// <summary>Scene (struct): a whitelist of safe members, its root GameObjects, and Save / SetActive / Close.</summary>
    public sealed class UnityEngine_Scene_Adapter : RqlTypeAdapter<Scene>
    {
        protected override void Members(MemberTable<Scene> m)
        {
            // A Scene is a handle: a copy refers to the same loaded scene, so calls on items are real (writes-through).
            m.Untyped.Only("name", "path", "buildIndex", "isLoaded", "isDirty", "rootCount").WritesThrough();
            m.Add("roots", s => s.GetRootGameObjects().ToList(), description: "root GameObjects", owning: true)
             .Function("Save", s => EditorSceneManager.SaveScene(s), "save the scene to its asset (false when it failed)")
             .Function<string, bool>("SaveAs", (s, path) => EditorSceneManager.SaveScene(s, path), "path", "save the scene to a new asset path")
             .Function("SetActive", s => SceneManager.SetActiveScene(s), "make it the active scene (new GameObjects go there)")
             .Function("Close", s => EditorSceneManager.CloseScene(s, removeScene: true), "close the scene (one scene must stay open; unsaved changes are lost)");
        }
    }
}
