/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Navigation)     │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.IO;
using System.Linq;
using Rql.Execution;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Navigation
{
    using NavMeshBuildSettings = UnityEngine.AI.NavMeshBuildSettings;
    using NavMeshData = UnityEngine.AI.NavMeshData;
    using NavMeshSurface = global::Unity.AI.Navigation.NavMeshSurface;

    /// <summary>
    /// NavMeshSurface: Bake() / Clear() in the Editor, agentType / defaultAreaName as names, the effective buildSettings
    /// (read-only), backing fields hidden behind their properties.
    /// </summary>
    public sealed class Unity_AI_Navigation_NavMeshSurface_Adapter : RqlTypeAdapter<NavMeshSurface>
    {
        public override string Description =>
            "NavMeshSurface: call Bake() / Clear(); agentType and defaultAreaName take names; buildSettings shows the effective bake settings";

        protected override void Members(MemberTable<NavMeshSurface> m)
        {
            NavigationMembers.AddAgentType(m, s => s.agentTypeID, (s, id) => s.agentTypeID = id);
            NavigationMembers.AddAreaName(m, "defaultAreaName", s => s.defaultArea, (s, i) => s.defaultArea = i, "defaultArea");
            m.Add<NavMeshBuildSettings>("buildSettings", s => s.GetBuildSettings(),
                    description: "effective bake settings: the agent type's (navigation | get agentTypes) plus this surface's overrides")
             .Method("Bake", s => Bake(s), "build the NavMesh now; in a saved scene the data is stored as an asset next to it (undoable reference)",
                 // dry runs report a folder in the way (Play Mode keeps the bake in memory: nothing is written)
                 s => EditorApplication.isPlaying ? null : RqlEditorApi.Rejection(() => RqlEditorApi.EnsureFolder(AssetFolder(s), create: false)))
             .Method("Clear", s => Clear(s), "remove the baked NavMesh (and the asset Bake() created for it)");
            NavigationMembers.HideBackingFields(m.Untyped);
        }

        /// <summary>
        /// Synchronous bake (the Inspector's Bake button is asynchronous). Like the Inspector, the result is saved as
        /// "&lt;scene folder&gt;/&lt;scene name&gt;/NavMesh-&lt;name&gt;.asset" when the scene has a path, replacing the asset of the previous bake.
        /// Unsaved scenes and Play Mode keep the data in memory.
        /// </summary>
        public static void Bake(NavMeshSurface surface)
        {
            var before = surface.navMeshData;
            var previous = OwnedAssetPath(surface);
            surface.BuildNavMesh();
            // A failed build (BuildNavMeshData returned null) keeps the previous data: its asset must not be deleted and
            // re-created from the deleted object.
            if (surface.navMeshData == null || surface.navMeshData == before || EditorApplication.isPlaying) return;

            var folder = AssetFolder(surface);
            if (folder == null) return;
            var invalid = Path.GetInvalidFileNameChars();
            var name = new string(surface.name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            // Missing folders are created, and removed again when the asset cannot be saved (the NavMesh then stays in memory,
            // which is why this is not SaveNewAsset: that destroys the object on failure).
            var path = RqlEditorApi.InFolder(folder, () =>
            {
                var unique = AssetDatabase.GenerateUniqueAssetPath($"{folder}/NavMesh-{name}.asset");
                AssetDatabase.CreateAsset(surface.navMeshData, unique); // throws for most bad paths, only logs for some
                return AssetDatabase.Contains(surface.navMeshData)
                    ? unique
                    : throw new System.InvalidOperationException($"could not save the NavMesh of '{surface.name}' at '{unique}' (it stays in memory)");
            });
            // The previous bake's asset goes only once the new one is saved, and the new one takes its name.
            if (previous == null) return;
            AssetDatabase.DeleteAsset(previous);
            AssetDatabase.MoveAsset(path, previous); // on failure (an error message) the new asset keeps its unique name
        }

        public static void Clear(NavMeshSurface surface)
        {
            var owned = OwnedAssetPath(surface);
            surface.RemoveData();
            surface.navMeshData = null;
            if (owned != null && !EditorApplication.isPlaying) AssetDatabase.DeleteAsset(owned);
        }

        // The scene's NavMesh folder, as the Inspector uses it ("Assets/Scenes/Level" for "Assets/Scenes/Level.unity").
        private static string? AssetFolder(NavMeshSurface surface)
        {
            var scenePath = surface.gameObject.scene.path;
            if (string.IsNullOrEmpty(scenePath)) return null;
            return (Path.GetDirectoryName(scenePath) + "/" + Path.GetFileNameWithoutExtension(scenePath)).Replace('\\', '/');
        }

        // The asset of an earlier bake of this surface (in the scene's NavMesh folder); other referenced NavMeshData is left alone.
        private static string? OwnedAssetPath(NavMeshSurface surface)
        {
            NavMeshData? data = surface.navMeshData;
            if (data == null) return null;
            var path = AssetDatabase.GetAssetPath(data);
            var folder = AssetFolder(surface);
            return !string.IsNullOrEmpty(path) && folder != null && path.StartsWith(folder + "/NavMesh-", System.StringComparison.Ordinal) ? path : null;
        }
    }
}
