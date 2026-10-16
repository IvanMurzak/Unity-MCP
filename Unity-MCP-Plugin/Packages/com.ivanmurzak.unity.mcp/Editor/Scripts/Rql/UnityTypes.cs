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
// UAC0005 (Unity 6.6 analyzer) suggests CurrentAssemblies APIs that older supported Unity versions lack; the scan only reads type metadata.
#pragma warning disable UAC0005
using System;
using System.Collections.Generic;
using System.Linq;
using Rql.Execution;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    /// <summary>
    /// The types an agent names (rql-schema "Light", AddComponent("Rigidbody")): UnityEngine.Object types from TypeCache,
    /// plus public UnityEngine structs and every public enum. One index per domain, built on first use; lookups are by
    /// short or full name, case-insensitive, best match first (exact case, then UnityEngine, then project types).
    /// </summary>
    public static class UnityTypes
    {
        private static Dictionary<string, Type[]>? _index;

        /// <summary>Candidate types for a name, best first.</summary>
        public static IReadOnlyList<Type> Candidates(string name) =>
            Index().TryGetValue(name, out var hits)
                ? hits.OrderBy(t => t.Name == name || t.FullName == name ? 0 : 1).ThenBy(t => IsUnityEngine(t) ? 0 : 1).ToList()
                : (IReadOnlyList<Type>)Array.Empty<Type>();

        /// <summary>The best concrete type for a name that derives from <paramref name="baseType"/>, or null.</summary>
        public static Type? Find(string name, Type baseType) =>
            Candidates(name).FirstOrDefault(t => baseType.IsAssignableFrom(t) && !t.IsAbstract);

        /// <summary>Every indexed short name (for "did you mean" suggestions).</summary>
        public static IReadOnlyCollection<string> ShortNames() => Index().Keys.Where(k => k.IndexOf('.') < 0).ToList();

        private static Dictionary<string, Type[]> Index()
        {
            if (_index != null) return _index;
            var byName = new Dictionary<string, List<Type>>(StringComparer.OrdinalIgnoreCase);
            void Add(Type t)
            {
                // IsVisible, not IsPublic: nested public types (ParticleSystem.MinMaxCurve, ParticleSystem.MainModule …) count too.
                if (!t.IsVisible || t.IsGenericTypeDefinition) return;
                foreach (var key in new[] { t.Name, t.FullName })
                {
                    if (key == null) continue;
                    if (!byName.TryGetValue(key, out var list)) byName[key] = list = new List<Type>(1);
                    if (!list.Contains(t)) list.Add(t);
                }
            }
            foreach (var t in TypeCache.GetTypesDerivedFrom<UnityEngine.Object>()) Add(t);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                foreach (var t in TypeModel.LoadableTypes(asm))
                    if (t.IsEnum || (t.IsValueType && IsUnityEngine(t))) Add(t);
            return _index = byName.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsUnityEngine(Type t) => t.Namespace?.StartsWith("UnityEngine", StringComparison.Ordinal) == true;
    }
}
