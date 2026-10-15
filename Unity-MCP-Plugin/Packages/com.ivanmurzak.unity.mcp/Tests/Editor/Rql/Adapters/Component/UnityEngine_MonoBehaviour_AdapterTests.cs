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
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Adapters
{
    public class UnityEngine_MonoBehaviour_AdapterTests : AdapterTestBase
    {
        [Test]
        public void EditorBookkeepingMembersAreHidden()
        {
            new GameObject("B").AddComponent<RqlTestBehaviour>();
            var schema = Schema("RqlTestBehaviour");
            foreach (var hidden in new[] { "useGUILayout", "runInEditMode", "didStart", "didAwake", "destroyCancellationToken" })
                StringAssert.DoesNotContain(hidden, schema, hidden);
            StringAssert.Contains("speed", schema);
        }

        [Test]
        public void WritesIntoPlainNestedObjectsAreUndoneOnTheComponent()
        {
            var b = new GameObject("B").AddComponent<RqlTestBehaviour>();
            AssertOk(M("objects | get components | where $ is RqlTestBehaviour | set settings.speed = 7"));
            Assert.AreEqual(7f, b.settings.speed, 1e-6f);
            UnityEditor.Undo.PerformUndo();
            Assert.AreEqual(1f, b.settings.speed, 1e-6f);
        }

        [Test]
        public void EnabledAndScriptFieldsAreWritable()
        {
            var b = new GameObject("B").AddComponent<RqlTestBehaviour>();
            AssertOk(M("objects | get components | where $ is RqlTestBehaviour | set enabled = false, speed = speed * 3"));
            Assert.IsFalse(b.enabled);
            Assert.AreEqual(3f, b.speed, 1e-6f);
        }
    }
}
