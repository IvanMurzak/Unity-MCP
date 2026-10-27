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
using com.IvanMurzak.Unity.MCP.Editor.Rql;
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

        // Like CreateMaterial / CopyAsset / NewScene: the missing folders of a new prefab path are created.
        [Test]
        public void SaveAsPrefabCreatesMissingFolders()
        {
            new GameObject("Crate");
            AssertOk(M($"objects | where name == \"Crate\" | call SaveAsPrefab(\"{Folder}/New/Deep/Crate.prefab\")"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/New/Deep/Crate.prefab"));
            AssertFails(M("objects | where name == \"Crate\" | call SaveAsPrefab(\"Other/Crate.prefab\")"), "invalid-value", "not under Assets/");
        }

        // A save that fails after the missing folders were created removes them: Unity refuses part of a prefab instance.
        [Test]
        public void AFailedSaveAsPrefabLeavesNoNewFolders()
        {
            var parent = new GameObject("Holder");
            new GameObject("Part").transform.SetParent(parent.transform);
            AssertOk(M($"objects | where name == \"Holder\" | call SaveAsPrefab(\"{Folder}/Holder.prefab\")"));
            AssertFails(M($"objects | where name == \"Part\" | call SaveAsPrefab(\"{Folder}/New/Deep/Part.prefab\")"), "call-threw");
            Assert.IsFalse(AssetDatabase.IsValidFolder($"{Folder}/New"), "the folders the failed call created must be removed");
        }

        // Two targets of one rql-mutate call saving to one path: the second save would fail after the first one happened, so
        // the call is rejected before either (on a dry run too).
        [Test]
        public void SeveralGameObjectsCannotSaveToOnePrefabPathInOneCall()
        {
            var a = new GameObject("PA");
            var b = new GameObject("PB");
            var path = $"{Folder}/Same.prefab";
            const string Targets = "objects | where name in [\"PA\", \"PB\"]";
            AssertFails(RqlUnity.Mutate($"{Targets} | call SaveAsPrefab(\"{path}\")", dryRun: true), "invalid-value", "also the path of");
            AssertFails(RqlUnity.Mutate($"{Targets} | call SaveAsPrefab(\"{path}\")"), "invalid-value", "give each GameObject its own prefab path");
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            Assert.IsFalse(PrefabUtility.IsPartOfPrefabInstance(a));
            AssertOk(RqlUnity.Mutate($"{Targets} | call SaveAsPrefab(\"{Folder}/\" + name + \".prefab\")"));
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(b));
        }

        // Saving over an existing prefab would replace it (every instance changes); deleting it would leave missing prefabs.
        [Test]
        public void AnExistingPrefabIsNeitherReplacedNorDeletedWhileInUse()
        {
            var path = MakePrefab();
            new GameObject("Other");
            AssertFails(M($"objects | where name == \"Other\" | call SaveAsPrefab(\"{path}\")"), "invalid-value", "already exists");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<BoxCollider>());
            AssertFails(M($"editor | call DeleteAsset(\"{path}\")"), "invalid-value", "used by 1 object(s) in the open scenes: Crate.");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        }

        // An instance of Outer uses Inner too (nested in Outer): deleting Inner would leave a missing prefab inside it.
        [Test]
        public void ANestedPrefabIsInUseThroughItsOuterPrefab()
        {
            var inner = MakePrefab(); // Crate.prefab; its scene instance is removed below
            var crateInstance = GameObject.Find("Crate");
            var outer = new GameObject("Outer");
            ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(inner))).transform.SetParent(outer.transform, false);
            PrefabUtility.SaveAsPrefabAssetAndConnect(outer, $"{Folder}/Outer.prefab", InteractionMode.AutomatedAction);
            Object.DestroyImmediate(crateInstance); // only Outer's instance is left in the scene
            AssertFails(M($"editor | call DeleteAsset(\"{inner}\")"), "invalid-value", "used by 1 object(s) in the open scenes: Outer.");
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
