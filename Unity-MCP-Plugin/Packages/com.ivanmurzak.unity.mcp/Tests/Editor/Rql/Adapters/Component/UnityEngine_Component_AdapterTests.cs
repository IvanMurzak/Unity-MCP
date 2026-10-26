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
    public class UnityEngine_Component_AdapterTests : AdapterTestBase
    {
        [Test]
        public void BackPointersAreReadable()
        {
            new GameObject("Owner", typeof(Light));
            Assert.AreEqual("Owner", Cell(Q("objects | get components | where $ is Light | select gameObject.name")));
            Assert.AreEqual("Owner", Cell(Q("objects | get components | where $ is Light | select transform.gameObject.name")));
        }

        [Test]
        public void DestroyRemovesTheComponentUndoably()
        {
            var go = new GameObject("Owner", typeof(Light));
            AssertOk(M("objects | get components | where $ is Light | call Destroy()"));
            Assert.IsNull(go.GetComponent<Light>());
            Undo.PerformUndo();
            Assert.IsNotNull(go.GetComponent<Light>());
        }

        [Test]
        public void AGameObjectStandsForItsComponentInComponentFields()
        {
            var holder = new GameObject("Holder").AddComponent<RqlTestBehaviour>();
            var lamp = new GameObject("Lamp", typeof(Light));
            var lampRef = Cell(Q("objects | where name == \"Lamp\" | select $ref"));
            AssertOk(M($"objects | get components | where $ is RqlTestBehaviour | set lamp = ref(\"{lampRef}\"), anchor = ref(\"{lampRef}\")"));
            Assert.AreSame(lamp.GetComponent<Light>(), holder.lamp);
            Assert.AreSame(lamp.transform, holder.anchor);
        }

        [Test]
        public void AGameObjectWithoutTheComponentIsRejected()
        {
            var holder = new GameObject("Holder").AddComponent<RqlTestBehaviour>();
            new GameObject("Empty");
            var emptyRef = Cell(Q("objects | where name == \"Empty\" | select $ref"));
            AssertFails(M($"objects | get components | where $ is RqlTestBehaviour | set lamp = ref(\"{emptyRef}\")"), "coercion-failed", "has no Light");
            Assert.IsNull(holder.lamp);
        }

        [Test]
        public void DestroyFailsWhenUnityKeepsTheComponent()
        {
            LogAssert.ignoreFailingMessages = true; // Unity logs why it refused
            var go = new GameObject("Owner", typeof(Rigidbody), typeof(FixedJoint));
            AssertFails(M("objects | get components | where $ is Transform | call Destroy()"), "getter-threw", "cannot be removed");
            // FixedJoint requires the Rigidbody: Unity keeps it and only logs.
            AssertFails(M("objects | get components | where $ is Rigidbody | call Destroy()"), "getter-threw", "did not remove Rigidbody");
            Assert.IsNotNull(go.GetComponent<Rigidbody>());
            AssertOk(M("objects | get components | where $ is FixedJoint | call Destroy()"));
            AssertOk(M("objects | get components | where $ is Rigidbody | call Destroy()"));
            Assert.IsNull(go.GetComponent<Rigidbody>());
        }
    }
}
