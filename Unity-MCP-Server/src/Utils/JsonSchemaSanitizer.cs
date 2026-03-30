/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace com.IvanMurzak.Unity.MCP.Server.Utils
{
    /// <summary>
    /// Sanitizes JSON Schema <c>$defs</c> keys and <c>$ref</c> values so that
    /// C#-style type names (arrays, generics, nested types) become valid,
    /// deterministic, URI-safe identifiers compatible with strict MCP clients.
    /// </summary>
    public static class JsonSchemaSanitizer
    {
        private const string DefsProperty = "$defs";
        private const string RefProperty  = "$ref";
        private const string RefPrefix    = "#/$defs/";

        /// <summary>
        /// Sanitizes a raw C# type name into a safe schema-definition identifier.
        /// The result contains only <c>[a-zA-Z0-9._-]</c> characters.
        /// </summary>
        public static string? SanitizeTypeName(string? typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return typeName;

            var result = typeName;

            // Order matters — replace multi-char sequences before their constituents.
            result = result.Replace("[]", "_Array");        // array suffix
            result = result.Replace("+", ".");              // nested-type separator → dot
            result = result.Replace(", ", "_And_");         // generic arg separator (with space)
            result = result.Replace(",", "_And_");          // generic arg separator (no space)
            result = result.Replace("<", "_Of_");           // generic open bracket
            result = result.Replace(">", "");               // generic close bracket (drop)

            // Replace any remaining URI-unsafe characters with underscore.
            result = Regex.Replace(result, @"[^a-zA-Z0-9._\-]", "_");

            // Collapse runs of underscores and trim leading/trailing underscores.
            result = Regex.Replace(result, @"_{2,}", "_");
            result = result.Trim('_');

            return result;
        }

        /// <summary>
        /// Rewrites every <c>$defs</c> key and matching <c>$ref</c> value in the
        /// given JSON Schema element so that identifiers are sanitized.
        /// Returns the original element unchanged when no rewriting is needed.
        /// </summary>
        public static JsonElement SanitizeSchema(JsonElement schema)
        {
            if (schema.ValueKind != JsonValueKind.Object)
                return schema;

            // Fast-path: skip allocation when there is no $defs or all keys are already safe.
            if (!NeedsSanitization(schema))
                return schema;

            var node = JsonNode.Parse(schema.GetRawText());
            if (node is not JsonObject rootObj)
                return schema;

            // Collect the old→new key mapping from the top-level $defs.
            var mapping = BuildDefsMapping(rootObj);
            if (mapping.Count == 0)
                return schema; // nothing to change

            // Apply the mapping: rewrite $defs keys, then patch every $ref.
            RewriteDefs(rootObj, mapping);
            RewriteRefs(rootObj, mapping);

            // Round-trip back to JsonElement.
            return JsonSerializer.Deserialize<JsonElement>(rootObj.ToJsonString());
        }

        /// <summary>
        /// Lightweight check using <see cref="JsonElement"/> (no allocation) to
        /// determine whether any <c>$defs</c> key contains unsafe characters.
        /// </summary>
        private static bool NeedsSanitization(JsonElement schema)
        {
            if (!schema.TryGetProperty(DefsProperty, out var defs))
                return false;

            if (defs.ValueKind != JsonValueKind.Object)
                return false;

            foreach (var property in defs.EnumerateObject())
            {
                if (SanitizeTypeName(property.Name) != property.Name)
                    return true;
            }

            return false;
        }

        // -----------------------------------------------------------------
        //  Internal helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Scans the <c>$defs</c> object and builds a dictionary mapping each
        /// original key that requires sanitization to its sanitized form.
        /// Handles collisions by appending a numeric suffix.
        /// </summary>
        internal static Dictionary<string, string> BuildDefsMapping(JsonObject root)
        {
            var mapping = new Dictionary<string, string>();

            if (!root.TryGetPropertyValue(DefsProperty, out var defsNode) || defsNode is not JsonObject defs)
                return mapping;

            // Track sanitized names already in use (including keys that don't change).
            var usedNames = new HashSet<string>();
            var keysToSanitize = new List<(string original, string sanitized)>();

            foreach (var kvp in defs)
            {
                var original  = kvp.Key;
                var sanitized = SanitizeTypeName(original)!;

                if (sanitized == original)
                {
                    usedNames.Add(sanitized); // unchanged key occupies its slot
                }
                else
                {
                    keysToSanitize.Add((original, sanitized));
                }
            }

            foreach (var (original, sanitized) in keysToSanitize)
            {
                var unique = sanitized;
                var counter = 2;
                while (!usedNames.Add(unique))
                {
                    unique = $"{sanitized}_{counter}";
                    counter++;
                }
                mapping[original] = unique;
            }

            return mapping;
        }

        /// <summary>
        /// Replaces <c>$defs</c> keys according to the mapping.
        /// </summary>
        private static void RewriteDefs(JsonObject root, Dictionary<string, string> mapping)
        {
            if (!root.TryGetPropertyValue(DefsProperty, out var defsNode) || defsNode is not JsonObject defs)
                return;

            // Collect entries to move (can't modify dictionary while iterating).
            var entries = new List<(string oldKey, string newKey, JsonNode? value)>();
            foreach (var (oldKey, newKey) in mapping)
            {
                if (defs.ContainsKey(oldKey))
                {
                    var value = defs[oldKey];
                    entries.Add((oldKey, newKey, value));
                }
            }

            foreach (var (oldKey, newKey, value) in entries)
            {
                defs.Remove(oldKey);
                // DeepClone because removing detaches the node from its parent.
                defs[newKey] = value?.DeepClone();
            }
        }

        /// <summary>
        /// Recursively walks the JSON tree and rewrites every <c>$ref</c> value
        /// whose referenced name appears in the mapping.
        /// </summary>
        private static void RewriteRefs(JsonNode? node, Dictionary<string, string> mapping)
        {
            if (node is JsonObject obj)
            {
                // Check for a $ref that points into $defs.
                if (obj.TryGetPropertyValue(RefProperty, out var refNode)
                    && refNode is JsonValue refValue
                    && refValue.TryGetValue<string>(out var refStr)
                    && refStr != null
                    && refStr.StartsWith(RefPrefix, StringComparison.Ordinal))
                {
                    var key = refStr.Substring(RefPrefix.Length);
                    if (mapping.TryGetValue(key, out var newKey))
                    {
                        obj[RefProperty] = RefPrefix + newKey;
                    }
                }

                // Recurse into all child properties (including $defs values).
                foreach (var kvp in obj.ToList())
                {
                    RewriteRefs(kvp.Value, mapping);
                }
            }
            else if (node is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    RewriteRefs(item, mapping);
                }
            }
        }
    }
}
