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
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.EditorApi
{
    /// <summary>GameObject prefabSource / prefabOverrides and the Apply / Revert calls on a prefab instance.</summary>
    public class RqlPrefabInstanceTests : EditorApiTestBase
    {
        private string Path => $"{Folder}/Coin.prefab";

        private GameObject MakeInstance()
        {
            var root = new GameObject("Coin");
            new GameObject("Mesh").transform.SetParent(root.transform);
            AssertOk(M($"objects | where name == \"Coin\" | call SaveAsPrefab(\"{Path}\")"));
            return root;
        }

        [Test]
        public void PrefabSourceNamesTheAssetOfAnInstanceAndItsParts()
        {
            MakeInstance();
            new GameObject("Plain");
            Assert.AreEqual(Path, Cell(Q("objects | where name == \"Coin\" | select prefabSource")));
            Assert.AreEqual(Path, Cell(Q("objects | where name == \"Mesh\" | select prefabSource")));
            Assert.AreEqual("null", Cell(Q("objects | where name == \"Plain\" | select prefabSource")));
            Assert.AreEqual("null", Cell(Q("objects | where name == \"Plain\" | select prefabOverrides")));
            // explicit-only: a plain select of the object does not ask the prefab system
            StringAssert.DoesNotContain("prefabSource", Table(Q("objects | where name == \"Coin\"")));
        }

        [Test]
        public void PrefabOverridesListPropertyChangesAndAddedComponentsButNotTheRootPlacement()
        {
            var instance = MakeInstance();
            Assert.AreEqual("[]", Cell(Q("objects | where name == \"Coin\" | select prefabOverrides")));
            AssertOk(M("objects | where name == \"Coin\" | set transform.position = \"(5, 0, 0)\"")); // a default override: per-instance by design
            Assert.AreEqual("[]", Cell(Q("objects | where name == \"Coin\" | select prefabOverrides")));

            AssertOk(M("objects | where name == \"Mesh\" | set transform.localScale = \"(2, 2, 2)\""));
            AssertOk(M("objects | where name == \"Coin\" | call AddComponent(\"BoxCollider\")"));
            var overrides = Cell(Q("objects | where name == \"Coin\" | select prefabOverrides"));
            StringAssert.Contains("Mesh/Transform.m_LocalScale.x = 2", overrides);
            StringAssert.Contains("added component Coin/BoxCollider", overrides);
            StringAssert.DoesNotContain("m_LocalPosition", overrides);
            // a part reports only its own overrides, not the whole instance's again
            var part = Cell(Q("objects | where name == \"Mesh\" | select prefabOverrides"));
            StringAssert.Contains("Mesh/Transform.m_LocalScale.x = 2", part);
            StringAssert.DoesNotContain("BoxCollider", part);
        }

        [Test]
        public void PrefabOverridesOfAPartAreScopedToIt()
        {
            var instance = MakeInstance();
            new GameObject("Other").transform.SetParent(instance.transform);
            AssertOk(M("objects | where name == \"Coin\" | set tag = \"EditorOnly\""));
            AssertOk(M("objects | where name == \"Mesh\" | call AddComponent(\"SphereCollider\")"));
            var root = Cell(Q("objects | where name == \"Coin\" | select prefabOverrides"));
            StringAssert.Contains("m_TagString = EditorOnly", root);
            StringAssert.Contains("added component Coin/Mesh/SphereCollider", root);
            StringAssert.Contains("added GameObject Coin/Other", root);
            var mesh = Cell(Q("objects | where name == \"Mesh\" | select prefabOverrides"));
            StringAssert.Contains("added component Coin/Mesh/SphereCollider", mesh);
            StringAssert.DoesNotContain("m_TagString", mesh);
            StringAssert.DoesNotContain("Other", mesh);
        }

        [Test]
        public void PrefabOverridesListARemovedGameObject()
        {
            MakeInstance();
            AssertOk(M("objects | where name == \"Mesh\" | call Destroy()"));
            var roots = Table(Q("roots | select name prefabSource prefabOverrides"));
            StringAssert.Contains("removed GameObject Coin/Mesh", roots);
        }

        [Test]
        public void RevertDropsTheOverridesAndIsUndoable()
        {
            var instance = MakeInstance();
            var mesh = instance.transform.GetChild(0);
            AssertOk(M("objects | where name == \"Mesh\" | set transform.localScale = \"(2, 2, 2)\""));
            AssertOk(M("objects | where name == \"Mesh\" | call RevertPrefabOverrides()")); // through a part: the whole instance
            Assert.AreEqual(Vector3.one, mesh.localScale);
            Assert.AreEqual("[]", Cell(Q("objects | where name == \"Coin\" | select prefabOverrides")));
            Undo.PerformUndo();
            Assert.AreEqual(new Vector3(2, 2, 2), mesh.localScale);
        }

        [Test]
        public void ApplyWritesTheOverridesIntoThePrefab()
        {
            MakeInstance();
            AssertOk(M("objects | where name == \"Coin\" | set transform.localScale = \"(0.5, 0.5, 0.5)\""));
            AssertOk(M("objects | where name == \"Coin\" | call ApplyPrefabOverrides()"));
            Assert.AreEqual(new Vector3(0.5f, 0.5f, 0.5f), AssetDatabase.LoadAssetAtPath<GameObject>(Path).transform.localScale);
            Assert.AreEqual("[]", Cell(Q("objects | where name == \"Coin\" | select prefabOverrides")));
        }

        // Prefab Mode contents are not scene instances: Apply there would write a variant's overrides into its base.
        [Test]
        public void PrefabModeContentsAreNoSceneInstances()
        {
            MakeInstance();
            AssertOk(M($"editor | call OpenPrefab(\"{Path}\")"));
            Assert.AreEqual("null", Cell(Q("prefab | select prefabSource")));
            AssertFails(M("prefab | call RevertPrefabOverrides()", dryRun: true), "invalid-value", "not part of a prefab instance");
        }

        [Test]
        public void ApplyAndRevertRefuseAnObjectThatIsNoInstance()
        {
            new GameObject("Plain");
            AssertFails(M("objects | where name == \"Plain\" | call RevertPrefabOverrides()"), "invalid-value", "not part of a prefab instance");
            AssertFails(M("objects | where name == \"Plain\" | call ApplyPrefabOverrides()", dryRun: true), "invalid-value", "not part of a prefab instance");
        }

        // Unity saves a prefab instance as a Prefab Variant of its prefab: SaveAsPrefab says so.
        [Test]
        public void SaveAsPrefabOfAnInstanceMakesAVariant()
        {
            MakeInstance();
            AssertOk(M($"objects | where name == \"Coin\" | call SaveAsPrefab(\"{Folder}/GoldCoin.prefab\")"));
            var variant = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/GoldCoin.prefab");
            Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(variant));
            StringAssert.Contains("Prefab Variant", Schema("GameObject"));
        }
    }
}
