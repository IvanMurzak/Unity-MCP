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
using Rql.Execution;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Lighting
{
    /// <summary>
    /// ReflectionProbe: <c>Bake()</c> renders a Custom probe into a texture asset in the scene's lighting folder
    /// ("Assets/Scenes/Level/ReflectionProbe-Mirror.exr"), replacing its previous bake there; a Realtime probe re-renders.
    /// A Baked probe's texture belongs to the scene's Lighting Data asset, which only the lighting bake writes (a texture
    /// assigned here would be lost on reload), so it is baked with <c>lighting | call Bake()</c>.
    /// </summary>
    public sealed class UnityEngine_ReflectionProbe_Adapter : RqlTypeAdapter<ReflectionProbe>
    {
        public override string Description =>
            "ReflectionProbe: call Bake() to render it now (Custom: into a texture next to the saved scene; Realtime: re-render; " +
            "Baked: bakes with lighting | call Bake())";

        protected override void Members(MemberTable<ReflectionProbe> m) => m
            .Function<string>("Bake", p => Bake(p),
                "render this Custom probe into a texture next to its scene and return the texture's asset path; a Realtime probe re-renders " +
                "(returns \"\"); a Baked probe bakes with the scene's lighting (lighting | call Bake())",
                p => RqlEditorApi.Rejection(() => // dry runs report a texture folder that cannot be created
                {
                    if (p.mode == ReflectionProbeMode.Custom && LightingScenes.Folder(p.gameObject.scene) is string folder)
                        RqlEditorApi.EnsureFolder(folder, create: false);
                }));

        /// <summary>Bake one Custom probe and return the texture's asset path, or re-render a Realtime one ("").</summary>
        public static string Bake(ReflectionProbe probe)
        {
            LightingScenes.RequireEditMode("baking a reflection probe");
            if (probe.mode == ReflectionProbeMode.Realtime)
            {
                probe.RenderProbe();
                return "";
            }
            if (probe.mode == ReflectionProbeMode.Baked)
                throw new InvalidOperationException(
                    $"reflection probe '{probe.name}' is Baked: its texture is part of the scene's lighting, so bake it with lighting | call Bake(), " +
                    "or set mode = \"Custom\" to bake it on its own");

            var folder = LightingScenes.Folder(probe.gameObject.scene)
                ?? throw new InvalidOperationException(
                    $"reflection probe '{probe.name}' is in an unsaved scene: a baked probe is stored next to its scene. " + LightingScenes.SaveSceneHint);
            // The folder first: GenerateUniqueAssetPath returns "" for a folder that does not exist yet. A folder created
            // here is removed again when the bake fails.
            var path = RqlEditorApi.InFolder(folder, () =>
            {
                var texture = TexturePath(probe, folder);
                if (string.IsNullOrEmpty(texture))
                    throw new InvalidOperationException($"no asset path for reflection probe '{probe.name}' in '{folder}'");
                return Lightmapping.BakeReflectionProbe(probe, texture)
                    ? texture
                    : throw new InvalidOperationException($"Unity could not bake reflection probe '{probe.name}' to '{texture}'");
            });

            // Show the new texture (serialized on the probe); the next Bake() of this probe overwrites the same file.
            probe.customBakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(path);
            return path;
        }

        // The probe's previous bake in this scene's folder is overwritten (the Inspector does the same); otherwise a new,
        // unique file. HDR probes bake to EXR, the others to PNG. Only a baked "ReflectionProbe-…" texture no other probe
        // shows is reused: a duplicated Custom probe shares its original's texture, and a hand-made cubemap is not a bake.
        private static string TexturePath(ReflectionProbe probe, string folder)
        {
            var extension = probe.hdr ? ".exr" : ".png";
            var previous = probe.customBakedTexture;
            var previousPath = previous != null ? AssetDatabase.GetAssetPath(previous) : null;
            if (!string.IsNullOrEmpty(previousPath)
                && previousPath!.StartsWith(folder + "/ReflectionProbe-", StringComparison.Ordinal)
                && previousPath.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
                && !ShownByAnotherProbe(probe, previous!))
                return previousPath;
            return AssetDatabase.GenerateUniqueAssetPath($"{folder}/ReflectionProbe-{LightingScenes.SafeFileName(probe.name)}{extension}");
        }

        private static bool ShownByAnotherProbe(ReflectionProbe probe, Texture texture)
        {
            foreach (var root in RqlUnity.RootGameObjects())
                foreach (var other in root.GetComponentsInChildren<ReflectionProbe>(true))
                    if (other != probe && (other.customBakedTexture == texture || other.bakedTexture == texture)) return true;
            return false;
        }
    }
}
