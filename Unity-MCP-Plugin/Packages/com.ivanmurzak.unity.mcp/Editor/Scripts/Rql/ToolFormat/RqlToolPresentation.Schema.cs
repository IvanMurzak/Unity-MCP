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
using System.Text.Json;
using System.Text.Json.Nodes;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.ToolFormat
{
    public sealed partial class RqlToolPresentation
    {
        /// <summary>JSON schema of a tool's input → plain schema for simple parameters, RQL type text for complex ones.</summary>
        internal static class SchemaText
        {
            public const string TypesHeader =
                "Argument types (pass these arguments as an RQL literal string: {key: value, …}, [a, b], \"text\", 1.5, true, null; " +
                "leave out keys you do not need):";

            public static ToolText Convert(string source, bool memberDescriptions = true)
            {
                var result = new ToolText();
                var schema = source.Length == 0 ? null : JsonNode.Parse(source) as JsonObject;
                if (schema == null || !(schema["properties"] is JsonObject properties))
                {
                    result.Schema = Element(schema ?? new JsonObject { ["type"] = "object", ["properties"] = new JsonObject() });
                    return result;
                }
                var writer = new Writer(schema["$defs"] as JsonObject, memberDescriptions);
                var newProperties = new JsonObject();
                foreach (var kv in properties)
                {
                    if (!(kv.Value is JsonObject prop)) continue;
                    var description = Str(prop, "description");
                    var resolved = writer.Resolve(prop);
                    if (writer.Simple(resolved) is JsonObject simple)
                    {
                        if (description != null) simple["description"] = description;
                        newProperties[kv.Key] = simple;
                        continue;
                    }
                    var type = writer.Type(prop);
                    result.Literals[kv.Key] = type;
                    newProperties[kv.Key] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = (description == null ? "" : description.TrimEnd() + " ") + "RQL literal: " + type,
                    };
                }
                var newSchema = new JsonObject { ["type"] = "object", ["properties"] = newProperties };
                if (schema["required"] is JsonArray required) newSchema["required"] = required.DeepClone();
                if (schema["additionalProperties"] is JsonNode additional) newSchema["additionalProperties"] = additional.DeepClone();
                result.Schema = Element(newSchema);
                result.Types = writer.TypeBlock();
                return result;
            }

            private static JsonElement Element(JsonNode node)
            {
                using var doc = JsonDocument.Parse(node.ToJsonString());
                return doc.RootElement.Clone();
            }

            internal static string? Str(JsonObject o, string key) =>
                o.TryGetPropertyValue(key, out var v) && v is JsonValue jv && jv.TryGetValue<string>(out var s) ? s : null;

            /// <summary>Renders schema nodes as RQL type text; object types with properties become named entries of the type block.</summary>
            private sealed class Writer
            {
                private readonly JsonObject? _defs;
                private readonly bool _memberDescriptions;
                private readonly Dictionary<string, string> _names = new Dictionary<string, string>(); // $defs key → short name
                private readonly List<(string name, JsonObject def)> _named = new List<(string, JsonObject)>();

                public Writer(JsonObject? defs, bool memberDescriptions) { _defs = defs; _memberDescriptions = memberDescriptions; }

                /// <summary>Follows $ref chains to the definition (the referencing node's own keys win, as JSON schema does).</summary>
                public JsonObject Resolve(JsonObject node)
                {
                    for (int guard = 0; guard < 16 && Ref(node) is string key && _defs?[key] is JsonObject def; guard++)
                    {
                        var merged = (JsonObject)def.DeepClone();
                        foreach (var kv in node)
                            if (kv.Key != "$ref") merged[kv.Key] = kv.Value?.DeepClone();
                        node = merged;
                    }
                    return node;
                }

                private static string? Ref(JsonObject node)
                {
                    var r = Str(node, "$ref");
                    return r != null && r.StartsWith("#/$defs/", StringComparison.Ordinal) ? r.Substring("#/$defs/".Length) : null;
                }

                /// <summary>A plain JSON schema for primitives, enums and lists of those; null for anything complex.</summary>
                public JsonObject? Simple(JsonObject node)
                {
                    var type = PrimaryType(node);
                    if (node["enum"] is JsonArray values) return new JsonObject { ["type"] = type ?? "string", ["enum"] = values.DeepClone() };
                    switch (type)
                    {
                        case "string": case "number": case "integer": case "boolean":
                            return new JsonObject { ["type"] = type };
                        case "array":
                            return node["items"] is JsonObject items && Simple(Resolve(items)) is JsonObject item
                                ? new JsonObject { ["type"] = "array", ["items"] = item }
                                : null;
                        default:
                            return null;
                    }
                }

                private static string? PrimaryType(JsonObject node)
                {
                    switch (node["type"])
                    {
                        case JsonValue v when v.TryGetValue<string>(out var s): return s;
                        case JsonArray a: return a.Select(x => x?.GetValue<string>()).FirstOrDefault(x => x != "null");
                        default: return null;
                    }
                }

                public string Type(JsonObject node, int depth = 0)
                {
                    if (depth > 12) return "any";
                    if (Ref(node) is string key && _defs?[key] is JsonObject def)
                        return IsRecord(def) ? Name(key, def) : Type(def, depth + 1);
                    if (node["enum"] is JsonArray values) return string.Join("|", values.Select(v => v?.ToString() ?? "null"));
                    foreach (var combinator in new[] { "oneOf", "anyOf" })
                        if (node[combinator] is JsonArray options)
                            return string.Join(" or ", options.OfType<JsonObject>().Select(o => Type(o, depth + 1)).Distinct());
                    switch (PrimaryType(node))
                    {
                        case "string": return "string";
                        case "integer": return "int";
                        case "number": return "float";
                        case "boolean": return "bool";
                        case "array": return "[" + (node["items"] is JsonObject items ? Type(items, depth + 1) : "any") + "]";
                        case "object":
                            if (node["properties"] is JsonObject) return Record(node, depth + 1);
                            if (node["additionalProperties"] is JsonObject values2) return "{string: " + Type(values2, depth + 1) + "}";
                            return "object";
                        default:
                            return node["properties"] is JsonObject ? Record(node, depth + 1) : "any";
                    }
                }

                private static bool IsRecord(JsonObject def) => def["properties"] is JsonObject p && p.Count > 0;

                private string Name(string key, JsonObject def)
                {
                    if (_names.TryGetValue(key, out var known)) return known;
                    var name = ShortName(key);
                    if (_names.ContainsValue(name)) name = key;
                    _names[key] = name;
                    _named.Add((name, def));
                    return name;
                }

                /// <summary>"com.IvanMurzak.ReflectorNet.Model.SerializedMember" → "SerializedMember", "Tool_X-Mode" → "Mode".</summary>
                private static string ShortName(string key)
                {
                    var cut = key.LastIndexOfAny(new[] { '.', '-', '+' });
                    return cut >= 0 && cut < key.Length - 1 ? key.Substring(cut + 1) : key;
                }

                /// <summary>Inline form of an anonymous object type: <c>{name type, …}</c>.</summary>
                private string Record(JsonObject node, int depth) => "{" + string.Join(", ", Members(node, depth)) + "}";

                private IEnumerable<string> Members(JsonObject node, int depth)
                {
                    var required = node["required"] is JsonArray r ? new HashSet<string>(r.Select(x => x?.GetValue<string>() ?? "")) : new HashSet<string>();
                    foreach (var kv in (JsonObject)node["properties"]!)
                    {
                        if (!(kv.Value is JsonObject member)) continue;
                        var part = kv.Key + " " + Type(member, depth);
                        if (required.Contains(kv.Key)) part += " required";
                        if (_memberDescriptions && Str(member, "description") is string d && d.Length > 0) part += " (" + d.Trim() + ")";
                        yield return part;
                    }
                }

                /// <summary>Every named type reached so far, one per line (named types found while writing are appended).</summary>
                public string TypeBlock()
                {
                    if (_named.Count == 0) return "";
                    var sb = new StringBuilder(TypesHeader).Append('\n');
                    for (int i = 0; i < _named.Count; i++)
                    {
                        var (name, def) = _named[i];
                        sb.Append(name).Append(' ').Append(Record(def, 1));
                        if (Str(def, "description") is string d && d.Length > 0) sb.Append(" — ").Append(d.Trim());
                        sb.Append('\n');
                    }
                    return sb.ToString().TrimEnd();
                }
            }
        }
    }
}
