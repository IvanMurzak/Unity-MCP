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

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.EditorApi
{
    public class RqlEditorApi_PrefabsTests : EditorApiTestBase
    {
        private string MakePrefab()
        {
            new GameObject("Crate", typeof(BoxCollider));
            AssertOk(M($"objects | where name == \"Crate\" | call SaveAsPrefab(\"{Folder}/Crate.prefab\")"));
            return $"{Folder}/Crate.prefab";
        }

        [Test]
        public void SaveAsPrefabConnectsTheInstance()
        {
            var path = MakePrefab();
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(GameObject.Find("Crate")));
        }

        [Test]
        public void InstantiatePrefabUnderAParentAtAPosition()
        {
            var path = MakePrefab();
            var parent = new GameObject("Props");
            var parentRef = Cell(Q("objects | where name == \"Props\" | select $ref"));
            var r = M($"editor | call InstantiatePrefab(\"{path}\", ref(\"{parentRef}\"), \"(1, 2, 3)\")");
            AssertOk(r);
            var instance = parent.transform.GetChild(0).gameObject;
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(instance));
            Assert.AreEqual(new Vector3(1, 2, 3), instance.transform.localPosition);
            Undo.PerformUndo();
            Assert.AreEqual(0, parent.transform.childCount);
        }

        [Test]
        public void EditAPrefabInPrefabModeAndSave()
        {
            var path = MakePrefab();
            AssertOk(M($"editor | call OpenPrefab(\"{path}\")"));
            Assert.AreEqual("Crate", Cell(Q("prefab | select name")));
            AssertOk(M("prefab | call AddComponent(\"Rigidbody\")"));
            AssertOk(M("editor | call ClosePrefab()"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Rigidbody>());
            Assert.IsNull(PrefabStageUtility.GetCurrentPrefabStage());
        }

        [Test]
        public void DuplicateKeepsThePrefabLinkAndANewName()
        {
            MakePrefab();
            AssertOk(M("objects | where name == \"Crate\" | call Duplicate()"));
            var copies = Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None);
            Assert.AreEqual(2, copies.Length);
            foreach (var c in copies) Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(c.gameObject), c.name);
            Assert.AreNotEqual(copies[0].name, copies[1].name);
        }

        [Test]
        public void DuplicatePlainObjects()
        {
            var a = new GameObject("Plain");
            a.transform.position = new Vector3(4, 0, 0);
            AssertOk(M("objects | where name == \"Plain\" | call Duplicate()"));
            Assert.AreEqual(2, Q("objects | where name starts-with \"Plain\" | select name").Rows!.Count);
        }
    }
}
