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
    }
}
