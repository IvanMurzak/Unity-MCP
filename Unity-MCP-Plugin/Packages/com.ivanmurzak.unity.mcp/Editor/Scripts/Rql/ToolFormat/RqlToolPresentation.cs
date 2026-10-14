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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.McpPlugin.Common.Model;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.ToolFormat
{
    /// <summary>
    /// EXPERIMENTAL tool presentation in RQL style, for every tool (not only the RQL ones):
    /// <list type="bullet">
    /// <item>simple parameters (string, number, bool, enum, list of those) keep a plain JSON schema without $ref / $defs;</item>
    /// <item>a complex parameter becomes a string holding an RQL literal (<c>{name: "Lamp", path: "Root/Lamp"}</c>); its type is
    /// described once per tool in the compact form of <c>rql-schema</c> (<c>GameObjectRef {instanceID int, name string, …}</c>);</item>
    /// <item>the output schema is dropped and a JSON result is returned as compact text (tables for lists of flat objects).</item>
    /// </list>
    /// The literal is converted back to the JSON the tool's own schema describes, so deserialization and the tool are unchanged.
    /// </summary>
    public sealed partial class RqlToolPresentation : IToolPresentation
    {
        public static readonly RqlToolPresentation Instance = new RqlToolPresentation(memberDescriptions: true);

        /// <summary>Like <see cref="Instance"/> without the member descriptions in the type text (shorter, less guidance).</summary>
        public static readonly RqlToolPresentation Lean = new RqlToolPresentation(memberDescriptions: false);

        public static bool Enabled
        {
            get => ToolPresentation.Current == Instance;
            set => ToolPresentation.Current = value ? Instance : (ToolPresentation.Current == Instance ? null : ToolPresentation.Current);
        }

        private readonly bool _memberDescriptions;

        private RqlToolPresentation(bool memberDescriptions) { _memberDescriptions = memberDescriptions; }

        // Per tool: the converted schema, keyed by the original schema text (tools can change at runtime).
        private readonly ConcurrentDictionary<string, (string source, ToolText text)> _cache = new ConcurrentDictionary<string, (string, ToolText)>();

        public void List(IRunTool tool, ResponseListTool listed)
        {
            var text = For(tool);
            listed.InputSchema = text.Schema;
            if (text.Types.Length > 0)
                listed.Description = string.IsNullOrWhiteSpace(listed.Description) ? text.Types : listed.Description!.TrimEnd() + "\n\n" + text.Types;
            listed.OutputSchema = null;
        }

        public string? Arguments(IRunTool tool, IReadOnlyDictionary<string, JsonElement> arguments, out IReadOnlyDictionary<string, JsonElement> converted)
        {
            converted = arguments;
            var text = For(tool);
            if (text.Literals.Count == 0) return null;
            Dictionary<string, JsonElement>? result = null;
            foreach (var kv in arguments)
            {
                if (!text.Literals.TryGetValue(kv.Key, out var type) || kv.Value.ValueKind != JsonValueKind.String) continue;
                var literal = kv.Value.GetString() ?? "";
                if (!Literal.TryToJson(literal, out var json, out var error))
                    return $"argument '{kv.Key}': {error}. Expected an RQL literal of {type}";
                result ??= new Dictionary<string, JsonElement>(arguments);
                result[kv.Key] = JsonSerializer.SerializeToElement(json);
            }
            if (result != null) converted = result;
            return null;
        }

        public ResponseCallTool Result(IRunTool tool, ResponseCallTool result)
        {
            if (result.StructuredContent != null)
            {
                // McpPlugin wraps a returned value as {"result": value}.
                var node = result.StructuredContent is JsonObject o && o.Count == 1 && o.ContainsKey("result") ? o["result"] : result.StructuredContent;
                result.StructuredContent = null;
                result.Content = new List<ContentBlock> { Text(Compact.Render(node)) };
                return result;
            }
            if (result.Content == null) return result;
            for (int i = 0; i < result.Content.Count; i++)
            {
                var block = result.Content[i];
                if (block.Type != Consts.ContentType.Text || !(block.Text is string s)) continue;
                var trimmed = s.TrimStart();
                if (trimmed.Length == 0 || (trimmed[0] != '{' && trimmed[0] != '[')) continue;
                JsonNode? node;
                try { node = JsonNode.Parse(s); }
                catch (JsonException) { continue; }
                result.Content[i] = Text(Compact.Render(node));
            }
            return result;
        }

        private static ContentBlock Text(string text) => new ContentBlock { Type = Consts.ContentType.Text, Text = text };

        private ToolText For(IRunTool tool)
        {
            var source = tool.InputSchema?.ToJsonString() ?? "";
            if (_cache.TryGetValue(tool.Name, out var hit) && hit.source == source) return hit.text;
            var text = SchemaText.Convert(source, _memberDescriptions);
            _cache[tool.Name] = (source, text);
            return text;
        }

        /// <summary>One tool's converted input: the new schema, the type block for the description, the literal parameters.</summary>
        internal sealed class ToolText
        {
            public JsonElement Schema;
            public string Types = "";
            /// <summary>Parameter name → its type text, for the parameters that take an RQL literal.</summary>
            public Dictionary<string, string> Literals = new Dictionary<string, string>();
        }
    }
}
