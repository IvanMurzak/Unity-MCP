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
using com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters;
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

        // Map entries streamed by get are set targets; each write goes through the material's properties map
        // (the key-path write properties["_Color"]), validated like it, the receipt naming the material.
        [Test]
        public void PropertyEntriesStreamedByGetWriteThroughTheMaterial()
        {
            AssertOk(M(Mat + " | get properties | where Key == \"_Glossiness\" | set Value = Value + 0.25"));
            Assert.AreEqual(0.75f, _mat.GetFloat("_Glossiness"), 1e-6f);
            AssertOk(M(Mat + " | get properties | where Key == \"_Color\" | set Value = \"#00ff00\""));
            Assert.AreEqual(Color.green, _mat.GetColor("_Color"));
            AssertFails(M(Mat + " | get properties | where Key == \"_Glossiness\" | set Value = 2"), "invalid-value", "must be within");
            Assert.AreEqual(0.75f, _mat.GetFloat("_Glossiness"), 1e-6f);
        }

        [Test]
        public void RangePropertiesAreValidated()
        {
            AssertFails(M(Mat + " | set properties[\"_Glossiness\"] = 2"), "invalid-value", "must be within");
            Assert.AreEqual(0.5f, _mat.GetFloat("_Glossiness"), 1e-6f);
        }

        [Test]
        public void UnknownPropertiesAreRejected() =>
            AssertFails(M(Mat + " | set properties[\"_NoSuchProperty\"] = 1"), "invalid-value", "no entry");

        // A Material Variant: parent is writable, isVariant reports it; null makes it a plain material again.
        [Test]
        public void ParentMakesAMaterialVariant()
        {
            var baseMat = new Material(_mat.shader) { name = "Base" };
            try
            {
                Assert.AreEqual("false", Cell(Q(Mat + " | select isVariant")));
                AssertOk(M(Mat + $" | set parent = ref(\"{RefOf(baseMat)}\")"));
                Assert.AreSame(baseMat, _mat.parent);
                Assert.AreEqual("true", Cell(Q(Mat + " | select isVariant")));
                AssertOk(M(Mat + " | set parent = null"));
                Assert.IsNull(_mat.parent);
            }
            finally { Object.DestroyImmediate(baseMat); }
        }

        // Unity ignores a parent that would close a cycle (it only logs): refused before anything is written.
        [Test]
        public void AParentCycleIsRefused()
        {
            var child = new Material(_mat.shader) { name = "Child", parent = _mat };
            try
            {
                AssertFails(M(Mat + $" | set parent = ref(\"{RefOf(child)}\")"), "invalid-value", "a cycle");
                AssertFails(M(Mat + $" | set parent = ref(\"{RefOf(_mat)}\")"), "invalid-value", "its own parent");
                Assert.IsNull(_mat.parent);
            }
            finally { Object.DestroyImmediate(child); }
        }

        private static string RefOf(Object o) => com.IvanMurzak.Unity.MCP.Editor.Rql.RqlUnity.Engine.RefOf(o)!;

        [Test]
        public void OnlySafeMembersAreExposed()
        {
            var schema = Schema("Material");
            StringAssert.Contains("properties", schema);
            StringAssert.Contains("shader", schema);
            StringAssert.DoesNotContain("mainTextureOffset", schema); // color / mainTexture are the adapter's, which never log for a shader without them
            StringAssert.DoesNotContain("shaderKeywords", schema);
        }

        // The built-in materials are not project assets: their name is the way to assign one; Default-Material is the
        // material a new primitive gets (the render pipeline's default).
        [Test]
        public void ABuiltInMaterialNameAssignsIt()
        {
            var renderer = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshRenderer>();
            renderer.gameObject.name = "Bare";
            var builtIn = renderer.sharedMaterial;
            renderer.sharedMaterial = null;
            const string Bare = "objects | where name == \"Bare\" | get components | where $ is MeshRenderer";
            AssertOk(M(Bare + " | set sharedMaterial = \"default-material\"")); // any case
            Assert.AreSame(builtIn, renderer.sharedMaterial);
            AssertOk(M(Bare + " | set sharedMaterial = \"Sprites-Default\""));
            Assert.AreEqual("Sprites-Default", renderer.sharedMaterial.name);
            AssertFails(M(Bare + " | set sharedMaterial = \"Glow\""), "coercion-failed", "Default-Material");
            Assert.AreEqual("Sprites-Default", renderer.sharedMaterial.name);
        }

        // Coercion also runs for dry runs: resolving Default-Material must not create (and so dirty) anything.
        [Test]
        public void ResolvingTheDefaultMaterialChangesNothing()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            Assert.IsFalse(scene.isDirty);
            var builtIn = UnityEngine_Material_Adapter.BuiltinMaterial(UnityEngine_Material_Adapter.DefaultMaterialName);
            Assert.IsNotNull(builtIn);
            Assert.IsFalse(scene.isDirty, "resolving the default material dirtied the scene");
            Assert.AreEqual(0, scene.rootCount);
            var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try { Assert.AreSame(probe.GetComponent<MeshRenderer>().sharedMaterial, builtIn, "not the material a new primitive gets"); }
            finally { Object.DestroyImmediate(probe); }
        }

        [Test]
        public void EveryListedBuiltInMaterialExists()
        {
            foreach (var name in UnityEngine_Material_Adapter.Builtins)
                Assert.IsNotNull(UnityEngine_Material_Adapter.BuiltinMaterial(name), name);
        }

        // color / mainTexture: the shader's main color and texture, the properties Material.color / mainTexture use.
        [Test]
        public void ColorAndMainTextureAreTheMainProperties()
        {
            AssertTuple("(0.1, 0.2, 0.3, 1)", Cell(Q(Mat + " | select color")));
            AssertOk(M(Mat + " | set color = \"#00ff00\""));
            Assert.AreEqual(Color.green, _mat.GetColor("_Color"));
            var tex = new Texture2D(2, 2) { name = "Probe" };
            try
            {
                AssertOk(M(Mat + $" | set mainTexture = ref(\"{RefOf(tex)}\")"));
                Assert.AreSame(tex, _mat.GetTexture("_MainTex"));
                Assert.AreEqual("Probe", Cell(Q(Mat + " | select mainTexture.name")));
            }
            finally { Object.DestroyImmediate(tex); }
        }

        [Test]
        public void AShaderWithoutAMainColorSaysSo()
        {
            var shader = Shader.Find("Skybox/Procedural");
            Assume.That(shader != null, "Skybox/Procedural not available");
            _mat.shader = shader;
            Assert.AreEqual("null", Cell(Q(Mat + " | select color")));
            AssertFails(M(Mat + " | set color = \"#ff0000\""), "invalid-value", "has no main color");
        }
    }
}
