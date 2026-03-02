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
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Type
    {
        public const string TypeGetJsonSchemaToolId = "type-get-json-schema";
        [McpPluginTool
        (
            TypeGetJsonSchemaToolId,
            Title = "Type / Get Json Schema",
            ReadOnlyHint = true,
            DestructiveHint = false,
            IdempotentHint = true
        )]
        [Description("Generates a JSON Schema for a given C# type name using reflection. " +
            "Supports primitives, enums, arrays, generic collections, dictionaries, and complex objects. " +
            "The type must be present in any loaded assembly. " +
            "Use the full type name (e.g. 'UnityEngine.Vector3') for best results.")]
        public string GetJsonSchema
        (
            [Description("Full C# type name to generate the schema for. " +
                "Examples: 'System.String', 'UnityEngine.Vector3', 'System.Collections.Generic.List<System.Int32>'. " +
                "Simple names like 'Vector3' are also accepted when unambiguous.")]
            string typeName,

            [Description("Controls the type-level 'description' field. " +
                "Include: keep on the target type only. " +
                "IncludeRecursively: keep on the target type and inside $defs entries. " +
                "Ignore: strip all type-level descriptions. Default: Ignore.")]
            DescriptionMode descriptionMode = DescriptionMode.Ignore,

            [Description("Controls 'description' fields on properties, fields, and array items. " +
                "Include: keep on the target type's own properties/items only. " +
                "IncludeRecursively: keep on all properties/items including those inside $defs entries. " +
                "Ignore: strip all property/item descriptions. Default: Ignore.")]
            DescriptionMode propertyDescriptionMode = DescriptionMode.Ignore,

            [Description("When true, complex nested types are extracted into '$defs' and referenced via '$ref' " +
                "instead of being inlined. Useful for large or recursive types. Default: false.")]
            bool includeNestedTypes = false,

            [Description("Whether to format the output JSON with indentation for readability. Default: false.")]
            bool writeIndented = false
        )
        {
            var type = TypeUtils.GetType(typeName);
            if (type == null)
                throw new ArgumentException($"Type '{typeName}' not found in any loaded assembly. " +
                    "Use the full type name including namespace (e.g. 'UnityEngine.Vector3').", nameof(typeName));

            var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");

            var schema = reflector.GetSchema(type);
            if (schema is JsonObject jsonObj)
            {
                // Root type-level description
                if (descriptionMode == DescriptionMode.Ignore)
                    jsonObj.Remove(JsonSchema.Description);

                if (!includeNestedTypes)
                    jsonObj.Remove(JsonSchema.Defs);
                else
                {
                    if (jsonObj.TryGetPropertyValue(JsonSchema.Defs, out var defsNode) &&
                        defsNode is JsonObject defsObj)
                    {
                        foreach (var def in defsObj)
                        {
                            if (def.Value is not JsonObject defObj)
                                continue;

                            // $defs entry type description: only kept when IncludeRecursively
                            if (descriptionMode != DescriptionMode.IncludeRecursively)
                                defObj.Remove(JsonSchema.Description);

                            // $defs entry property/items descriptions: only kept when IncludeRecursively
                            if (propertyDescriptionMode != DescriptionMode.IncludeRecursively)
                            {
                                if (defObj.TryGetPropertyValue(JsonSchema.Properties, out var defPropsNode) &&
                                    defPropsNode is JsonObject defPropsObj)
                                {
                                    foreach (var prop in defPropsObj)
                                    {
                                        if (prop.Value is JsonObject propObj)
                                            propObj.Remove(JsonSchema.Description);
                                    }
                                }
                                if (defObj.TryGetPropertyValue(JsonSchema.Items, out var defItemsNode) &&
                                    defItemsNode is JsonObject defItemsObj)
                                {
                                    defItemsObj.Remove(JsonSchema.Description);
                                }
                            }
                        }
                    }
                }

                // Root property/items descriptions
                if (propertyDescriptionMode == DescriptionMode.Ignore)
                {
                    if (jsonObj.TryGetPropertyValue(JsonSchema.Properties, out var propertiesNode) &&
                        propertiesNode is JsonObject propertiesObj)
                    {
                        foreach (var prop in propertiesObj)
                        {
                            if (prop.Value is JsonObject propObj)
                                propObj.Remove(JsonSchema.Description);
                        }
                    }
                    if (jsonObj.TryGetPropertyValue(JsonSchema.Items, out var itemsNode) &&
                        itemsNode is JsonObject itemsObj)
                    {
                        itemsObj.Remove(JsonSchema.Description);
                    }
                }
            }
            return schema.ToJsonString(new JsonSerializerOptions { WriteIndented = writeIndented });
        }
    }
}
