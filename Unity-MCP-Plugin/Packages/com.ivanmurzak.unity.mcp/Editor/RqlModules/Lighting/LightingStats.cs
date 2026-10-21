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
using Rql;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Lighting
{
    /// <summary>
    /// A snapshot of what the loaded scenes ask of the lighting (<c>lighting | select stats</c>): how many lights of each bake
    /// type, which of them cast real-time shadows, how much geometry contributes to global illumination, and the probes.
    /// It answers "why is my bake / frame slow" without reading every object.
    /// </summary>
    [RqlDescription("counts over the loaded scenes: lights per bake type, real-time shadow casters, GI contributors, probes, baked lightmaps")]
    public sealed class LightingStats
    {
        internal static LightingStats Capture()
        {
            var lights = LightingEditorApi.Lights();
            var renderers = LightingEditorApi.InLoadedScenes<MeshRenderer>();
            var probeGroups = LightingEditorApi.ProbeGroups();
            var reflectionProbes = LightingEditorApi.ReflectionProbes();
            return new LightingStats
            {
                lights = lights.Count,
                realtimeLights = lights.Count(l => l.lightmapBakeType == LightmapBakeType.Realtime),
                mixedLights = lights.Count(l => l.lightmapBakeType == LightmapBakeType.Mixed),
                bakedLights = lights.Count(l => l.lightmapBakeType == LightmapBakeType.Baked),
                realtimeShadowCasters = lights.Count(l => l.isActiveAndEnabled && l.shadows != LightShadows.None
                                                          && l.lightmapBakeType != LightmapBakeType.Baked),
                meshRenderers = renderers.Count,
                giContributors = renderers.Count(r => GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.ContributeGI)),
                lightProbeGroups = probeGroups.Count,
                lightProbes = probeGroups.Sum(g => g.probePositions.Length),
                reflectionProbes = reflectionProbes.Count,
                bakedReflectionProbes = reflectionProbes.Count(p => p.mode != ReflectionProbeMode.Realtime),
                lightmaps = LightmapSettings.lightmaps.Length,
            };
        }

        private LightingStats() { }

        [RqlDescription("Light components (inactive ones included)")]
        public int lights { get; private set; }

        [RqlDescription("lights with lightmapBakeType Realtime: lit every frame, no bake")]
        public int realtimeLights { get; private set; }

        [RqlDescription("lights with lightmapBakeType Mixed: baked indirect light, real-time direct light")]
        public int mixedLights { get; private set; }

        [RqlDescription("lights with lightmapBakeType Baked: free at run time, only affect static objects and light probes")]
        public int bakedLights { get; private set; }

        [RqlDescription("active Realtime / Mixed lights that cast shadows: each renders shadow maps every frame")]
        public int realtimeShadowCasters { get; private set; }

        public int meshRenderers { get; private set; }

        [RqlDescription("MeshRenderers whose GameObject is Contribute GI static: what a bake lightmaps (more = slower bakes)")]
        public int giContributors { get; private set; }

        public int lightProbeGroups { get; private set; }

        [RqlDescription("probes over every LightProbeGroup: they light dynamic objects from baked light")]
        public int lightProbes { get; private set; }

        public int reflectionProbes { get; private set; }

        [RqlDescription("reflection probes in Baked or Custom mode (rendered by a bake, not every frame)")]
        public int bakedReflectionProbes { get; private set; }

        [RqlDescription("lightmaps the last bake produced for the loaded scenes")]
        public int lightmaps { get; private set; }

        public override string ToString() =>
            $"{lights} lights ({realtimeLights} realtime, {mixedLights} mixed, {bakedLights} baked), {giContributors} GI contributors, {lightProbes} light probes";
    }
}
