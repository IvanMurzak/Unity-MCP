/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Terrain)        │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.IO;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Terrains
{
    /// <summary>
    /// Create (with a dataPath) / CreateLayer create the missing folders of their path and remove them again when Unity cannot
    /// create the asset; a folder that is on disk but not imported is refused before anything is created (dry runs report it).
    /// </summary>
    public class TerrainFolderRollbackTests : TerrainTestBase
    {
        // Longer than any file system allows: Unity cannot create the asset after the folders were made.
        private static readonly string TooLong = new string('x', 300);

        [Test]
        public void AFailedCreateLeavesNoNewFolders()
        {
            LogAssert.ignoreFailingMessages = true; // Unity logs why it could not write the asset
            EnsureFolder();
            Assert.IsFalse(M($"terrain | call Create(\"Island\", \"{Folder}/New/Deep/{TooLong}.asset\", 33)").Ok);
            Assert.IsFalse(AssetDatabase.IsValidFolder($"{Folder}/New"), "Create must remove the folders it created");
            Assert.IsNull(GameObject.Find("Island"), "no terrain without its data asset");
            Assert.IsFalse(M($"terrain | call CreateLayer(\"{Folder}/New/Deep/{TooLong}.terrainlayer\")").Ok);
            Assert.IsFalse(AssetDatabase.IsValidFolder($"{Folder}/New"), "CreateLayer must remove the folders it created");

            Ok($"terrain | call CreateLayer(\"{Folder}/New/Deep/Sand.terrainlayer\")"); // the same folders with a valid name stay
            Assert.IsTrue(AssetDatabase.IsValidFolder($"{Folder}/New/Deep"));
        }

        [Test]
        public void AFolderUnityHasNotImportedIsRefused()
        {
            EnsureFolder();
            var ghost = $"{Folder}/Ghost";
            Directory.CreateDirectory(ghost);
            try
            {
                foreach (var call in new[]
                {
                    $"terrain | call Create(\"Island\", \"{ghost}/Sub/Island.asset\", 33)",
                    $"terrain | call CreateLayer(\"{ghost}/Sub/Sand.terrainlayer\")",
                })
                {
                    var dry = RqlUnity.Mutate(call, dryRun: true);
                    Assert.IsFalse(dry.Ok, Table(dry));
                    StringAssert.Contains("has not imported", Table(dry));
                    Fails(call, "has not imported");
                }
                Assert.IsNull(GameObject.Find("Island"));
                Assert.IsTrue(Directory.Exists(ghost), "the folder on disk stays");
                Assert.IsFalse(Directory.Exists($"{ghost} 1"));
                Assert.IsFalse(Directory.Exists($"{ghost}/Sub"));
            }
            finally { Directory.Delete(ghost, true); }
        }
    }
}
