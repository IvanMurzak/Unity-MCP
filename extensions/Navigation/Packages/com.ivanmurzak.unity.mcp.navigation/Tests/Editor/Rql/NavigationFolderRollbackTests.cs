/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Navigation)     │
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
using UnityEditor.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Navigation
{
    /// <summary>
    /// NavMeshSurface.Bake() in a saved scene creates the scene's NavMesh folder and removes it again when Unity cannot save
    /// the asset; a folder that is on disk but not imported is refused before anything is created (dry runs report it).
    /// </summary>
    public class NavigationFolderRollbackTests : NavigationTestBase
    {
        private const string Folder = "Assets/RqlNavigationRollbackTests";
        private const string SceneFolder = Folder + "/Level";

        [SetUp]
        public void CreateFolder()
        {
            AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.CreateFolder("Assets", "RqlNavigationRollbackTests");
        }

        [TearDown]
        public void DeleteFolder()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(Folder);
        }

        [Test]
        public void AFailedBakeLeavesNoNewFolder()
        {
            LogAssert.ignoreFailingMessages = true; // Unity logs why it could not write the asset
            var s = CreateGround();
            s.name = new string('x', 300); // the asset name: longer than any file system allows
            Assert.IsTrue(EditorSceneManager.SaveScene(s.gameObject.scene, Folder + "/Level.unity"));
            var r = M(Surfaces + " | call Bake()");
            Assert.IsFalse(r.Ok, Table(r));
            Assert.IsFalse(AssetDatabase.IsValidFolder(SceneFolder), "Bake must remove the folder it created");

            s.name = "Ground";
            Ok(M(Surfaces + " | call Bake()")); // the same folder with a valid name stays
            Assert.AreEqual(SceneFolder + "/NavMesh-Ground.asset", AssetDatabase.GetAssetPath(s.navMeshData));
        }

        [Test]
        public void AFolderUnityHasNotImportedIsRefused()
        {
            var s = CreateGround();
            Assert.IsTrue(EditorSceneManager.SaveScene(s.gameObject.scene, Folder + "/Level.unity"));
            Directory.CreateDirectory(SceneFolder);
            try
            {
                var call = Surfaces + " | call Bake()";
                var dry = RqlUnity.Mutate(call, dryRun: true);
                Assert.IsFalse(dry.Ok, Table(dry));
                StringAssert.Contains("has not imported", Table(dry));
                var r = M(call);
                Assert.IsFalse(r.Ok, Table(r));
                StringAssert.Contains("has not imported", Table(r));
                Assert.IsNull(s.navMeshData, "nothing was baked");
                Assert.IsTrue(Directory.Exists(SceneFolder), "the folder on disk stays");
                Assert.IsFalse(Directory.Exists(SceneFolder + " 1"));
            }
            finally { Directory.Delete(SceneFolder, true); }
        }
    }
}
