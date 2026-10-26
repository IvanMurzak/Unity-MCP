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
using com.IvanMurzak.Unity.MCP.TestFiles;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Adapters
{
    public class UnityEngine_GameObject_AdapterTests : AdapterTestBase
    {
        [Test]
        public void ComponentsAndChildrenAreMembers()
        {
            var parent = new GameObject("Parent", typeof(Light));
            new GameObject("Child").transform.SetParent(parent.transform, false);
            Assert.AreEqual("2", Cell(Q("objects | where name == \"Parent\" | select components.count()")));
            Assert.AreEqual("Light", Cell(Q("objects | where name == \"Parent\" | get components | where $ is Light | select $type")));
            Assert.AreEqual("Child", Cell(Q("objects | where name == \"Parent\" | get children | select name")));
        }

        [Test]
        public void ActiveWritesThroughSetActive()
        {
            var go = new GameObject("Toggle");
            AssertOk(M("objects | where name == \"Toggle\" | set active = false"));
            Assert.IsFalse(go.activeSelf);
            Assert.AreEqual("false", Cell(Q("objects | where name == \"Toggle\" | select active")));
        }

        [Test]
        public void StaticFlagsAreReadFilteredAndWrittenPerFlag()
        {
            var ground = new GameObject("Ground");
            new GameObject("Prop");
            AssertOk(M("objects | where name == \"Ground\" | set staticFlags = [\"ContributeGI\", \"BatchingStatic\"]"));
            Assert.AreEqual(StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic, GameObjectUtility.GetStaticEditorFlags(ground));
            Assert.AreEqual(1, Count("objects | where staticFlags contains \"ContributeGI\" | count"));
            Assert.AreEqual("Ground", Cell(Q("objects | where staticFlags contains \"BatchingStatic\" | select name")));
            // StaticEditorFlags.LightmapStatic is an obsolete alias of ContributeGI: the current name is printed.
            var printed = Table(Q("objects | where name == \"Ground\" | select staticFlags"));
            StringAssert.Contains("ContributeGI", printed);
            StringAssert.DoesNotContain("LightmapStatic", printed);

            AssertOk(M("objects | where name == \"Ground\" | set staticFlags = []"));
            Assert.AreEqual((StaticEditorFlags)0, GameObjectUtility.GetStaticEditorFlags(ground));
            Assert.AreEqual(0, Count("objects | where staticFlags contains \"ContributeGI\" | count"));
        }

        [Test]
        public void StaticFlagsWritesAreUndoable()
        {
            var ground = new GameObject("Ground");
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.OccluderStatic);
            AssertOk(M("objects | where name == \"Ground\" | set staticFlags = [\"ContributeGI\"]"));
            Assert.AreEqual(StaticEditorFlags.ContributeGI, GameObjectUtility.GetStaticEditorFlags(ground));
            Undo.PerformUndo();
            Assert.AreEqual(StaticEditorFlags.OccluderStatic, GameObjectUtility.GetStaticEditorFlags(ground));
        }

        [Test]
        public void UnknownStaticFlagsChangeNothing()
        {
            var ground = new GameObject("Ground");
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.OccluderStatic);
            var r = M("objects | where name == \"Ground\" | set staticFlags = [\"ContributeGI\", \"NoSuchFlag\"]");
            Assert.IsFalse(r.Ok, Table(r));
            Assert.IsFalse(M("objects | where name == \"Ground\" | set staticFlags = [\"ContributeGI\", \"1024\"]").Ok);
            Assert.IsFalse(M("objects | where name == \"Ground\" | set staticFlags = [\"LightmapStatic\"]").Ok, "obsolete names are rejected");
            Assert.IsFalse(M("objects | where name == \"Ground\" | set staticFlags = [\"contributegi\"]").Ok, "names are exact, as reads print them");
            Assert.AreEqual(StaticEditorFlags.OccluderStatic, GameObjectUtility.GetStaticEditorFlags(ground));
            StringAssert.Contains("staticFlags", Schema("GameObject"));
        }

        [Test]
        public void AddComponentByNameReturnsTheComponentAndIsUndoable()
        {
            var go = new GameObject("Crate");
            var r = M("objects | where name == \"Crate\" | call AddComponent(\"Rigidbody\")");
            AssertOk(r);
            StringAssert.Contains("Rigidbody#", Table(r));
            Assert.IsNotNull(go.GetComponent<Rigidbody>());
            Undo.PerformUndo();
            Assert.IsNull(go.GetComponent<Rigidbody>());
            AssertOk(M("objects | where name == \"Crate\" | call AddComponent(\"RqlTestBehaviour\")"));
            Assert.IsNotNull(go.GetComponent<RqlTestBehaviour>());
        }

        [Test]
        public void AddComponentRejectsUnknownAndNonComponentTypes()
        {
            var go = new GameObject("Crate");
            var unknown = M("objects | where name == \"Crate\" | call AddComponent(\"NoSuchComponentType\")");
            Assert.IsFalse(unknown.Ok, Table(unknown));
            StringAssert.Contains("no component type named 'NoSuchComponentType'", Table(unknown));
            Assert.IsFalse(M("objects | where name == \"Crate\" | call AddComponent(\"Material\")").Ok);
            Assert.AreEqual(1, go.GetComponents<Component>().Length);
        }

        [Test]
        public void DestroyIsUndoable()
        {
            new GameObject("Doomed");
            AssertOk(M("objects | where name == \"Doomed\" | call Destroy()"));
            Assert.IsNull(GameObject.Find("Doomed"));
            Undo.PerformUndo();
            Assert.IsNotNull(GameObject.Find("Doomed"));
        }

        [Test]
        public void TheSelfReferenceIsHidden()
        {
            new GameObject("Self");
            Assert.IsTrue(Warns(Q("objects | where name == \"Self\" | select gameObject"), "unknown-member"));
            StringAssert.Contains("components", Schema("GameObject"));
        }

        [Test]
        public void EmptyNamesAreRejected()
        {
            var go = new GameObject("Named");
            AssertFails(M("objects | where name == \"Named\" | set name = \"\""), "coercion-failed", "cannot be empty");
            AssertFails(M("objects | where name == \"Named\" | set name = \"   \""), "coercion-failed", "cannot be empty");
            Assert.AreEqual("Named", go.name);
            AssertOk(M("objects | where name == \"Named\" | set name = \"Renamed\""));
            Assert.AreEqual("Renamed", go.name);
        }

        // Unity only logs an error for an unknown tag or a layer outside 0..31 and keeps the old value: the write must fail
        // before anything changes, the other writes of the same set included.
        [Test]
        public void UnknownTagsAndOutOfRangeLayersFailBeforeAnyWrite()
        {
            var go = new GameObject("Tagged");
            AssertFails(M("objects | where name == \"Tagged\" | set transform.position = \"(5, 5, 5)\", tag = \"NoSuchTag\""), "coercion-failed", "not defined");
            Assert.AreEqual(Vector3.zero, go.transform.position);
            Assert.AreEqual("Untagged", go.tag);
            AssertFails(M("objects | where name == \"Tagged\" | set layer = 32"), "coercion-failed", "0..31");
            AssertFails(M("objects | where name == \"Tagged\" | set layer = -1"), "coercion-failed", "0..31");
            Assert.AreEqual(0, go.layer);

            AssertOk(M("objects | where name == \"Tagged\" | set tag = \"Player\", layer = 4"));
            Assert.AreEqual("Player", go.tag);
            Assert.AreEqual(4, go.layer);
        }

        // The parent comes first, so its child is already gone when the call reaches it.
        [Test]
        public void DestroyingAParentAndItsChildInOneCallSucceeds()
        {
            var parent = new GameObject("Enemy");
            new GameObject("EnemyChild").transform.SetParent(parent.transform, false);
            AssertOk(M("objects | where name starts-with \"Enemy\" | call Destroy()"));
            Assert.IsNull(GameObject.Find("Enemy"));
            Undo.PerformUndo();
            Assert.IsNotNull(GameObject.Find("Enemy/EnemyChild"));
        }

        [Test]
        public void AddComponentFailsInsteadOfReturningNull()
        {
            LogAssert.ignoreFailingMessages = true; // Unity logs why it refused
            var go = new GameObject("Host", typeof(Rigidbody));
            AssertFails(M("objects | where name == \"Host\" | call AddComponent(\"Transform\")"), "getter-threw", "a Transform cannot be added");
            AssertFails(M("objects | where name == \"Host\" | call AddComponent(\"Renderer\")"), "getter-threw", "did not add Renderer");
            AssertFails(M("objects | where name == \"Host\" | call AddComponent(\"Rigidbody\")"), "getter-threw", "did not add Rigidbody");
            AssertFails(M("objects | where name == \"Host\" | call AddComponent(\"Rigidbody2D\")"), "getter-threw", "did not add Rigidbody2D");
            Assert.AreEqual(2, go.GetComponents<Component>().Length);
            AssertOk(M("objects | where name == \"Host\" | call AddComponent(\"BoxCollider\")"));
            Assert.IsNotNull(go.GetComponent<BoxCollider>());
        }
    }
}
