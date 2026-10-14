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
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using Rql;
using Rql.Execution;
using UnityEditor.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Adapters
{
    /// <summary>Per-adapter tests: a fresh empty scene and engine per test, plus query helpers.</summary>
    public abstract class AdapterTestBase
    {
        [SetUp]
        public void NewScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RqlUnity.Reset();
        }

        protected static QueryResult Q(string rql) => RqlUnity.Engine.Execute(rql);

        protected static QueryResult M(string rql, bool dryRun = false) =>
            RqlUnity.Engine.Execute(rql, null, new QueryOptions { AllowMutation = true, DryRun = dryRun });

        protected static string Table(QueryResult r) => AgentFormat.Render(r, OutputFormat.Table);

        /// <summary>A cell of a successful result as text (JSON strings unquoted).</summary>
        protected static string Cell(QueryResult r, int row = 0, int col = 0)
        {
            Assert.IsTrue(r.Ok, Table(r));
            Assert.Greater(r.Rows!.Count, row, Table(r));
            var node = r.Rows![row][col];
            return node == null ? "null" : node.ToJsonString().Trim('"');
        }

        protected static void AssertOk(QueryResult r) => Assert.IsTrue(r.Ok, Table(r));

        /// <summary>The query failed with <paramref name="code"/> and its message contains <paramref name="text"/>.</summary>
        protected static void AssertFails(QueryResult r, string code, string? text = null)
        {
            Assert.IsFalse(r.Ok, Table(r));
            Assert.AreEqual(code, r.Diagnostics[0].Code, Table(r));
            if (text != null) StringAssert.Contains(text, Table(r));
        }

        protected static bool Warns(QueryResult r, string code) => r.Diagnostics.Any(d => d.Code == code);

        protected static string Schema(string type) => RqlUnitySchema.Render(RqlUnity.Engine, type);
    }
}
