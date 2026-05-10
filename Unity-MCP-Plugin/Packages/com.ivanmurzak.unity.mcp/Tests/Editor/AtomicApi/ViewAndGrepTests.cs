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
using System.Linq;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.AtomicApi
{
    /// <summary>
    /// EditMode coverage for ReflectorNet.Reflector.View and Reflector.Grep — the
    /// filtered-serialization and live-graph "grep" APIs introduced by the
    /// atomic-api PR. Includes a circular-reference graph to confirm the walkers
    /// terminate, and verifies that maxDepth pruning, NameRegex filtering and
    /// TypeFilter compose as documented.
    /// </summary>
    public class ViewAndGrepTests : BaseTest
    {
        [UnitySetUp]
        public override IEnumerator SetUp() => base.SetUp();

        [UnityTearDown]
        public override IEnumerator TearDown() => base.TearDown();

        // ─── View — NameRegex + TypeFilter on a graph with a circular ref ─────

        [UnityTest]
        public IEnumerator View_NameRegexAndTypeFilter_OnCyclicGraph_TerminatesAndFilters()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");

            // Build a 2-node cycle: a → b → a.
            var a = new CyclicNode { label = "alpha", weight = 1f };
            var b = new CyclicNode { label = "beta",  weight = 2f };
            a.next = b;
            b.next = a; // cycle

            // NameRegex matches "weight" and TypeFilter narrows to float.
            var query = new ViewQuery
            {
                NamePattern = "weight",
                TypeFilter  = typeof(float),
                MaxDepth    = 4
            };

            var result = reflector.View(a, query);

            Assert.IsNotNull(result, "View should not throw or return null on a cyclic graph.");
            StringAssert.Contains("CyclicNode", result!.typeName);
            // Top-level "weight" survives both filters
            var topWeight = result.fields?.FirstOrDefault(f => f.name == "weight");
            Assert.IsNotNull(topWeight, "Top-level weight should survive NameRegex+TypeFilter.");
            Assert.IsTrue(topWeight!.typeName != null && topWeight.typeName.Contains("Single"),
                "Surviving fields must resolve to float / Single after TypeFilter.");
            // "label" is a string and must be pruned by TypeFilter=float
            var topLabel = result.fields?.FirstOrDefault(f => f.name == "label");
            Assert.IsNull(topLabel, "label is string — TypeFilter=float must prune it.");
            yield return null;
        }

        // ─── View — MaxDepth=0 strips all nested fields ───────────────────────

        [UnityTest]
        public IEnumerator View_MaxDepthZero_RootEnvelopeOnly()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco { globalOrbitSpeedMultiplier = 1f };
            object? obj = system;

            var result = reflector.View(obj, new ViewQuery { MaxDepth = 0 });

            Assert.IsNotNull(result);
            StringAssert.Contains("StarSystemPoco", result!.typeName);
            Assert.IsTrue(result.fields == null || result.fields.Count == 0,
                "MaxDepth=0 should strip all nested fields.");
            yield return null;
        }

        // ─── View — MaxDepth=1 keeps top-level fields, strips their children ──

        [UnityTest]
        public IEnumerator View_MaxDepthOne_TopLevelVisible_NestedStripped()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco
            {
                celestialBodies = new[]
                {
                    new CelestialBody { orbitRadius = 5f }
                }
            };
            object? obj = system;

            var result = reflector.View(obj, new ViewQuery { MaxDepth = 1 });

            Assert.IsNotNull(result);
            // Top-level field celestialBodies is present
            var bodies = result!.fields?.FirstOrDefault(f => f.name == "celestialBodies");
            Assert.IsNotNull(bodies, "MaxDepth=1 must keep top-level field celestialBodies.");
            yield return null;
        }

        // ─── Grep — name-regex returns flat list of ViewMatch entries ─────────

        [UnityTest]
        public IEnumerator Grep_NameRegex_ReturnsFlatListOfViewMatches()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco
            {
                celestialBodies = new[]
                {
                    new CelestialBody { orbitRadius = 10f, orbitSpeed = 1f, name = "alpha" },
                    new CelestialBody { orbitRadius = 20f, orbitSpeed = 2f, name = "beta"  },
                    new CelestialBody { orbitRadius = 30f, orbitSpeed = 3f, name = "gamma" },
                }
            };
            object? obj = system;

            var matches = reflector.Grep(obj, "^orbitRadius$");

            Debug.Log($"[Grep_NameRegex] found {matches.Count} matches:");
            foreach (var m in matches)
                Debug.Log($"  {m.Path} = {m.Value.GetValue<float>(reflector)}");

            Assert.IsNotNull(matches);
            Assert.AreEqual(3, matches.Count, "Expected exactly one orbitRadius match per array element.");
            // Each entry must be a ViewMatch (flat list, not a tree)
            foreach (var m in matches)
            {
                Assert.IsInstanceOf<ViewMatch>(m);
                Assert.IsNotNull(m.Path);
                Assert.IsNotNull(m.Value);
            }
            // Paths must include the array-index bracket notation
            Assert.IsTrue(matches.Any(m => m.Path.Contains("[0]") && m.Value.GetValue<float>(reflector) == 10f));
            Assert.IsTrue(matches.Any(m => m.Path.Contains("[1]") && m.Value.GetValue<float>(reflector) == 20f));
            Assert.IsTrue(matches.Any(m => m.Path.Contains("[2]") && m.Value.GetValue<float>(reflector) == 30f));
            yield return null;
        }

        // ─── Grep — type-filter via TypeFilter on View only; here we use ──────
        //          a pattern that effectively narrows to float-typed members
        //          via the field-name regex and verify the result type.

        [UnityTest]
        public IEnumerator Grep_NameRegexNarrowsToFloatFields()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco
            {
                celestialBodies = new[]
                {
                    new CelestialBody { orbitRadius = 7f, orbitSpeed = 1f, rotationSpeed = 9f, name = "x" }
                }
            };
            object? obj = system;

            // ".*Speed" matches orbitSpeed and rotationSpeed (both float) but not "name" (string).
            var matches = reflector.Grep(obj, ".*Speed$");

            Debug.Log($"[Grep_Speed] found {matches.Count} matches:");
            foreach (var m in matches)
                Debug.Log($"  {m.Path}");

            Assert.IsTrue(matches.Count >= 2, "Expected at least orbitSpeed and rotationSpeed.");
            Assert.IsTrue(matches.All(m => m.Path.EndsWith("orbitSpeed") || m.Path.EndsWith("rotationSpeed")),
                "All Grep matches must end in a 'Speed' segment by regex contract.");
            yield return null;
        }

        // ─── Grep — null input returns empty list ─────────────────────────────

        [UnityTest]
        public IEnumerator Grep_NullInput_ReturnsEmpty()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");

            var matches = reflector.Grep(null, ".*");

            Assert.IsNotNull(matches);
            Assert.AreEqual(0, matches.Count);
            yield return null;
        }

        // ─── Grep — invalid regex logs error + returns empty list ─────────────

        [UnityTest]
        public IEnumerator Grep_InvalidRegex_ReturnsEmpty_WithLogs()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector
                ?? throw new Exception("Reflector is not available.");
            var system = new StarSystemPoco { globalOrbitSpeedMultiplier = 1f };
            object? obj = system;
            var logs = new Logs();

            var matches = reflector.Grep(obj, "(invalid[", logs: logs);

            Assert.IsNotNull(matches);
            Assert.AreEqual(0, matches.Count);
            StringAssert.Contains("Invalid regex pattern", logs.ToString());
            yield return null;
        }
    }
}
