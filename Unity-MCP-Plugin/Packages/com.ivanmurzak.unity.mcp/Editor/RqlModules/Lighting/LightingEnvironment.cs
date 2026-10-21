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
using System.Reflection;
using Rql;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Lighting
{
    /// <summary>
    /// The active scene's environment lighting (Window > Rendering > Lighting > Environment), <c>lighting | get environment</c>.
    /// RenderSettings is static, so this handle reads and writes it; each write is recorded for Undo on the scene's
    /// RenderSettings object and marks the scene modified, like an edit in the Lighting window.
    /// </summary>
    [RqlDescription("the active scene's environment lighting: ambient, fog, skybox, sun, default reflections (writable, undoable)")]
    public sealed class LightingEnvironment
    {
        internal static readonly LightingEnvironment Instance = new LightingEnvironment();

        private LightingEnvironment() { }

        // ------------------------------------------------------------ ambient

        [RqlDescription("where ambient light comes from: Skybox, Trilight (sky / equator / ground colors) or Flat (ambientLight)")]
        public AmbientMode ambientMode { get => RenderSettings.ambientMode; set => Write(() => RenderSettings.ambientMode = value); }

        [RqlDescription("ambient color in Flat mode (same as ambientSkyColor)")]
        public Color ambientLight { get => RenderSettings.ambientLight; set => Write(() => RenderSettings.ambientLight = value); }

        [RqlDescription("ambient color from above (Trilight)")]
        public Color ambientSkyColor { get => RenderSettings.ambientSkyColor; set => Write(() => RenderSettings.ambientSkyColor = value); }

        [RqlDescription("ambient color from the sides (Trilight)")]
        public Color ambientEquatorColor { get => RenderSettings.ambientEquatorColor; set => Write(() => RenderSettings.ambientEquatorColor = value); }

        [RqlDescription("ambient color from below (Trilight)")]
        public Color ambientGroundColor { get => RenderSettings.ambientGroundColor; set => Write(() => RenderSettings.ambientGroundColor = value); }

        [RqlDescription("ambient light multiplier in Skybox mode (0 - 8)")]
        public float ambientIntensity
        {
            get => RenderSettings.ambientIntensity;
            set { Require(value, value >= 0f && value <= 8f, "between 0 and 8"); Write(() => RenderSettings.ambientIntensity = value); }
        }

        [RqlDescription("color of real-time shadows in Subtractive mixed lighting")]
        public Color subtractiveShadowColor { get => RenderSettings.subtractiveShadowColor; set => Write(() => RenderSettings.subtractiveShadowColor = value); }

        // ------------------------------------------------------------ sky and sun

        [RqlDescription("skybox material (an asset path works, e.g. \"Assets/Sky/Dusk.mat\"); null = none")]
        public Material? skybox { get => RenderSettings.skybox; set => Write(() => RenderSettings.skybox = value); }

        [RqlDescription("the directional light that drives the procedural skybox (a GameObject with a Light works, e.g. unity.find(\"Sun\"))")]
        public Light? sun { get => RenderSettings.sun; set => Write(() => RenderSettings.sun = value); }

        // ------------------------------------------------------------ fog

        [RqlDescription("fog on")]
        public bool fog { get => RenderSettings.fog; set => Write(() => RenderSettings.fog = value); }

        [RqlDescription("Linear (fogStartDistance - fogEndDistance), Exponential or ExponentialSquared (fogDensity)")]
        public FogMode fogMode { get => RenderSettings.fogMode; set => Write(() => RenderSettings.fogMode = value); }

        public Color fogColor { get => RenderSettings.fogColor; set => Write(() => RenderSettings.fogColor = value); }

        [RqlDescription("density of Exponential fog (0 - 1)")]
        public float fogDensity
        {
            get => RenderSettings.fogDensity;
            set { Require(value, value >= 0f && value <= 1f, "between 0 and 1"); Write(() => RenderSettings.fogDensity = value); }
        }

        [RqlDescription("where Linear fog starts, meters (>= 0)")]
        public float fogStartDistance
        {
            get => RenderSettings.fogStartDistance;
            set { Require(value, value >= 0f, ">= 0"); Write(() => RenderSettings.fogStartDistance = value); }
        }

        [RqlDescription("where Linear fog is complete, meters (>= 0)")]
        public float fogEndDistance
        {
            get => RenderSettings.fogEndDistance;
            set { Require(value, value >= 0f, ">= 0"); Write(() => RenderSettings.fogEndDistance = value); }
        }

        // ------------------------------------------------------------ reflections

        [RqlDescription("source of the default reflection: Skybox or Custom")]
        public DefaultReflectionMode defaultReflectionMode
        {
            get => RenderSettings.defaultReflectionMode;
            set => Write(() => RenderSettings.defaultReflectionMode = value);
        }

        [RqlDescription("resolution of the default reflection cubemap (16 - 2048, a power of two)")]
        public int defaultReflectionResolution
        {
            get => RenderSettings.defaultReflectionResolution;
            set { Require(value, value >= 16 && value <= 2048 && (value & (value - 1)) == 0, "a power of two between 16 and 2048"); Write(() => RenderSettings.defaultReflectionResolution = value); }
        }

        [RqlDescription("how strongly the skybox / custom reflection shows on objects (0 - 1)")]
        public float reflectionIntensity
        {
            get => RenderSettings.reflectionIntensity;
            set { Require(value, value >= 0f && value <= 1f, "between 0 and 1"); Write(() => RenderSettings.reflectionIntensity = value); }
        }

        [RqlDescription("how many times reflections bounce between reflection probes (1 - 5)")]
        public int reflectionBounces
        {
            get => RenderSettings.reflectionBounces;
            set { Require(value, value >= 1 && value <= 5, "between 1 and 5"); Write(() => RenderSettings.reflectionBounces = value); }
        }

        public override string ToString() => $"environment ({ambientMode} ambient{(fog ? ", fog" : "")})";

        // ------------------------------------------------------------ writes

        private static void Write(Action assign)
        {
            var settings = SceneRenderSettings();
            if (settings != null) Undo.RecordObject(settings, "RQL lighting environment");
            assign();
            if (settings != null) EditorUtility.SetDirty(settings);
            LightingScenes.MarkActiveSceneDirty();
        }

        // The active scene's RenderSettings object, the one the Lighting window edits. Unity exposes the values only as static
        // properties, and the object only through an internal accessor; without it the writes still work, just without Undo.
        private static readonly MethodInfo? GetRenderSettings =
            typeof(RenderSettings).GetMethod("GetRenderSettings", BindingFlags.Static | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

        internal static Object? SceneRenderSettings() => GetRenderSettings?.Invoke(null, null) as Object;

        // Validated before Write, so a rejected value records nothing for Undo.
        private static void Require<T>(T value, bool ok, string range)
        {
            if (!ok) throw new ArgumentOutOfRangeException(nameof(value), value, "must be " + range);
        }
    }
}
