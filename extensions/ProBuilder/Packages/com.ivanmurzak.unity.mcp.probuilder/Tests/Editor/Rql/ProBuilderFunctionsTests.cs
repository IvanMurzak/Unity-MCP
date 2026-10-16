/*
┌──────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                     │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-ProBuilder)  │
│  Copyright (c) 2026 Ivan Murzak                                          │
│  Licensed under the Apache License, Version 2.0.                         │
│  See the LICENSE file in the project root for more information.          │
└──────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.ProBuilder
{
    public class ProBuilderFunctionsTests : ProBuilderTestBase
    {
        [Test]
        public void FacingReturnsFaceIndices()
        {
            var up = Facing(Cube, Vector3.up).Single();
            Assert.AreEqual($"[{up}]", Cell(Ok(Q(M + " | select f=(probuilder.facing($, \"up\"))"))));
            Assert.AreEqual("[]", Cell(Ok(Q(M + " | select f=(probuilder.facing($, \"Other\"))"))));
            Assert.AreEqual($"[{up}]", Cell(Ok(Q("objects | where name == \"Cube\" | select f=(probuilder.facing($, \"Up\"))"))), "a GameObject works too");
            StringAssert.Contains("unknown direction", Table(Q(M + " | select f=(probuilder.facing($, \"Sideways\"))")));
        }

        [Test]
        public void FacingFeedsACall()
        {
            Ok(M + " | call Extrude(probuilder.facing($, \"Up\"), 1)");
            Assert.AreEqual(10, Cube.faceCount);
        }

        [Test]
        public void EdgesListVertexPairs()
        {
            var r = Ok(Q(M + " | select all=(probuilder.edges($)) one=(probuilder.edges($, [0]))"));
            Assert.AreEqual(4, r.Rows![0][1]!.AsArray().Count);
            Assert.AreEqual(2, r.Rows[0][1]!.AsArray()[0]!.AsArray().Count);
            StringAssert.Contains("face 9 does not exist", Table(Q(M + " | select e=(probuilder.edges($, [9]))")));
        }

        [Test]
        public void EdgesFeedACall()
        {
            Ok(M + " | call Bevel(probuilder.edges($, probuilder.facing($, \"Up\")), 0.1)");
            Assert.Greater(Cube.faceCount, 6);
        }

        [Test]
        public void OpenEdgesAreTheBorder()
        {
            Assert.AreEqual("[]", Cell(Ok(Q(M + " | select e=(probuilder.openEdges($))"))));
            Ok(M + " | call DeleteFaces(probuilder.facing($, \"Up\"))");
            var r = Ok(Q(M + " | select e=(probuilder.openEdges($))"));
            Assert.AreEqual(4, r.Rows![0][0]!.AsArray().Count);
            Ok(M + " | call Bridge(probuilder.openEdges($)[0], probuilder.openEdges($)[2])");
            Assert.AreEqual(6, Cube.faceCount);
        }

        [Test]
        public void VerticesPageThePositions()
        {
            var r = Ok(Q(M + " | select v=(probuilder.vertices($, 2, 3))"));
            var rows = r.Rows![0][0]!.AsArray();
            Assert.AreEqual(3, rows.Count);
            Assert.AreEqual("2", rows[0]!["index"]!.ToJsonString());
            Assert.AreEqual(20, Ok(Q(M + " | select v=(probuilder.vertices($))")).Rows![0][0]!.AsArray().Count);
            StringAssert.Contains("1 … 20 vertices", Table(Q(M + " | select v=(probuilder.vertices($, 0, 50))")));
            StringAssert.Contains("start 99 is outside", Table(Q(M + " | select v=(probuilder.vertices($, 99))")));
        }
    }
}
