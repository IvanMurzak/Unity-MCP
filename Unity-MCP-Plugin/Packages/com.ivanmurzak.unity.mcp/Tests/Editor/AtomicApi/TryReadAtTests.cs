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
using System.Linq;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.AtomicApi
{
    /// <summary>
    /// EditMode coverage for ReflectorNet.Reflector.TryReadAt — the read-side
    /// counterpart of TryModifyAt. Each path here mirrors a path used by
    /// <see cref="TryModifyAtTests"/> to validate read↔write symmetry: the
    /// value written by TryModifyAt is observed back through TryReadAt.
    /// </summary>
    public class TryReadAtTests : BaseTest
    {
        [UnitySetUp]
        public override IEnumerator SetUp() => base.SetUp();

        [UnityTearDown]
        public override IEnumerator TearDown() => base.TearDown();

        // ─── TryReadAt — root field ──────────────────────────────────────────

        [UnityTest]
        public IEnumerator TryReadAt_RootField_ReadWriteSymmetry()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco { globalOrbitSpeedMultiplier = 1f };
            object? obj = system;

            // Write
            var wrote = reflector.TryModifyAt<float>(ref obj, "globalOrbitSpeedMultiplier", 5f);
            Assert.IsTrue(wrote);

            // Read back the same path
            var read = reflector.TryReadAt(obj, "globalOrbitSpeedMultiplier", out var member);
            Assert.IsTrue(read);
            Assert.IsNotNull(member);
            Assert.AreEqual(5f, member!.GetValue<float>(reflector));
            yield return null;
        }

        // ─── TryReadAt — array element ───────────────────────────────────────

        [UnityTest]
        public IEnumerator TryReadAt_ArrayElement_ReadWriteSymmetry()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco
            {
                celestialBodies = new[]
                {
                    new CelestialBody { orbitRadius = 10f },
                    new CelestialBody { orbitRadius = 20f },
                }
            };
            object? obj = system;

            // Write
            var wrote = reflector.TryModifyAt<float>(ref obj, "celestialBodies/[1]/orbitRadius", 999f);
            Assert.IsTrue(wrote);

            // Read leaf back
            var read = reflector.TryReadAt(obj, "celestialBodies/[1]/orbitRadius", out var member);
            Assert.IsTrue(read);
            Assert.AreEqual(999f, member!.GetValue<float>(reflector));

            // And read the parent element to confirm both fields are observable
            var readElem = reflector.TryReadAt(obj, "celestialBodies/[1]", out var elem);
            Assert.IsTrue(readElem);
            Assert.IsNotNull(elem);
            StringAssert.Contains("CelestialBody", elem!.typeName);
            var radius = elem.fields?.FirstOrDefault(f => f.name == "orbitRadius");
            Assert.IsNotNull(radius);
            Assert.AreEqual(999f, radius!.GetValue<float>(reflector));
            yield return null;
        }

        // ─── TryReadAt — Dictionary string key ───────────────────────────────

        [UnityTest]
        public IEnumerator TryReadAt_DictionaryStringKey_ReadWriteSymmetry()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco
            {
                config = new Dictionary<string, int> { ["timeout"] = 10, ["retries"] = 3 }
            };
            object? obj = system;

            var wrote = reflector.TryModifyAt<int>(ref obj, "config/[timeout]", 60);
            Assert.IsTrue(wrote);

            var read = reflector.TryReadAt(obj, "config/[timeout]", out var member);
            Assert.IsTrue(read);
            Assert.AreEqual(60, member!.GetValue<int>(reflector));
            yield return null;
        }

        // ─── TryReadAt — Dictionary integer key ──────────────────────────────

        [UnityTest]
        public IEnumerator TryReadAt_DictionaryIntKey_ReadWriteSymmetry()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco
            {
                lookup = new Dictionary<int, string> { [1] = "one", [2] = "two" }
            };
            object? obj = system;

            var wrote = reflector.TryModifyAt<string>(ref obj, "lookup/[2]", "TWO");
            Assert.IsTrue(wrote);

            var read = reflector.TryReadAt(obj, "lookup/[2]", out var member);
            Assert.IsTrue(read);
            Assert.AreEqual("TWO", member!.GetValue<string>(reflector));
            yield return null;
        }

        // ─── TryReadAt — invalid path returns false + detailed log ───────────

        [UnityTest]
        public IEnumerator TryReadAt_InvalidPath_ReturnsFalse_WithLogs()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco { globalOrbitSpeedMultiplier = 1f };
            object? obj = system;
            var logs = new Logs();

            var read = reflector.TryReadAt(obj, "doesNotExist", out var member, logs: logs);

            Assert.IsFalse(read);
            Assert.IsNull(member);
            var logsText = logs.ToString();
            Debug.Log($"[TryReadAt_InvalidPath] logs:\n{logsText}");
            StringAssert.Contains("doesNotExist", logsText);
            StringAssert.Contains("not found",    logsText);
            yield return null;
        }
    }
}
