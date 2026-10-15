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
    /// <summary>RQL value literal (the syntax of <c>set x = …</c>) → the JSON the tool's schema expects.</summary>
    internal static class RqlLiteral
    {
        public static bool TryToJson(string text, out JsonNode? json, out string? error)
        {
            json = null;
            error = null;
            if (string.IsNullOrWhiteSpace(text)) return true; // empty = not given
            Expr expr;
            try { expr = RqlParser.ParseValue(text); }
            catch (RqlException ex)
            {
                error = ex.Message;
                return false;
            }
            return TryConvert(expr, out json, out error);
        }

        private static bool TryConvert(Expr e, out JsonNode? json, out string? error)
        {
            json = null;
            error = null;
            switch (e)
            {
                case LitExpr l:
                    json = l.Value switch
                    {
                        null => null,
                        bool b => JsonValue.Create(b),
                        string s => JsonValue.Create(s),
                        double d when Math.Floor(d) == d && Math.Abs(d) < 9e15 => JsonValue.Create((long)d),
                        double d => JsonValue.Create(d),
                        long n => JsonValue.Create(n),
                        var other => JsonValue.Create(System.Convert.ToString(other, CultureInfo.InvariantCulture)),
                    };
                    return true;
                case ListExpr list:
                    var array = new JsonArray();
                    foreach (var item in list.Items)
                    {
                        if (!TryConvert(item, out var v, out error)) return false;
                        array.Add(v);
                    }
                    json = array;
                    return true;
                case ObjectExpr obj:
                    var o = new JsonObject();
                    foreach (var kv in obj.Entries)
                    {
                        if (!TryConvert(kv.Value, out var v, out error)) return false;
                        o[kv.Key] = v;
                    }
                    json = o;
                    return true;
                // A bare word (an enum value such as Directional) reads as its text.
                case PathExpr p when p.On == null && p.Segments.Count == 1 && p.Segments[0].Kind == SegmentKind.Member:
                    json = JsonValue.Create(p.Segments[0].Name);
                    return true;
                default:
                    error = "only literal values are allowed here (no paths, operators or function calls)";
                    return false;
            }
        }
    }

    /// <summary>JSON result (a ResponseCallTool built by the tool itself) → compact text: <c>key: value</c> lines, RQL-style literals, tables for lists of flat objects.</summary>
    internal sealed class CompactText
    {
        private readonly ToolTextEscaping _escaping;

        public CompactText(ToolTextEscaping escaping) { _escaping = escaping; }

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
            if (!(node is JsonArray a) || a.Count < 2 || !a.All(x => x is JsonObject xo && xo.All(kv => !(kv.Value is JsonObject) && !(kv.Value is JsonArray))))
                return null;
            var columns = new List<string>();
            foreach (JsonObject row in a)
                foreach (var kv in row)
                    if (!IsEmpty(kv.Value) && !columns.Contains(kv.Key)) columns.Add(kv.Key);
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
            ToolText.Quote(s, _escaping).Length != s.Length + 2 ||
            s.Length == 0 || s.Trim() != s || s.IndexOfAny(new[] { '|', ',', '\n', '\r', '{', '}', '[', ']', '"', ':' }) >= 0 ||
            s == "null" || s == "true" || s == "false" || double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _);

        private string Key(string key) =>
            key.Length > 0 && (char.IsLetter(key[0]) || key[0] == '_' || key[0] == '$') && key.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '$')
                ? key : ToolText.Quote(key, _escaping);

        private static bool IsEmpty(JsonNode? v) => v == null || (v is JsonArray a && a.Count == 0) || (v is JsonObject o && o.Count == 0);
    }
}
