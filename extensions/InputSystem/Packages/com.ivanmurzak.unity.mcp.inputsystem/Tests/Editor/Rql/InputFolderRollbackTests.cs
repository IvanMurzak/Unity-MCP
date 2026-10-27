/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-InputSystem)    │
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

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.InputSystem
{
    /// <summary>
    /// CreateAsset creates the missing folders of its path and removes them again when the file cannot be written; a folder
    /// that is on disk but not imported is refused before anything is created (dry runs report it).
    /// </summary>
    public class InputFolderRollbackTests : InputSystemTestBase
    {
        // Longer than any file system allows: the file cannot be written after the folders were made.
        private static readonly string TooLong = new string('x', 300);

        [Test]
        public void AFailedCreateLeavesNoNewFolders()
        {
            Assert.IsFalse(M($"input | call CreateAsset(\"{Folder}/New/Deep/{TooLong}.inputactions\")").Ok);
            Assert.IsFalse(AssetDatabase.IsValidFolder($"{Folder}/New"), "CreateAsset must remove the folders it created");
            Assert.IsFalse(Directory.Exists($"{Folder}/New"));

            // A bad map name is refused before any folder is made.
            Fails($"input | call CreateAsset(\"{Folder}/New/Deep/Bad.inputactions\", \"A/B\")", "must not");
            Assert.IsFalse(AssetDatabase.IsValidFolder($"{Folder}/New"));

            Ok($"input | call CreateAsset(\"{Folder}/New/Deep/Player.inputactions\")"); // the same folders with a valid name stay
            Assert.IsTrue(AssetDatabase.IsValidFolder($"{Folder}/New/Deep"));
        }

        [Test]
        public void AFolderUnityHasNotImportedIsRefused()
        {
            var ghost = $"{Folder}/Ghost";
            Directory.CreateDirectory(ghost);
            try
            {
                var call = $"input | call CreateAsset(\"{ghost}/Sub/Player.inputactions\")";
                var dry = RqlUnity.Mutate(call, dryRun: true);
                Assert.IsFalse(dry.Ok, Table(dry));
                StringAssert.Contains("has not imported", Table(dry));
                Fails(call, "has not imported");
                Assert.IsTrue(Directory.Exists(ghost), "the folder on disk stays");
                Assert.IsFalse(Directory.Exists($"{ghost} 1"));
                Assert.IsFalse(Directory.Exists($"{ghost}/Sub"));
            }
            finally { Directory.Delete(ghost, true); }
        }
    }
}
