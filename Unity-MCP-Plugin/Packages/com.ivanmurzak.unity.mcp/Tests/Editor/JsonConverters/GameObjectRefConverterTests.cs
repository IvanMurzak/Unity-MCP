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
using com.IvanMurzak.Unity.MCP.Runtime.Data;
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
    }
}
#endif
