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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet.Model;
using Rql;
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.ToolFormat
{
    /// <summary>
    /// EXPERIMENTAL phase 2: RQL is the data layer of every tool (McpPlugin <see cref="IToolBinding"/>), in place of
    /// ReflectorNet. One type model, one set of adapters and one literal syntax for rql-query / rql-mutate and for every
    /// other tool:
    /// <list type="bullet">
    /// <item>schema: simple parameters (text, number, bool, enum, lists of those) are plain JSON schema; every other parameter
    /// is an RQL value given as text, its type described once per tool from RQL's type model and adapters;</item>
    /// <item>arguments: evaluated by RQL (literals, <c>ref("GameObject#12")</c>, <c>unity.find("Name")</c>) and coerced with
    /// the same rules as <c>set</c>;</item>
    /// <item>results: returned values are shown as RQL query results; JSON results as compact text.</item>
    /// </list>
    /// </summary>
    public sealed class RqlToolBinding : IToolBinding
    {
        public static readonly RqlToolBinding Instance = new RqlToolBinding();

        /// <summary>
        /// Install the data layer the configuration asks for on <paramref name="tools"/>: <see cref="ToolPresentationFormat.Rql"/>
        /// = this binding, <see cref="ToolPresentationFormat.Json"/> = none (ReflectorNet).
        /// </summary>
        public static void Apply(IToolManager? tools, ToolPresentationFormat format, ToolTextEscaping escaping)
        {
            Instance.Escaping = escaping;
            if (tools == null) return;
            if (format == ToolPresentationFormat.Rql) Instance.Prebuild(tools);
            tools.ToolBinding = format == ToolPresentationFormat.Rql ? Instance : null;
        }

        /// <summary>
        /// Build every tool's signature now, on the calling (main) thread, before the binding is installed. McpPlugin asks
        /// for a signature inside a locked Lazy, and building one waits for the main thread: a tools/list on a background
        /// thread would otherwise hold that lock while the main thread (a tools window reading the schema) waits on it.
        /// Prebuilt signatures come from the cache without touching the main thread.
        /// </summary>
        private void Prebuild(IToolManager tools)
        {
            foreach (var tool in tools.GetAllTools())
                if (tool is RunTool run && run.Method != null)
                {
                    // The class member is ReflectorNet's description, the one McpPlugin passes. A failure here is left to
                    // McpPlugin's guarded Describe (it logs and keeps ReflectorNet's signature for that tool).
                    try { Describe(run.Name, run.Method, run.Description); }
                    catch (Exception) { }
                }
        }

        private CompactText _compact = new CompactText(ToolTextEscaping.Minimal);

        /// <summary>How quoted text in results is escaped (Minimal keeps non-ASCII and most characters as they are).</summary>
        public ToolTextEscaping Escaping
        {
            get => _compact.Escaping;
            set { if (value != _compact.Escaping) _compact = new CompactText(value); }
        }

        private const string TypesHeader =
            "Argument types — give these arguments as RQL values (as in rql-mutate): {key: value, …}, [a, b], \"text\", 1.5, true, " +
            "ref(\"GameObject#12\") from a query result, unity.find(\"Name\"); leave out keys you do not need:";

        private readonly ConcurrentDictionary<MethodInfo, ToolSignature> _signatures = new ConcurrentDictionary<MethodInfo, ToolSignature>();

        private static RqlEngine Engine => RqlUnity.Engine;

        // ------------------------------------------------------------ schema

        public ToolSignature? Describe(string toolName, MethodInfo method, string? description) =>
            _signatures.GetOrAdd(method, m => com.IvanMurzak.ReflectorNet.Utils.MainThread.Instance.Run(() => Build(m, description)));

        /// <summary>Forget cached signatures (after modules / adapters change).</summary>
        public void Reset()
        {
            _signatures.Clear();
        }

        private ToolSignature Build(MethodInfo method, string? description)
        {
            var writer = new TypeWriter(Engine);
            var properties = new JsonObject();
            var required = new JsonArray();
            foreach (var p in AgentParameters(method))
            {
                var nullable = Nullable.GetUnderlyingType(p.ParameterType) != null;
                var text = (p.GetCustomAttribute<RqlArgumentAttribute>()?.Description ?? p.GetCustomAttribute<DescriptionAttribute>()?.Description)?.Trim();
                var schema = Simple(p.ParameterType);
                if (schema == null)
                {
                    schema = new JsonObject { ["type"] = "string" };
                    text = (text == null ? "" : text + " ") + "RQL: " + writer.Ref(p.ParameterType);
                }
                if (!string.IsNullOrEmpty(text)) schema["description"] = text;
                properties[p.Name!] = schema;
                if (!p.HasDefaultValue && !p.IsOptional && !nullable) required.Add(p.Name);
            }
            var block = writer.Block();
            foreach (var kv in properties)
                if (kv.Value is JsonObject schema && schema["description"] is JsonValue d && d.TryGetValue<string>(out var text))
                    schema["description"] = writer.Resolve(text);
            var input = new JsonObject { ["type"] = "object", ["properties"] = properties };
            if (required.Count > 0) input["required"] = required;
            var full = block.Length == 0 ? description : string.IsNullOrWhiteSpace(description) ? block : description!.TrimEnd() + "\n\n" + block;
            return new ToolSignature(full, input, outputSchema: null);
        }

        private static IEnumerable<ParameterInfo> AgentParameters(MethodInfo method) =>
            method.GetParameters().Where(p => p.GetCustomAttribute<RequestIDAttribute>() == null && p.ParameterType != typeof(CancellationToken));

        private enum Scalar { None, Text, Bool, Enum, Int, Float }

        /// <summary>The scalar kind of <paramref name="t"/> (already unwrapped from Nullable), shared by the schema and the type text.</summary>
        private static Scalar ScalarOf(Type t)
        {
            if (t == typeof(string) || t == typeof(char)) return Scalar.Text;
            if (t == typeof(bool)) return Scalar.Bool;
            if (t.IsEnum) return Scalar.Enum;
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Byte: case TypeCode.SByte: case TypeCode.Int16: case TypeCode.UInt16:
                case TypeCode.Int32: case TypeCode.UInt32: case TypeCode.Int64: case TypeCode.UInt64:
                    return Scalar.Int;
                case TypeCode.Single: case TypeCode.Double: case TypeCode.Decimal:
                    return Scalar.Float;
                default:
                    return Scalar.None;
            }
        }

        /// <summary>Plain JSON schema for text, numbers, bools, enums and lists of those; null for anything else.</summary>
        private static JsonObject? Simple(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            switch (ScalarOf(t))
            {
                case Scalar.Text: return new JsonObject { ["type"] = "string" };
                case Scalar.Bool: return new JsonObject { ["type"] = "boolean" };
                case Scalar.Enum: return new JsonObject { ["type"] = "string", ["enum"] = new JsonArray(Enum.GetNames(t).Select(n => (JsonNode)JsonValue.Create(n)!).ToArray()) };
                case Scalar.Int: return new JsonObject { ["type"] = "integer" };
                case Scalar.Float: return new JsonObject { ["type"] = "number" };
            }
            var element = t.IsArray ? t.GetElementType() : t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>) ? t.GetGenericArguments()[0] : null;
            return element != null && Simple(element) is JsonObject item ? new JsonObject { ["type"] = "array", ["items"] = item } : null;
        }

        /// <summary>Types of one tool as RQL text: inline refs plus a block that describes each named type once.</summary>
        private sealed class TypeWriter
        {
            private readonly RqlEngine _engine;
            private readonly List<Type> _named = new List<Type>();
            private readonly Dictionary<Type, string> _names = new Dictionary<Type, string>();

            public TypeWriter(RqlEngine engine) { _engine = engine; }

            public string Ref(Type t, int depth = 0)
            {
                t = Nullable.GetUnderlyingType(t) ?? t;
                if (depth > 8) return "any";
                switch (ScalarOf(t))
                {
                    case Scalar.Text: return "string";
                    case Scalar.Bool: return "bool";
                    case Scalar.Enum: return string.Join("|", Enum.GetNames(t));
                    case Scalar.Int: return "int";
                    case Scalar.Float: return "float";
                }
                if (t == typeof(object) || t == typeof(JsonElement) || t == typeof(JsonNode)) return "any";
                var info = _engine.Types.Info(t);
                if (info.Description == null)
                {
                    if (info.Kind == TypeKind.List && info.ElementType != null) return "[" + Ref(info.ElementType, depth + 1) + "]";
                    if (info.Kind == TypeKind.Map && info.ElementType != null) return "{" + Ref(info.KeyType ?? typeof(string), depth + 1) + ": " + Ref(info.ElementType, depth + 1) + "}";
                }
                if (!_named.Contains(t)) _named.Add(t);
                if (info.Description == null && !typeof(UnityEngine.Object).IsAssignableFrom(t))
                    foreach (var m in Writable(info)) Ref(m.Type, depth + 1); // name the member types too
                return Placeholder(t);
            }

            // Names are final only when every type is known (clashes rename all types involved): refs use a placeholder.
            private static string Placeholder(Type t) => "\u0001" + t.AssemblyQualifiedName + "\u0002";

            private static IEnumerable<MemberAccessor> Writable(global::Rql.Execution.TypeInfo info) => info.Members.Where(m => m.CanWrite && !m.ExplicitOnly);

            public string Block()
            {
                var assigned = TypeNames.Assign(_named.Select(TypeNames.FullName));
                foreach (var t in _named) _names[t] = assigned[TypeNames.FullName(t)];
                if (_named.Count == 0) return "";
                var sb = new StringBuilder(TypesHeader).Append('\n');
                foreach (var t in _named.ToList())
                {
                    var info = _engine.Types.Info(t);
                    sb.Append(_names[t]);
                    if (info.Description != null) sb.Append(" — ").Append(info.Description);
                    else if (typeof(UnityEngine.Object).IsAssignableFrom(t))
                        sb.Append(" — ref(\"").Append(info.ShortName).Append("#12\") from a query").Append(typeof(UnityEngine.GameObject) == t ? ", unity.find(\"Name\")" : "").Append(" or an asset path \"Assets/…\"");
                    else
                    {
                        sb.Append(" {").Append(string.Join(", ", Writable(info).Select(m =>
                        {
                            var part = m.Name + " " + Ref(m.Type, 1);
                            var d = m.Description ?? m.Member?.GetCustomAttribute<DescriptionAttribute>()?.Description;
                            return string.IsNullOrWhiteSpace(d) ? part : part + " (" + d!.Trim() + ")";
                        }))).Append('}');
                        var typeDescription = t.GetCustomAttribute<DescriptionAttribute>()?.Description;
                        if (!string.IsNullOrWhiteSpace(typeDescription)) sb.Append(" — ").Append(typeDescription!.Trim());
                    }
                    sb.Append('\n');
                }
                return Resolve(sb.ToString().TrimEnd());
            }

            /// <summary>Text with final type names in place of the placeholders (names are final after <see cref="Block"/>).</summary>
            public string Resolve(string text)
            {
                foreach (var kv in _names) text = text.Replace(Placeholder(kv.Key), kv.Value);
                // A type first met while writing the block (beyond the discovery depth): its plain short name.
                return System.Text.RegularExpressions.Regex.Replace(text, "\u0001([^\u0002]*)\u0002",
                    m => Type.GetType(m.Groups[1].Value) is Type t ? TypeModel.FriendlyName(t) : "any");
            }
        }

        // ------------------------------------------------------------ arguments

        public bool TryConvert(MethodInfo method, ParameterInfo parameter, JsonElement value, out object? converted)
        {
            converted = null;
            var type = parameter.ParameterType;
            var target = Nullable.GetUnderlyingType(type) ?? type;
            if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
            {
                if (type.IsValueType && Nullable.GetUnderlyingType(type) == null) return false;
                return true;
            }
            if (target == typeof(string))
            {
                converted = value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
                return true;
            }
            // A JSON string carries RQL text ("3", "Front", "{a: 1}", "ref(\"GameObject#12\")"); other JSON is RQL already.
            var text = value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.GetRawText();
            object? result = null;
            string? error = null;
            var ok = com.IvanMurzak.ReflectorNet.Utils.MainThread.Instance.Run(() =>
                Engine.TryEvaluateValue(text, target, out result, out error)
                // Not an expression (e.g. "Root/Lamp" reads as a division): the text itself is the value.
                || (value.ValueKind == JsonValueKind.String && Engine.TryEvaluateValue(JsonSerializer.Serialize(text), target, out result, out _)));
            if (!ok)
                throw new ArgumentException($"'{parameter.Name}': {error}. Expected {TypeText(target)}");
            converted = result;
            return true;
        }

        /// <summary>One type as the agent sees it in a tool's schema (for error messages).</summary>
        private static string TypeText(Type type)
        {
            var writer = new TypeWriter(Engine);
            var inline = writer.Ref(type);
            var block = writer.Block();
            var name = writer.Resolve(inline);
            var line = block.Split('\n').Skip(1).FirstOrDefault(l => l.StartsWith(name + " ", StringComparison.Ordinal));
            return line ?? name;
        }

        // ------------------------------------------------------------ results

        public ResponseCallTool? Result(MethodInfo method, object? value)
        {
            switch (value)
            {
                case null:
                    return null;
                case ResponseCallTool response:
                    if (response.StructuredContent == null) return null;
                    var node = response.StructuredContent is JsonObject o && o.Count == 1 && o.ContainsKey("result") ? o["result"] : response.StructuredContent;
                    response.StructuredContent = null;
                    response.Content = new List<ContentBlock> { Text(_compact.Render(node)) };
                    return response;
                case string s:
                    return ResponseCallTool.Success(s);
                case SerializedMember member:
                    return ResponseCallTool.Success(Render(member));
                default:
                    return com.IvanMurzak.ReflectorNet.Utils.MainThread.Instance.Run(() =>
                    {
                        var presented = Engine.Present(value);
                        // A value RQL could not present is reported as an error, not as success with error text.
                        if (!presented.Ok) return ResponseCallTool.Error(Show(presented));
                        // One object (not a list) reads as the object itself, without a "1 result" header.
                        return ResponseCallTool.Success(Engine.Types.Info(value.GetType()).Kind != TypeKind.List && presented.Items is { Count: 1 } one
                            ? AgentFormat.Cell(one[0])
                            : Show(presented));
                    });
            }
        }

        /// <summary>
        /// A presented value as text: a list of flat objects as rows ("2 results / name|count / a|1 …", like rql-query
        /// output, keys once instead of on every line); anything else in RQL's agent format.
        /// </summary>
        internal string Show(QueryResult r)
        {
            if (r.Ok && r.Items is { } items && CompactText.FlatColumns(items, v => v != null) is List<string> columns)
            {
                // One column would read like a list of plain values: keep the "{k:v}" item lines then.
                if (columns.Count < 2) return AgentFormat.Render(r, OutputFormat.Table);
                var sb = new StringBuilder();
                sb.Append(items.Count).Append(" results");
                if (r.Total is long total && total > items.Count) sb.Append(" (of ").Append(total).Append(')');
                sb.Append('\n').Append(string.Join("|", columns));
                foreach (JsonObject row in items)
                    sb.Append('\n').Append(string.Join("|", columns.Select(c => row.TryGetPropertyValue(c, out var v) ? _compact.RowCell(v) : "")));
                return sb.ToString();
            }
            return AgentFormat.Render(r, OutputFormat.Table);
        }

        private static ContentBlock Text(string text) => new ContentBlock { Type = com.IvanMurzak.McpPlugin.Common.Consts.ContentType.Text, Text = text };

        /// <summary>script-execute's result: "System.Int32: 42"; nested fields / properties as compact text.</summary>
        private string Render(SerializedMember member)
        {
            var value = member.valueJsonElement is JsonElement e && e.ValueKind != JsonValueKind.Undefined ? JsonSerializer.SerializeToNode(e) : null;
            var text = member.typeName + ": " + _compact.Render(value);
            if (member.fields is { Count: > 0 } || member.props is { Count: > 0 })
            {
                var nested = new JsonObject();
                foreach (var f in (member.fields ?? new SerializedMemberList()).Concat(member.props ?? new SerializedMemberList()))
                    nested[f.name ?? "?"] = f.valueJsonElement is JsonElement fe ? JsonSerializer.SerializeToNode(fe) : null;
                text += "\n" + _compact.Render(nested);
            }
            return text;
        }
    }
}
