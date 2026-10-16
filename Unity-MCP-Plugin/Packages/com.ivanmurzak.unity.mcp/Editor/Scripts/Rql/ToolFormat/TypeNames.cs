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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.ToolFormat
{
    /// <summary>
    /// Short, unique display names for the types one tool describes. A type gets its short name ("Settings") unless another
    /// type of the same tool shares it; then every type in that clash keeps as many trailing namespace segments as it takes to
    /// tell them apart ("Alpha.Settings", "Beta.Settings"), independent of the order they appear in. Generic arguments are
    /// shortened the same way and written as &lt;…&gt;: "Wrapper(Game.Item)" → "Wrapper&lt;Item&gt;".
    /// </summary>
    internal static class TypeNames
    {
        public static Dictionary<string, string> Assign(IEnumerable<string> fullNames)
        {
            var keys = fullNames.Distinct().ToList();
            var result = new Dictionary<string, string>();
            var pending = keys;
            for (int depth = 1; pending.Count > 0 && depth <= 12; depth++)
            {
                var names = pending.ToDictionary(k => k, k => Display(k, depth));
                var taken = new HashSet<string>(result.Values);
                var clashing = new HashSet<string>(names.GroupBy(kv => kv.Value).Where(g => g.Count() > 1).SelectMany(g => g.Select(kv => kv.Key)));
                foreach (var kv in names)
                    if (!clashing.Contains(kv.Key) && !taken.Contains(kv.Value))
                        result[kv.Key] = kv.Value;
                pending = pending.Where(k => !result.ContainsKey(k)).ToList();
            }
            foreach (var k in pending) result[k] = k; // identical displays at every depth: keep the full name
            return result;
        }

        /// <summary>
        /// The name with each identifier cut to its last <paramref name="depth"/> segments ('.', '+' nested, '-' ReflectorNet
        /// nested); ReflectorNet's generic form "A(B,C)" written as "A&lt;B, C&gt;".
        /// </summary>
        public static string Display(string fullName, int depth)
        {
            var sb = new StringBuilder();
            var ident = new StringBuilder();
            void Flush()
            {
                if (ident.Length == 0) return;
                var segments = ident.ToString().Split('.', '+', '-');
                sb.Append(string.Join(".", segments.Skip(System.Math.Max(0, segments.Length - depth))));
                ident.Clear();
            }
            foreach (var c in fullName)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '+' || c == '-') { ident.Append(c); continue; }
                Flush();
                if (c == '(') sb.Append('<');
                else if (c == ')') sb.Append('>');
                else if (c == ',') sb.Append(", ");
            }
            Flush();
            return sb.ToString();
        }

        /// <summary>A CLR type in the same form ReflectorNet uses for $defs keys: "Ns.Outer+Inner", generics "Ns.Box(Ns.Item)".</summary>
        public static string FullName(System.Type type)
        {
            if (type.IsArray) return FullName(type.GetElementType()!) + "[]";
            var name = (type.IsGenericType ? type.GetGenericTypeDefinition().FullName : type.FullName) ?? type.Name;
            var tick = name.IndexOf('`');
            if (tick >= 0) name = name.Substring(0, tick);
            return type.IsGenericType ? name + "(" + string.Join(",", type.GetGenericArguments().Select(FullName)) + ")" : name;
        }
    }
}
