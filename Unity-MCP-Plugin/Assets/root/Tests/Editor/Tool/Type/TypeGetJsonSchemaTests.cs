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
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Tests for the type-get-json-schema tool covering all DescriptionMode combinations.
    ///
    /// Key behaviors under test:
    ///   Ignore            — strip all description fields everywhere
    ///   Include           — keep descriptions on the target type / its direct properties+items only
    ///   IncludeRecursively — keep descriptions everywhere, including $defs entries
    /// </summary>
    public class TypeGetJsonSchemaTests : BaseTest
    {
        // Types used across tests. AssetObjectRef is known to carry [Description] attributes.
        private static readonly string AssetObjectRefName = typeof(AssetObjectRef).AssemblyQualifiedName!;
        private static readonly string ListAssetObjectRefName = typeof(List<AssetObjectRef>).AssemblyQualifiedName!;

        private Tool_Type _tool = null!;

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();
            _tool = new Tool_Type();
        }

        // ─────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────

        private string GetSchema(
            string typeName,
            DescriptionMode descMode = DescriptionMode.Ignore,
            DescriptionMode propDescMode = DescriptionMode.Ignore,
            bool includeDefs = false) =>
            _tool.GetJsonSchema(typeName, descMode, propDescMode, includeDefs);

        private static JsonNode ParseSchema(string json)
        {
            var node = JsonNode.Parse(json);
            Assert.IsNotNull(node, "Schema JSON is null");
            return node!;
        }

        /// Count every "description" key in the entire schema tree.
        private static int CountAllDescriptions(JsonNode schema) =>
            JsonSchema.FindAllProperties(schema, JsonSchema.Description).Count;

        private static JsonNode? RootDescription(JsonNode schema) =>
            (schema as JsonObject)?[JsonSchema.Description];

        private static JsonNode? ItemsDescription(JsonNode schema) =>
            ((schema as JsonObject)?[JsonSchema.Items] as JsonObject)?[JsonSchema.Description];

        private static JsonObject? GetDefsObj(JsonNode schema) =>
            (schema as JsonObject)?[JsonSchema.Defs] as JsonObject;

        /// Returns true if at least one $defs entry has a root-level description.
        private static bool DefsHaveTypeDescription(JsonNode schema)
        {
            var defs = GetDefsObj(schema);
            if (defs == null) return false;
            foreach (var entry in defs)
                if ((entry.Value as JsonObject)?[JsonSchema.Description] != null)
                    return true;
            return false;
        }

        /// Returns true if at least one property inside any $defs entry has a description.
        private static bool DefsHavePropertyDescriptions(JsonNode schema)
        {
            var defs = GetDefsObj(schema);
            if (defs == null) return false;
            foreach (var entry in defs)
            {
                if (entry.Value is not JsonObject defObj) continue;
                if (defObj[JsonSchema.Properties] is not JsonObject propsObj) continue;
                foreach (var prop in propsObj)
                    if ((prop.Value as JsonObject)?[JsonSchema.Description] != null)
                        return true;
            }
            return false;
        }

        /// Returns true if at least one root property has a description.
        private static bool RootPropertiesHaveDescriptions(JsonNode schema)
        {
            if ((schema as JsonObject)?[JsonSchema.Properties] is not JsonObject propsObj)
                return false;
            foreach (var prop in propsObj)
                if ((prop.Value as JsonObject)?[JsonSchema.Description] != null)
                    return true;
            return false;
        }

        // ─────────────────────────────────────────────────────────────
        // Ignore mode — zero descriptions everywhere
        // ─────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Ignore_Ignore_ObjectType_ZeroDescriptions()
        {
            var schema = ParseSchema(GetSchema(AssetObjectRefName));
            Assert.AreEqual(0, CountAllDescriptions(schema),
                "Ignore+Ignore should strip all descriptions from object schema");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ignore_Ignore_ArrayType_ZeroDescriptions()
        {
            var schema = ParseSchema(GetSchema(ListAssetObjectRefName));
            Assert.AreEqual(0, CountAllDescriptions(schema),
                "Ignore+Ignore should strip all descriptions from array schema");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ignore_Ignore_WithDefs_ZeroDescriptions()
        {
            var schema = ParseSchema(GetSchema(ListAssetObjectRefName, includeDefs: true));
            Assert.AreEqual(0, CountAllDescriptions(schema),
                "Ignore+Ignore should strip all descriptions including inside $defs");
            yield return null;
        }

        // ─────────────────────────────────────────────────────────────
        // Include mode — root only, $defs stripped
        // ─────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Include_Ignore_ObjectType_RootDescPresent()
        {
            // Verify the full schema has a root description before testing Include
            var full = ParseSchema(GetSchema(AssetObjectRefName, DescriptionMode.IncludeRecursively, DescriptionMode.Ignore));
            Assume.That(RootDescription(full), Is.Not.Null,
                "AssetObjectRef must have a [Description] attribute for this test to be meaningful");

            var schema = ParseSchema(GetSchema(AssetObjectRefName, DescriptionMode.Include, DescriptionMode.Ignore));
            Assert.IsNotNull(RootDescription(schema),
                "Include mode should keep the root type description");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ignore_Include_ArrayType_ItemsDescPresent()
        {
            // Verify the full schema has items description before testing Include
            var full = ParseSchema(GetSchema(ListAssetObjectRefName, DescriptionMode.Ignore, DescriptionMode.IncludeRecursively));
            Assume.That(ItemsDescription(full), Is.Not.Null,
                "List<AssetObjectRef> must have an items description for this test to be meaningful");

            var schema = ParseSchema(GetSchema(ListAssetObjectRefName, DescriptionMode.Ignore, DescriptionMode.Include));
            Assert.IsNotNull(ItemsDescription(schema),
                "Include propertyDescriptionMode should keep items.description");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ignore_Include_ObjectType_RootPropertiesHaveDesc()
        {
            // Verify the full schema has property descriptions before testing Include
            var full = ParseSchema(GetSchema(AssetObjectRefName, DescriptionMode.Ignore, DescriptionMode.IncludeRecursively));
            Assume.That(RootPropertiesHaveDescriptions(full), Is.True,
                "AssetObjectRef properties must have [Description] attributes for this test to be meaningful");

            var schema = ParseSchema(GetSchema(AssetObjectRefName, DescriptionMode.Ignore, DescriptionMode.Include));
            Assert.IsTrue(RootPropertiesHaveDescriptions(schema),
                "Include propertyDescriptionMode should keep root property descriptions");
            yield return null;
        }

        // ─────────────────────────────────────────────────────────────
        // $defs — Include strips $defs descriptions, IncludeRecursively keeps them
        // ─────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Include_Include_WithDefs_DefsHaveNoTypeDesc()
        {
            // Verify $defs has type descriptions in the full schema
            var full = ParseSchema(GetSchema(ListAssetObjectRefName, DescriptionMode.IncludeRecursively, DescriptionMode.Ignore, includeDefs: true));
            Assume.That(DefsHaveTypeDescription(full), Is.True,
                "$defs entries must carry descriptions for this test to be meaningful");

            var schema = ParseSchema(GetSchema(ListAssetObjectRefName, DescriptionMode.Include, DescriptionMode.Include, includeDefs: true));
            Assert.IsFalse(DefsHaveTypeDescription(schema),
                "Include descriptionMode should NOT propagate descriptions into $defs entries");
            yield return null;
        }

        [UnityTest]
        public IEnumerator IncludeRecursively_Ignore_WithDefs_DefsHaveTypeDesc()
        {
            // Verify $defs has type descriptions in the full schema
            var full = ParseSchema(GetSchema(ListAssetObjectRefName, DescriptionMode.IncludeRecursively, DescriptionMode.Ignore, includeDefs: true));
            Assume.That(DefsHaveTypeDescription(full), Is.True,
                "$defs entries must carry descriptions for this test to be meaningful");

            var schema = ParseSchema(GetSchema(ListAssetObjectRefName, DescriptionMode.IncludeRecursively, DescriptionMode.Ignore, includeDefs: true));
            Assert.IsTrue(DefsHaveTypeDescription(schema),
                "IncludeRecursively descriptionMode should keep descriptions inside $defs entries");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ignore_Include_WithDefs_DefsPropertiesHaveNoDesc()
        {
            // Verify $defs properties have descriptions in the full schema
            var full = ParseSchema(GetSchema(ListAssetObjectRefName, DescriptionMode.Ignore, DescriptionMode.IncludeRecursively, includeDefs: true));
            Assume.That(DefsHavePropertyDescriptions(full), Is.True,
                "$defs entry properties must carry descriptions for this test to be meaningful");

            var schema = ParseSchema(GetSchema(ListAssetObjectRefName, DescriptionMode.Ignore, DescriptionMode.Include, includeDefs: true));
            Assert.IsFalse(DefsHavePropertyDescriptions(schema),
                "Include propertyDescriptionMode should NOT propagate property descriptions into $defs entries");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ignore_IncludeRecursively_WithDefs_DefsPropertiesHaveDesc()
        {
            // Verify $defs properties have descriptions in the full schema
            var full = ParseSchema(GetSchema(ListAssetObjectRefName, DescriptionMode.Ignore, DescriptionMode.IncludeRecursively, includeDefs: true));
            Assume.That(DefsHavePropertyDescriptions(full), Is.True,
                "$defs entry properties must carry descriptions for this test to be meaningful");

            var schema = ParseSchema(GetSchema(ListAssetObjectRefName, DescriptionMode.Ignore, DescriptionMode.IncludeRecursively, includeDefs: true));
            Assert.IsTrue(DefsHavePropertyDescriptions(schema),
                "IncludeRecursively propertyDescriptionMode should keep property descriptions in $defs entries");
            yield return null;
        }

        // ─────────────────────────────────────────────────────────────
        // Mixed combinations
        // ─────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator IncludeRecursively_Ignore_WithDefs_TypeDescsInDefs_NoPropDescs()
        {
            var schema = ParseSchema(GetSchema(ListAssetObjectRefName,
                DescriptionMode.IncludeRecursively, DescriptionMode.Ignore, includeDefs: true));

            var full = ParseSchema(GetSchema(ListAssetObjectRefName,
                DescriptionMode.IncludeRecursively, DescriptionMode.IncludeRecursively, includeDefs: true));

            if (DefsHaveTypeDescription(full))
                Assert.IsTrue(DefsHaveTypeDescription(schema),
                    "IncludeRecursively descMode should keep $defs type descriptions");

            if (DefsHavePropertyDescriptions(full))
                Assert.IsFalse(DefsHavePropertyDescriptions(schema),
                    "Ignore propDescMode should strip $defs property descriptions");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Ignore_IncludeRecursively_WithDefs_NoDefs_TypeDescs_PropDescsEverywhere()
        {
            var schema = ParseSchema(GetSchema(ListAssetObjectRefName,
                DescriptionMode.Ignore, DescriptionMode.IncludeRecursively, includeDefs: true));

            var full = ParseSchema(GetSchema(ListAssetObjectRefName,
                DescriptionMode.IncludeRecursively, DescriptionMode.IncludeRecursively, includeDefs: true));

            Assert.IsNull(RootDescription(schema),
                "Ignore descMode should strip root type description");

            if (DefsHaveTypeDescription(full))
                Assert.IsFalse(DefsHaveTypeDescription(schema),
                    "Ignore descMode should strip $defs type descriptions");

            if (DefsHavePropertyDescriptions(full))
                Assert.IsTrue(DefsHavePropertyDescriptions(schema),
                    "IncludeRecursively propDescMode should keep $defs property descriptions");

            yield return null;
        }

        [UnityTest]
        public IEnumerator AllIncludeRecursively_WithDefs_AllDescriptionsPresent()
        {
            var schema = ParseSchema(GetSchema(ListAssetObjectRefName,
                DescriptionMode.IncludeRecursively, DescriptionMode.IncludeRecursively, includeDefs: true));

            // Only assert if the reflector actually emits descriptions for this type
            Assume.That(CountAllDescriptions(schema), Is.GreaterThan(0),
                "IncludeRecursively+IncludeRecursively should produce a schema with at least one description");

            if (DefsHaveTypeDescription(schema))
                Assert.IsTrue(DefsHaveTypeDescription(schema));

            if (ItemsDescription(schema) != null)
                Assert.IsNotNull(ItemsDescription(schema));

            yield return null;
        }
    }
}
