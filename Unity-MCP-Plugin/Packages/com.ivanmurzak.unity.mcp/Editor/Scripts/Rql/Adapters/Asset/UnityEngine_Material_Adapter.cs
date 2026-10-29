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
using System.Globalization;
using Rql.Execution;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters
{
    /// <summary>
    /// Material: safe members plus every shader property as a typed, validated map: <c>properties["_Color"]</c>; color and
    /// mainTexture for the shader's main color / texture. Material values also take a built-in material's name:
    /// <c>set sharedMaterial = "Default-Material"</c> (the built-in materials are not project assets, so no path reaches them).
    /// </summary>
    public sealed class UnityEngine_Material_Adapter : RqlTypeAdapter<Material>
    {
        public override string Description => "Material: shader properties via properties[\"_Name\"] (colors, vectors, floats, textures, ints), " +
                                               "the main color / texture as color / mainTexture; a Material Variant: set parent = unity.asset(\"….mat\"); " +
                                               $"write as a ref, an asset path, or a built-in material's name ({BuiltinNames})";

        /// <summary>The material a new primitive gets (the render pipeline's default), by this name.</summary>
        internal const string DefaultMaterialName = "Default-Material";

        // Built-in extra resources that exist in every supported Editor (unity_builtin_extra). Default-Material is resolved
        // through a primitive instead, so that it is the active render pipeline's default material.
        internal static readonly string[] Builtins =
        {
            DefaultMaterialName, "Default-Line", "Default-Particle", "Default-ParticleSystem", "Default-Skybox",
            "Default-Terrain-Standard", "Default-Terrain-Diffuse", "Sprites-Default", "Sprites-Mask",
        };
        private static readonly string BuiltinNames = string.Join(", ", Builtins);

        private static Material? _defaultMaterial;
        private static Object? _defaultMaterialPipeline;

        public override AdapterOutcome TryCoerce(object? value, Type target, RqlKernel kernel, out object? result, out string? reason)
        {
            result = null;
            reason = null;
            if (!(value is string text) || UnityEngine_Object_Adapter.TryAssetPath(text, out _))
                return AdapterOutcome.NotHandled; // refs and asset paths: the Object adapter
            var name = Array.Find(Builtins, n => string.Equals(n, text.Trim(), StringComparison.OrdinalIgnoreCase));
            result = name == null ? null : BuiltinMaterial(name);
            if (result != null) return AdapterOutcome.Ok;
            reason = name != null
                ? $"the built-in material '{name}' is not available in this Editor"
                : $"'{text}' is neither an asset path nor a built-in material ({BuiltinNames})";
            return AdapterOutcome.Failed;
        }

        /// <summary>
        /// A Material is a value (RQL spec §4.5): a typed literal builds a new one with the render pipeline's default shader,
        /// e.g. <c>set sharedMaterial = {$type: "Material", color: "#ff0000"}</c>; written into a scene object it is saved with
        /// the scene. Into an asset or the open prefab it is refused: they would lose it (RqlUnity.ValidateBatch).
        /// </summary>
        public override LiteralKind? LiteralKindOf(Type type) => type == typeof(Material) ? LiteralKind.Value : (LiteralKind?)null;

        public override object? CreateForLiteral(Type type, object? outer) =>
            type == typeof(Material) ? RqlUnity.TrackLiteral(new Material(RqlEditorApi.DefaultShader())) : null;

        /// <summary>A built-in material by its name; <see cref="DefaultMaterialName"/> is the one GameObject > 3D Object gives.</summary>
        internal static Material? BuiltinMaterial(string name)
        {
            if (name != DefaultMaterialName) return AssetDatabase.GetBuiltinExtraResource<Material>(name + ".mat");
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (_defaultMaterial != null && ReferenceEquals(_defaultMaterialPipeline, pipeline)) return _defaultMaterial;
            // Read without creating anything: coercion also runs for dry runs, and a probe primitive would dirty the scene.
#pragma warning disable CS0618 // defaultMaterial is what CreatePrimitive assigns under a render pipeline
            _defaultMaterial = pipeline != null ? pipeline.defaultMaterial : null;
#pragma warning restore CS0618
            if (_defaultMaterial == null) _defaultMaterial = AssetDatabase.GetBuiltinExtraResource<Material>(DefaultMaterialName + ".mat");
            _defaultMaterialPipeline = pipeline;
            return _defaultMaterial;
        }

        protected override void Members(MemberTable<Material> m)
        {
            m.Untyped.Only("name", "assetPath", "shader", "renderQueue", "enableInstancing", "doubleSidedGI", "globalIlluminationFlags", "passCount");
            m.Untyped.AddMixedMap("properties", o => Keys((Material)o), (o, k) => EntryType((Material)o, k),
                (o, k) => Get((Material)o, k), (o, k, v) => Set((Material)o, k, v), (o, k, v) => Validate((Material)o, k, v),
                "shader properties by name", has: (o, k) => ((Material)o).HasProperty(k));
            m.Add<Material?>("parent", o => o.parent, (o, v) => o.parent = v, (o, v) => ValidateParent(o, v),
                    "the material this one is a Material Variant of (it takes the parent's shader; properties set on it become overrides); null = a plain material")
             .Add<bool>("isVariant", o => o.isVariant, description: "true when this material is a Material Variant (has a parent)")
             .Add<Color?>("color", o => MainColor(o) is { } p ? o.GetColor(p) : (Color?)null,
                 (o, v) => o.SetColor(MainColor(o) ?? throw new InvalidOperationException(NoMain(o, "color")), v!.Value),
                 (o, v) => v == null ? "color cannot be null"
                     : MainColor(o) == null ? NoMain(o, "color") : null,
                 "the shader's main color ([MainColor], else _Color; _BaseColor in URP / HDRP); null when the shader has none")
             .Add<Texture?>("mainTexture", o => MainTexture(o) is { } p ? o.GetTexture(p) : null,
                 (o, v) => o.SetTexture(MainTexture(o) ?? throw new InvalidOperationException(NoMain(o, "texture")), v),
                 (o, v) => MainTexture(o) == null ? NoMain(o, "texture") : null,
                 "the shader's main texture ([MainTexture], else _MainTex; _BaseMap in URP / HDRP); null when the shader has none");
        }

        private static string? MainColor(Material m) => MainProperty(m, ShaderPropertyFlags.MainColor, "_Color", ShaderPropertyType.Color);
        private static string? MainTexture(Material m) => MainProperty(m, ShaderPropertyFlags.MainTexture, "_MainTex", ShaderPropertyType.Texture);

        // The property Material.color / mainTexture use, found without Unity's "Material doesn't have a color property" error log.
        internal static string? MainProperty(Material m, ShaderPropertyFlags flag, string fallback, ShaderPropertyType kind)
        {
            var shader = m.shader;
            if (shader == null) return null;
            int n = shader.GetPropertyCount();
            for (int i = 0; i < n; i++)
                if ((shader.GetPropertyFlags(i) & flag) != 0) return shader.GetPropertyName(i);
            int at = shader.FindPropertyIndex(fallback);
            return at >= 0 && shader.GetPropertyType(at) == kind ? fallback : null;
        }

        private static string NoMain(Material m, string what) =>
            $"shader '{(m.shader != null ? m.shader.name : "none")}' has no main {what}: set its property by name, properties[\"_Name\"]";

        // Unity ignores a parent that would make a cycle (it logs an error and keeps the old one): refused before any write.
        internal static string? ValidateParent(Material material, Material? parent)
        {
            if (parent == material) return "a material cannot be its own parent";
            for (var p = parent; p != null; p = p.parent)
                if (p == material)
                    return $"'{parent!.name}' is already a variant of '{material.name}' (directly or through its parents): a cycle";
            return null;
        }

        private static IEnumerable<string> Keys(Material m)
        {
            var shader = m.shader;
            if (shader == null) yield break;
            int n = shader.GetPropertyCount();
            for (int i = 0; i < n; i++) yield return shader.GetPropertyName(i);
        }

        private static ShaderPropertyType? Kind(Material m, string key)
        {
            var shader = m.shader;
            if (shader == null) return null;
            int i = shader.FindPropertyIndex(key);
            return i < 0 ? (ShaderPropertyType?)null : shader.GetPropertyType(i);
        }

        private static Type EntryType(Material m, string key) => Kind(m, key) switch
        {
            ShaderPropertyType.Color => typeof(Color),
            ShaderPropertyType.Vector => typeof(Vector4),
            ShaderPropertyType.Float => typeof(float),
            ShaderPropertyType.Range => typeof(float),
            ShaderPropertyType.Texture => typeof(Texture),
            ShaderPropertyType.Int => typeof(int),
            _ => typeof(object),
        };

        // The int property ids skip the per-call name hashing of the string overloads.
        private static object? Get(Material m, string key)
        {
            var kind = Kind(m, key);
            if (kind == null) return null;
            int id = Shader.PropertyToID(key);
            return kind switch
            {
                ShaderPropertyType.Color => m.GetColor(id),
                ShaderPropertyType.Vector => m.GetVector(id),
                ShaderPropertyType.Float => m.GetFloat(id),
                ShaderPropertyType.Range => m.GetFloat(id),
                ShaderPropertyType.Texture => m.GetTexture(id),
                ShaderPropertyType.Int => m.GetInteger(id),
                _ => null,
            };
        }

        private static void Set(Material m, string key, object? value)
        {
            int id = Shader.PropertyToID(key);
            switch (Kind(m, key))
            {
                case ShaderPropertyType.Color: m.SetColor(id, (Color)value!); break;
                case ShaderPropertyType.Vector: m.SetVector(id, (Vector4)value!); break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range: m.SetFloat(id, (float)value!); break;
                case ShaderPropertyType.Texture: m.SetTexture(id, (Texture?)value); break;
                case ShaderPropertyType.Int: m.SetInteger(id, (int)value!); break;
                default: throw new InvalidOperationException($"shader property '{key}' has an unsupported type");
            }
        }

        private static string? Validate(Material m, string key, object? value)
        {
            if (Kind(m, key) != ShaderPropertyType.Range || !(value is float f)) return null;
            var limits = m.shader.GetPropertyRangeLimits(m.shader.FindPropertyIndex(key));
            return f < limits.x || f > limits.y
                ? $"{key} must be within [{limits.x.ToString(CultureInfo.InvariantCulture)}, {limits.y.ToString(CultureInfo.InvariantCulture)}]"
                : null;
        }
    }
}
