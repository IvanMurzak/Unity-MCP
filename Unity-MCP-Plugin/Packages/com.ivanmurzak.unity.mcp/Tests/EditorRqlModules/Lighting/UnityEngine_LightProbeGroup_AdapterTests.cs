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
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Lighting
{
    public class UnityEngine_LightProbeGroup_AdapterTests : LightingTestBase
    {
        private const string Groups = "objects | get components | where $ is LightProbeGroup";

        private LightProbeGroup _group = null!;

        [SetUp]
        public void CreateGroup()
        {
            _group = new GameObject("Probes").AddComponent<LightProbeGroup>();
            _group.transform.position = new Vector3(5, 0, 0);   // positions are local: the group's position must not leak in
        }

        [Test]
        public void PlaceGridFillsTheBoxAroundTheGroup()
        {
            Assert.AreEqual("108", CallResult(M(Groups + " | call PlaceGrid(\"(10, 4, 10)\", 2)")));   // 6 x 3 x 6
            var p = _group.probePositions;
            Assert.AreEqual(108, p.Length);
            Assert.AreEqual(new Vector3(-5, -2, -5), new Vector3(p.Min(v => v.x), p.Min(v => v.y), p.Min(v => v.z)));
            Assert.AreEqual(new Vector3(5, 2, 5), new Vector3(p.Max(v => v.x), p.Max(v => v.y), p.Max(v => v.z)));
            Assert.AreEqual(108, p.Distinct().Count());
            Assert.AreEqual("108", Cell(Q(Groups + " | select probeCount")));
        }

        [Test]
        public void TheSpacingIsAnUpperBound()
        {
            // 5 m every 2 m: 4 probes per axis (1.67 m apart) so both box edges get one.
            Assert.AreEqual("4", CallResult(M(Groups + " | call PlaceGrid(\"(5, 0, 0)\", 2)")));
            var xs = _group.probePositions.Select(v => v.x).OrderBy(x => x).ToArray();
            Assert.AreEqual(-2.5f, xs[0], 1e-4f);
            Assert.AreEqual(2.5f, xs[3], 1e-4f);
            Assert.Less(xs[1] - xs[0], 2f);
        }

        [Test]
        public void AFlatAxisGetsOneLayer()
        {
            Assert.AreEqual("9", CallResult(M(Groups + " | call PlaceGrid(\"(4, 0, 4)\", 2)")));
            Assert.IsTrue(_group.probePositions.All(v => v.y == 0f));
        }

        [Test]
        public void BadArgumentsChangeNothing()
        {
            var before = _group.probePositions;
            Fails(M(Groups + " | call PlaceGrid(\"(10, 4, 10)\", 0)"), "> 0");
            Fails(M(Groups + " | call PlaceGrid(\"(1000, 100, 1000)\", 1)"), "10000");
            Fails(M(Groups + " | call PlaceGrid(\"(-1, 4, 10)\", 2)"), "negative");
            CollectionAssert.AreEqual(before, _group.probePositions);
        }

        [Test]
        public void PlaceGridIsUndoable()
        {
            var before = _group.probePositions;
            Ok(M(Groups + " | call PlaceGrid(\"(2, 2, 2)\", 1)"));
            Assert.AreEqual(27, _group.probePositions.Length);
            Undo.PerformUndo();
            CollectionAssert.AreEqual(before, _group.probePositions);
        }

        [Test]
        public void ClearProbesEmptiesTheGroup()
        {
            Ok(M(Groups + " | call PlaceGrid(\"(2, 2, 2)\", 2)"));
            Ok(M(Groups + " | call ClearProbes()"));
            Assert.AreEqual(0, _group.probePositions.Length);
            Assert.AreEqual("0", Cell(Q(Groups + " | select probeCount")));
        }
    }
}
