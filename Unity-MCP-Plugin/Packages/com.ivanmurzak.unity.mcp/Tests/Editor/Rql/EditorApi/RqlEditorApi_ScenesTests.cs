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
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.EditorApi
{
    public class RqlEditorApi_ScenesTests : EditorApiTestBase
    {
        [Test]
        public void NewSceneOpenSceneAndSave()
        {
            AssertOk(M($"editor | call NewScene(\"{Folder}/A.unity\", true)"));
            Assert.AreEqual("A", SceneManager.GetActiveScene().name);
            new GameObject("Marker");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssertOk(M("scenes | where isDirty | call Save()"));
            Assert.IsFalse(SceneManager.GetActiveScene().isDirty);

            AssertOk(M($"editor | call NewScene(\"{Folder}/B.unity\", true)"));
            Assert.AreEqual("B", SceneManager.GetActiveScene().name);
            AssertOk(M($"editor | call OpenScene(\"{Folder}/A.unity\")"));
            Assert.AreEqual("A", SceneManager.GetActiveScene().name);
            Assert.IsNotNull(GameObject.Find("Marker"));
        }

        [Test]
        public void AdditiveScenesSetActiveAndClose()
        {
            AssertOk(M($"editor | call NewScene(\"{Folder}/Main.unity\", true)"));
            AssertOk(M($"editor | call NewScene(\"{Folder}/Extra.unity\", true, true)"));
            Assert.AreEqual(2, SceneManager.sceneCount);
            Assert.AreEqual("Extra", SceneManager.GetActiveScene().name); // an additive new scene becomes the active one
            Assert.AreEqual("true", Cell(M("scenes | where name == \"Main\" | call SetActive()"), 0, 2));
            Assert.AreEqual("Main", SceneManager.GetActiveScene().name);
            // Unity's SetActiveScene answers false for the scene that is already active: the call reports true.
            Assert.AreEqual("true", Cell(M("scenes | where name == \"Main\" | call SetActive()"), 0, 2));
            AssertOk(M("scenes | where name == \"Extra\" | call Close()"));
            Assert.AreEqual(1, SceneManager.sceneCount);
        }

        // A non-additive NewScene replaces the open scenes: a path problem must fail before that, leaving the user's scene.
        [Test]
        public void NewSceneChecksThePathBeforeReplacingTheOpenScene()
        {
            AssertOk(M($"editor | call NewScene(\"{Folder}/Keep.unity\", true)"));
            new GameObject("Unsaved");
            AssertFails(M($"editor | call NewScene(\"{Folder}/Keep.unity\")"), "invalid-value", "already exists");
            AssertFails(M($"editor | call NewScene(\"{Folder}/Bad.txt\")"), "invalid-value", "must end with .unity");
            AssertFails(M("editor | call NewScene(\"Other/X.unity\")"), "invalid-value", "not under Assets/");
            AssertFails(M($"editor | call NewScene(\"{Folder}/Keep.unity\")", dryRun: true), "invalid-value", "already exists"); // a dry run reports it too
            Assert.AreEqual("Keep", SceneManager.GetActiveScene().name);
            Assert.IsNotNull(GameObject.Find("Unsaved"));
        }

        [Test]
        public void NewSceneAndSaveAsCreateMissingFoldersButNeverOverwrite()
        {
            AssertOk(M($"editor | call NewScene(\"{Folder}/Deep/Er/N.unity\", true)"));
            Assert.AreEqual($"{Folder}/Deep/Er/N.unity", SceneManager.GetActiveScene().path);
            AssertOk(M($"editor | call NewScene(\"{Folder}/Other.unity\", true, true)"));
            const string Taken = "scenes | where name == \"Other\" | call SaveAs(\"" + Folder + "/Deep/Er/N.unity\")";
            AssertFails(RqlUnity.Mutate(Taken, dryRun: true), "invalid-value", "already exists"); // the call validator: dry runs report it
            AssertFails(M(Taken), "invalid-value", "already exists");
            AssertOk(M($"scenes | where name == \"Other\" | call SaveAs(\"{Folder}/Copies/Other2.unity\")"));
            Assert.IsTrue(File.Exists($"{Folder}/Copies/Other2.unity"));
        }

        [Test]
        public void OpeningAMissingSceneFails()
        {
            var r = M("editor | call OpenScene(\"Assets/NoSuchScene.unity\")");
            Assert.IsFalse(r.Ok, Table(r));
        }

        // Wave 7: a scene file edited outside the Editor stayed stale in the open scene even after RefreshAssets.
        static string SavedSceneWith(string name, string objectName, bool additive = false)
        {
            var path = $"{Folder}/{name}.unity";
            AssertOk(M($"editor | call NewScene(\"{path}\", true, {(additive ? "true" : "false")})"));
            var scene = SceneManager.GetSceneByPath(path);
            SceneManager.MoveGameObjectToScene(new GameObject(objectName), scene);
            Assert.IsTrue(EditorSceneManager.SaveScene(scene));
            return path;
        }

        static void RenameOnDisk(string path, string from, string to)
        {
            var text = File.ReadAllText(path);
            StringAssert.Contains("m_Name: " + from, text);
            File.WriteAllText(path, text.Replace("m_Name: " + from, "m_Name: " + to));
        }

        [Test]
        public void RefreshAssetsReloadsAnOpenSceneChangedOnDisk()
        {
            var path = SavedSceneWith("Ext", "Before");
            RenameOnDisk(path, "Before", "ChangedOutside");

            var r = M("editor | call RefreshAssets()");
            AssertOk(r);
            StringAssert.Contains($"reloaded '{path}'", Table(r));
            Assert.IsNotNull(GameObject.Find("ChangedOutside"));
            Assert.IsNull(GameObject.Find("Before"));
            Assert.AreEqual(path, SceneManager.GetActiveScene().path);
        }

        [Test]
        public void RefreshAssetsKeepsADirtySceneAndDoesNotSaveItOverTheFile()
        {
            var path = SavedSceneWith("ExtDirty", "Before");
            RenameOnDisk(path, "Before", "ChangedOutside");
            new GameObject("UnsavedEdit");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            var r = M("editor | call RefreshAssets()");
            AssertOk(r);
            StringAssert.Contains("unsaved changes in the Editor", Table(r));
            Assert.IsNotNull(GameObject.Find("UnsavedEdit"));
            Assert.IsNotNull(GameObject.Find("Before"));
            Assert.IsTrue(SceneManager.GetActiveScene().isDirty);
            StringAssert.Contains("m_Name: ChangedOutside", File.ReadAllText(path)); // no save after the call wrote over it
        }

        // The first of two scenes, and the active one: reloaded at its place in the Hierarchy and active again.
        [Test]
        public void RefreshAssetsReloadsAnAdditiveSceneInPlace()
        {
            var main = SavedSceneWith("ExtMain", "Before");
            var extra = SavedSceneWith("ExtExtra", "ExtraObject", additive: true);
            SceneManager.SetActiveScene(SceneManager.GetSceneByPath(main));
            RenameOnDisk(main, "Before", "ChangedOutside");

            var r = M("editor | call RefreshAssets()");
            AssertOk(r);
            StringAssert.Contains($"reloaded '{main}'", Table(r));
            StringAssert.DoesNotContain(extra, Table(r));
            Assert.AreEqual(2, SceneManager.sceneCount);
            Assert.AreEqual(main, SceneManager.GetSceneAt(0).path); // its place in the Hierarchy
            Assert.AreEqual(main, SceneManager.GetActiveScene().path);
            Assert.IsNotNull(GameObject.Find("ChangedOutside"));
            Assert.IsNotNull(GameObject.Find("ExtraObject"));
        }

        // Another tool's refresh (script-update-or-create, tests-run …) imported the changed file first: RefreshAssets still
        // reloads the scene, because it compares the file with what the Editor loaded, not with the last import.
        [Test]
        public void RefreshAssetsReloadsASceneAnEarlierRefreshAlreadyImported()
        {
            var path = SavedSceneWith("ExtEarlier", "Before");
            RenameOnDisk(path, "Before", "ChangedOutside");
            AssetDatabase.Refresh();
            Assert.IsNotNull(GameObject.Find("Before")); // Unity imported the file but kept the open scene

            var r = M("editor | call RefreshAssets()");
            AssertOk(r);
            StringAssert.Contains($"reloaded '{path}'", Table(r));
            Assert.IsNotNull(GameObject.Find("ChangedOutside"));
        }

        // One loaded scene next to an unloaded one in the Hierarchy: reloaded, and the unloaded scene stays listed.
        [Test]
        public void RefreshAssetsReloadsTheOnlyLoadedSceneAndKeepsUnloadedOnes()
        {
            var main = SavedSceneWith("ExtLoaded", "Before");
            var extra = SavedSceneWith("ExtUnloaded", "ExtraObject", additive: true);
            Assert.IsTrue(EditorSceneManager.CloseScene(SceneManager.GetSceneByPath(extra), removeScene: false));
            RenameOnDisk(main, "Before", "ChangedOutside");

            var r = M("editor | call RefreshAssets()");
            AssertOk(r);
            StringAssert.Contains($"reloaded '{main}'", Table(r));
            Assert.AreEqual(2, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByPath(extra).isLoaded);
            Assert.IsNotNull(GameObject.Find("ChangedOutside"));
        }

        [Test]
        public void RefreshAssetsKeepsASceneWhoseFileWasDeleted()
        {
            var main = SavedSceneWith("ExtMain2", "MainObject");
            var gone = SavedSceneWith("ExtGone", "Kept", additive: true);
            File.Delete(gone);
            File.Delete(gone + ".meta");

            var r = M("editor | call RefreshAssets()");
            AssertOk(r);
            StringAssert.Contains($"'{gone}' was deleted or moved on disk", Table(r));
            Assert.AreEqual(2, SceneManager.sceneCount);
            Assert.IsNotNull(GameObject.Find("Kept"));
            Assert.IsNotNull(GameObject.Find("MainObject"));
        }

        [Test]
        public void RefreshAssetsWithNoChangeReloadsNothing()
        {
            SavedSceneWith("ExtSame", "Kept");
            var kept = GameObject.Find("Kept");
            var r = M("editor | call RefreshAssets()");
            AssertOk(r);
            StringAssert.DoesNotContain("reloaded", Table(r));
            Assert.AreSame(kept, GameObject.Find("Kept")); // the same object: the scene was not loaded again
        }

        // An edit saved in the Editor is no outside change; and a reload records the new file, so the next call reloads nothing.
        [Test]
        public void RefreshAssetsIgnoresEditorSavesAndReloadsOnce()
        {
            var path = SavedSceneWith("ExtSaves", "Kept");
            new GameObject("SavedInEditor");
            Assert.IsTrue(EditorSceneManager.SaveScene(SceneManager.GetActiveScene()));
            var kept = GameObject.Find("Kept");
            Assert.AreEqual("refreshed", Cell(M("editor | call RefreshAssets()"), 0, 2), "after a save in the Editor");
            Assert.AreSame(kept, GameObject.Find("Kept"));

            RenameOnDisk(path, "Kept", "ChangedOutside");
            StringAssert.Contains("reloaded", Table(M("editor | call RefreshAssets()")));
            var reloaded = GameObject.Find("ChangedOutside");
            Assert.AreEqual("refreshed", Cell(M("editor | call RefreshAssets()"), 0, 2), "after a reload");
            Assert.AreSame(reloaded, GameObject.Find("ChangedOutside"));
        }

        // Saving a copy elsewhere writes another file: an outside change of the scene's own file is still reloaded.
        [Test]
        public void RefreshAssetsStillSeesAChangeAfterASaveAsCopy()
        {
            var path = SavedSceneWith("ExtCopy", "Before");
            RenameOnDisk(path, "Before", "ChangedOutside");
            Assert.IsTrue(EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), $"{Folder}/ExtCopy2.unity", saveAsCopy: true));
            StringAssert.Contains($"reloaded '{path}'", Table(M("editor | call RefreshAssets()")));
            Assert.IsNotNull(GameObject.Find("ChangedOutside"));
        }

        [Test]
        public void SaveScenesSavesEveryModifiedScene()
        {
            AssertOk(M($"editor | call NewScene(\"{Folder}/S.unity\", true)"));
            new GameObject("Dirty");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssertOk(M("editor | call SaveScenes()"));
            Assert.IsFalse(SceneManager.GetActiveScene().isDirty);
        }
    }
}
