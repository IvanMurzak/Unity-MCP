/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Timeline)       │
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

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Timelines
{
    /// <summary>
    /// Create / CreateSignal create the missing folders of their path and remove them again when Unity cannot create the
    /// asset; a folder that is on disk but not imported is refused before anything is created (dry runs report it).
    /// </summary>
    public class TimelineFolderRollbackTests : TimelineTestBase
    {
        // Longer than any file system allows: Unity cannot create the asset after the folders were made.
        private static readonly string TooLong = new string('x', 300);

        [Test]
        public void AFailedCreateLeavesNoNewFolders()
        {
            LogAssert.ignoreFailingMessages = true; // Unity logs why it could not write the asset
            Assert.IsFalse(M($"timeline | call Create(\"{Folder}/New/Deep/{TooLong}.playable\")").Ok);
            Assert.IsFalse(AssetDatabase.IsValidFolder($"{Folder}/New"), "Create must remove the folders it created");
            Assert.IsFalse(M($"timeline | call CreateSignal(\"{Folder}/New/Deep/{TooLong}.signal\")").Ok);
            Assert.IsFalse(AssetDatabase.IsValidFolder($"{Folder}/New"), "CreateSignal must remove the folders it created");

            Ok($"timeline | call CreateSignal(\"{Folder}/New/Deep/Boom.signal\")"); // the same folders with a valid name stay
            Assert.IsTrue(AssetDatabase.IsValidFolder($"{Folder}/New/Deep"));
        }

        [Test]
        public void AFolderUnityHasNotImportedIsRefused()
        {
            var ghost = $"{Folder}/Ghost";
            Directory.CreateDirectory(ghost);
            try
            {
                foreach (var call in new[]
                {
                    $"timeline | call Create(\"{ghost}/Sub/Intro.playable\")",
                    $"timeline | call CreateSignal(\"{ghost}/Sub/Boom.signal\")",
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
