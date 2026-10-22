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
using Rql;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Lighting
{
    /// <summary>
    /// The active scene's environment lighting (Window > Rendering > Lighting > Environment), <c>lighting | get environment</c>.
    /// RenderSettings is static, so this handle reads and writes it; as an <see cref="IRqlUndoOwners"/> it names the RenderSettings
    /// objects, so a mutation records them for Undo and marks the scene modified, like an edit in the Lighting window.
    /// </summary>
    [RqlDescription("the active scene's environment lighting: ambient, fog, skybox, sun, default reflections (writable, undoable)")]
    public sealed class LightingEnvironment : IRqlUndoOwners
    {
        internal static readonly LightingEnvironment Instance = new LightingEnvironment();

        private LightingEnvironment() { }

        // ------------------------------------------------------------ ambient

        [RqlDescription("where ambient light comes from: Skybox, Trilight (sky / equator / ground colors) or Flat (ambientLight)")]
        public AmbientMode ambientMode { get => RenderSettings.ambientMode; set => RenderSettings.ambientMode = value; }

        [RqlDescription("ambient color in Flat mode (same as ambientSkyColor)")]
        public Color ambientLight { get => RenderSettings.ambientLight; set => RenderSettings.ambientLight = value; }

        [RqlDescription("ambient color from above (Trilight)")]
        public Color ambientSkyColor { get => RenderSettings.ambientSkyColor; set => RenderSettings.ambientSkyColor = value; }

        [RqlDescription("ambient color from the sides (Trilight)")]
        public Color ambientEquatorColor { get => RenderSettings.ambientEquatorColor; set => RenderSettings.ambientEquatorColor = value; }

        [RqlDescription("ambient color from below (Trilight)")]
        public Color ambientGroundColor { get => RenderSettings.ambientGroundColor; set => RenderSettings.ambientGroundColor = value; }

        [RqlDescription("ambient light multiplier in Skybox mode (0 - 8)")]
        public float ambientIntensity { get => RenderSettings.ambientIntensity; set { Require(value, value >= 0f && value <= 8f, "between 0 and 8"); RenderSettings.ambientIntensity = value; } }

        [RqlDescription("color of real-time shadows in Subtractive mixed lighting")]
        public Color subtractiveShadowColor { get => RenderSettings.subtractiveShadowColor; set => RenderSettings.subtractiveShadowColor = value; }

        // ------------------------------------------------------------ sky and sun

        [RqlDescription("skybox material (an asset path works, e.g. \"Assets/Sky/Dusk.mat\"); null = none")]
        public Material? skybox { get => RenderSettings.skybox; set => RenderSettings.skybox = value; }

        [RqlDescription("the directional light that drives the procedural skybox (a GameObject with a Light works, e.g. unity.find(\"Sun\"))")]
        public Light? sun { get => RenderSettings.sun; set => RenderSettings.sun = value; }

        // ------------------------------------------------------------ fog

        [RqlDescription("fog on")]
        public bool fog { get => RenderSettings.fog; set => RenderSettings.fog = value; }

        [RqlDescription("Linear (fogStartDistance - fogEndDistance), Exponential or ExponentialSquared (fogDensity)")]
        public FogMode fogMode { get => RenderSettings.fogMode; set => RenderSettings.fogMode = value; }

        public Color fogColor { get => RenderSettings.fogColor; set => RenderSettings.fogColor = value; }

        [RqlDescription("density of Exponential fog (0 - 1)")]
        public float fogDensity { get => RenderSettings.fogDensity; set { Require(value, value >= 0f && value <= 1f, "between 0 and 1"); RenderSettings.fogDensity = value; } }

        [RqlDescription("where Linear fog starts, meters (>= 0)")]
        public float fogStartDistance { get => RenderSettings.fogStartDistance; set { Require(value, value >= 0f, ">= 0"); RenderSettings.fogStartDistance = value; } }

        [RqlDescription("where Linear fog is complete, meters (>= 0)")]
        public float fogEndDistance { get => RenderSettings.fogEndDistance; set { Require(value, value >= 0f, ">= 0"); RenderSettings.fogEndDistance = value; } }

        // ------------------------------------------------------------ reflections

        [RqlDescription("source of the default reflection: Skybox or Custom")]
        public DefaultReflectionMode defaultReflectionMode
        {
            get => RenderSettings.defaultReflectionMode;
            set => RenderSettings.defaultReflectionMode = value;
        }

        [RqlDescription("resolution of the default reflection cubemap (16 - 2048, a power of two)")]
        public int defaultReflectionResolution
        {
            get => RenderSettings.defaultReflectionResolution;
            set { Require(value, value >= 16 && value <= 2048 && (value & (value - 1)) == 0, "a power of two between 16 and 2048"); RenderSettings.defaultReflectionResolution = value; }
        }

        [RqlDescription("how strongly the skybox / custom reflection shows on objects (0 - 1)")]
        public float reflectionIntensity { get => RenderSettings.reflectionIntensity; set { Require(value, value >= 0f && value <= 1f, "between 0 and 1"); RenderSettings.reflectionIntensity = value; } }

        [RqlDescription("how many times reflections bounce between reflection probes (1 - 5)")]
        public int reflectionBounces { get => RenderSettings.reflectionBounces; set { Require(value, value >= 1 && value <= 5, "between 1 and 5"); RenderSettings.reflectionBounces = value; } }

        public override string ToString() => $"environment ({ambientMode} ambient{(fog ? ", fog" : "")})";

        // The objects behind RenderSettings' static properties (one per loaded scene; the active scene's is the one written),
        // found publicly and all recorded, which also covers the active one.
        IReadOnlyList<UnityEngine.Object> IRqlUndoOwners.UndoOwners => Resources.FindObjectsOfTypeAll<RenderSettings>();

        // Checked before the assignment: a rejected value changes nothing (the engine rolls back the earlier ones of the set).
        private static void Require<T>(T value, bool ok, string range)
        {
            if (!ok) throw new ArgumentOutOfRangeException(nameof(value), value, "must be " + range);
        }
    }
}
