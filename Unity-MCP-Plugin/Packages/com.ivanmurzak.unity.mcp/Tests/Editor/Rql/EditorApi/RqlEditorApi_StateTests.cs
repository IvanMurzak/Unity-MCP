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
using com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Adapters;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.EditorApi
{
    public class RqlEditorApi_StateTests : AdapterTestBase
    {
        [Test]
        public void EditorStateIsReadable()
        {
            Assert.AreEqual("false", Cell(Q("editor | select isPlaying")));
            Assert.AreEqual(Application.unityVersion, Cell(Q("editor | select unityVersion")));
        }

        [Test]
        public void SelectionIsReadAndWritten()
        {
            var a = new GameObject("A");
            var b = new GameObject("B");
            var aRef = Cell(Q("objects | where name == \"A\" | select $ref"));
            var bRef = Cell(Q("objects | where name == \"B\" | select $ref"));
            AssertOk(M($"editor | set selection = [ref(\"{aRef}\"), ref(\"{bRef}\")]"));
            CollectionAssert.AreEquivalent(new Object[] { a, b }, Selection.objects);
            Assert.AreEqual("2", Cell(Q("editor | select selection.count()")));
            AssertOk(M("editor | set selection = []"));
            Assert.AreEqual(0, Selection.objects.Length);
        }

        [Test]
        public void SchemaDescribesSourcesByName()
        {
            var editor = RqlUnitySchema.Render(RqlUnity.Engine, "editor");
            StringAssert.Contains("OpenScene(", editor);
            StringAssert.Contains("isPlaying", editor);
            StringAssert.Contains("Message", RqlUnitySchema.Render(RqlUnity.Engine, "console"));
        }

        [Test]
        public void ComponentTypesListAddableTypes()
        {
            Assert.AreEqual("Rigidbody", Cell(Q("componentTypes | where name == \"Rigidbody\" | select name")));
            Assert.AreEqual(0, Q("componentTypes | where name == \"Component\" | select name").Rows!.Count);
        }

        [Test]
        public void ShadersIncludeBuiltIns()
        {
            Assert.Greater(Q("shaders | where name starts-with \"Hidden/\" | select name").Rows!.Count, 0);
        }

        [Test]
        public void ConsoleEntriesAreQueryableAndClearable()
        {
            Debug.Log("rql console probe 1234");
            Assert.AreEqual("rql console probe 1234", Cell(Q("console | where message contains \"probe 1234\" | select message")));
            AssertOk(M("editor | call ClearConsole()"));
            Assert.AreEqual(0, Q("console | where message contains \"probe 1234\" | select message").Rows!.Count);
        }

        [Test]
        public void ConsoleSequenceSupportsIncrementalReads()
        {
            var marker = System.Guid.NewGuid().ToString("N");
            Debug.Log($"cursor-first {marker}");
            UnityMcpPluginEditor.Instance.LogCollector!.Save();
            var first = Cell(Q($"console | where message contains \"cursor-first {marker}\" | select sequence"));
            Debug.Log($"cursor-second {marker}");
            UnityMcpPluginEditor.Instance.LogCollector!.Save();
            var newer = Table(Q($"console | where sequence > {first} and message contains \"{marker}\" | select message"));
            StringAssert.Contains("cursor-second", newer);
            StringAssert.DoesNotContain("cursor-first", newer);
        }

        [Test]
        public void CreateGameObjectAtAPosition()
        {
            AssertOk(M("editor | call CreateGameObject(\"Spot\", null, \"Sphere\", \"(1, 2, 3)\")"));
            var go = GameObject.Find("Spot");
            Assert.AreEqual(new Vector3(1, 2, 3), go.transform.localPosition);
            Assert.IsNotNull(go.GetComponent<MeshFilter>());
        }

        [Test]
        public void ExecuteMenuItemReportsMissingItems()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("no menu named 'No/Such/Menu'"));
            var r = M("editor | call ExecuteMenuItem(\"No/Such/Menu\")");
            AssertOk(r);
            StringAssert.Contains("false", Table(r));
        }
    }
}
