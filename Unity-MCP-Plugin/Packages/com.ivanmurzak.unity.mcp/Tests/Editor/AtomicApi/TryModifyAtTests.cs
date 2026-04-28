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
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.AtomicApi
{
    /// <summary>
    /// EditMode coverage for ReflectorNet.Reflector.TryModifyAt — the atomic
    /// path-based field/array-element/dictionary-entry modification API.
    /// The tests run in a real Unity Editor process (managed assembly load
    /// context) to validate that the API behaves correctly when consumed by
    /// the Unity-MCP plugin.
    /// </summary>
    public class TryModifyAtTests : BaseTest
    {
        [UnitySetUp]
        public override IEnumerator SetUp() => base.SetUp();

        [UnityTearDown]
        public override IEnumerator TearDown() => base.TearDown();

        // ─── TryModifyAt — root field ─────────────────────────────────────────

        [UnityTest]
        public IEnumerator TryModifyAt_RootField()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco { globalOrbitSpeedMultiplier = 1f, globalSizeMultiplier = 2f };
            object? obj = system;

            var success = reflector.TryModifyAt<float>(ref obj, "globalOrbitSpeedMultiplier", 5f);

            Assert.IsTrue(success, "TryModifyAt should succeed on a root field.");
            var result = (StarSystemPoco)obj!;
            Assert.AreEqual(5f, result.globalOrbitSpeedMultiplier);
            Assert.AreEqual(2f, result.globalSizeMultiplier, "Sibling root field must be untouched.");
            yield return null;
        }

        // ─── TryModifyAt — nested field via array element ─────────────────────

        [UnityTest]
        public IEnumerator TryModifyAt_ArrayElementField()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco
            {
                celestialBodies = new[]
                {
                    new CelestialBody { orbitRadius = 10f, orbitSpeed = 1f, name = "alpha" },
                    new CelestialBody { orbitRadius = 20f, orbitSpeed = 2f, name = "beta" },
                }
            };
            object? obj = system;

            var success = reflector.TryModifyAt<float>(ref obj, "celestialBodies/[0]/orbitRadius", 999f);

            Assert.IsTrue(success);
            var result = (StarSystemPoco)obj!;
            Assert.AreEqual(999f, result.celestialBodies![0].orbitRadius);
            Assert.AreEqual(1f,   result.celestialBodies![0].orbitSpeed,  "Sibling field on same element must be untouched.");
            Assert.AreEqual(20f,  result.celestialBodies![1].orbitRadius, "Sibling array element must be untouched.");
            Assert.AreEqual(2f,   result.celestialBodies![1].orbitSpeed,  "Sibling array element must be untouched.");
            yield return null;
        }

        // ─── TryModifyAt — Dictionary string key ──────────────────────────────

        [UnityTest]
        public IEnumerator TryModifyAt_DictionaryStringKey()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco
            {
                config = new Dictionary<string, int> { ["timeout"] = 10, ["retries"] = 3 }
            };
            object? obj = system;

            var success = reflector.TryModifyAt<int>(ref obj, "config/[timeout]", 60);

            Assert.IsTrue(success);
            var result = (StarSystemPoco)obj!;
            Assert.AreEqual(60, result.config["timeout"]);
            Assert.AreEqual(3,  result.config["retries"], "Sibling dict entry must be untouched.");
            yield return null;
        }

        // ─── TryModifyAt — Dictionary integer key ─────────────────────────────

        [UnityTest]
        public IEnumerator TryModifyAt_DictionaryIntKey()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco
            {
                lookup = new Dictionary<int, string> { [1] = "one", [2] = "two", [3] = "three" }
            };
            object? obj = system;

            var success = reflector.TryModifyAt<string>(ref obj, "lookup/[2]", "TWO");

            Assert.IsTrue(success);
            var result = (StarSystemPoco)obj!;
            Assert.AreEqual("TWO",   result.lookup[2]);
            Assert.AreEqual("one",   result.lookup[1], "Sibling dict entry must be untouched.");
            Assert.AreEqual("three", result.lookup[3], "Sibling dict entry must be untouched.");
            yield return null;
        }

        // ─── TryModifyAt — partial element patch via SerializedMember ─────────
        // Mirrors ReflectorNet's own TryModifyAt_PartialPatch_ArrayElement test:
        // navigate to an array element and apply a partial SerializedMember
        // patch — only the specified field changes, sibling fields are
        // preserved.

        [UnityTest]
        public IEnumerator TryModifyAt_PartialPatch_ArrayElement()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco
            {
                celestialBodies = new[]
                {
                    new CelestialBody { orbitRadius = 10f, orbitSpeed = 3f, name = "alpha" },
                    new CelestialBody { orbitRadius = 20f, orbitSpeed = 4f, name = "beta"  },
                }
            };
            object? obj = system;

            // Build a SerializedMember that only specifies orbitRadius, then apply at [1].
            var patch = new SerializedMember { typeName = typeof(CelestialBody).GetTypeId() };
            patch.SetFieldValue(reflector, "orbitRadius", 777f);

            var logs = new Logs();
            var success = reflector.TryModifyAt(ref obj, "celestialBodies/[1]", patch, logs: logs);

            Debug.Log($"[TryModifyAt_PartialPatch] logs:\n{logs}");
            Assert.IsTrue(success);
            var result = (StarSystemPoco)obj!;
            Assert.AreEqual(777f, result.celestialBodies![1].orbitRadius);
            Assert.AreEqual(4f,   result.celestialBodies![1].orbitSpeed,
                "Sibling field on the same element must be untouched by partial patch.");
            Assert.AreEqual("beta", result.celestialBodies![1].name,
                "Sibling field on the same element must be untouched by partial patch.");
            Assert.AreEqual(10f,  result.celestialBodies![0].orbitRadius,
                "Sibling array element must be untouched.");
            yield return null;
        }

        // ─── TryModifyAt — out-of-range index logs error ──────────────────────

        [UnityTest]
        public IEnumerator TryModifyAt_OutOfRangeIndex_LogsError_LeavesGraphUntouched()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco
            {
                celestialBodies = new[]
                {
                    new CelestialBody { orbitRadius = 1f },
                    new CelestialBody { orbitRadius = 2f },
                }
            };
            object? obj = system;
            var logs = new Logs();

            var success = reflector.TryModifyAt<float>(
                ref obj, "celestialBodies/[99]/orbitRadius", 0f, logs: logs);

            Assert.IsFalse(success);
            var logsText = logs.ToString();
            Debug.Log($"[TryModifyAt_OutOfRange] logs:\n{logsText}");
            StringAssert.Contains("[99]", logsText);
            StringAssert.Contains("out of range", logsText);
            // Graph must be untouched
            Assert.AreEqual(1f, ((StarSystemPoco)obj!).celestialBodies![0].orbitRadius);
            Assert.AreEqual(2f, ((StarSystemPoco)obj!).celestialBodies![1].orbitRadius);
            yield return null;
        }

        // ─── TryModifyAt — missing member logs detailed error ─────────────────

        [UnityTest]
        public IEnumerator TryModifyAt_MissingMember_LogsDetailedError()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco { globalOrbitSpeedMultiplier = 1f };
            object? obj = system;
            var logs = new Logs();

            var success = reflector.TryModifyAt<float>(ref obj, "doesNotExist", 5f, logs: logs);

            Assert.IsFalse(success);
            var logsText = logs.ToString();
            Debug.Log($"[TryModifyAt_MissingMember] logs:\n{logsText}");
            StringAssert.Contains("doesNotExist", logsText);
            StringAssert.Contains("not found",    logsText);
            // Sibling left untouched
            Assert.AreEqual(1f, ((StarSystemPoco)obj!).globalOrbitSpeedMultiplier);
            yield return null;
        }
    }
}
