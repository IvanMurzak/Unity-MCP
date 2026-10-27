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
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    /// <summary>
    /// The <c>settings</c> source: Project Settings pages as view objects, e.g. <c>settings | select time.fixedDeltaTime</c>,
    /// <c>settings | set physics.gravity = "(0, -12, 0)"</c>. Each page is an <see cref="IRqlUndoOwners"/> naming its
    /// ProjectSettings/*.asset object: a write is recorded for Undo, dirtied and saved with the assets, like an edit in the
    /// Project Settings window. Pages of optional engine modules (physics) are added by their RQL module's adapter.
    /// </summary>
    [RqlDescription("Project Settings pages (time, quality, player; physics with the Physics module): writable, undoable, saved to ProjectSettings/")]
    public sealed class RqlProjectSettings
    {
        public static readonly RqlProjectSettings Instance = new RqlProjectSettings();

        private RqlProjectSettings() { }

        [RqlDescription("Project Settings > Time")]
        public RqlTimeSettings time => RqlTimeSettings.Instance;

        [RqlDescription("Project Settings > Quality (the values of the current level)")]
        public RqlQualitySettings quality => RqlQualitySettings.Instance;

        [RqlDescription("Project Settings > Player")]
        public RqlPlayerSettings player => RqlPlayerSettings.Instance;

        public override string ToString() => "project settings";

        /// <summary>
        /// The object stored in <c>ProjectSettings/&lt;file&gt;.asset</c> (e.g. "TimeManager"), the target of the page's Undo
        /// record and dirty flag; an empty list when the Editor has none (a stripped module).
        /// </summary>
        public static IReadOnlyList<Object> Asset(string file)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath($"ProjectSettings/{file}.asset");
            return all.Length > 0 && all[0] != null ? new[] { all[0] } : Array.Empty<Object>();
        }

        /// <summary>Checked before the assignment: a rejected value changes nothing (the engine rolls back earlier writes of the set).</summary>
        public static void Require<T>(T value, bool ok, string range)
        {
            if (!ok) throw new ArgumentOutOfRangeException(nameof(value), value, "must be " + range);
        }
    }

    [RqlDescription("Project Settings > Time: fixedDeltaTime (physics step), maximumDeltaTime, timeScale, maximumParticleDeltaTime")]
    public sealed class RqlTimeSettings : IRqlUndoOwners
    {
        internal static readonly RqlTimeSettings Instance = new RqlTimeSettings();

        private RqlTimeSettings() { }

        [RqlDescription("Fixed Timestep: seconds between physics / FixedUpdate steps (> 0, e.g. 0.02 = 50 Hz)")]
        public float fixedDeltaTime
        {
            get => Time.fixedDeltaTime;
            set { RqlProjectSettings.Require(value, value > 0f, "> 0"); Time.fixedDeltaTime = value; }
        }

        [RqlDescription("Maximum Allowed Timestep: the longest frame physics and FixedUpdate catch up on, seconds (> 0)")]
        public float maximumDeltaTime
        {
            get => Time.maximumDeltaTime;
            set { RqlProjectSettings.Require(value, value > 0f, "> 0"); Time.maximumDeltaTime = value; }
        }

        [RqlDescription("Time Scale: speed of game time (1 = real time, 0 = paused; 0 - 100)")]
        public float timeScale
        {
            get => Time.timeScale;
            set { RqlProjectSettings.Require(value, value >= 0f && value <= 100f, "between 0 and 100"); Time.timeScale = value; }
        }

        [RqlDescription("Maximum Particle Timestep, seconds (> 0)")]
        public float maximumParticleDeltaTime
        {
            get => Time.maximumParticleDeltaTime;
            set { RqlProjectSettings.Require(value, value > 0f, "> 0"); Time.maximumParticleDeltaTime = value; }
        }

        public override string ToString() => $"time (fixedDeltaTime {fixedDeltaTime})";

        IReadOnlyList<Object> IRqlUndoOwners.UndoOwners => RqlProjectSettings.Asset("TimeManager");
    }

    [RqlDescription("Project Settings > Quality: levels, currentLevel and the current level's vSyncCount, antiAliasing, shadows, lodBias …")]
    public sealed class RqlQualitySettings : IRqlUndoOwners
    {
        internal static readonly RqlQualitySettings Instance = new RqlQualitySettings();

        private RqlQualitySettings() { }

        [RqlDescription("the quality level names, by index")]
        public string[] levels => QualitySettings.names;

        [RqlDescription("index of the level the Editor uses (the values below are that level's); assign an index of levels")]
        public int currentLevel
        {
            get => QualitySettings.GetQualityLevel();
            set
            {
                RqlProjectSettings.Require(value, value >= 0 && value < QualitySettings.names.Length, $"a level index 0..{QualitySettings.names.Length - 1} ({string.Join(", ", QualitySettings.names)})");
                QualitySettings.SetQualityLevel(value, applyExpensiveChanges: true);
            }
        }

        [RqlDescription("V Sync Count: 0 = off, 1 = every V Blank, 2 = every second V Blank")]
        public int vSyncCount
        {
            get => QualitySettings.vSyncCount;
            set { RqlProjectSettings.Require(value, value >= 0 && value <= 4, "between 0 and 4"); QualitySettings.vSyncCount = value; }
        }

        [RqlDescription("MSAA samples: 0 (off), 2, 4 or 8")]
        public int antiAliasing
        {
            get => QualitySettings.antiAliasing;
            set { RqlProjectSettings.Require(value, value == 0 || value == 2 || value == 4 || value == 8, "0, 2, 4 or 8"); QualitySettings.antiAliasing = value; }
        }

        [RqlDescription("Shadows: Disable, HardOnly or All")]
        public ShadowQuality shadows { get => QualitySettings.shadows; set => QualitySettings.shadows = value; }

        [RqlDescription("Shadow Resolution: Low, Medium, High or VeryHigh")]
        public ShadowResolution shadowResolution { get => QualitySettings.shadowResolution; set => QualitySettings.shadowResolution = value; }

        [RqlDescription("Shadow Distance from the camera, meters (>= 0)")]
        public float shadowDistance
        {
            get => QualitySettings.shadowDistance;
            set { RqlProjectSettings.Require(value, value >= 0f, ">= 0"); QualitySettings.shadowDistance = value; }
        }

        [RqlDescription("Shadow Cascades: 1, 2 or 4")]
        public int shadowCascades
        {
            get => QualitySettings.shadowCascades;
            set { RqlProjectSettings.Require(value, value == 1 || value == 2 || value == 4, "1, 2 or 4"); QualitySettings.shadowCascades = value; }
        }

        [RqlDescription("Pixel Light Count: per-pixel lights per object in Forward rendering (>= 0)")]
        public int pixelLightCount
        {
            get => QualitySettings.pixelLightCount;
            set { RqlProjectSettings.Require(value, value >= 0, ">= 0"); QualitySettings.pixelLightCount = value; }
        }

        [RqlDescription("LOD Bias: > 1 keeps detailed LODs longer (> 0)")]
        public float lodBias
        {
            get => QualitySettings.lodBias;
            set { RqlProjectSettings.Require(value, value > 0f, "> 0"); QualitySettings.lodBias = value; }
        }

        [RqlDescription("Maximum LOD Level: LODs below it are never used (>= 0)")]
        public int maximumLODLevel
        {
            get => QualitySettings.maximumLODLevel;
            set { RqlProjectSettings.Require(value, value >= 0, ">= 0"); QualitySettings.maximumLODLevel = value; }
        }

        [RqlDescription("Global Mipmap Limit: 0 = full resolution textures, 1 = half, 2 = quarter, 3 = eighth")]
        public int globalTextureMipmapLimit
        {
            get => QualitySettings.globalTextureMipmapLimit;
            set { RqlProjectSettings.Require(value, value >= 0 && value <= 3, "between 0 and 3"); QualitySettings.globalTextureMipmapLimit = value; }
        }

        [RqlDescription("Anisotropic Textures: Disable, Enable or ForceEnable")]
        public AnisotropicFiltering anisotropicFiltering { get => QualitySettings.anisotropicFiltering; set => QualitySettings.anisotropicFiltering = value; }

        [RqlDescription("Soft Particles")]
        public bool softParticles { get => QualitySettings.softParticles; set => QualitySettings.softParticles = value; }

        [RqlDescription("Realtime Reflection Probes")]
        public bool realtimeReflectionProbes { get => QualitySettings.realtimeReflectionProbes; set => QualitySettings.realtimeReflectionProbes = value; }

        public override string ToString() => $"quality ({(currentLevel < levels.Length ? levels[currentLevel] : currentLevel.ToString())})";

        IReadOnlyList<Object> IRqlUndoOwners.UndoOwners => RqlProjectSettings.Asset("QualitySettings");
    }

    [RqlDescription("Project Settings > Player: companyName, productName, bundleVersion, colorSpace, default screen, runInBackground …")]
    public sealed class RqlPlayerSettings : IRqlUndoOwners
    {
        internal static readonly RqlPlayerSettings Instance = new RqlPlayerSettings();

        private RqlPlayerSettings() { }

        [RqlDescription("Company Name (not empty)")]
        public string companyName
        {
            get => PlayerSettings.companyName;
            set { RqlProjectSettings.Require(value, !string.IsNullOrWhiteSpace(value), "a non-empty name"); PlayerSettings.companyName = value; }
        }

        [RqlDescription("Product Name: the application's name (not empty)")]
        public string productName
        {
            get => PlayerSettings.productName;
            set { RqlProjectSettings.Require(value, !string.IsNullOrWhiteSpace(value), "a non-empty name"); PlayerSettings.productName = value; }
        }

        [RqlDescription("Version, e.g. \"1.2.0\" (not empty)")]
        public string bundleVersion
        {
            get => PlayerSettings.bundleVersion;
            set { RqlProjectSettings.Require(value, !string.IsNullOrWhiteSpace(value), "a non-empty version"); PlayerSettings.bundleVersion = value; }
        }

        [RqlDescription("Color Space: Gamma or Linear (changing it reimports textures)")]
        public ColorSpace colorSpace
        {
            get => PlayerSettings.colorSpace;
            set { RqlProjectSettings.Require(value, value == ColorSpace.Gamma || value == ColorSpace.Linear, "Gamma or Linear"); PlayerSettings.colorSpace = value; }
        }

        [RqlDescription("Run In Background: the player keeps running without focus")]
        public bool runInBackground { get => PlayerSettings.runInBackground; set => PlayerSettings.runInBackground = value; }

        [RqlDescription("Fullscreen Mode of standalone players: ExclusiveFullScreen, FullScreenWindow, MaximizedWindow or Windowed")]
        public FullScreenMode fullScreenMode { get => PlayerSettings.fullScreenMode; set => PlayerSettings.fullScreenMode = value; }

        [RqlDescription("Default Screen Width of a windowed standalone player, pixels (> 0)")]
        public int defaultScreenWidth
        {
            get => PlayerSettings.defaultScreenWidth;
            set { RqlProjectSettings.Require(value, value > 0, "> 0"); PlayerSettings.defaultScreenWidth = value; }
        }

        [RqlDescription("Default Screen Height of a windowed standalone player, pixels (> 0)")]
        public int defaultScreenHeight
        {
            get => PlayerSettings.defaultScreenHeight;
            set { RqlProjectSettings.Require(value, value > 0, "> 0"); PlayerSettings.defaultScreenHeight = value; }
        }

        public override string ToString() => $"player ({productName})";

        IReadOnlyList<Object> IRqlUndoOwners.UndoOwners => RqlProjectSettings.Asset("ProjectSettings");
    }
}
