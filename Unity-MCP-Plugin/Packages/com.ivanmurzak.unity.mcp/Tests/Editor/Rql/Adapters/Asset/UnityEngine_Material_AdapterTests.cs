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
            Assert.AreEqual("Sprites-Default", renderer.sharedMaterial!.name);
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

        // URP / HDRP Lit keep a hidden legacy _Color / _MainTex but render _BaseColor / _BaseMap: a write to the legacy name
        // also reaches the main property, or the receipt reports a change nothing shows (the tutorial's white ground).
        private const string UrpLikeShader =
            "Shader \"Hidden/UnityMcpTests/UrpLike\" { Properties { " +
            "[MainColor] _BaseColor(\"Base Color\", Color) = (1,1,1,1) [MainTexture] _BaseMap(\"Base Map\", 2D) = \"white\" {} " +
            "[HideInInspector] _Color(\"Color\", Color) = (1,1,1,1) [HideInInspector] _MainTex(\"Main Tex\", 2D) = \"white\" {} } " +
            "SubShader { Pass { } } }";

        [Test]
        public void ALegacyColorOrTextureWriteReachesTheMainPropertyInUrpLikeShaders() => WithShader(UrpLikeShader, () =>
        {
            var tex = new Texture2D(2, 2) { name = "Probe" };
            try
            {
                AssertOk(M(Mat + " | set properties[\"_Color\"] = \"#1E4D1E\""));
                Assert.AreEqual(_mat.GetColor("_Color"), _mat.GetColor("_BaseColor"));
                AssertTuple("(0.117647, 0.301961, 0.117647, 1)", Cell(Q(Mat + " | select color")));

                AssertOk(M(Mat + $" | set properties[\"_MainTex\"] = ref(\"{RefOf(tex)}\")"));
                Assert.AreEqual(tex, _mat.GetTexture("_BaseMap"));

                // The legacy name reads the rendered property: a relative write starts from it, not from a stale _Color.
                AssertOk(M(Mat + " | set properties[\"_BaseColor\"] = \"#ff0000\""));
                AssertTuple("(1, 0, 0, 1)", Cell(Q(Mat + " | select properties[\"_Color\"]")));
                AssertOk(M(Mat + " | set properties[\"_Color\"].a = 0.5"));
                Assert.AreEqual(new Color(1, 0, 0, 0.5f), _mat.GetColor("_BaseColor"));

                // Listed once, by the rendered name, so a loop over the map does not change the same color twice.
                AssertOk(M(Mat + " | get properties | where Key ends-with \"Color\" | set Value = \"#0000ff\""));
                Assert.AreEqual(Color.blue, _mat.GetColor("_BaseColor"));
                Assert.AreEqual("_BaseColor", Cell(Q(Mat + " | get properties | where Key ends-with \"Color\" | select Key")));
            }
            finally { Object.DestroyImmediate(tex); }
        });

        [Test]
        public void ALegacyNameThatIsTheMainPropertyIsWrittenOnce() =>
            Assert.IsNull(UnityEngine_Material_Adapter.LegacyAliasOf(_mat, "_Color"), "Standard renders _Color itself");

        // Each case: a shader source, then a check on the material made with it.
        private void WithShader(string source, System.Action check)
        {
            var shader = UnityEditor.ShaderUtil.CreateShaderAsset(source);
            try
            {
                _mat.shader = shader;
                check();
            }
            finally { Object.DestroyImmediate(shader); }
        }

        // A URP-style shader without [MainColor] (custom or older code): the hidden _Color is a stand-in, _BaseColor renders.
        [Test]
        public void AHiddenLegacyColorNextToAVisibleBaseColorIsNotTheMainColor() => WithShader(
            "Shader \"Hidden/UnityMcpTests/NoMainAttribute\" { Properties { _BaseColor(\"Base Color\", Color) = (1,1,1,1) " +
            "[HideInInspector] _Color(\"Color\", Color) = (1,1,1,1) } SubShader { Pass { } } }",
            () =>
            {
                AssertOk(M(Mat + " | set color = \"#ff0000\""));
                Assert.AreEqual(Color.red, _mat.GetColor("_BaseColor"));
                AssertOk(M(Mat + " | set properties[\"_Color\"] = \"#00ff00\""));
                Assert.AreEqual(Color.green, _mat.GetColor("_BaseColor"), "the legacy name reaches the rendered one");
            });

        // URP 2D sprite shaders hide _Color and still render it: alone, the hidden legacy name stays the main color.
        [Test]
        public void AHiddenLegacyColorAloneIsTheMainColor() => WithShader(
            "Shader \"Hidden/UnityMcpTests/SpriteLike\" { Properties { [HideInInspector] _Color(\"Tint\", Color) = (1,1,1,1) } " +
            "SubShader { Pass { } } }",
            () =>
            {
                AssertOk(M(Mat + " | set color = \"#ff0000\""));
                Assert.AreEqual(Color.red, _mat.GetColor("_Color"));
            });

        // A shader that names its color freely: the refusal lists the color properties to write by name.
        [Test]
        public void AShaderWithoutAMainColorListsItsColorProperties() => WithShader(
            "Shader \"Hidden/UnityMcpTests/Tint\" { Properties { _Tint(\"Tint\", Color) = (1,1,1,1) _Glow(\"Glow\", Color) = (0,0,0,1) " +
            "[HideInInspector] _Internal(\"Internal\", Color) = (1,1,1,1) _Albedo(\"Albedo\", 2D) = \"white\" {} } SubShader { Pass { } } }",
            () =>
            {
                Assert.AreEqual("null", Cell(Q(Mat + " | select color")));
                AssertFails(M(Mat + " | set color = \"#ff0000\""), "invalid-value", "properties[\"_Tint\"] (_Tint, _Glow)"); // _Internal is hidden
                AssertFails(M(Mat + " | set mainTexture = null"), "invalid-value", "properties[\"_Albedo\"] (_Albedo)");
                AssertOk(M(Mat + " | set properties[\"_Tint\"] = \"#ff0000\""));
                Assert.AreEqual(Color.red, _mat.GetColor("_Tint"));
            });
    }
}
