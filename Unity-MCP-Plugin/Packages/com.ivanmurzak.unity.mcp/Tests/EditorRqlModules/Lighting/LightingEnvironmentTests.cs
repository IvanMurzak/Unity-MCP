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
using System.Globalization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Lighting
{
    /// <summary><c>lighting | get environment</c>: the active scene's RenderSettings.</summary>
    public class LightingEnvironmentTests : LightingTestBase
    {
        private const string Env = "lighting | get environment";

        [Test]
        public void ReadsTheActiveScenesRenderSettings()
        {
            RenderSettings.fogDensity = 0.05f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            Assert.AreEqual(0.05, double.Parse(Cell(Q(Env + " | select fogDensity")), CultureInfo.InvariantCulture), 1e-5);
            Assert.AreEqual("Flat", Cell(Q(Env + " | select ambientMode")));
        }

        [Test]
        public void WritesTheEnvironment()
        {
            AssertOk(M(Env + " | set ambientMode = \"Flat\", ambientLight = \"#ff0000\", ambientIntensity = 1.5"));
            AssertOk(M(Env + " | set fog = true, fogMode = \"Linear\", fogStartDistance = 5, fogEndDistance = 50, fogColor = \"#808080\""));
            AssertOk(M(Env + " | set reflectionIntensity = 0.25, reflectionBounces = 2, defaultReflectionResolution = 256"));

            Assert.AreEqual(AmbientMode.Flat, RenderSettings.ambientMode);
            Assert.AreEqual(Color.red, RenderSettings.ambientLight);
            Assert.AreEqual(1.5f, RenderSettings.ambientIntensity, 1e-5f);
            Assert.IsTrue(RenderSettings.fog);
            Assert.AreEqual(FogMode.Linear, RenderSettings.fogMode);
            Assert.AreEqual(5f, RenderSettings.fogStartDistance, 1e-5f);
            Assert.AreEqual(50f, RenderSettings.fogEndDistance, 1e-5f);
            Assert.AreEqual(0.25f, RenderSettings.reflectionIntensity, 1e-5f);
            Assert.AreEqual(2, RenderSettings.reflectionBounces);
            Assert.AreEqual(256, RenderSettings.defaultReflectionResolution);
        }

        [Test]
        public void WritesAreUndoable()
        {
            RenderSettings.fog = false;
            RenderSettings.fogDensity = 0.01f;
            AssertOk(M(Env + " | set fog = true, fogDensity = 0.3"));
            Assert.IsTrue(RenderSettings.fog);

            Undo.PerformUndo();
            Assert.IsFalse(RenderSettings.fog, "one Undo step reverts the whole mutation");
            Assert.AreEqual(0.01f, RenderSettings.fogDensity, 1e-5f);
        }

        [Test]
        public void OutOfRangeValuesChangeNothing()
        {
            RenderSettings.fogDensity = 0.01f;
            RenderSettings.ambientIntensity = 1f;
            Fails(M(Env + " | set fogDensity = 0.5, ambientIntensity = 9"), "8");
            Assert.AreEqual(0.01f, RenderSettings.fogDensity, 1e-5f, "all-or-nothing");
            Assert.AreEqual(1f, RenderSettings.ambientIntensity, 1e-5f);

            Fails(M(Env + " | set defaultReflectionResolution = 300"), "power of two");
            Fails(M(Env + " | set reflectionBounces = 0"), "between 1 and 5");
        }

        [Test]
        public void TheSunTakesAGameObject()
        {
            var sun = CreateLight("Sun", LightType.Directional);
            AssertOk(M(Env + " | set sun = unity.find(\"Sun\")"));
            Assert.AreEqual(sun, RenderSettings.sun);
            Assert.AreEqual("Sun", Cell(Q(Env + " | select sun.gameObject.name")));
        }

        [Test]
        public void TheSkyboxTakesAnAssetPath()
        {
            EnsureTestFolder();
            var sky = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(sky, TestFolder + "/Sky.mat");
            AssertOk(M(Env + " | set skybox = \"" + TestFolder + "/Sky.mat\""));
            Assert.AreEqual(sky, RenderSettings.skybox);
        }

        [Test]
        public void AWriteMarksTheSceneModified()
        {
            var scene = SaveScene();
            Assert.IsFalse(scene.isDirty);
            AssertOk(M(Env + " | set fog = true"));
            Assert.IsTrue(SceneManager.GetActiveScene().isDirty);
        }
    }
}
