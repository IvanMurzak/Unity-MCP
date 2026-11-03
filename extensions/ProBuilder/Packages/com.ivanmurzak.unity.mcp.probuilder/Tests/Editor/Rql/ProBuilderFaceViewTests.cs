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
    public class ProBuilderFaceViewTests : ProBuilderTestBase
    {
        [Test]
        public void EveryFaceIsAView()
        {
            var r = Ok(Q(M + " | get faces | select index direction vertexCount triangleCount submeshIndex smoothingGroup"));
            Assert.AreEqual(6, r.Rows!.Count);
            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(i.ToString(), Cell(r, 0, i));
                Assert.AreEqual("4", Cell(r, 2, i));
                Assert.AreEqual("2", Cell(r, 3, i));
                Assert.AreEqual("0", Cell(r, 4, i));
            }
            CollectionAssert.AreEquivalent(new[] { "Up", "Down", "Left", "Right", "Forward", "Back" },
                Enumerable.Range(0, 6).Select(i => Cell(r, 1, i)).ToArray());
        }

        [Test]
        public void FacesFilterByDirection()
        {
            var up = Facing(Cube, Vector3.up).Single();
            var r = Ok(Q(M + " | get faces | where direction == \"Up\" | select index center normal vertices edges material.name"));
            Assert.AreEqual(1, r.Rows!.Count);
            Assert.AreEqual(up.ToString(), Cell(r, 0));
            StringAssert.Contains("0.5", Cell(r, 1));
            StringAssert.Contains("1", Cell(r, 2));
            Assert.AreEqual(4, r.Rows[0][3]!.AsArray().Count);
            Assert.AreEqual(4, r.Rows[0][4]!.AsArray().Count);
        }

        [Test]
        public void FacesAreReadOnly()
        {
            Fails(M + " | get faces | where index == 0 | set submeshIndex = 1", "submeshIndex");
            Assert.AreEqual(0, Cube.faces[0].submeshIndex);
        }

        [Test]
        public void FaceIndicesFeedTheMeshMethods()
        {
            Ok(M + " | call DeleteFaces([faces.first(direction == \"Up\").index])");
            Assert.AreEqual(5, Cube.faceCount);
            Assert.AreEqual(0, Facing(Cube, Vector3.up).Length);
        }
    }
}
