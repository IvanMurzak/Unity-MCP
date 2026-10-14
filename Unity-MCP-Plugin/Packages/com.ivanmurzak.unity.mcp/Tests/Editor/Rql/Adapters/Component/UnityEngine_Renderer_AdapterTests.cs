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
    public class UnityEngine_Renderer_AdapterTests : AdapterTestBase
    {
        private Material _red = null!, _blue = null!;
        private MeshRenderer _renderer = null!;

        [SetUp]
        public void Materials()
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Hidden/InternalErrorShader");
            _red = new Material(shader) { name = "Red" };
            _blue = new Material(shader) { name = "Blue" };
            _renderer = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshRenderer>();
            _renderer.sharedMaterial = _red;
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(_red);
            Object.DestroyImmediate(_blue);
        }

        [Test]
        public void ReadingRenderersNeverInstantiatesMaterials()
        {
            AssertOk(Q("objects | get components | where $ is MeshRenderer"));
            AssertOk(Q("roots | recurse children | get components"));
            Assert.AreSame(_red, _renderer.sharedMaterial, "a material instance was created by a read");
            Assert.AreEqual("Red", Cell(Q("objects | get components | where $ is MeshRenderer | select sharedMaterial.name")));
            // material / materials read the shared materials: selecting them explicitly creates no copies either.
            Assert.AreEqual("Red", Cell(Q("objects | get components | where $ is MeshRenderer | select material.name")));
            AssertOk(Q("objects | get components | where $ is MeshRenderer | select materials"));
            Assert.AreSame(_red, _renderer.sharedMaterial, "a material instance was created by reading material(s)");
        }

        [Test]
        public void MaterialsWritesGoToTheSharedMaterials()
        {
            _renderer.sharedMaterial = _blue;
            var blueRef = Cell(Q("objects | where name == \"Cube\" | get components | where $ is MeshRenderer | get sharedMaterial | select $ref"));
            _renderer.sharedMaterial = _red;
            AssertOk(M($"objects | where name == \"Cube\" | get components | where $ is MeshRenderer | set materials[0] = ref(\"{blueRef}\")"));
            Assert.AreSame(_blue, _renderer.sharedMaterial);
        }

        [Test]
        public void SharedMaterialsElementWritesAreAssignedBack()
        {
            var other = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            other.name = "Other";
            other.GetComponent<MeshRenderer>().sharedMaterial = _blue;
            var blueRef = Cell(Q("objects | where name == \"Other\" | get components | where $ is MeshRenderer | get sharedMaterial | select $ref"));
            StringAssert.StartsWith("Material#", blueRef);
            // sharedMaterials returns a copy of the array: the element write must be assigned back to the renderer.
            AssertOk(M($"objects | where name == \"Cube\" | get components | where $ is MeshRenderer | set sharedMaterials[0] = ref(\"{blueRef}\")"));
            Assert.AreSame(_blue, _renderer.sharedMaterial);
        }
    }
}
