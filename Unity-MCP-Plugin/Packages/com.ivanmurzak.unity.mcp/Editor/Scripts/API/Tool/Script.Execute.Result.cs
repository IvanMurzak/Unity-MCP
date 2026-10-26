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
                    {
                        var entries = new List<object?>();
                        foreach (DictionaryEntry e in dictionary)
                        {
                            if (entries.Count == MaxItems) { entries.Add(More(dictionary.Count)); break; }
                            entries.Add(new Entry { key = e.Key, value = e.Value });
                        }
                        return entries;
                    }
                    case ICollection collection when collection.Count <= MaxItems:
                        return value;
                    case IEnumerable sequence:
                    {
                        // A lazy sequence is enumerated once, at most MaxItems + 1 items (it may be endless).
                        var items = new List<object?>();
                        var total = (value as ICollection)?.Count;
                        foreach (var item in sequence)
                        {
                            if (items.Count == MaxItems) { items.Add(More(total)); break; }
                            items.Add(item);
                        }
                        return items;
                    }
                    default:
                        return value;
                }
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
