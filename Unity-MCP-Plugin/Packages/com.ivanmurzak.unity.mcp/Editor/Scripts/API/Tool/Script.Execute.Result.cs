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
using System.Collections;
using System.Collections.Generic;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public static partial class Tool_Script
    {
        /// <summary>
        /// Shapes a snippet's result before it is serialized: the result goes into the agent's context, so it is bounded
        /// (a 200 000-item list must not arrive whole; Code-Exec already clips a string), and values the serializer reads
        /// poorly are made readable (a dictionary as key / value entries instead of its interface properties, a LINQ query
        /// as its items instead of the iterator's state).
        /// </summary>
        internal static class ResultValue
        {
            /// <summary>Most characters of a shaped result once serialized to JSON, beyond which only its beginning is returned.</summary>
            public const int MaxSerializedChars = 131072;

            /// <summary>Most items of a collection result.</summary>
            public const int MaxItems = 1000;

            public sealed class Entry
            {
                public object? key;
                public object? value;
            }

            public static object? Shape(object? value)
            {
                switch (value)
                {
                    case null:
                        return null;
                    case string _: // clipped by Code-Exec (MaxValueChars); not a sequence of chars
                    case UnityEngine.Object _:
                        return value;
                    case IDictionary dictionary:
                        return Cap(Entries(dictionary), dictionary.Count);
                    case IEnumerable sequence:
                        // A collection is returned as it is unless it is too long; a lazy query or iterator as its items; any
                        // other enumerable type (it may have fields of its own) as it is.
                        if (CountOf(value) is int count) return count <= MaxItems ? value : Cap(sequence, count);
                        return IsLazy(value.GetType()) ? Cap(sequence, null) : value;
                    default:
                        return value;
                }
            }

            // ICollection, or a public int Count (HashSet<T> and other generic-only collections).
            static int? CountOf(object value) =>
                value is ICollection collection ? collection.Count
                : value.GetType().GetProperty("Count", typeof(int))?.GetValue(value) is int count ? count : (int?)null;

            // LINQ operators and compiler-generated iterators (yield return): serialized as is, they show their state.
            static bool IsLazy(System.Type type) =>
                type.Namespace == "System.Linq" || type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false);

            // IDictionary's own enumerator: a generic dictionary's IEnumerable yields KeyValuePairs, not DictionaryEntry.
            static IEnumerable<Entry> Entries(IDictionary dictionary)
            {
                var e = dictionary.GetEnumerator();
                while (e.MoveNext()) yield return new Entry { key = e.Key, value = e.Value };
            }

            // Enumerated once, at most MaxItems + 1 items: a lazy sequence may be endless.
            static List<object?> Cap(IEnumerable items, int? total)
            {
                var list = new List<object?>();
                foreach (var item in items)
                {
                    if (list.Count == MaxItems) { list.Add(More(total)); break; }
                    list.Add(item);
                }
                return list;
            }

            static string More(int? total) => total is int n
                ? $"… {n - MaxItems} more items (first {MaxItems} of {n} shown): return a smaller selection, e.g. .Take(100), or a summary"
                : $"… more items (first {MaxItems} shown): return a smaller selection, e.g. .Take(100), or a summary";
        }
    }
}
