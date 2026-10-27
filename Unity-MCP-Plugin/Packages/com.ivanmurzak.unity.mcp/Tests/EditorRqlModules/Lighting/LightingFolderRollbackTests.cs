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
using System.IO;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Lighting
{
    /// <summary>
    /// CreateSettings and a Custom ReflectionProbe's Bake() create the missing folders they write into and remove them again
    /// when Unity cannot write the asset; a folder that is on disk but not imported is refused before anything is created
    /// (dry runs report it).
    /// </summary>
    public class LightingFolderRollbackTests : LightingTestBase
    {
        private const string Probes = "objects | get components | where $ is ReflectionProbe";
        private const string SceneFolder = TestFolder + "/LightingTest";

        // Longer than any file system allows: Unity cannot write the asset after the folders were made.
        private static readonly string TooLong = new string('x', 300);

        [Test]
        public void AFailedCreateSettingsLeavesNoNewFolders()
        {
            LogAssert.ignoreFailingMessages = true; // Unity logs why it could not write the asset
            EnsureTestFolder();
            Assert.IsFalse(M($"lighting | call CreateSettings(\"{TestFolder}/New/Deep/{TooLong}.lighting\")").Ok);
            Assert.IsFalse(AssetDatabase.IsValidFolder($"{TestFolder}/New"), "CreateSettings must remove the folders it created");
            Assert.IsFalse(Lightmapping.TryGetLightingSettings(out _), "nothing is assigned");

            AssertOk(M($"lighting | call CreateSettings(\"{TestFolder}/New/Deep/Day.lighting\")")); // a valid name keeps them
            Assert.IsTrue(AssetDatabase.IsValidFolder($"{TestFolder}/New/Deep"));
        }

        [Test]
        public void AFailedProbeBakeLeavesNoNewFolder()
        {
            LogAssert.ignoreFailingMessages = true; // Unity logs why it could not write the asset
            var probe = new GameObject(TooLong).AddComponent<ReflectionProbe>(); // the texture's name
            probe.resolution = 16;
            probe.mode = ReflectionProbeMode.Custom;
            probe.hdr = true;
            SaveScene();
            Assert.IsFalse(M(Probes + " | call Bake()").Ok);
            Assert.IsFalse(AssetDatabase.IsValidFolder(SceneFolder), "Bake must remove the folder it created");

            probe.name = "Mirror";
            Assert.AreEqual(SceneFolder + "/ReflectionProbe-Mirror.exr", CallResult(M(Probes + " | call Bake()")));
        }

        [Test]
        public void AFolderUnityHasNotImportedIsRefused()
        {
            var probe = new GameObject("Mirror").AddComponent<ReflectionProbe>();
            probe.resolution = 16;
            probe.mode = ReflectionProbeMode.Custom;
            probe.hdr = true;
            SaveScene();
            Directory.CreateDirectory(SceneFolder);
            try
            {
                foreach (var call in new[]
                {
                    $"lighting | call CreateSettings(\"{SceneFolder}/Sub/Day.lighting\")",
                    Probes + " | call Bake()",
                })
                {
                    Fails(RqlUnity.Mutate(call, dryRun: true), "has not imported");
                    Fails(M(call), "has not imported");
                }
                Assert.IsNull(probe.customBakedTexture, "nothing was baked");
                Assert.IsTrue(Directory.Exists(SceneFolder), "the folder on disk stays");
                Assert.IsFalse(Directory.Exists(SceneFolder + " 1"));
                Assert.IsFalse(Directory.Exists(SceneFolder + "/Sub"));
            }
            finally { Directory.Delete(SceneFolder, true); }
        }
    }
}
