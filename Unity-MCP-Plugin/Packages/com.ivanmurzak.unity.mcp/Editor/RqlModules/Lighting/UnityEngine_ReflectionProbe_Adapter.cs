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
    /// ReflectionProbe: <c>Bake()</c> does what the Inspector's Bake button does. A Baked or Custom probe is rendered into a
    /// texture asset in the scene's lighting folder ("Assets/Scenes/Level/ReflectionProbe-Mirror.exr"), replacing the
    /// texture of its previous bake there; a Realtime probe re-renders.
    /// </summary>
    public sealed class UnityEngine_ReflectionProbe_Adapter : RqlTypeAdapter<ReflectionProbe>
    {
        public override string Description =>
            "ReflectionProbe: call Bake() to render it now (Baked / Custom: into a texture next to the saved scene; Realtime: re-render)";

        protected override void Members(MemberTable<ReflectionProbe> m) => m
            .Function<string>("Bake", p => Bake(p),
                "render this probe now; returns the baked texture's asset path (\"\" for a Realtime probe, which re-renders instead)");

        /// <summary>Bake one probe; returns the texture's asset path, or "" for a Realtime probe.</summary>
        public static string Bake(ReflectionProbe probe)
        {
            LightingScenes.RequireEditMode("baking a reflection probe");
            if (probe.mode == ReflectionProbeMode.Realtime)
            {
                probe.RenderProbe();
                return "";
            }

            var folder = LightingScenes.Folder(probe.gameObject.scene)
                ?? throw new InvalidOperationException(
                    $"reflection probe '{probe.name}' is in an unsaved scene: a baked probe is stored next to its scene. " + LightingScenes.SaveSceneHint);
            // The folder first: GenerateUniqueAssetPath returns "" for a folder that does not exist yet.
            RqlEditorApi.Instance.CreateFolder(folder);
            var path = TexturePath(probe, folder);
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException($"no asset path for reflection probe '{probe.name}' in '{folder}'");
            if (!Lightmapping.BakeReflectionProbe(probe, path))
                throw new InvalidOperationException($"Unity could not bake reflection probe '{probe.name}' to '{path}'");

            // Show the new texture now, as the Inspector's Bake does; the next Bake() of this probe overwrites the same file.
            var texture = AssetDatabase.LoadAssetAtPath<Texture>(path);
            if (probe.mode == ReflectionProbeMode.Custom) probe.customBakedTexture = texture;
            else probe.bakedTexture = texture;
            return path;
        }

        // The probe's previous bake in this scene's folder is overwritten (the Inspector does the same); otherwise a new,
        // unique file. HDR probes bake to EXR, the others to PNG.
        private static string TexturePath(ReflectionProbe probe, string folder)
        {
            var extension = probe.hdr ? ".exr" : ".png";
            var previous = probe.mode == ReflectionProbeMode.Custom ? probe.customBakedTexture : probe.bakedTexture;
            var previousPath = previous != null ? AssetDatabase.GetAssetPath(previous) : null;
            if (!string.IsNullOrEmpty(previousPath)
                && previousPath!.StartsWith(folder + "/", StringComparison.Ordinal)
                && previousPath.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                return previousPath;
            return AssetDatabase.GenerateUniqueAssetPath($"{folder}/ReflectionProbe-{LightingScenes.SafeFileName(probe.name)}{extension}");
        }
    }
}
