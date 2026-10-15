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
using System;
using com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters;
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Modules
{
    /// <summary>
    /// The Unity core: sources, asset functions, the adapters for UnityEngine core types and the type blacklist. Optional
    /// Unity packages (Particle System, Terrain, Splines …) contribute their own modules from separate assemblies that only
    /// compile when the package is present, so they appear and disappear with the package.
    /// </summary>
    public sealed class UnityCoreModule : IRqlModule
    {
        public int Order => 0;

        public void Configure(RqlModuleContext ctx)
        {
            ctx.AddSource("objects", RqlUnity.AllGameObjects, "every GameObject in the loaded scenes, depth-first, inactive ones included")
               .AddSource("roots", RqlUnity.RootGameObjects, "root GameObjects of the loaded scenes")
               .AddSource("scenes", RqlUnity.Scenes, "loaded scenes")
               .AddSource("selection", RqlUnity.SelectedObjects, "objects selected in the Editor")
               .AddSource("editor", () => RqlEditorApi.Instance,
                   "Editor state (isPlaying, isPaused, selection …) and operations: CreateGameObject, OpenScene, NewScene, SaveScenes, " +
                   "InstantiatePrefab, OpenPrefab / SavePrefab / ClosePrefab, CopyAsset / MoveAsset / DeleteAsset / CreateFolder, " +
                   "CreateMaterial, SaveAssets, RefreshAssets, ClearConsole, ExecuteMenuItem (rql-schema \"editor\" for signatures)")
               .AddSource("prefab", RqlEditorApi.OpenPrefabRoot, "root GameObject of the prefab open in prefab editing mode (none otherwise)")
               .AddSource("console", RqlEditorApi.ConsoleEntries, $"captured Editor log entries, oldest first (last {RqlEditorApi.MaxLogEntries}): logType message timestamp stackTrace sequence")
               .AddSource("shaders", RqlEditorApi.Shaders, "every shader the Editor knows (built-in and project): name supported hasErrors")
               .AddSource("componentTypes", RqlEditorApi.ComponentTypes, "component types that can be added (name fullName namespace)")
               .AddSource("tests", RqlTests.All,
                   "test methods (fullName name fixture namespace assembly mode categories parameterized explicit ignored); run a selection " +
                   "with tests-run query, e.g. tests | where fixture == \"PlayerTests\"")
               .AddSource("testResults", RqlTests.LastResults, "results of the last tests-run: name status durationMs message stackTrace")
               .AddSource("profiler", () => RqlProfiler.Instance,
                   "Unity profiler: enabled supported memory rendering script frame modules (live snapshots, MB / ms); calls Start, Stop, " +
                   "ClearFrames, EnableModule, Save, Load (rql-schema \"profiler\" for signatures)")
               .AddFunction("unity.assets", RqlUnity.FindAssets,
                   $"unity.assets(filter, folder?): AssetDatabase search, e.g. \"t:Material\", \"t:Prefab Enemy\" (at most {RqlUnity.MaxAssets}, loaded as needed)")
               .AddFunction("unity.find", RqlUnity.FindGameObject,
                   "unity.find(name): the GameObject with that unique name or hierarchy path (\"Level/Pillar_1\"), e.g. set Follow = unity.find(\"Player\")")
               .AddFunction("unity.asset", RqlUnity.LoadAsset, "unity.asset(path): the main asset at a path, e.g. \"Assets/Mats/Red.mat\"");

            // Transform enumerates its children, but RQL must treat it as an object with members.
            ctx.TreatAsObject(typeof(Transform));

            // Never expose native pointers / unmanaged views (crash or garbage when read), nor engine plumbing members by type:
            // cancellation tokens (MonoBehaviour.destroyCancellationToken) and TransformHandle (Unity 6.x, every component).
            ctx.ExcludeTypes(t => t == typeof(IntPtr) || t == typeof(UIntPtr) || t.IsPointer || t == typeof(System.Threading.CancellationToken)
                                  || t.FullName == "UnityEngine.TransformHandle"
                                  || (t.IsGenericType && (t.GetGenericTypeDefinition().FullName ?? "").StartsWith("Unity.Collections.Native", StringComparison.Ordinal)));

            // UnityEngine.Object family
            ctx.AddAdapter(new UnityEngine_Object_Adapter())
               .AddAdapter(new UnityEngine_GameObject_Adapter())
               .AddAdapter(new UnityEngine_Component_Adapter())
               .AddAdapter(new UnityEngine_MonoBehaviour_Adapter())
               .AddAdapter(new UnityEngine_Transform_Adapter())
               .AddAdapter(new UnityEngine_Renderer_Adapter())
               .AddAdapter(new UnityEngine_MeshFilter_Adapter());

            // Assets
            ctx.AddAdapter(new UnityEngine_Material_Adapter())
               .AddAdapter(new UnityEngine_Sprite_Adapter());

            // Structs
            ctx.AddAdapter(new UnityEngine_Vector2_Adapter())
               .AddAdapter(new UnityEngine_Vector3_Adapter())
               .AddAdapter(new UnityEngine_Vector4_Adapter())
               .AddAdapter(new UnityEngine_Vector2Int_Adapter())
               .AddAdapter(new UnityEngine_Vector3Int_Adapter())
               .AddAdapter(new UnityEngine_LayerMask_Adapter())
               .AddAdapter(new UnityEngine_BoundsInt_Adapter())
               .AddAdapter(new UnityEngine_Quaternion_Adapter())
               .AddAdapter(new UnityEngine_Color_Adapter())
               .AddAdapter(new UnityEngine_Gradient_Adapter())
               .AddAdapter(new UnityEngine_Scene_Adapter());
        }
    }
}
