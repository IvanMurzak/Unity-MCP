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
using System.Text.Json;
using com.IvanMurzak.McpPlugin.Common.Model;
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

        /// <summary>A tuple such as "(0.1, 0.2, 0.3, 1)", compared per component: some Unity versions round-trip colors
        /// through the color space, so 0.1 can come back as 0.09999997.</summary>
        protected static void AssertTuple(string expected, string actual, float tolerance = 1e-4f)
        {
            float[] Parse(string t) => t.Trim('(', ')').Split(',').Select(x => float.Parse(x.Trim(), System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            var e = Parse(expected);
            var a = Parse(actual);
            Assert.AreEqual(e.Length, a.Length, actual);
            for (int i = 0; i < e.Length; i++) Assert.AreEqual(e[i], a[i], tolerance, actual);
        }

        /// <summary>The value of a query ending in <c>count</c>.</summary>
        protected static long Count(string rql)
        {
            var r = Q(rql);
            Assert.IsTrue(r.Ok, Table(r));
            return r.Value!.GetValue<long>();
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

        /// <summary>A tool call through the plugin's McpPlugin pipeline (the agent's path), e.g. rql-query.</summary>
        protected static ResponseData<ResponseCallTool> CallThroughMcpPlugin(string tool, string json)
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
            return UnityMcpPluginEditor.Instance.Tools!.RunCallTool(new RequestCallTool(tool, args)).Result;
        }

        /// <summary>A tool response's text content, one block per line.</summary>
        protected static string Text(ResponseCallTool response) => string.Join("\n", response.Content.Select(c => c.Text));

        protected static string Schema(string type) => RqlUnitySchema.Render(RqlUnity.Engine, type);
    }
}
