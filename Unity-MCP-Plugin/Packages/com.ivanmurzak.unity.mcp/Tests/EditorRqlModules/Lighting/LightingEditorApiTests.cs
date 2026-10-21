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
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.Rql.Lighting;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Lighting
{
    /// <summary>The lighting source: Lighting Settings assets, baking.</summary>
    public class LightingEditorApiTests : LightingTestBase
    {
        [Test]
        public void SettingsIsNullUntilAnAssetIsAssigned()
        {
            Assert.IsNull(LightingEditorApi.Instance.settings);
            Assert.AreEqual("null", Cell(Q("lighting | select settings")));
        }

        [Test]
        public void CreateSettingsMakesAnAssetAndAssignsIt()
        {
            var path = TestFolder + "/Night/Night.lighting";   // missing folders are created
            Ok(M($"lighting | call CreateSettings(\"{path}\")"));
            var asset = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            Assert.IsNotNull(asset);
            Assert.AreEqual(asset, Lightmapping.lightingSettings);
            Assert.AreEqual(asset, LightingEditorApi.Instance.settings);
        }

        [Test]
        public void CreateSettingsRejectsBadPaths()
        {
            Fails(M("lighting | call CreateSettings(\"Night.lighting\")"), "Assets/");
            Fails(M("lighting | call CreateSettings(\"Assets/Night.asset\")"), ".lighting");
            Ok(M($"lighting | call CreateSettings(\"{TestFolder}/Day.lighting\")"));
            Fails(M($"lighting | call CreateSettings(\"{TestFolder}/Day.lighting\")"), "UseSettings");
        }

        [Test]
        public void SettingsAreWritableAndStayOnTheAsset()
        {
            Ok(M($"lighting | call CreateSettings(\"{TestFolder}/Bake.lighting\")"));
            Ok(M("lighting | get settings | set lightmapper = \"ProgressiveCPU\", lightmapResolution = 7, lightmapMaxSize = 256, bakedGI = true"));
            var asset = AssetDatabase.LoadAssetAtPath<LightingSettings>(TestFolder + "/Bake.lighting");
            if (CpuLightmapperSupported()) Assert.AreEqual(LightingSettings.Lightmapper.ProgressiveCPU, asset.lightmapper);
            Assert.AreEqual(7f, asset.lightmapResolution, 1e-4f);
            Assert.AreEqual(256, asset.lightmapMaxSize);
            Assert.IsTrue(asset.bakedGI);
        }

        [Test]
        public void UseSettingsAssignsAnExistingAsset()
        {
            EnsureTestFolder();
            var asset = new LightingSettings { lightmapResolution = 3 };
            AssetDatabase.CreateAsset(asset, TestFolder + "/Existing.lighting");
            Ok(M($"lighting | call UseSettings(\"{TestFolder}/Existing.lighting\")"));
            Assert.AreEqual(asset, Lightmapping.lightingSettings);
            Assert.AreEqual(3.0, double.Parse(Cell(Q("lighting | select settings.lightmapResolution")), System.Globalization.CultureInfo.InvariantCulture), 1e-4);
        }

        [Test]
        public void AssigningSettingsIsUndoable()
        {
            Ok(M($"lighting | call CreateSettings(\"{TestFolder}/First.lighting\")"));
            var first = Lightmapping.lightingSettings;
            Ok(M($"lighting | call CreateSettings(\"{TestFolder}/Second.lighting\")"));
            Assert.AreNotEqual(first, Lightmapping.lightingSettings);
            Undo.PerformUndo();
            Assert.AreEqual(first, LightingEditorApi.Instance.settings);
        }

        [Test]
        public void BakingNeedsSavedScenes()
        {
            Fails(M("lighting | call Bake()"), "SaveAs");
            Assert.IsFalse(Lightmapping.isRunning);
        }

        [Test]
        public void CancelBakeWhenIdleReturnsFalse()
        {
            Assert.AreEqual("false", CallResult(M("lighting | call CancelBake()")));
            Assert.AreEqual("false", Cell(Q("lighting | select isBaking")));
            Assert.AreEqual(0.0, double.Parse(Cell(Q("lighting | select bakeProgress")), System.Globalization.CultureInfo.InvariantCulture));
        }

        [Test]
        public void BakeRefusesTheGpuLightmapperWithoutAGraphicsDevice()
        {
            if (HasGraphicsDevice) Assert.Ignore("needs an Editor without a graphics device (-nographics)");
            SaveScene();
            Ok(M($"lighting | call CreateSettings(\"{TestFolder}/Gpu.lighting\")"));
            Ok(M("lighting | get settings | set lightmapper = \"ProgressiveGPU\""));
            Fails(M("lighting | call Bake()"), "ProgressiveCPU");
            Assert.IsFalse(Lightmapping.isRunning);
        }

        [Test]
        public void ABlockingBakeProducesLightingDataAndClearRemovesIt()
        {
            // Without a graphics device only the CPU lightmapper bakes (Bake refuses the GPU one, see above).
            if (!HasGraphicsDevice && !CpuLightmapperSupported()) Assert.Ignore("the CPU lightmapper is not supported here and there is no graphics device");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.ContributeGI);
            CreateLight("Sun", LightType.Directional, LightmapBakeType.Baked);
            SaveScene();
            // The smallest bake that still produces a lightmap: CPU, tiny maps, few samples.
            Ok(M($"lighting | call CreateSettings(\"{TestFolder}/Tiny.lighting\")"));
            Ok(M("lighting | get settings | set lightmapper = \"ProgressiveCPU\", bakedGI = true, realtimeGI = false, lightmapResolution = 1, " +
                 "lightmapMaxSize = 32, directSampleCount = 1, indirectSampleCount = 8, environmentSampleCount = 8"));
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Assert.AreEqual("true", CallResult(M("lighting | call Bake(true)")));
            Assert.IsFalse(Lightmapping.isRunning);
            Assert.AreEqual("true", Cell(Q("lighting | select hasBakedData")));
            Assert.Greater(LightmapSettings.lightmaps.Length, 0, "the static plane gets a lightmap");

            Assert.AreEqual("true", CallResult(M("lighting | call ClearBakedData()")));
            Assert.IsNull(Lightmapping.lightingDataAsset);
            Assert.AreEqual(0, LightmapSettings.lightmaps.Length);
        }

        [Test]
        public void BakeReflectionProbesBakesEveryActiveProbe()
        {
            var baked = new GameObject("Mirror").AddComponent<ReflectionProbe>();
            baked.mode = ReflectionProbeMode.Custom;
            baked.resolution = 16;
            new GameObject("Live").AddComponent<ReflectionProbe>().mode = ReflectionProbeMode.Realtime;
            var off = new GameObject("Off").AddComponent<ReflectionProbe>();
            off.mode = ReflectionProbeMode.Custom;
            off.gameObject.SetActive(false);
            SaveScene();

            Assert.AreEqual("2", CallResult(M("lighting | call BakeReflectionProbes()")));
            Assert.IsNotNull(baked.customBakedTexture);
            StringAssert.StartsWith(TestFolder + "/LightingTest/ReflectionProbe-Mirror", AssetDatabase.GetAssetPath(baked.customBakedTexture));
            Assert.IsNull(off.customBakedTexture, "inactive probes are skipped");
        }
    }
}
