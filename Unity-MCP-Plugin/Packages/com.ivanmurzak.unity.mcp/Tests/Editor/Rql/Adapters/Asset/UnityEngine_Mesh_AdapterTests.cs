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
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Adapters
{
    public class UnityEngine_Mesh_AdapterTests : AdapterTestBase
    {
        private const string Filter = "objects | where name == \"Pillar\" | get components | where $ is MeshFilter";

        private static Mesh MeshOf(PrimitiveType type)
        {
            var probe = GameObject.CreatePrimitive(type);
            try { return probe.GetComponent<MeshFilter>().sharedMesh; }
            finally { Object.DestroyImmediate(probe); }
        }

        // The built-in meshes are not project assets: a primitive's name is the way to assign one.
        [Test]
        public void APrimitiveNameAssignsTheBuiltInMesh()
        {
            var filter = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>();
            filter.gameObject.name = "Pillar";
            AssertOk(M(Filter + " | set sharedMesh = \"Cylinder\""));
            Assert.AreSame(MeshOf(PrimitiveType.Cylinder), filter.sharedMesh);
            AssertOk(M(Filter + " | set sharedMesh = \"sphere\"")); // any case
            Assert.AreSame(MeshOf(PrimitiveType.Sphere), filter.sharedMesh);
#if UNITY_6000_5_OR_NEWER
            Assert.AreEqual(1, Object.FindObjectsByType<MeshFilter>().Length, "the probe primitive stayed in the scene");
#else
            Assert.AreEqual(1, Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Length, "the probe primitive stayed in the scene");
#endif
        }

        [Test]
        public void AnythingElseNamesTheChoices()
        {
            var filter = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>();
            filter.gameObject.name = "Pillar";
            AssertFails(M(Filter + " | set sharedMesh = \"Pyramid\""), "coercion-failed", "Cylinder");
            AssertFails(M(Filter + " | set sharedMesh = \"2\""), "coercion-failed", "neither an asset path nor a primitive");
            // an asset path keeps its own message (the Object adapter)
            AssertFails(M(Filter + " | set sharedMesh = \"Assets/NoSuch/Rock.fbx\""), "coercion-failed", "no asset at");
            Assert.AreEqual("Cube", filter.sharedMesh.name);
        }
    }
}
