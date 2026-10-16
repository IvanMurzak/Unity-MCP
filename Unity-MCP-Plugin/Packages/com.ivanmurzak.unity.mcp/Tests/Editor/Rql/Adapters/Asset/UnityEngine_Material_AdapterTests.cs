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
    public class UnityEngine_Material_AdapterTests : AdapterTestBase
    {
        private Material _mat = null!;

        private const string Mat = "objects | get components | where $ is MeshRenderer | get sharedMaterial";

        [SetUp]
        public void Probe()
        {
            var shader = Shader.Find("Standard");
            Assume.That(shader != null, "Standard shader not available in this render pipeline");
            _mat = new Material(shader) { name = "Probe", color = new Color(0.1f, 0.2f, 0.3f, 1) };
            GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshRenderer>().sharedMaterial = _mat;
        }

        [TearDown]
        public void Cleanup() => Object.DestroyImmediate(_mat);

        [Test]
        public void ShaderPropertiesAreATypedMap()
        {
            AssertTuple("(0.1, 0.2, 0.3, 1)", Cell(Q(Mat + " | select properties[\"_Color\"]")));
            Assert.AreEqual("0.5", Cell(Q(Mat + " | select properties[\"_Glossiness\"]")));
            AssertOk(M(Mat + " | set properties[\"_Glossiness\"] = 0.8, properties[\"_Color\"] = \"#ff0000\""));
            Assert.AreEqual(0.8f, _mat.GetFloat("_Glossiness"), 1e-6f);
            Assert.AreEqual(Color.red, _mat.GetColor("_Color"));
        }

        [Test]
        public void ChangesOneComponentOfAColorProperty()
        {
            AssertOk(M(Mat + " | set properties[\"_Color\"].a = 0.25"));
            var c = _mat.GetColor("_Color");
            Assert.AreEqual(0.25f, c.a, 1e-6f);
            Assert.AreEqual(0.2f, c.g, 1e-6f);
        }

        [Test]
        public void RangePropertiesAreValidated()
        {
            AssertFails(M(Mat + " | set properties[\"_Glossiness\"] = 2"), "coercion-failed", "must be within");
            Assert.AreEqual(0.5f, _mat.GetFloat("_Glossiness"), 1e-6f);
        }

        [Test]
        public void UnknownPropertiesAreRejected() =>
            AssertFails(M(Mat + " | set properties[\"_NoSuchProperty\"] = 1"), "coercion-failed", "no entry");

        [Test]
        public void OnlySafeMembersAreExposed()
        {
            var schema = Schema("Material");
            StringAssert.Contains("properties", schema);
            StringAssert.Contains("shader", schema);
            StringAssert.DoesNotContain("mainTexture", schema);
            StringAssert.DoesNotContain("shaderKeywords", schema);
        }
    }
}
