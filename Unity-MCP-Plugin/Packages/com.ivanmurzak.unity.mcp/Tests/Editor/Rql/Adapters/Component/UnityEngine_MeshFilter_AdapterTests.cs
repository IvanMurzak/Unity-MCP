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
    public class UnityEngine_MeshFilter_AdapterTests : AdapterTestBase
    {
        [Test]
        public void ReadingMeshFiltersNeverInstantiatesMeshes()
        {
            var filter = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>();
            var shared = filter.sharedMesh;
            AssertOk(Q("objects | get components | where $ is MeshFilter"));
            AssertOk(Q("roots | recurse children | get components"));
            Assert.AreSame(shared, filter.sharedMesh, "a mesh instance was created by a read");
            Assert.AreEqual(shared.name, Cell(Q("objects | get components | where $ is MeshFilter | select sharedMesh.name")));
            StringAssert.Contains("explicit-only", Schema("MeshFilter"));
        }
    }
}
