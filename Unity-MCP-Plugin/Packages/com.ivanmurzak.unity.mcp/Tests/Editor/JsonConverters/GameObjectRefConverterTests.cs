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
#if UNITY_6000_5_OR_NEWER
using System;
using System.Collections;
using System.Globalization;
using System.Text.Json;
using AIGD;
using com.IvanMurzak.Unity.MCP.JsonConverters;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.JsonConverter
{
    using com.IvanMurzak.Unity.MCP.Editor.Tests;

    public class GameObjectRefConverterTests : BaseTest
    {
        static void RoundTrip(GameObjectRef source, Action<GameObjectRef> assertions)
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");

            var json = reflector.JsonSerializer.Serialize(source);
            Assert.IsFalse(string.IsNullOrEmpty(json), "Serialized JSON should not be empty.");

            var deserialized = reflector.JsonSerializer.Deserialize<GameObjectRef>(json);
            Assert.IsNotNull(deserialized, $"Deserialized GameObjectRef should not be null. JSON: {json}");

            assertions(deserialized!);
        }

        [UnityTest]
        public IEnumerator GameObjectRef_InstanceID_RoundTrip()
        {
            var go = new GameObject("TestGO_InstanceID");
            try
            {
                var source = new GameObjectRef(go);
                var expectedEntityId = go.GetEntityId();
                Assert.AreNotEqual(UnityEngine.EntityId.None, expectedEntityId, "Created GameObject should have a valid EntityId.");

                RoundTrip(source, deserialized =>
                {
                    Assert.AreEqual(expectedEntityId, deserialized.InstanceID,
                        $"InstanceID should round-trip. Expected '{expectedEntityId}', got '{deserialized.InstanceID}'.");
                });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameObjectRef_EmptyInstanceID_RoundTrip()
        {
            var source = new GameObjectRef();
            RoundTrip(source, deserialized =>
            {
                Assert.AreEqual(UnityEngine.EntityId.None, deserialized.InstanceID,
                    "Empty GameObjectRef should round-trip with EntityId.None.");
            });
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameObjectRef_LargeEntityId_RoundTrip()
        {
            // ulong value larger than int.MaxValue to ensure ulong serialization path works
            var largeId = UnityEngine.EntityId.FromULong(9_223_372_036_854_775_000UL);
            var source = new GameObjectRef(largeId);

            RoundTrip(source, deserialized =>
            {
                Assert.AreEqual(largeId, deserialized.InstanceID,
                    $"Large EntityId should round-trip. Expected '{largeId}', got '{deserialized.InstanceID}'.");
            });
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameObjectRef_ByName_RoundTrip()
        {
            var source = new GameObjectRef { Name = "MyGameObject" };

            RoundTrip(source, deserialized =>
            {
                Assert.AreEqual("MyGameObject", deserialized.Name);
                Assert.AreEqual(UnityEngine.EntityId.None, deserialized.InstanceID);
            });
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameObjectRef_ByPath_RoundTrip()
        {
            var source = new GameObjectRef { Path = "Parent/Child/Leaf" };

            RoundTrip(source, deserialized =>
            {
                Assert.AreEqual("Parent/Child/Leaf", deserialized.Path);
                Assert.AreEqual(UnityEngine.EntityId.None, deserialized.InstanceID);
            });
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameObjectRef_AllFields_RoundTrip()
        {
            var go = new GameObject("TestGO_AllFields");
            try
            {
                var source = new GameObjectRef(go)
                {
                    Path = "Root/TestGO_AllFields",
                    Name = "TestGO_AllFields"
                };

                RoundTrip(source, deserialized =>
                {
                    Assert.AreEqual(go.GetEntityId(), deserialized.InstanceID);
                    Assert.AreEqual(source.Path, deserialized.Path);
                    Assert.AreEqual(source.Name, deserialized.Name);
                });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
            yield return null;
        }

        // ── #759 wire-format tests ────────────────────────────────────
        // Outbound: instanceID must be a JSON string (not a JSON number),
        // so JS clients can JSON.parse without IEEE-754 precision loss.
        // Inbound: accepts both string (preferred) and number (back-compat).

        [Test]
        public void GameObjectRef_InstanceID_BeyondJsSafeInteger_SurvivesJsParse()
        {
            // Pick a value > 2^53 - 1 (the JS safe-integer ceiling) so any
            // number-based serialization would round it. Build an EntityId
            // whose raw ulong is exactly this magnitude to prove the wire
            // emits decimal digits unmodified.
            const ulong RawId = 568_105_584_918_935_294UL;
            var entityId = UnityEngine.EntityId.FromULong(RawId);
            var source = new GameObjectRef(entityId);

            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var json = reflector.JsonSerializer.Serialize(source);
            Assert.IsFalse(string.IsNullOrEmpty(json), "Serialized JSON should not be empty.");

            using var doc = JsonDocument.Parse(json);
            Assert.IsTrue(doc.RootElement.TryGetProperty("instanceID", out var instanceIdElement),
                $"Serialized JSON must contain 'instanceID' property. JSON: {json}");
            Assert.AreEqual(JsonValueKind.String, instanceIdElement.ValueKind,
                $"'instanceID' must serialize as a JSON string (the #759 wire contract), not {instanceIdElement.ValueKind}. JSON: {json}");

            var stringValue = instanceIdElement.GetString();
            Assert.AreEqual(RawId.ToString(CultureInfo.InvariantCulture), stringValue,
                $"'instanceID' string must be the exact decimal representation of the raw EntityId ulong. JSON: {json}");
        }

        [Test]
        public void GameObjectRef_InstanceID_FromJsonString_Parses()
        {
            // Inbound: a JSON string value of decimal digits must deserialize
            // back to the original raw ulong with no precision loss.
            const ulong RawId = 568_105_584_918_935_294UL;
            var json = $"{{\"instanceID\":\"{RawId.ToString(CultureInfo.InvariantCulture)}\"}}";

            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var deserialized = reflector.JsonSerializer.Deserialize<GameObjectRef>(json);
            Assert.IsNotNull(deserialized, $"Deserialized GameObjectRef should not be null. JSON: {json}");

            Assert.AreEqual(UnityEngine.EntityId.FromULong(RawId), deserialized!.InstanceID,
                $"InstanceID must round-trip exactly when read from a JSON string. JSON: {json}");
        }

        [Test]
        public void GameObjectRef_InstanceID_FromJsonNumber_StillParses()
        {
            // Inbound back-compat: legacy clients pre-#759 may still post a
            // JSON number for instanceID. That path MUST still round-trip,
            // even past the JS-safe-integer boundary (the .NET STJ parser
            // itself is precision-clean; the rounding only happens in JS).
            const ulong RawId = 568_105_584_918_935_294UL;
            var json = $"{{\"instanceID\":{RawId.ToString(CultureInfo.InvariantCulture)}}}";

            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var deserialized = reflector.JsonSerializer.Deserialize<GameObjectRef>(json);
            Assert.IsNotNull(deserialized, $"Deserialized GameObjectRef should not be null. JSON: {json}");

            Assert.AreEqual(UnityEngine.EntityId.FromULong(RawId), deserialized!.InstanceID,
                $"InstanceID must round-trip exactly when read from a JSON number (back-compat). JSON: {json}");
        }

        [Test]
        public void GameObjectRef_InstanceID_FromJsonString_WithZero_Parses()
        {
            // The string "0" represents the null entity (EntityId.None) —
            // same convention as legacy number 0.
            const string Json = "{\"instanceID\":\"0\"}";

            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var deserialized = reflector.JsonSerializer.Deserialize<GameObjectRef>(Json);
            Assert.IsNotNull(deserialized, $"Deserialized GameObjectRef should not be null. JSON: {Json}");

            Assert.AreEqual(UnityEngine.EntityId.None, deserialized!.InstanceID,
                $"InstanceID '\"0\"' must deserialize to EntityId.None. JSON: {Json}");
        }

        [Test]
        public void EntityIdConverter_Schema_DeclaresString()
        {
            // Schema MUST advertise the new string wire format so AI clients
            // that consume the schema (Claude Agent SDK, etc.) generate
            // correctly-typed tool calls and validators reject any number-
            // shaped payload.
            var converter = new EntityIdConverter();
            var schema = converter.GetSchema();

            Assert.IsNotNull(schema, "GetSchema() must not return null.");
            var schemaJson = schema.ToJsonString();

            using var doc = JsonDocument.Parse(schemaJson);
            Assert.IsTrue(doc.RootElement.TryGetProperty("type", out var typeElement),
                $"Schema must declare a 'type' field. Schema: {schemaJson}");
            Assert.AreEqual(JsonValueKind.String, typeElement.ValueKind,
                $"Schema 'type' must be a JSON string. Schema: {schemaJson}");
            Assert.AreEqual("string", typeElement.GetString(),
                $"Schema 'type' must be 'string' (the #759 wire contract). Schema: {schemaJson}");

            Assert.IsTrue(doc.RootElement.TryGetProperty("pattern", out var patternElement),
                $"Schema must declare a 'pattern' field for the string format. Schema: {schemaJson}");
            Assert.AreEqual(JsonValueKind.String, patternElement.ValueKind,
                $"Schema 'pattern' must be a JSON string. Schema: {schemaJson}");
            Assert.IsFalse(string.IsNullOrEmpty(patternElement.GetString()),
                $"Schema 'pattern' must not be empty. Schema: {schemaJson}");
        }
    }
}
#endif
