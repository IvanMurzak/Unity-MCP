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
using System.Collections.Generic;
using System.Linq;
using Rql;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Navigation
{
    using NavMesh = UnityEngine.AI.NavMesh;
    using NavMeshSurface = global::Unity.AI.Navigation.NavMeshSurface;

    /// <summary>
    /// The <c>navigation</c> source: project-wide navigation state that has no owner component — agent types (bake
    /// settings), areas — and operations over every surface, e.g. <c>navigation | call BakeAll()</c>.
    /// </summary>
    public sealed class NavigationApi
    {
        public static readonly NavigationApi Instance = new NavigationApi();

        [RqlDescription("agent types with their bake settings (agentRadius, agentHeight, agentSlope, agentClimb …; writable)")]
        public List<NavMeshAgentType> agentTypes =>
            Enumerable.Range(0, NavMesh.GetSettingsCount()).Select(i => new NavMeshAgentType(NavMesh.GetSettingsByIndex(i).agentTypeID)).ToList();

        [RqlDescription("the 32 NavMesh areas (index, name, cost; name and cost writable); unused slots have an empty name")]
        public List<NavMeshArea> areas => Enumerable.Range(0, NavMeshNames.AreaCount).Select(i => new NavMeshArea(i)).ToList();

        [RqlCallable("bake every NavMeshSurface in the loaded scenes (active ones; no 20-target limit); returns how many were baked")]
        public int BakeAll() => ForEachSurface(Unity_AI_Navigation_NavMeshSurface_Adapter.Bake, "RQL bake NavMesh", activeOnly: true);

        [RqlCallable("clear the baked NavMesh of every NavMeshSurface in the loaded scenes; returns how many were cleared")]
        public int ClearAll() => ForEachSurface(Unity_AI_Navigation_NavMeshSurface_Adapter.Clear, "RQL clear NavMesh", activeOnly: false);

        // The call is recorded against this API object, so the surfaces are recorded for undo and marked dirty here.
        private static int ForEachSurface(System.Action<NavMeshSurface> action, string undoName, bool activeOnly)
        {
            var surfaces = RqlUnity.RootGameObjects().SelectMany(r => r.GetComponentsInChildren<NavMeshSurface>(true))
                .Where(s => s != null && (!activeOnly || s.isActiveAndEnabled)).ToList();
            foreach (var s in surfaces)
            {
                Undo.RecordObject(s, undoName);
                action(s);
                EditorUtility.SetDirty(s);
                if (!EditorApplication.isPlaying) EditorSceneManager.MarkSceneDirty(s.gameObject.scene);
            }
            return surfaces.Count;
        }
    }
}
