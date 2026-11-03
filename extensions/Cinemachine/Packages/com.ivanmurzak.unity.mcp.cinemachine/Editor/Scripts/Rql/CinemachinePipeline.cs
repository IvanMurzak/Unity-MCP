/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Cinemachine)    │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Cinemachine
{
    using CameraPipelineAttribute = global::Unity.Cinemachine.CameraPipelineAttribute;
    using CinemachineBasicMultiChannelPerlin = global::Unity.Cinemachine.CinemachineBasicMultiChannelPerlin;
    using CinemachineCamera = global::Unity.Cinemachine.CinemachineCamera;
    using CinemachineComponentBase = global::Unity.Cinemachine.CinemachineComponentBase;
    using CinemachineExtension = global::Unity.Cinemachine.CinemachineExtension;
    using NoiseSettings = global::Unity.Cinemachine.NoiseSettings;
    using Stage = global::Unity.Cinemachine.CinemachineCore.Stage;

    /// <summary>
    /// The CinemachineCamera pipeline operations shared by the camera adapter and the <c>cinemachine</c> source: the
    /// component types of each stage (Body / Aim / Noise), replacing the component of a stage, noise profiles by name and
    /// targets given as a GameObject name or reference. Every change goes through Undo.
    /// </summary>
    public static class CinemachinePipeline
    {
        public const string None = "None";
        private const string Prefix = "Cinemachine";

        // Stage -> its component types; TypeCache is fixed per domain, so the attribute scan runs once per stage.
        private static readonly Dictionary<Stage, Type[]> StageTypeCache = new Dictionary<Stage, Type[]>();

        /// <summary>The current (non-deprecated) component types of a pipeline stage, by short name (Follow, RotationComposer …).</summary>
        public static List<Type> StageTypes(Stage stage)
        {
            if (!StageTypeCache.TryGetValue(stage, out var types))
                StageTypeCache[stage] = types = TypeCache.GetTypesWithAttribute<CameraPipelineAttribute>()
                    .Where(t => !t.IsAbstract && typeof(CinemachineComponentBase).IsAssignableFrom(t)
                                && !t.IsDefined(typeof(ObsoleteAttribute), false)
                                && t.GetCustomAttribute<CameraPipelineAttribute>()!.Stage == stage)
                    .OrderBy(t => t.Name, StringComparer.Ordinal)
                    .ToArray();
            return types.ToList();
        }

        public static List<string> StageTypeNames(Stage stage) => StageTypes(stage).Select(ShortName).ToList();

        /// <summary>The current CinemachineExtension types (Deoccluder, Confiner3D …), added with <c>call AddComponent(name)</c>.</summary>
        public static List<string> ExtensionTypeNames() =>
            TypeCache.GetTypesDerivedFrom<CinemachineExtension>()
                .Where(t => !t.IsAbstract && !t.IsDefined(typeof(ObsoleteAttribute), false) && t.IsPublic)
                .Select(t => t.Name)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

        public static string ShortName(Type t) =>
            t.Name.StartsWith(Prefix, StringComparison.Ordinal) && t.Name.Length > Prefix.Length ? t.Name.Substring(Prefix.Length) : t.Name;

        /// <summary>The stage component type named <paramref name="name"/> ("Follow" or "CinemachineFollow"); null for "None".</summary>
        public static Type? ResolveStageType(Stage stage, string? name)
        {
            if (string.IsNullOrWhiteSpace(name) || string.Equals(name, None, StringComparison.OrdinalIgnoreCase)) return null;
            var types = StageTypes(stage);
            return types.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)
                                             || string.Equals(ShortName(t), name, StringComparison.OrdinalIgnoreCase)
                                             || string.Equals(t.FullName, name, StringComparison.OrdinalIgnoreCase))
                   ?? throw new ArgumentException($"no {stage} component named '{name}'; use one of: {None}, {string.Join(", ", types.Select(ShortName))}");
        }

        /// <summary>The camera's component of a stage (the first one), or null.</summary>
        public static CinemachineComponentBase? Component(CinemachineCamera camera, Stage stage) =>
            camera.GetComponents<CinemachineComponentBase>().FirstOrDefault(c => c != null && c.Stage == stage);

        /// <summary>
        /// Make <paramref name="typeName"/> the camera's component of <paramref name="stage"/>: other components of that stage are
        /// removed, an existing component of the requested type is kept with its settings. "None" removes the stage. Undoable.
        /// </summary>
        public static CinemachineComponentBase? Replace(CinemachineCamera camera, Stage stage, string? typeName)
        {
            var type = ResolveStageType(stage, typeName);
            CinemachineComponentBase? kept = null;
            foreach (var c in camera.GetComponents<CinemachineComponentBase>())
            {
                if (c == null || c.Stage != stage) continue;
                if (type != null && kept == null && c.GetType() == type) { kept = c; continue; }
                Undo.DestroyObjectImmediate(c);
            }
            if (type == null || kept != null) return kept;
            return (CinemachineComponentBase)Undo.AddComponent(camera.gameObject, type);
        }

        /// <summary>
        /// Procedural noise: a CinemachineBasicMultiChannelPerlin with the NoiseSettings profile named <paramref name="profile"/>
        /// (a preset such as "6D Shake" / "Handheld_normal_mild" or a project asset; name or asset path) and the gains. "None" removes it.
        /// </summary>
        public static CinemachineBasicMultiChannelPerlin? SetNoise(CinemachineCamera camera, string? profile, float amplitudeGain, float frequencyGain)
        {
            if (string.IsNullOrWhiteSpace(profile) || string.Equals(profile, None, StringComparison.OrdinalIgnoreCase))
            {
                Replace(camera, Stage.Noise, None);
                return null;
            }
            var settings = FindNoiseProfile(profile!);
            var perlin = (CinemachineBasicMultiChannelPerlin)Replace(camera, Stage.Noise, nameof(CinemachineBasicMultiChannelPerlin))!;
            Undo.RecordObject(perlin, "RQL set noise");
            perlin.NoiseProfile = settings;
            perlin.AmplitudeGain = amplitudeGain;
            perlin.FrequencyGain = frequencyGain;
            EditorUtility.SetDirty(perlin);
            return perlin;
        }

        /// <summary>Every NoiseSettings asset (the Cinemachine presets and the project's own), by name.</summary>
        public static List<NoiseSettings> NoiseProfiles() =>
            AssetDatabase.FindAssets("t:NoiseSettings")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .Select(AssetDatabase.LoadAssetAtPath<NoiseSettings>)
                .Where(n => n != null)
                .OrderBy(n => n.name, StringComparer.OrdinalIgnoreCase)
                .ToList();

        public static NoiseSettings FindNoiseProfile(string profile)
        {
            var byPath = AssetDatabase.LoadAssetAtPath<NoiseSettings>(profile);
            if (byPath != null) return byPath;
            var all = NoiseProfiles();
            return all.FirstOrDefault(n => string.Equals(n.name, profile, StringComparison.OrdinalIgnoreCase))
                   ?? throw new ArgumentException($"no NoiseSettings profile named '{profile}'; available: {string.Join(", ", all.Select(n => n.name))}");
        }

        /// <summary>
        /// A target given as a GameObject / component reference or as a GameObject name (unique in the loaded scenes);
        /// null or "" = no target.
        /// </summary>
        public static Transform? ResolveTarget(object? target, string parameter)
        {
            switch (target)
            {
                case null: return null;
                case Transform t: return t;
                case Component c: return c != null ? c.transform : throw new ArgumentException($"{parameter}: the object was destroyed");
                case GameObject g: return g != null ? g.transform : throw new ArgumentException($"{parameter}: the GameObject was destroyed");
                case string name when name.Length == 0: return null;
                case string name:
                    var found = RqlUnity.AllGameObjects().Where(g => g.name == name).ToList();
                    if (found.Count == 1) return found[0].transform;
                    throw new ArgumentException(found.Count == 0
                        ? $"{parameter}: no GameObject named '{name}' in the loaded scenes"
                        : $"{parameter}: {found.Count} GameObjects are named '{name}'; pass ref(\"GameObject#…\") instead");
                default:
                    throw new ArgumentException($"{parameter}: expected a GameObject name or reference, got {target.GetType().Name}");
            }
        }
    }
}
