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

        public const string TypesHeader =
            "Argument types — give these arguments as RQL values (as in rql-mutate): {key: value, …}, [a, b], \"text\", 1.5, true, " +
            "ref(\"GameObject#12\") from a query result, unity.find(\"Name\"); leave out keys you do not need:";

        private readonly ConcurrentDictionary<MethodInfo, ToolSignature> _signatures = new ConcurrentDictionary<MethodInfo, ToolSignature>();
        private readonly ConcurrentDictionary<Type, string> _typeRefs = new ConcurrentDictionary<Type, string>();

        private static RqlEngine Engine => RqlUnity.Engine;

        // ------------------------------------------------------------ schema

        public ToolSignature? Describe(string toolName, MethodInfo method, string? description) =>
            _signatures.GetOrAdd(method, m => com.IvanMurzak.ReflectorNet.Utils.MainThread.Instance.Run(() => Build(m, description)));

        /// <summary>Forget cached signatures (after modules / adapters change).</summary>
        public void Reset()
        {
            _signatures.Clear();
            _typeRefs.Clear();
        }

        private ToolSignature Build(MethodInfo method, string? description)
        {
            var writer = new TypeWriter(Engine);
            var properties = new JsonObject();
            var required = new JsonArray();
            foreach (var p in AgentParameters(method))
            {
                var type = Nullable.GetUnderlyingType(p.ParameterType) ?? p.ParameterType;
                var text = (p.GetCustomAttribute<RqlArgumentAttribute>()?.Description ?? p.GetCustomAttribute<DescriptionAttribute>()?.Description)?.Trim();
                var schema = Simple(type);
                if (schema == null)
                {
                    schema = new JsonObject { ["type"] = "string" };
                    text = (text == null ? "" : text + " ") + "RQL: " + writer.Ref(type);
                }
                if (!string.IsNullOrEmpty(text)) schema["description"] = text;
                properties[p.Name!] = schema;
                if (!p.HasDefaultValue && !p.IsOptional && Nullable.GetUnderlyingType(p.ParameterType) == null) required.Add(p.Name);
            }
            var block = writer.Block();
            foreach (var kv in properties)
                if (kv.Value is JsonObject schema && schema["description"] is JsonValue d && d.TryGetValue<string>(out var text))
                    schema["description"] = writer.Final(text);
            var input = new JsonObject { ["type"] = "object", ["properties"] = properties };
            if (required.Count > 0) input["required"] = required;
            var full = block.Length == 0 ? description : string.IsNullOrWhiteSpace(description) ? block : description!.TrimEnd() + "\n\n" + block;
            return new ToolSignature(full, input, outputSchema: null);
        }

        private static IEnumerable<ParameterInfo> AgentParameters(MethodInfo method) =>
            method.GetParameters().Where(p => p.GetCustomAttribute<RequestIDAttribute>() == null && p.ParameterType != typeof(CancellationToken));

        /// <summary>Plain JSON schema for text, numbers, bools, enums and lists of those; null for anything else.</summary>
        private static JsonObject? Simple(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            if (t == typeof(string) || t == typeof(char)) return new JsonObject { ["type"] = "string" };
            if (t == typeof(bool)) return new JsonObject { ["type"] = "boolean" };
            if (t.IsEnum) return new JsonObject { ["type"] = "string", ["enum"] = new JsonArray(Enum.GetNames(t).Select(n => (JsonNode)JsonValue.Create(n)!).ToArray()) };
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Byte: case TypeCode.SByte: case TypeCode.Int16: case TypeCode.UInt16:
                case TypeCode.Int32: case TypeCode.UInt32: case TypeCode.Int64: case TypeCode.UInt64:
                    return new JsonObject { ["type"] = "integer" };
                case TypeCode.Single: case TypeCode.Double: case TypeCode.Decimal:
                    return new JsonObject { ["type"] = "number" };
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
                if (t == typeof(string) || t == typeof(char)) return "string";
                if (t == typeof(bool)) return "bool";
                if (t == typeof(object) || t == typeof(JsonElement) || t == typeof(JsonNode)) return "any";
                if (t.IsEnum) return string.Join("|", Enum.GetNames(t));
                switch (Type.GetTypeCode(t))
                {
                    case TypeCode.Byte: case TypeCode.SByte: case TypeCode.Int16: case TypeCode.UInt16:
                    case TypeCode.Int32: case TypeCode.UInt32: case TypeCode.Int64: case TypeCode.UInt64:
                        return "int";
                    case TypeCode.Single: case TypeCode.Double: case TypeCode.Decimal:
                        return "float";
                }
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

            private string Resolve(string text)
            {
                foreach (var kv in _names) text = text.Replace(Placeholder(kv.Key), kv.Value);
                // A type first met while writing the block (beyond the discovery depth): its plain short name.
                return System.Text.RegularExpressions.Regex.Replace(text, "\u0001([^\u0002]*)\u0002",
                    m => Type.GetType(m.Groups[1].Value) is Type t ? TypeModel.FriendlyName(t) : "any");
            }

            public string Final(string inline) => Resolve(inline);
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
        private string TypeText(Type type) => _typeRefs.GetOrAdd(type, t =>
        {
            var writer = new TypeWriter(Engine);
            var inline = writer.Ref(t);
            var block = writer.Block();
            var name = writer.Final(inline);
            var line = block.Split('\n').Skip(1).FirstOrDefault(l => l.StartsWith(name + " ", StringComparison.Ordinal));
            return line ?? name;
        });

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
                    response.Content = new List<ContentBlock> { Text(RqlToolPresentation.Compact.Render(node)) };
                    return response;
                case string s:
                    return ResponseCallTool.Success(s);
                case SerializedMember member:
                    return ResponseCallTool.Success(Render(member));
                default:
                    return ResponseCallTool.Success(com.IvanMurzak.ReflectorNet.Utils.MainThread.Instance.Run(() => AgentFormat.Render(Engine.Present(value), OutputFormat.Table)));
            }
        }

        private static ContentBlock Text(string text) => new ContentBlock { Type = com.IvanMurzak.McpPlugin.Common.Consts.ContentType.Text, Text = text };

        /// <summary>script-execute's result: "System.Int32: 42"; nested fields / properties as compact text.</summary>
        private static string Render(SerializedMember member)
        {
            var value = member.valueJsonElement is JsonElement e && e.ValueKind != JsonValueKind.Undefined ? JsonNode.Parse(e.GetRawText()) : null;
            var text = member.typeName + ": " + RqlToolPresentation.Compact.Render(value);
            if (member.fields is { Count: > 0 } || member.props is { Count: > 0 })
            {
                var nested = new JsonObject();
                foreach (var f in (member.fields ?? new SerializedMemberList()).Concat(member.props ?? new SerializedMemberList()))
                    nested[f.name ?? "?"] = f.valueJsonElement is JsonElement fe ? JsonNode.Parse(fe.GetRawText()) : null;
                text += "\n" + RqlToolPresentation.Compact.Render(nested);
            }
            return text;
        }
    }
}
