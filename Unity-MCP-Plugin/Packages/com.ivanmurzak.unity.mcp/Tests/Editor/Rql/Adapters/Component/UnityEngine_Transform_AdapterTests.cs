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
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Adapters
{
    public class UnityEngine_Transform_AdapterTests : AdapterTestBase
    {
        [Test]
        public void TransformIsAnObjectNotAListOfChildren()
        {
            var parent = new GameObject("Parent");
            parent.transform.position = new Vector3(1, 2, 3);
            new GameObject("Child").transform.SetParent(parent.transform, false);
            Assert.AreEqual("(1, 2, 3)", Cell(Q("objects | where name == \"Parent\" | select transform.position")));
            Assert.AreEqual("1", Cell(Q("objects | where name == \"Parent\" | select transform.childCount")));
        }

        [Test]
        public void ParentAndRootAreReferences()
        {
            var root = new GameObject("Root");
            var mid = new GameObject("Mid");
            mid.transform.SetParent(root.transform, false);
            new GameObject("Leaf").transform.SetParent(mid.transform, false);
            Assert.AreEqual("Mid", Cell(Q("objects | where name == \"Leaf\" | select transform.parent.gameObject.name")));
            Assert.AreEqual("Root", Cell(Q("objects | where name == \"Leaf\" | select transform.root.gameObject.name")));
        }

        [Test]
        public void ReparentWithAGameObjectRefKeepsTheWorldPosition()
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(10, 0, 0);
            var lamp = new GameObject("Lamp");
            lamp.transform.position = new Vector3(1, 1, 1);
            var playerRef = Cell(Q("objects | where name == \"Player\" | select $ref"));
            AssertOk(M($"objects | where name == \"Lamp\" | set transform.parent = ref(\"{playerRef}\")"));
            Assert.AreSame(player.transform, lamp.transform.parent);
            Assert.AreEqual(new Vector3(1, 1, 1), lamp.transform.position);    // Transform.parent keeps the world position
            Assert.AreEqual(new Vector3(-9, 1, 1), lamp.transform.localPosition);
        }

        [Test]
        public void FindNamesAGameObjectInsideExpressions()
        {
            var player = new GameObject("Player");
            var lamp = new GameObject("Lamp");
            AssertOk(M("objects | where name == \"Lamp\" | set transform.parent = unity.find(\"Player\")"));
            Assert.AreSame(player.transform, lamp.transform.parent);
            Assert.AreEqual("Lamp", Cell(Q("unity.find(\"Player/Lamp\") | select name")));
            new GameObject("Player");
            var ambiguous = M("objects | where name == \"Lamp\" | set transform.parent = unity.find(\"Player\")");
            Assert.IsFalse(ambiguous.Ok, Table(ambiguous));
            StringAssert.Contains("2 GameObjects are named 'Player'", Table(ambiguous));
        }

        [Test]
        public void EulerAnglesAndLocalScaleAreWritable()
        {
            var go = new GameObject("Spin");
            AssertOk(M("objects | where name == \"Spin\" | set transform.eulerAngles = \"(0, 90, 0)\", transform.localScale = [2, 2, 2]"));
            Assert.AreEqual(90f, go.transform.eulerAngles.y, 1e-3f);
            Assert.AreEqual(Vector3.one * 2, go.transform.localScale);
        }

        // Unity logs and keeps the hierarchy for these parents: the set must fail instead of reporting a reparent.
        [Test]
        public void ParentingUnderItselfOrADescendantFails()
        {
            var group = new GameObject("Group");
            var child = new GameObject("Child");
            child.transform.SetParent(group.transform, false);
            AssertFails(M("objects | where name == \"Group\" | set transform.parent = unity.find(\"Group\")"), "coercion-failed", "itself or one of its children");
            AssertFails(M("objects | where name == \"Group\" | set transform.parent = unity.find(\"Group/Child\")"), "coercion-failed", "itself or one of its children");
            Assert.IsNull(group.transform.parent);
            Assert.AreSame(group.transform, child.transform.parent);
        }

        [Test]
        public void ChildrenOfAPrefabInstanceCannotBeReparented()
        {
            const string folder = "Assets/RqlTransformTests";
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder)) UnityEditor.AssetDatabase.CreateFolder("Assets", "RqlTransformTests");
            try
            {
                var root = new GameObject("PrefabRoot");
                new GameObject("Inner").transform.SetParent(root.transform, false);
                UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(root, folder + "/Root.prefab", UnityEditor.InteractionMode.AutomatedAction);
                AssertFails(M("objects | where name == \"Inner\" | set transform.parent = null"), "coercion-failed", "inside a prefab instance");
                Assert.AreSame(root.transform, GameObject.Find("PrefabRoot/Inner").transform.parent);
                // The instance root itself moves freely.
                var other = new GameObject("Other");
                AssertOk(M("objects | where name == \"PrefabRoot\" | set transform.parent = unity.find(\"Other\")"));
                Assert.AreSame(other.transform, root.transform.parent);
            }
            finally
            {
                UnityEditor.AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
