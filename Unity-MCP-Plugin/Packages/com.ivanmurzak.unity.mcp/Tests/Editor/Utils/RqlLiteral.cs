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
using Rql;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    /// <summary>RQL value literal (the syntax of <c>set x = …</c>) → the JSON the tool's schema expects.</summary>
    /// <remarks>Reads RQL tool output back into JSON so tests can assert on values under either data layer.</remarks>
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
}
