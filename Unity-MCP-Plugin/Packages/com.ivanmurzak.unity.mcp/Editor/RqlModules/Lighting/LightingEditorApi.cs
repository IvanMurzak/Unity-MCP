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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rql;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Lighting
{
    /// <summary>
    /// The <c>lighting</c> source: lighting state that has no owner component — the active scene's environment and Lighting
    /// Settings asset, the bake — e.g. <c>lighting | get environment | set fog = true</c>, <c>lighting | call Bake()</c>. Lights
    /// and probes are components: <c>objects | get components | where $ is Light</c>.
    /// </summary>
    public sealed class LightingEditorApi
    {
        public static readonly LightingEditorApi Instance = new LightingEditorApi();

        private LightingEditorApi() { }

        // ------------------------------------------------------------ state

        [RqlDescription("the active scene's environment: ambient, fog, skybox, sun, default reflections (writable, undoable)")]
        public LightingEnvironment environment => LightingEnvironment.Instance;

        [RqlDescription("the active scene's Lighting Settings asset (lightmapper, lightmapResolution, lightmapMaxSize, bakedGI, realtimeGI, " +
                        "mixedBakeMode, ao …; writable, saved with the asset); null until one is assigned (call CreateSettings)")]
        public LightingSettings? settings => Lightmapping.TryGetLightingSettings(out var s) ? s : null;

        [RqlDescription("a bake is running (started by Bake() or the Lighting window)")]
        public bool isBaking => Lightmapping.isRunning;

        [RqlDescription("progress of the running bake, 0 - 1 (0 when idle)")]
        public float bakeProgress => Lightmapping.isRunning ? Lightmapping.buildProgress : 0f;

        [RqlDescription("the active scene has baked lighting (a Lighting Data asset)")]
        public bool hasBakedData => Lightmapping.lightingDataAsset != null;

        [RqlDescription("lightmaps loaded for the open scenes")]
        public int lightmapCount => LightmapSettings.lightmaps.Length;

        // ------------------------------------------------------------ operations

        [RqlCallable("create a Lighting Settings asset with Unity's defaults at path (\"Assets/…/X.lighting\") and assign it to the active " +
                     "scene; returns it")]
        public LightingSettings CreateSettings(string path)
        {
            LightingScenes.RequireEditMode("assigning Lighting Settings");
            path = (path ?? "").Replace('\\', '/').Trim();
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !path.EndsWith(".lighting", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"'{path}' must be an asset path under Assets/ ending in .lighting, e.g. \"Assets/Lighting/Outdoor.lighting\"");
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new ArgumentException($"an asset already exists at '{path}'; assign it with: lighting | call UseSettings(\"{path}\")");

            var folder = Path.GetDirectoryName(path)!.Replace('\\', '/');
            RqlEditorApi.Instance.CreateFolder(folder);
            var created = new LightingSettings { name = Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(created, path);
            Assign(created);
            return created;
        }

        [RqlCallable("assign an existing Lighting Settings asset (path \"Assets/…/X.lighting\") to the active scene; returns it")]
        public LightingSettings UseSettings(LightingSettings asset)
        {
            if (asset == null) throw new ArgumentException("a Lighting Settings asset is required, e.g. \"Assets/Lighting/Outdoor.lighting\"");
            LightingScenes.RequireEditMode("assigning Lighting Settings");
            Assign(asset);
            return asset;
        }

        [RqlCallable("bake the lighting of the loaded scenes (lightmaps, light probes, baked reflection probes; the scenes must be saved). " +
                     "Only Baked / Mixed lights bake, and only into Contribute GI static geometry and light probes. " +
                     "By default it runs in the background: poll isBaking / bakeProgress. wait = true blocks the Editor until it is done " +
                     "(small scenes only). Returns false when a bake could not start (one is already running)")]
        [RqlNoAutoSave] // a bake writes its own assets; the call itself changes no scene state
        public bool Bake(bool wait = false)
        {
            LightingScenes.RequireSaved("baking");
            RequireBakeableDevice();
            if (Lightmapping.isRunning) return false;
            return wait ? Lightmapping.Bake() : Lightmapping.BakeAsync();
        }

        /// <summary>
        /// The GPU lightmapper crashes the Editor natively when there is no graphics device (-nographics). Unity also runs
        /// the GPU one when ProgressiveCPU is set but not supported (e.g. Apple silicon in Unity 2022.3): the settings asset
        /// then reads back ProgressiveGPU.
        /// </summary>
        private static void RequireBakeableDevice()
        {
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            if (EffectiveLightmapper() == LightingSettings.Lightmapper.ProgressiveCPU) return;
            throw new InvalidOperationException(
                "this Editor has no graphics device (-nographics), and the GPU lightmapper cannot bake without one. Set the CPU " +
                "lightmapper (lighting | get settings | set lightmapper = \"ProgressiveCPU\") or start the Editor with graphics");
        }

        private static LightingSettings.Lightmapper EffectiveLightmapper()
        {
            if (Lightmapping.TryGetLightingSettings(out var assigned)) return assigned.lightmapper;
            // No asset assigned: the bake uses Unity's defaults, which a new LightingSettings has.
            var defaults = new LightingSettings();
            try { return defaults.lightmapper; }
            finally { UnityEngine.Object.DestroyImmediate(defaults); }
        }

        [RqlCallable("stop the running bake (its partial results are discarded); returns whether one was running")]
        [RqlNoAutoSave]
        public bool CancelBake()
        {
            if (!Lightmapping.isRunning) return false;
            Lightmapping.Cancel();
            return true;
        }

        [RqlCallable("delete the baked lighting of the loaded scenes (lightmaps, light probe data, the Lighting Data asset), as the " +
                     "Lighting window's Clear Baked Data does; returns whether there was any")]
        public bool ClearBakedData()
        {
            LightingScenes.RequireEditMode("clearing baked data");
            if (Lightmapping.isRunning) Lightmapping.Cancel();
            var had = Lightmapping.lightingDataAsset != null || LightmapSettings.lightmaps.Length > 0;
            Lightmapping.Clear();
            Lightmapping.ClearLightingDataAsset();
            LightingScenes.MarkActiveSceneDirty();
            return had;
        }

        [RqlCallable("bake every active Custom reflection probe of the loaded scenes into a texture next to its scene (those scenes " +
                     "must be saved) and re-render the Realtime ones; returns how many. Baked probes bake with Bake()")]
        public int BakeReflectionProbes()
        {
            LightingScenes.RequireEditMode("baking reflection probes");
            var probes = ReflectionProbes().Where(p => p.isActiveAndEnabled && p.mode != ReflectionProbeMode.Baked).ToList();
            var custom = probes.Where(p => p.mode == ReflectionProbeMode.Custom).ToList();
            // Checked up front: calls are not rolled back, so a later unsaved scene must not leave half the probes baked.
            var unsaved = custom.FirstOrDefault(p => LightingScenes.Folder(p.gameObject.scene) == null);
            if (unsaved != null)
                throw new InvalidOperationException(
                    $"reflection probe '{unsaved.name}' is in an unsaved scene: a baked probe is stored next to its scene. " + LightingScenes.SaveSceneHint);
            // The call is recorded against this API object, so each Custom probe (its texture changes) is recorded here.
            if (custom.Count > 0) Undo.RecordObjects(custom.ToArray(), "RQL bake reflection probes");
            foreach (var probe in probes) UnityEngine_ReflectionProbe_Adapter.Bake(probe);
            foreach (var probe in custom)
            {
                EditorUtility.SetDirty(probe);
                EditorSceneManager.MarkSceneDirty(probe.gameObject.scene);
            }
            return probes.Count;
        }

        public override string ToString() => "lighting";

        // ------------------------------------------------------------ helpers

        private static void Assign(LightingSettings asset)
        {
            // The assignment lives on the scene's LightmapSettings object (one per loaded scene); recording them all makes it
            // part of the mutation's Undo step, like the environment writes.
            var owners = Resources.FindObjectsOfTypeAll<LightmapSettings>();
            if (owners.Length > 0) Undo.RecordObjects(owners, "RQL lighting settings");
            Lightmapping.lightingSettings = asset;
            LightingScenes.MarkActiveSceneDirty();
        }

        // Every reflection probe of the loaded scenes, inactive ones included: one native call per root (like the objects source).
        private static List<ReflectionProbe> ReflectionProbes() =>
            RqlUnity.RootGameObjects().SelectMany(r => r.GetComponentsInChildren<ReflectionProbe>(true)).ToList();
    }
}
