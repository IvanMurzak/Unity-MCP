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
using System.Linq;
using System.Text;
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    /// <summary>
    /// Compact schema for agents. RQL's generic schema walks every reachable type and subclass, which for UnityEngine means
    /// hundreds of component types; here the overview lists what the loaded scenes actually contain, and a type name shows
    /// one type's members (any loaded type, present in the scene or not). Lines use the core format (AgentFormat.TypeSchema).
    /// </summary>
    public static class RqlUnitySchema
    {
        public static string Render(RqlEngine engine, string? typeName)
        {
            if (typeName != null) return RenderType(engine, typeName);

            var sb = new StringBuilder();
            sb.Append("sources:\n");
            foreach (var s in engine.Sources.Values)
                sb.Append("  ").Append(s.Name).Append(s.Description != null ? " — " + s.Description : "").Append('\n');
            sb.Append("functions:\n");
            foreach (var f in engine.Functions.Keys.OrderBy(k => k, StringComparer.Ordinal))
                sb.Append("  ").Append(engine.FunctionDescriptions.TryGetValue(f, out var d) ? d : f + "(…)").Append('\n');

            var counts = new Dictionary<Type, int>();
            var components = new List<Component>();
            foreach (var go in RqlUnity.AllGameObjects())
            {
                go.GetComponents(components);
                foreach (var c in components)
                {
                    if (c == null) continue;
                    var t = c.GetType();
                    counts[t] = counts.TryGetValue(t, out var n) ? n + 1 : 1;
                }
            }
            sb.Append("component types in the loaded scenes (rql-schema \"<type>\" for members): ");
            sb.Append(string.Join(", ", counts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key.Name, StringComparer.Ordinal)
                .Select(kv => kv.Key.Name + " ×" + kv.Value)));
            sb.Append('\n');
            sb.Append(AgentFormat.TypeSchema(engine, typeof(GameObject), markExplicitOnly: true)).Append('\n');
            // name / tag / gameObject … are on GameObject above.
            sb.Append(AgentFormat.TypeSchema(engine, typeof(Transform), inheritedFrom: typeof(Component), markExplicitOnly: true));
            return sb.ToString();
        }

        /// <summary>A source name ("editor", "console") or the type of a source's items, e.g. RqlEditorApi or LogEntry.</summary>
        private static IEnumerable<Type> SourceTypes(RqlEngine engine, string name)
        {
            foreach (var s in engine.Sources.Values)
            {
                var t = SourceItemType(engine, s);
                if (t == null) continue;
                if (string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase) || string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
                    yield return t;
            }
        }

        // A source's item type: learned by calling its getter once (some walk the whole scene, read the log file or list every
        // shader), then cached per engine and source; a source that returned nothing is asked again next time.
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<RqlEngine, Dictionary<string, Type>> ItemTypes =
            new System.Runtime.CompilerServices.ConditionalWeakTable<RqlEngine, Dictionary<string, Type>>();

        private static Type? SourceItemType(RqlEngine engine, SourceRegistration source)
        {
            var cache = ItemTypes.GetOrCreateValue(engine);
            if (cache.TryGetValue(source.Name, out var known)) return known;
            Type? t;
            try { t = source.Getter()?.GetType(); } catch { return null; }
            if (t == null) return null;
            var info = engine.Types.Info(t);
            if ((info.IsList || info.IsMap) && info.ElementType != null) t = info.ElementType;
            return cache[source.Name] = t;
        }

        private static string RenderType(RqlEngine engine, string typeName)
        {
            var candidates = SourceTypes(engine, typeName).Concat(UnityTypes.Candidates(typeName)).Distinct().Take(3).ToList();
            if (candidates.Count == 0)
            {
                // Types only reachable through sources (e.g. an extension's view objects): RQL's own schema walk finds them.
                var reachable = AgentFormat.Schema(engine, typeName);
                return reachable.StartsWith("error unknown-type", StringComparison.Ordinal)
                    ? AgentFormat.UnknownTypeError(typeName, UnityTypes.ShortNames())
                    : reachable;
            }
            var sb = new StringBuilder();
            foreach (var t in candidates)
            {
                if (sb.Length > 0) sb.Append('\n');
                // Components list their own members; the shared Object / Component / Behaviour ones are summarised once.
                var shared = typeof(Component).IsAssignableFrom(t) && t != typeof(Component) ? typeof(MonoBehaviour) : null;
                sb.Append(AgentFormat.TypeSchema(engine, t, inheritedFrom: shared, markExplicitOnly: true));
                if (shared != null)
                {
                    var own = new HashSet<string>(engine.Types.Info(t).Members.Select(m => m.Name), StringComparer.Ordinal);
                    var inherited = engine.Types.Info(shared).Members.Select(m => m.Name).Where(own.Contains).ToList();
                    if (inherited.Count > 0) sb.Append("\n  (+ inherited: ").Append(string.Join(", ", inherited)).Append(')');
                }
            }
            return sb.ToString();
        }
    }
}
