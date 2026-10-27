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
            AssertFails(M($"scenes | where name == \"Other\" | call SaveAs(\"{Folder}/Deep/Er/N.unity\")"), "call-threw", "already exists");
            AssertOk(M($"scenes | where name == \"Other\" | call SaveAs(\"{Folder}/Copies/Other2.unity\")"));
            Assert.IsTrue(File.Exists($"{Folder}/Copies/Other2.unity"));
        }

        [Test]
        public void OpeningAMissingSceneFails()
        {
            var r = M("editor | call OpenScene(\"Assets/NoSuchScene.unity\")");
            Assert.IsFalse(r.Ok, Table(r));
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
