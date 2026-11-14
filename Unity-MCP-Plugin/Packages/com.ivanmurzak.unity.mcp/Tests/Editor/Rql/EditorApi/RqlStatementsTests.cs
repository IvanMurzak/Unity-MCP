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
using System.IO;
using System.Linq;
using System.Text.Json;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.EditorApi
{
    /// <summary>
    /// Several statements separated by ; in one rql-query / rql-mutate call (RQL spec §14): one result block per statement,
    /// and in a change each statement is its own all-or-nothing, undoable step; the first that fails stops the rest, and the
    /// changes before it are saved.
    /// </summary>
    public class RqlStatementsTests : EditorApiTestBase
    {
        private static GameObject? Find(string name) => SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == name);

        [Test]
        public void AQueryOfTwoStatementsReturnsABlockPerStatement()
        {
            new GameObject("Alpha");
            new GameObject("Beta");
            const string Query = "objects | where name == \"Alpha\" | count; objects | where name == \"Beta\" | select name";
            var r = Q(Query);
            AssertOk(r);
            Assert.AreEqual(2, r.Statements!.Count);
            Assert.AreEqual(1L, r.Statements[0].Result!.Value!.GetValue<long>());
            Assert.AreEqual("Beta", Cell(r.Statements[1].Result!));

            var response = CallThroughMcpPlugin(Tool_Rql.QueryToolId, JsonSerializer.Serialize(new { query = Query }));
            Assert.AreEqual(ResponseStatus.Success, response.Value!.Status);
            var text = Text(response.Value);
            StringAssert.Contains("[1/2] objects | where name == \"Alpha\" | count", text);
            StringAssert.Contains("[2/2] objects | where name == \"Beta\" | select name", text);
        }

        [Test]
        public void EachStatementOfAChangeIsItsOwnUndoStep()
        {
            Undo.ClearAll();
            AssertOk(RqlUnity.Mutate("editor | call CreateGameObject(\"First\"); editor | call CreateGameObject(\"Second\")"));
            Assert.IsNotNull(Find("First"));
            Assert.IsNotNull(Find("Second"));
            Undo.PerformUndo();
            Assert.IsNull(Find("Second"), "the last statement undoes first");
            Assert.IsNotNull(Find("First"), "the first statement is a step of its own");
            Undo.PerformUndo();
            Assert.IsNull(Find("First"));
        }

        [Test]
        public void AFailedStatementStopsTheRestAndTheEarlierChangeIsSaved()
        {
            var path = $"{Folder}/Statements.unity";
            AssertOk(M($"editor | call NewScene(\"{path}\", true)"));
            int saving = 0;
            void OnSaving() => saving++;
            RqlUnity.Saving += OnSaving;
            try
            {
                var r = RqlUnity.Mutate(
                    "editor | call CreateGameObject(\"Kept\"); " +
                    "objects | where name == \"Kept\" | set transform.position = [1, 2, 3], nosuchmember = 1; " +
                    "editor | call CreateGameObject(\"Never\")");
                Assert.IsFalse(r.Ok, Table(r));
                Assert.AreEqual(3, r.Statements!.Count, Table(r));
                Assert.IsTrue(r.Statements[0].Result!.Ok, Table(r));
                Assert.IsFalse(r.Statements[1].Result!.Ok, Table(r));
                StringAssert.Contains("statement 2 failed", r.Statements[2].Skipped, Table(r));

                var kept = Find("Kept");
                Assert.IsNotNull(kept, "the first statement's change stays");
                Assert.AreEqual(Vector3.zero, kept!.transform.position, "the failed statement changed nothing");
                Assert.IsNull(Find("Never"), "the statements after the failure do not run");
                Assert.AreEqual(1, saving, "one save for the whole request");
                Assert.IsFalse(SceneManager.GetActiveScene().isDirty, Table(r));
                StringAssert.Contains("m_Name: Kept", File.ReadAllText(path));
            }
            finally
            {
                RqlUnity.Saving -= OnSaving;
            }
        }

        [Test]
        public void AnInvalidStatementChangesNothing()
        {
            var r = RqlUnity.Mutate("editor | call CreateGameObject(\"Never\"); objects | select name");
            Assert.IsFalse(r.Ok, Table(r));
            StringAssert.Contains("nothing was changed", r.Statements![0].Skipped, Table(r));
            Assert.IsNull(Find("Never"));
        }

        [Test]
        public void ADryRunOfTwoStatementsChangesNothing()
        {
            var target = new GameObject("Target");
            int saving = 0, discarded = 0;
            void OnSaving() => saving++;
            void OnDiscarded() => discarded++;
            RqlUnity.Saving += OnSaving;
            RqlUnity.Discarded += OnDiscarded;
            try
            {
                var r = RqlUnity.Mutate("objects | where name == \"Target\" | set active = false; editor | call CreateGameObject(\"Never\")", dryRun: true);
                AssertOk(r);
                Assert.IsTrue(r.DryRun);
                Assert.IsTrue(r.Statements!.All(s => s.Result!.DryRun), Table(r));
                Assert.IsTrue(target.activeSelf);
                Assert.IsNull(Find("Never"));
                Assert.AreEqual((0, 1), (saving, discarded));
            }
            finally
            {
                RqlUnity.Saving -= OnSaving;
                RqlUnity.Discarded -= OnDiscarded;
            }
        }
    }
}
