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
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.ProBuilder
{
    public class ProBuilderMaterialViewTests : ProBuilderTestBase
    {
        [Test]
        public void MaterialSlotsCountTheirFaces()
        {
            var r = Ok(Q(M + " | get materials | select index material faceCount"));
            Assert.AreEqual(1, r.Rows!.Count);
            Assert.AreEqual("0", Cell(r, 0));
            Assert.AreEqual("6", Cell(r, 2));
        }

        [Test]
        public void MaterialSlotsFollowSetFaceMaterial()
        {
            var red = MaterialAsset("Red");
            Ok(M + " | call SetFaceMaterial([0, 1], unity.asset(\"" + Folder + "/Red.mat\"))");
            var r = Ok(Q(M + " | get materials | select index material.name faceCount"));
            Assert.AreEqual(2, r.Rows!.Count);
            Assert.AreEqual("4", Cell(r, 2, 0));
            Assert.AreEqual("Red", Cell(r, 1, 1));
            Assert.AreEqual("2", Cell(r, 2, 1));
            Assert.AreEqual(red, Cube.GetComponent<MeshRenderer>().sharedMaterials[1]);
        }

        [Test]
        public void MaterialSlotsAreReadOnly()
        {
            Fails(M + " | get materials | set faceCount = 1", "faceCount");
        }
    }
}
