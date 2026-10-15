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
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.Rql.ToolFormat;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    /// <summary>
    /// A tool's returned value as data whatever the tools' data layer: ReflectorNet's structured content, or RQL's agent text
    /// read back (rows "N results / a|b / 1|2", item lines "{k:v,…}", a single value). Tests assert on the value, not on the
    /// wire format, so they run under both layers.
    /// </summary>
    public static class ToolResults
    {
        public static bool IsRql => ToolDataLayerSetUp.Binding is RqlToolBinding;

        /// <summary>The value wrapped as ReflectorNet's {"result": value} JSON text, for tests that parse that shape.</summary>
        public static string ResultJson(ResponseCallTool response) => new JsonObject { ["result"] = Data(response)?.DeepClone() }.ToJsonString();

        /// <param name="rql">The layer that produced the response; null = the suite's (<see cref="IsRql"/>).</param>
        public static JsonNode? Data(ResponseCallTool response, bool? rql = null)
        {
            if (response.StructuredContent is JsonObject structured)
                return structured.Count == 1 && structured.ContainsKey("result") ? structured["result"] : structured;
            var text = string.Join("\n", response.Content.Where(c => c.Text != null).Select(c => c.Text)).Trim();
            if (!(rql ?? IsRql))
            {
                try
                {
                    var node = JsonNode.Parse(text);
                    return node is JsonObject o && o.Count == 1 && o.ContainsKey("result") ? o["result"] : node;
                }
                catch (System.Text.Json.JsonException) { return JsonValue.Create(text); }
            }
            return ParseRql(text);
        }

        private static readonly Regex Header = new Regex(@"^(\d+) results?( \(of \d+\))?$|^\d+ of \d+ \(truncated");

        private static JsonNode? ParseRql(string text)
        {
            var lines = text.Split('\n').Where(l => !l.StartsWith("warning ")).ToList();
            if (lines.Count == 0) return null;
            if (!Header.IsMatch(lines[0])) return Value(string.Join("\n", lines));
            var rest = lines.Skip(1).Where(l => l.Length > 0 && !l.StartsWith("note: ")).ToList();
            var array = new JsonArray();
            if (rest.Count == 0) return array;
            // Rows have a header with at least two columns (RqlToolBinding.Show); anything else is one value per line.
            if (!Cells(rest[0]).Skip(1).Any())
            {
                foreach (var l in rest) array.Add(Value(l));
                return array;
            }
            var columns = Cells(rest[0]);
            foreach (var row in rest.Skip(1))
            {
                var cells = Cells(row);
                var o = new JsonObject();
                for (int i = 0; i < columns.Count; i++) o[columns[i]] = i < cells.Count ? Value(cells[i]) : null;
                array.Add(o);
            }
            return array;
        }

        // A cell or a line in RQL literal syntax; plain text when it is not one ("Hello World").
        private static JsonNode? Value(string s)
        {
            if (s == "null") return null;
            return RqlLiteral.TryToJson(s, out var json, out _) && json != null ? json : JsonValue.Create(s);
        }

        // Split a row on '|' outside double-quoted strings.
        private static List<string> Cells(string row)
        {
            var cells = new List<string>();
            var sb = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < row.Length; i++)
            {
                var c = row[i];
                if (c == '"' && (i == 0 || row[i - 1] != '\\')) quoted = !quoted;
                if (c == '|' && !quoted) { cells.Add(sb.ToString()); sb.Clear(); continue; }
                sb.Append(c);
            }
            cells.Add(sb.ToString());
            return cells;
        }
    }
}

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    public static class ToolResultExtensions
    {
        /// <summary>
        /// A property by name ignoring case: RQL shows C# member names ("Name"), ReflectorNet JSON the serializer's ("name").
        /// </summary>
        public static bool TryGetPropertyCI(this System.Text.Json.JsonElement element, string name, out System.Text.Json.JsonElement value)
        {
            if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
                foreach (var p in element.EnumerateObject())
                    if (string.Equals(p.Name, name, System.StringComparison.OrdinalIgnoreCase)) { value = p.Value; return true; }
            value = default;
            return false;
        }

        public static System.Text.Json.JsonElement GetPropertyCI(this System.Text.Json.JsonElement element, string name) =>
            element.TryGetPropertyCI(name, out var v) ? v : throw new System.Collections.Generic.KeyNotFoundException(name);

        /// <summary>The returned value as {"result": value} JSON under any data layer (see <see cref="ToolResults"/>).</summary>
        public static string ToolResultJson(this com.IvanMurzak.McpPlugin.Common.Model.ResponseCallTool response) => ToolResults.ResultJson(response);
    }
}
