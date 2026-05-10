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
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.AtomicApi
{
    /// <summary>
    /// EditMode coverage for ReflectorNet.Reflector.TryPatch — the JSON Merge
    /// Patch-style API that touches multiple fields at different depths in a
    /// single call. Includes the polymorphic "$type" hint scenario.
    /// </summary>
    public class TryPatchTests : BaseTest
    {
        [UnitySetUp]
        public override IEnumerator SetUp() => base.SetUp();

        [UnityTearDown]
        public override IEnumerator TearDown() => base.TearDown();

        // ─── TryPatch — multiple top-level fields at once ─────────────────────

        [UnityTest]
        public IEnumerator TryPatch_MultipleTopLevelFields()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco
            {
                globalOrbitSpeedMultiplier = 1f,
                globalSizeMultiplier = 1f,
                celestialBodies = new[]
                {
                    new CelestialBody { orbitRadius = 10f, orbitSpeed = 1f },
                }
            };
            object? obj = system;

            var json = @"{
                ""globalOrbitSpeedMultiplier"": 5.0,
                ""globalSizeMultiplier"": 2.0
            }";

            var logs = new Logs();
            var success = reflector.TryPatch(ref obj, json, logs: logs);

            Debug.Log($"[TryPatch_Top] logs:\n{logs}");
            Assert.IsTrue(success);
            var result = (StarSystemPoco)obj!;
            Assert.AreEqual(5f,  result.globalOrbitSpeedMultiplier);
            Assert.AreEqual(2f,  result.globalSizeMultiplier);
            Assert.AreEqual(10f, result.celestialBodies![0].orbitRadius, "Untouched array element must be preserved.");
            yield return null;
        }

        // ─── TryPatch — multi-field touching nested array AND dictionary ──────

        [UnityTest]
        public IEnumerator TryPatch_NestedArrayAndDictionary()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco
            {
                celestialBodies = new[]
                {
                    new CelestialBody { orbitRadius = 10f, orbitSpeed = 1f },
                    new CelestialBody { orbitRadius = 20f, orbitSpeed = 2f },
                },
                config = new Dictionary<string, int> { ["timeout"] = 10, ["retries"] = 3 }
            };
            object? obj = system;

            // Modify celestialBodies[0].orbitRadius AND config[timeout] in one patch.
            var json = @"{
                ""celestialBodies"": {
                    ""[0]"": {
                        ""orbitRadius"": 42.0
                    }
                },
                ""config"": {
                    ""[timeout]"": 60
                }
            }";

            var logs = new Logs();
            var success = reflector.TryPatch(ref obj, json, logs: logs);

            Debug.Log($"[TryPatch_NestedArrayAndDict] logs:\n{logs}");
            Assert.IsTrue(success);
            var result = (StarSystemPoco)obj!;
            Assert.AreEqual(42f, result.celestialBodies![0].orbitRadius);
            Assert.AreEqual(1f,  result.celestialBodies![0].orbitSpeed,  "Same-element sibling field must be untouched.");
            Assert.AreEqual(20f, result.celestialBodies![1].orbitRadius, "Sibling array element must be untouched.");
            Assert.AreEqual(60,  result.config["timeout"]);
            Assert.AreEqual(3,   result.config["retries"], "Sibling dict entry must be untouched.");
            yield return null;
        }

        // ─── TryPatch — JsonElement overload ──────────────────────────────────

        [UnityTest]
        public IEnumerator TryPatch_JsonElement_Overload()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco { globalOrbitSpeedMultiplier = 1f };
            object? obj = system;

            using var doc = JsonDocument.Parse(@"{ ""globalOrbitSpeedMultiplier"": 9.0 }");
            var logs = new Logs();
            var success = reflector.TryPatch(ref obj, doc.RootElement, logs: logs);

            Debug.Log($"[TryPatch_JsonElement] logs:\n{logs}");
            Assert.IsTrue(success);
            Assert.AreEqual(9f, ((StarSystemPoco)obj!).globalOrbitSpeedMultiplier);
            yield return null;
        }

        // ─── TryPatch — "$type" polymorphic replacement ────────────────────────

        [UnityTest]
        public IEnumerator TryPatch_TypeHint_PolymorphicReplacement()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var container = new AnimalContainer { animal = new Animal { name = "Cat" } };
            object? obj = container;

            var dogTypeId = typeof(Dog).GetTypeId();
            // The "$type" key tells TryPatch to replace the existing Animal instance
            // with a fresh Dog instance, then apply the remaining fields onto it.
            var json = $@"{{""animal"": {{""$type"": ""{dogTypeId}"", ""name"": ""Rex"", ""breed"": ""Husky""}}}}";

            var logs = new Logs();
            var success = reflector.TryPatch(ref obj, json, logs: logs);

            Debug.Log($"[TryPatch_TypeHint] logs:\n{logs}");
            Assert.IsTrue(success);
            var result = (AnimalContainer)obj!;
            Assert.IsInstanceOf<Dog>(result.animal);
            Assert.AreEqual("Rex",   result.animal!.name);
            Assert.AreEqual("Husky", ((Dog)result.animal).breed);
            yield return null;
        }

        // ─── TryPatch — null sets field to null (RFC 7396) ────────────────────

        [UnityTest]
        public IEnumerator TryPatch_NullValue_SetsFieldToNull()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var container = new AnimalContainer { animal = new Animal { name = "Cat" } };
            object? obj = container;

            var logs = new Logs();
            var success = reflector.TryPatch(ref obj, @"{""animal"": null}", logs: logs);

            Debug.Log($"[TryPatch_Null] logs:\n{logs}");
            Assert.IsTrue(success);
            Assert.IsNull(((AnimalContainer)obj!).animal);
            yield return null;
        }

        // ─── TryPatch — invalid JSON returns false ────────────────────────────

        [UnityTest]
        public IEnumerator TryPatch_InvalidJson_ReturnsFalse()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco { globalOrbitSpeedMultiplier = 1f };
            object? obj = system;
            var logs = new Logs();

            var success = reflector.TryPatch(ref obj, "{ this is not valid json }", logs: logs);

            Assert.IsFalse(success);
            var logsText = logs.ToString();
            Debug.Log($"[TryPatch_InvalidJson] logs:\n{logsText}");
            StringAssert.Contains("Failed to parse JSON patch", logsText);
            Assert.AreEqual(1f, ((StarSystemPoco)obj!).globalOrbitSpeedMultiplier, "Graph must be untouched on parse failure.");
            yield return null;
        }
    }
}
