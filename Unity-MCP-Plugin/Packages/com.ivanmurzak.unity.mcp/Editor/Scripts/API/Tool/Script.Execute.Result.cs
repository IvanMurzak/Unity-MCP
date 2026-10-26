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
        /// (a 5 MB string or a 200 000-item list must not arrive whole), and values the serializer reads poorly are made
        /// readable (a dictionary as key / value entries instead of its interface properties, a LINQ query as its items
        /// instead of the iterator's state).
        /// </summary>
        internal static class ResultValue
        {
            /// <summary>Most characters of a result string (or of the serialized result), as rql-query's output limit.</summary>
            public const int MaxChars = 65536;

            /// <summary>Most characters of a shaped result once serialized to JSON, beyond which only its beginning is returned.</summary>
            public const int MaxSerializedChars = 2 * MaxChars;

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
                    case string s:
                        return s.Length <= MaxChars ? s : Truncate(s, "characters");
                    case UnityEngine.Object _:
                        return value;
                    case IDictionary dictionary:
                        return Cap(Entries(dictionary), dictionary.Count);
                    case ICollection collection when collection.Count <= MaxItems:
                        return value;
                    case IEnumerable sequence:
                        return Cap(sequence, (value as ICollection)?.Count);
                    default:
                        return value;
                }
            }

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

            public static string Truncate(string text, string what) =>
                text.Substring(0, MaxChars) + $"\n… truncated: {MaxChars} of {text.Length} {what} shown. Return a smaller value " +
                "(a substring, .Take(100), a count or a summary).";
        }
    }
}
