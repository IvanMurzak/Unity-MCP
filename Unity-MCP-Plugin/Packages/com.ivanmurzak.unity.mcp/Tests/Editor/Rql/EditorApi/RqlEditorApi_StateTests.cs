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
using System.Collections.Generic;
using System.Linq;
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

        // The echo McpToolManager logs for each tool response stays out of the console source (an agent's own failed call is
        // not an Editor error); other entries stay.
        [Test]
        public void ConsoleLeavesOutTheToolResponseEchoes()
        {
            var marker = System.Guid.NewGuid().ToString("N");
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            Debug.LogError($"fail: [AI] McpToolManager Error Response to AI:\nerror syntax-error {marker}");
            Debug.LogError($"real error {marker}");
            UnityMcpPluginEditor.Instance.LogCollector!.Save();
            var rows = Table(Q($"console | where message contains \"{marker}\" | select message"));
            StringAssert.Contains("real error", rows);
            StringAssert.DoesNotContain("McpToolManager", rows);
        }

        // The echoes are left out before the entry limit: a busy session whose newest MaxLogEntries entries are all echoes
        // still returns the real entry logged before them.
        [Test]
        public void ConsoleLeavesOutTheEchoesBeforeItsLimit()
        {
            var marker = System.Guid.NewGuid().ToString("N");
            Debug.Log($"real entry {marker}");
            for (int i = 0; i < RqlEditorApi.MaxLogEntries; i++)
                Debug.Log($"[AI] McpToolManager Success Response to AI: {marker} {i}");
            UnityMcpPluginEditor.Instance.LogCollector!.Save();
            var rows = Q($"console | where message contains \"{marker}\" | select message");
            Assert.AreEqual(1, rows.Rows!.Count, Table(rows));
            Assert.AreEqual($"real entry {marker}", Cell(rows));
        }

        // The window grows until it holds max real entries (or everything stored); the newest of them are kept, oldest first.
        [Test]
        public void NewestWithoutEchoesFillsTheLimitWithRealEntries()
        {
            var stored = new List<LogEntry>();
            for (int i = 0; i < 10; i++)
            {
                stored.Add(new LogEntry(LogType.Log, $"real {i}"));
                stored.Add(new LogEntry(LogType.Error, $"[AI] McpToolManager Error Response to AI: {i}"));
                stored.Add(new LogEntry(LogType.Log, $"[AI] McpToolManager Success Response to AI: {i}"));
            }
            var windows = new List<int>();
            IReadOnlyList<LogEntry> Newest(int n)
            {
                windows.Add(n);
                return stored.Skip(System.Math.Max(0, stored.Count - n)).ToList();
            }

            var four = RqlEditorApi.NewestWithoutEchoes(Newest, 4);
            CollectionAssert.AreEqual(new[] { "real 6", "real 7", "real 8", "real 9" }, four.Select(e => e.Message).ToArray());
            CollectionAssert.AreEqual(new[] { 4, 8, 16 }, windows);

            windows.Clear();
            var all = RqlEditorApi.NewestWithoutEchoes(Newest, 50); // fewer stored than asked: everything real, one read
            Assert.AreEqual(10, all.Count);
            Assert.AreEqual("real 0", all[0].Message);
            CollectionAssert.AreEqual(new[] { 50 }, windows);
        }

        // Tags and Layers: read the lists, add a layer / tag, and use the layer by name. The test project's TagManager is
        // restored afterwards.
        [Test]
        public void LayersAndTagsAreListedAndAdded()
        {
            var manager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var saved = EditorJsonUtility.ToJson(manager);
            try
            {
                StringAssert.Contains("Default", Table(Q("editor | select layers")));
                StringAssert.Contains("MainCamera", Table(Q("editor | select tags")));

                var index = int.Parse(Cell(M("editor | call AddLayer(\"RqlTestLayer\")"), 0, 2));
                Assert.AreEqual(index, LayerMask.NameToLayer("RqlTestLayer"));
                Assert.GreaterOrEqual(index, 8);
                Assert.AreEqual(index.ToString(), Cell(M("editor | call AddLayer(\"RqlTestLayer\")"), 0, 2)); // already there
                AssertFails(M("editor | call AddLayer(\"\")"), "invalid-value", "cannot be empty");

                var go = new GameObject("Layered");
                AssertOk(M("objects | where name == \"Layered\" | set layerName = \"RqlTestLayer\""));
                Assert.AreEqual(index, go.layer);

                Assert.AreEqual("true", Cell(M("editor | call AddTag(\"RqlTestTag\")"), 0, 2));
                Assert.AreEqual("false", Cell(M("editor | call AddTag(\"RqlTestTag\")"), 0, 2));
                AssertOk(M("objects | where name == \"Layered\" | set tag = \"RqlTestTag\""));
                Assert.AreEqual("RqlTestTag", go.tag);
            }
            finally
            {
                EditorJsonUtility.FromJsonOverwrite(saved, manager);
                EditorUtility.SetDirty(manager);
                AssetDatabase.SaveAssets();
            }
            Assert.AreEqual(-1, LayerMask.NameToLayer("RqlTestLayer"), "the TagManager was restored");
        }

        // rql-mutate's limit of 20 targets: the hint names the tool's max_targets argument, which raises it.
        [Test]
        public void MaxTargetsRaisesTheMutationLimit()
        {
            var root = new GameObject("Many");
            for (int i = 0; i < 25; i++) new GameObject("M" + i).transform.SetParent(root.transform, false);
            const string Q25 = "objects | where name == \"Many\" | get children | set active = false";
            var limited = RqlUnity.Mutate(Q25);
            AssertFails(limited, "mutation-limit");
            StringAssert.Contains("max_targets", Table(limited));
            StringAssert.DoesNotContain("budgets.", Table(limited));
            Assert.IsTrue(root.transform.GetChild(0).gameObject.activeSelf);
            AssertOk(RqlUnity.Mutate(Q25, maxTargets: 25));
            Assert.IsFalse(root.transform.GetChild(24).gameObject.activeSelf);
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
