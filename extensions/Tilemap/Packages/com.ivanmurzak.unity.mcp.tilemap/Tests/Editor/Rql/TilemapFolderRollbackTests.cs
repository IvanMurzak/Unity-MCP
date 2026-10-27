/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Tilemap)        │
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
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Tilemaps
{
    /// <summary>
    /// CreateTile / CreateRuleTile create the missing folders of their path (with the core's folder helper; Tilemap no longer
    /// has its own) and remove them again when Unity cannot create the asset; a folder that is on disk but not imported is
    /// refused before anything is created (dry runs report it).
    /// </summary>
    public class TilemapFolderRollbackTests : TilemapTestBase
    {
        // Longer than any file system allows: Unity cannot create the asset after the folders were made.
        private static readonly string TooLong = new string('x', 300);

        [Test]
        public void AFailedCreateLeavesNoNewFolders()
        {
            LogAssert.ignoreFailingMessages = true; // Unity logs why it could not write the asset
            EnsureFolder();
            Assert.IsFalse(M($"tilemap | call CreateTile(\"{Folder}/New/Deep/{TooLong}.asset\")").Ok);
            Assert.IsFalse(AssetDatabase.IsValidFolder($"{Folder}/New"), "CreateTile must remove the folders it created");
            Assert.IsFalse(M($"tilemap | call CreateRuleTile(\"{Folder}/New/Deep/{TooLong}.asset\")").Ok);
            Assert.IsFalse(AssetDatabase.IsValidFolder($"{Folder}/New"), "CreateRuleTile must remove the folders it created");

            Ok($"tilemap | call CreateTile(\"{Folder}/New/Deep/Grass.asset\")"); // the same folders with a valid name stay
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
                    $"tilemap | call CreateTile(\"{ghost}/Sub/Grass.asset\")",
                    $"tilemap | call CreateRuleTile(\"{ghost}/Sub/Ground.asset\")",
                })
                {
                    var dry = RqlUnity.Mutate(call, dryRun: true);
                    Assert.IsFalse(dry.Ok, Table(dry));
                    StringAssert.Contains("has not imported", Table(dry));
                    Fails(call, "has not imported");
                }
                Assert.IsTrue(Directory.Exists(ghost), "the folder on disk stays");
                Assert.IsFalse(Directory.Exists($"{ghost} 1"));
                Assert.IsFalse(Directory.Exists($"{ghost}/Sub"));
            }
            finally { Directory.Delete(ghost, true); }
        }
    }
}
