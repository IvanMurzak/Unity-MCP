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
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.McpPlugin;
using Rql;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.ToolFormat
{
    /// <summary>JSON result (a ResponseCallTool built by the tool itself) → compact text: <c>key: value</c> lines, RQL-style literals, tables for lists of flat objects.</summary>
    internal sealed class CompactText
    {
        private static readonly char[] Separators = { '|', ',', '\n', '\r', '{', '}', '[', ']', '"', ':' };
        private static readonly char[] RowBreakers = { '|', '\n', '\r' };

        private readonly ToolTextEscaping _escaping;

        public CompactText(ToolTextEscaping escaping) { _escaping = escaping; }

        public ToolTextEscaping Escaping => _escaping;

        /// <summary>
        /// The columns of <paramref name="items"/> when they are at least two objects whose values are all scalars (rows), in
        /// first-seen order, keeping the keys whose value passes <paramref name="keep"/>; null when they are not rows.
        /// </summary>
        public static List<string>? FlatColumns(IList<JsonNode?> items, Func<JsonNode?, bool> keep)
        {
            if (items.Count < 2 || !items.All(x => x is JsonObject xo && xo.All(kv => !(kv.Value is JsonObject) && !(kv.Value is JsonArray))))
                return null;
            var columns = new List<string>();
            foreach (JsonObject row in items)
                foreach (var kv in row)
                    if (keep(kv.Value) && !columns.Contains(kv.Key)) columns.Add(kv.Key);
            return columns;
        }

        /// <summary>One cell of an "a|b" row: text bare unless it is empty or holds a row separator.</summary>
        public string RowCell(JsonNode? v) => v switch
        {
            null => "null",
            JsonValue jv when jv.GetValueKind() == JsonValueKind.String => jv.GetValue<string>() is var s && (s.Length == 0 || s.IndexOfAny(RowBreakers) >= 0)
                ? ToolText.Quote(s, _escaping) : s,
            _ => v.ToJsonString(),
        };

        public string Render(JsonNode? node)
        {
            if (node is JsonObject o && o.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var kv in o)
                {
                    if (IsEmpty(kv.Value)) continue;
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append(Key(kv.Key)).Append(':');
                    if (Table(kv.Value) is string table) sb.Append('\n').Append(table);
                    else sb.Append(' ').Append(Inline(kv.Value));
                }
                return sb.ToString();
            }
            return Table(node) ?? Inline(node);
        }

        /// <summary>A list of at least two objects whose values are all scalars, as <c>a | b</c> rows under a header.</summary>
        private string? Table(JsonNode? node)
        {
            if (!(node is JsonArray a) || !(FlatColumns(a, v => !IsEmpty(v)) is List<string> columns))
                return null;
            var sb = new StringBuilder(string.Join(" | ", columns.Select(Key)));
            foreach (JsonObject row in a)
                sb.Append('\n').Append(string.Join(" | ", columns.Select(c => row.TryGetPropertyValue(c, out var v) && !IsEmpty(v) ? Inline(v) : "")));
            return sb.ToString();
        }

        private string Inline(JsonNode? node)
        {
            switch (node)
            {
                case null: return "null";
                case JsonObject o:
                    return "{" + string.Join(", ", o.Where(kv => !IsEmpty(kv.Value)).Select(kv => Key(kv.Key) + ": " + Inline(kv.Value))) + "}";
                case JsonArray a:
                    return "[" + string.Join(", ", a.Select(Inline)) + "]";
                case JsonValue v when v.GetValueKind() == JsonValueKind.String:
                    var s = v.GetValue<string>();
                    return NeedsQuotes(s) ? ToolText.Quote(s, _escaping) : s;
                default:
                    return node.ToJsonString();
            }
        }

        // Bare text unless it could be misread (empty, separators, a number / keyword look-alike, surrounding spaces) or the
        // configured escaping has to escape something in it.
        private bool NeedsQuotes(string s) =>
            s.Length == 0 || s.Trim() != s || s.IndexOfAny(Separators) >= 0 ||
            s == "null" || s == "true" || s == "false" || double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ||
            ToolText.Quote(s, _escaping).Length != s.Length + 2;

        private string Key(string key) =>
            key.Length > 0 && (char.IsLetter(key[0]) || key[0] == '_' || key[0] == '$') && key.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '$')
                ? key : ToolText.Quote(key, _escaping);

        private static bool IsEmpty(JsonNode? v) => v == null || (v is JsonArray a && a.Count == 0) || (v is JsonObject o && o.Count == 0);
    }
}
