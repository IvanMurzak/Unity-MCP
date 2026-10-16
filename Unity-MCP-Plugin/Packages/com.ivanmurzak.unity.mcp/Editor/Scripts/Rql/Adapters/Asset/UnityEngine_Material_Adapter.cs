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
using UnityEngine;
using UnityEngine.Rendering;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters
{
    /// <summary>Material: safe members plus every shader property as a typed, validated map: <c>properties["_Color"]</c>.</summary>
    public sealed class UnityEngine_Material_Adapter : RqlTypeAdapter<Material>
    {
        public override string Description => "Material: shader properties via properties[\"_Name\"] (colors, vectors, floats, textures, ints)";

        protected override void Members(MemberTable<Material> m)
        {
            m.Untyped.Only("name", "shader", "renderQueue", "enableInstancing", "doubleSidedGI", "globalIlluminationFlags", "passCount");
            m.Untyped.AddMixedMap("properties", o => Keys((Material)o), (o, k) => EntryType((Material)o, k),
                (o, k) => Get((Material)o, k), (o, k, v) => Set((Material)o, k, v), (o, k, v) => Validate((Material)o, k, v),
                "shader properties by name", has: (o, k) => ((Material)o).HasProperty(k));
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
