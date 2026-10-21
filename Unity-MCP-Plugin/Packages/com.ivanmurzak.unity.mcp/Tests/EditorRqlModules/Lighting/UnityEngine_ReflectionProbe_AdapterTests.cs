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
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Lighting
{
    public class UnityEngine_ReflectionProbe_AdapterTests : LightingTestBase
    {
        private const string Probes = "objects | get components | where $ is ReflectionProbe";

        private ReflectionProbe _probe = null!;

        [SetUp]
        public void CreateProbe()
        {
            _probe = new GameObject("Mirror").AddComponent<ReflectionProbe>();
            _probe.resolution = 16;
        }

        [Test]
        public void ARealtimeProbeReRenders()
        {
            _probe.mode = ReflectionProbeMode.Realtime;
            Assert.AreEqual("", CallResult(M(Probes + " | call Bake()")));
        }

        [Test]
        public void ABakedProbeNeedsASavedScene()
        {
            _probe.mode = ReflectionProbeMode.Baked;
            Fails(M(Probes + " | call Bake()"), "SaveAs");
        }

        [Test]
        public void ABakedProbeIsStoredNextToTheScene()
        {
            _probe.mode = ReflectionProbeMode.Baked;
            SaveScene();
            var path = CallResult(M(Probes + " | call Bake()"));
            Assert.AreEqual(TestFolder + "/LightingTest/ReflectionProbe-Mirror.exr", path);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture>(path));
            Assert.AreEqual(path, AssetDatabase.GetAssetPath(_probe.bakedTexture), "the probe shows its bake");
            Assert.AreEqual(path, CallResult(M(Probes + " | call Bake()")), "a second bake replaces the first texture");
        }

        [Test]
        public void ACustomProbeShowsItsBakeAndRebakesInPlace()
        {
            _probe.mode = ReflectionProbeMode.Custom;
            _probe.hdr = false;
            SaveScene();
            var first = CallResult(M(Probes + " | call Bake()"));
            Assert.AreEqual(TestFolder + "/LightingTest/ReflectionProbe-Mirror.png", first);
            Assert.AreEqual(first, AssetDatabase.GetAssetPath(_probe.customBakedTexture));

            Assert.AreEqual(first, CallResult(M(Probes + " | call Bake()")), "a second bake replaces the first texture");
        }
    }
}
