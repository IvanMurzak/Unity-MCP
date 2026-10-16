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
            AssertOk(M("scenes | where name == \"Main\" | call SetActive()"));
            Assert.AreEqual("Main", SceneManager.GetActiveScene().name);
            AssertOk(M("scenes | where name == \"Extra\" | call Close()"));
            Assert.AreEqual(1, SceneManager.sceneCount);
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
