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
            AssertFails(M("objects | get components | where $ is Transform | call Destroy()"), "invalid-value", "cannot be removed");
            // FixedJoint requires the Rigidbody: Unity keeps it and only logs.
            AssertFails(M("objects | get components | where $ is Rigidbody | call Destroy()"), "invalid-value", "FixedJoint on 'Owner' requires Rigidbody");
            Assert.IsNotNull(go.GetComponent<Rigidbody>());
            AssertOk(M("objects | get components | where $ is FixedJoint | call Destroy()"));
            AssertOk(M("objects | get components | where $ is Rigidbody | call Destroy()"));
            Assert.IsNull(go.GetComponent<Rigidbody>());
        }

        // rql-mutate (RqlUnity.Mutate) decides a required component's removal for the whole call: removing it together with
        // the components that require it works in either target order, on dry runs and real runs alike.
        [Test]
        public void ARequiredComponentGoesTogetherWithItsDependentsInOneCall()
        {
            LogAssert.ignoreFailingMessages = true;
            var go = new GameObject("Door", typeof(Rigidbody), typeof(HingeJoint));
            const string Both = "objects | get components | where $ is Rigidbody or $ is HingeJoint";

            AssertFails(RqlUnity.Mutate("objects | get components | where $ is Rigidbody | call Destroy()"), "invalid-value", "HingeJoint on 'Door' requires Rigidbody");
            AssertFails(RqlUnity.Mutate("objects | get components | where $ is Rigidbody | call Destroy()", dryRun: true), "invalid-value", "requires Rigidbody");
            Assert.IsNotNull(go.GetComponent<Rigidbody>());

            var dry = RqlUnity.Mutate(Both + " | call Destroy()", dryRun: true);
            AssertOk(dry);
            Assert.IsNotNull(go.GetComponent<HingeJoint>());

            // The Rigidbody comes first in the component list: its call removes the HingeJoint before itself.
            AssertOk(RqlUnity.Mutate(Both + " | call Destroy()"));
            Assert.IsNull(go.GetComponent<HingeJoint>());
            Assert.IsNull(go.GetComponent<Rigidbody>());
            Undo.PerformUndo(); // one step restores both
            Assert.IsNotNull(go.GetComponent<HingeJoint>());
            Assert.IsNotNull(go.GetComponent<Rigidbody>());
        }

        [Test]
        public void ADependentLeftOutOfTheCallStillBlocksTheRemoval()
        {
            LogAssert.ignoreFailingMessages = true;
            var door = new GameObject("Door", typeof(Rigidbody), typeof(HingeJoint));
            var gate = new GameObject("Gate", typeof(Rigidbody), typeof(HingeJoint));
            // Door's pair is complete, Gate's Rigidbody is targeted without its HingeJoint: nothing is removed.
            var r = RqlUnity.Mutate("objects | get components | where ($ is Rigidbody) or ($ is HingeJoint and gameObject.name == \"Door\") | call Destroy()");
            AssertFails(r, "invalid-value", "HingeJoint on 'Gate' requires Rigidbody");
            StringAssert.Contains("nothing was changed", Table(r));
            Assert.IsNotNull(door.GetComponent<Rigidbody>());
            Assert.IsNotNull(door.GetComponent<HingeJoint>());
            Assert.IsNotNull(gate.GetComponent<Rigidbody>());
        }

        // name and tag written through a component are its GameObject's: the GameObject's checks apply.
        [Test]
        public void NameAndTagThroughAComponentAreChecked()
        {
            var go = new GameObject("Lamp", typeof(Light));
            AssertFails(M("objects | get components | where $ is Light | set tag = \"NoSuchTag\""), "invalid-value", "not defined");
            AssertFails(M("objects | get components | where $ is Light | set name = \"\""), "invalid-value", "cannot be empty");
            Assert.AreEqual("Untagged", go.tag);
            Assert.AreEqual("Lamp", go.name);
            AssertOk(M("objects | get components | where $ is Light | set name = \"Bulb\""));
            Assert.AreEqual("Bulb", go.name);
        }
    }
}
