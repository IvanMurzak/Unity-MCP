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
using Rql;
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

        private static Dictionary<string, Type[]>? _serializable;

        /// <summary>
        /// [Serializable] plain classes and structs of the project and packages (not UnityEngine.Object), e.g. the row type of a
        /// ScriptableObject's list field: what an agent meets through <c>get rows</c>. Kept apart from <see cref="Candidates"/>,
        /// which names creatable / addable types; rql-schema asks here when nothing else has the name. One index per domain.
        /// </summary>
        public static IReadOnlyList<Type> SerializableCandidates(string name)
        {
            if (_serializable == null)
            {
                var byName = new Dictionary<string, List<Type>>(StringComparer.OrdinalIgnoreCase);
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (IsFrameworkAssembly(asm.GetName().Name)) continue;
                    foreach (var t in TypeModel.LoadableTypes(asm))
                    {
                        if (!t.IsVisible || t.IsGenericTypeDefinition || t.IsEnum || t.IsInterface || !t.IsSerializable
                            || typeof(UnityEngine.Object).IsAssignableFrom(t) || typeof(Delegate).IsAssignableFrom(t)) continue;
                        AddByName(byName, t);
                    }
                }
                _serializable = Freeze(byName);
            }
            return _serializable.TryGetValue(name, out var hits)
                ? hits.OrderBy(t => t.Name == name || t.FullName == name ? 0 : 1).ToList()
                : (IReadOnlyList<Type>)Array.Empty<Type>();
        }

        private static void AddByName(Dictionary<string, List<Type>> byName, Type t)
        {
            foreach (var key in new[] { t.Name, t.FullName })
            {
                if (key == null) continue;
                if (!byName.TryGetValue(key, out var list)) byName[key] = list = new List<Type>(1);
                if (!list.Contains(t)) list.Add(t);
            }
        }

        private static Dictionary<string, Type[]> Freeze(Dictionary<string, List<Type>> byName) =>
            byName.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray(), StringComparer.OrdinalIgnoreCase);

        private static bool IsFrameworkAssembly(string? name) =>
            name == null || name == "mscorlib" || name == "netstandard" || name.StartsWith("System", StringComparison.Ordinal)
            || name.StartsWith("Microsoft.", StringComparison.Ordinal) || name.StartsWith("Mono.", StringComparison.Ordinal);

        /// <summary>Every indexed short name (for "did you mean" suggestions).</summary>
        public static IReadOnlyCollection<string> ShortNames() => Index().Keys.Where(k => k.IndexOf('.') < 0).ToList();

        private static Dictionary<string, Type[]> Index()
        {
            if (_index != null) return _index;
            var byName = new Dictionary<string, List<Type>>(StringComparer.OrdinalIgnoreCase);
            void Add(Type t)
            {
                // IsVisible, not IsPublic: nested public types (ParticleSystem.MinMaxCurve, ParticleSystem.MainModule …) count too.
                if (t.IsVisible && !t.IsGenericTypeDefinition) AddByName(byName, t);
            }
            foreach (var t in TypeCache.GetTypesDerivedFrom<UnityEngine.Object>()) Add(t);
            // The RQL modules' own item types (ActionMapView, SplineView, LightingEnvironment …): the schema names them as member
            // types, and RQL's reachability walk stops long before them in a Unity project.
            var moduleAssemblies = new HashSet<System.Reflection.Assembly>(TypeCache.GetTypesDerivedFrom<IRqlModule>().Select(t => t.Assembly));
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var isModule = moduleAssemblies.Contains(asm);
                foreach (var t in TypeModel.LoadableTypes(asm))
                    if (t.IsEnum || (t.IsValueType && IsUnityEngine(t)) || (isModule && IsModuleItemType(t))) Add(t);
            }
            return _index = Freeze(byName);
        }

        // Member names declared by UnityEngine.Object and module item types (once per domain), and the engine whose adapters'
        // members were added on top of them.
        private static HashSet<string>? _declaredMembers;
        private static (RqlEngine engine, HashSet<string> names)? _members;

        /// <summary>
        /// <see cref="RqlEngine.IsKnownMember"/>: whether a UnityEngine.Object type (components, ScriptableObjects, assets, user
        /// scripts, internal ones included) or an RQL module item type has a member of this name, case-insensitive: its public
        /// properties and fields and its [SerializeField] fields (not [RqlIgnore] ones), plus the members the engine's registered
        /// adapters add (read from its type model for the adapters' target types only; building it for every type takes seconds).
        /// Collected on the first name RQL asks about: the declared names once per domain, the adapter names once per engine.
        /// </summary>
        public static bool IsKnownMember(RqlEngine engine, string name)
        {
            var cached = _members;
            if (cached == null || cached.Value.engine != engine)
            {
                var names = new HashSet<string>(_declaredMembers ??= DeclaredMembers(), StringComparer.OrdinalIgnoreCase);
                foreach (var adapter in engine.Adapters)
                {
                    try
                    {
                        foreach (var m in engine.Types.Info(adapter.Target).Members) names.Add(m.Name);
                    }
                    catch (Exception) { } // a type that cannot load contributes nothing
                }
                _members = cached = (engine, names);
            }
            return cached.Value.names.Contains(name);
        }

        private static HashSet<string> DeclaredMembers()
        {
            const System.Reflection.BindingFlags Own = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly;
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<Type>();
            var moduleItemTypes = Index().Values.SelectMany(types => types).Where(IsModuleItemType);
            foreach (var type in TypeCache.GetTypesDerivedFrom<UnityEngine.Object>().Concat(moduleItemTypes))
                for (var t = type; t != null && seen.Add(t); t = t.BaseType) // each declaring type once
                {
                    try
                    {
                        foreach (var p in t.GetProperties(Own))
                            if (p.GetMethod?.IsPublic == true && p.GetIndexParameters().Length == 0 && !p.IsDefined(typeof(RqlIgnoreAttribute), true))
                                names.Add(p.Name);
                        foreach (var f in t.GetFields(Own))
                            if ((f.IsPublic || f.IsDefined(typeof(SerializeField), false)) && !f.IsDefined(typeof(RqlIgnoreAttribute), true)
                                && f.Name.IndexOf('<') < 0) // compiler-generated backing fields
                                names.Add(f.Name);
                    }
                    catch (Exception) { } // a type that cannot load contributes nothing
                }
            return names;
        }

        /// <summary>
        /// <see cref="RqlEngine.EnumAliases"/>: an enum entry's Inspector name ([InspectorName], else the nicified entry name,
        /// e.g. "Two Sided") → the entry name, so values written as the Inspector shows them resolve. Names that are some entry's
        /// own name or unprefixed name, or that two entries share (any case), are left out: they would make a working name ambiguous.
        /// </summary>
        public static IReadOnlyDictionary<string, string>? EnumAliases(Type enumType)
        {
            var fields = enumType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            // Entry names and their unprefixed forms ("HAlign_Fill" -> "Fill"), which RQL also resolves.
            var entryNames = new HashSet<string>(fields.SelectMany(f => new[] { f.Name, f.Name.Substring(f.Name.IndexOf('_') + 1) }),
                StringComparer.OrdinalIgnoreCase);
            var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var shared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in fields)
            {
                var display = (Attribute.GetCustomAttribute(f, typeof(InspectorNameAttribute)) as InspectorNameAttribute)?.displayName
                              ?? ObjectNames.NicifyVariableName(f.Name);
                if (string.IsNullOrEmpty(display) || entryNames.Contains(display)) continue;
                if (!aliases.TryAdd(display, f.Name)) shared.Add(display);
            }
            foreach (var s in shared) aliases.Remove(s);
            return aliases.Count > 0 ? aliases : null;
        }

        private static bool IsUnityEngine(Type t) => t.Namespace?.StartsWith("UnityEngine", StringComparison.Ordinal) == true;

        // A concrete public class of an RQL namespace that agents read (views, editor APIs), not the adapters and modules.
        internal static bool IsModuleItemType(Type t) =>
            t.IsClass && !t.IsAbstract && t.IsVisible
            && (t.Namespace == typeof(UnityTypes).Namespace || t.Namespace?.StartsWith(typeof(UnityTypes).Namespace + ".", StringComparison.Ordinal) == true)
            && !typeof(RqlTypeAdapter).IsAssignableFrom(t) && !typeof(IRqlModule).IsAssignableFrom(t)
            && !typeof(Attribute).IsAssignableFrom(t) && !typeof(UnityEngine.Object).IsAssignableFrom(t);
    }
}
