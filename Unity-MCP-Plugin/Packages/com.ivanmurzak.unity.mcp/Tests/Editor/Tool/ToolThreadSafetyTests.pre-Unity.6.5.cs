/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
#if !UNITY_6000_5_OR_NEWER
using System.Collections;
using System.IO;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Thread-safety coverage for MCP tools — regression for
    /// <see href="https://github.com/IvanMurzak/Unity-MCP/issues/633">#633</see>.
    ///
    /// For each tool we want to guarantee the tool's body handles invocation from any
    /// thread (the MCP server dispatches tool calls from a SignalR background thread,
    /// so tools that touch Unity APIs must marshal back to the main thread via
    /// <c>MainThread.Instance.Run</c>). Forgetting to do so caused the regression in
    /// <see href="https://github.com/IvanMurzak/Unity-MCP/issues/632">#632</see> /
    /// <see href="https://github.com/IvanMurzak/Unity-MCP/pull/637">#637</see>
    /// (<c>console-clear-logs</c>).
    ///
    /// <b>Excluded tools</b>: <c>package-add</c>/<c>package-remove</c> (trigger domain
    /// reload), <c>tests-run</c> (recursive hang), system tools (<c>ping</c>,
    /// <c>unity-skill-*</c>). See inline comments for rationale.
    ///
    /// Two helpers from <see cref="BaseTest"/> are used:
    /// <list type="bullet">
    /// <item><see cref="BaseTest.RunToolBothThreads"/> — happy path; asserts the tool
    ///       succeeds on both threads. Used when crafting a valid input is practical.</item>
    /// <item><see cref="BaseTest.RunToolExpectNoThreadViolation"/> — lenient mode; tolerates
    ///       business-level errors and only fails the test if Unity surfaces a
    ///       <c>"can only be called from the main thread"</c> exception. Used for tools
    ///       that need elaborate setup (<c>SerializedMember</c>, prefab stages, etc.) or
    ///       whose happy path would mutate the project in risky ways
    ///       (<c>package-add</c>/<c>package-remove</c>, <c>tests-run</c>, etc.).</item>
    /// </list>
    /// </summary>
    public class ToolThreadSafetyTests : BaseTest
    {
        const string TmpFolderName = "BgThreadTests_TMP";
        const string TmpFolder = "Assets/" + TmpFolderName;

        [TearDown]
        public void BgTearDown()
        {
            if (!AssetDatabase.IsValidFolder(TmpFolder))
                return;

            // Settle imports created on a background thread (e.g. mat_bg.mat from
            // AssetsMaterialCreate_BothThreads, scene_bg.unity from SceneCreate_BothThreads)
            // before deleting the temp folder. Unity 6.x's async import worker can otherwise
            // still be flushing such an asset when DeleteAsset removes the folder, and the
            // deferred importer then logs an unexpected [Error] that fails whichever test
            // happens to be running — a shared-teardown race, not a defect in any single test.
            AssetDatabase.SaveAssets();

            // Bracket BOTH the import-settling Refresh() and the cleanup delete so benign
            // importer-teardown log noise can never fail a test: the deferred importer can
            // surface its [Error] while Refresh() flushes the import queue, not only during
            // DeleteAsset. Restored in finally per the project's LogAssert convention.
            LogAssert.ignoreFailingMessages = true;
            try
            {
                AssetDatabase.Refresh();
                AssetDatabase.DeleteAsset(TmpFolder);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        // ================================================================
        // RQL tools (replace the gameobject / assets / scene / editor / console tools)
        // ================================================================

        [UnityTest] public IEnumerator RqlQuery_BothThreads()
            => RunToolBothThreads(Tool_Rql.QueryToolId, @"{""query"":""objects | count""}");

        [UnityTest] public IEnumerator RqlSchema_BothThreads()
            => RunToolBothThreads(Tool_Rql.SchemaToolId, "{}");

        [UnityTest]
        public IEnumerator RqlMutate_BothThreads()
        {
            yield return RunToolMainThreadCoop(Tool_Rql.MutateToolId, @"{""query"":""editor | call CreateGameObject(\""bg_main\"")""}");
            Assert.IsNotNull(GameObject.Find("bg_main"));
            yield return RunToolFromBackgroundThread(Tool_Rql.MutateToolId, @"{""query"":""editor | call CreateGameObject(\""bg_bg\"")""}");
            Assert.IsNotNull(GameObject.Find("bg_bg"));
        }

        [UnityTest]
        public IEnumerator RqlMutateClearConsole_BothThreads()
        {
            // Regression for #632/#637 (formerly console-clear-logs): Debug.ClearDeveloperConsole() MUST run on the main thread.
            yield return RunToolBothThreads(Tool_Rql.MutateToolId, @"{""query"":""editor | call ClearConsole()""}");
        }

        // ================================================================
        // NO-ARG / READ-ONLY tools  (happy path, empty JSON)
        // ================================================================

        [UnityTest] public IEnumerator ToolList_BothThreads()
            => RunToolBothThreads(Tool_Tool.ToolListId, "{}");

        [UnityTest] public IEnumerator PackageList_BothThreads()
            => RunToolBothThreads(Tool_Package.PackageListToolId, "{}");

        // ================================================================
        // GAMEOBJECT tools — happy path, per-call setup
        // ================================================================

        // ================================================================
        // OBJECT tools
        // ================================================================

        // ================================================================
        // EDITOR tools
        // ================================================================

        // ================================================================
        // ASSETS tools
        // ================================================================

        // ---------- Prefabs ----------

        // ================================================================
        // SCENE tools
        // ================================================================

        // ================================================================
        // SCRIPT tools
        // ================================================================

        [UnityTest]
        public IEnumerator ScriptUpdateOrCreate_ReadDelete_BothThreads()
        {
            EnsureTmpFolder();
            var pathA = $"{TmpFolder}/S_a.cs";
            var pathB = $"{TmpFolder}/S_b.cs";
            const string SrcA = "public class BgSa { public static void M() {} }";
            const string SrcB = "public class BgSb { public static void M() {} }";

            // update-or-create
            yield return RunToolMainThreadCoop(Tool_Script.ScriptUpdateOrCreateToolId,
                $@"{{""filePath"":""{pathA}"",""content"":{JsonString(SrcA)}}}");
            yield return RunToolFromBackgroundThread(Tool_Script.ScriptUpdateOrCreateToolId,
                $@"{{""filePath"":""{pathB}"",""content"":{JsonString(SrcB)}}}");

            // read (main + bg)
            yield return RunToolBothThreads(Tool_Script.ScriptReadToolId,
                $@"{{""filePath"":""{pathA}""}}",
                $@"{{""filePath"":""{pathB}""}}");

            // delete
            yield return RunToolMainThreadCoop(Tool_Script.ScriptDeleteToolId, $@"{{""files"":[""{pathA}""]}}");
            yield return RunToolFromBackgroundThread(Tool_Script.ScriptDeleteToolId,
                $@"{{""files"":[""{pathB}""]}}");
        }

        [UnityTest]
        public IEnumerator ScriptExecute_BothThreads()
        {
            const string Code = "public class Script { public static int Main() { return 42; } }";
            yield return RunToolBothThreads(Tool_Script.ScriptExecuteToolId,
                $@"{{""csharpCode"":{JsonString(Code)},""className"":""Script"",""methodName"":""Main""}}");
        }

        // ================================================================
        // TOOL CONTROL
        // ================================================================

        [UnityTest]
        public IEnumerator ToolSetEnabledState_BothThreads()
        {
            // Enable unity-tool-list twice (idempotent, already enabled) from both threads.
            yield return RunToolBothThreads(Tool_Tool.ToolSetEnabledStateId,
                @"{""tools"":[{""name"":""unity-tool-list"",""enabled"":true}]}");
        }

        // ================================================================
        // REFLECTION (complex MethodRef schema → thread-safety only)
        // ================================================================

        // ================================================================
        // PACKAGE MANAGER — package-add / package-remove intentionally NOT
        // exercised here: both return status "Processing" and kick off a real
        // Unity PackageManager resolution on a background task, which in turn
        // forces a domain reload. Under the test runner that hangs the whole
        // batch-mode run. Their thread-safety is already covered by the fact
        // that their entry path is a synchronous parameter check.
        // ================================================================

        [UnityTest]
        public IEnumerator PackageSearch_ThreadSafetyOnly()
        {
            // Registry round-trip is slow and flaky under test; thread-safety only.
            yield return RunToolExpectNoThreadViolation(Tool_Package.PackageSearchToolId,
                @"{""query"":""com.unity.textmeshpro"",""maxResults"":1,""offlineMode"":true}");
        }

        // ================================================================
        // SCREENSHOTS — not guaranteed to have a camera/view in batchmode.
        // ================================================================

        [UnityTest]
        public IEnumerator ScreenshotCamera_BothThreads()
        {
            new GameObject("TestCamera").AddComponent<Camera>();
            yield return RunToolBothThreads(Tool_Screenshot.ScreenshotCameraToolId, @"{""width"":64,""height"":64}");
        }

        [UnityTest]
        public IEnumerator ScreenshotGameView_ThreadSafetyOnly()
            => RunToolExpectNoThreadViolation(Tool_Screenshot.ScreenshotGameViewToolId, "{}");

        [UnityTest]
        public IEnumerator ScreenshotSceneView_ThreadSafetyOnly()
            => RunToolExpectNoThreadViolation(Tool_Screenshot.ScreenshotSceneViewToolId, @"{""width"":64,""height"":64}");

        // ================================================================
        // TESTS — recursive; skip deliberately.
        // ================================================================
        // tests-run intentionally has no test here: executing it from within the test
        // runner causes recursion and a hang. Its thread safety is implicit because the
        // test runner itself schedules the invocation onto the main thread.

        // ================================================================
        // Helpers
        // ================================================================

        static void EnsureTmpFolder()
        {
            if (!AssetDatabase.IsValidFolder(TmpFolder))
                AssetDatabase.CreateFolder("Assets", TmpFolderName);
        }

        static string CreateTmpTextAsset(string fileName, string contents)
        {
            EnsureTmpFolder();
            var path = $"{TmpFolder}/{fileName}";
            File.WriteAllText(path, contents);
            AssetDatabase.ImportAsset(path);
            return path;
        }

        static string? FindFirstShaderPath()
        {
            // Prefer the built-in Standard shader.
            var standard = Shader.Find("Standard");
            if (standard != null)
            {
                var p = AssetDatabase.GetAssetPath(standard);
                if (!string.IsNullOrEmpty(p))
                    return p;
            }

            var guids = AssetDatabase.FindAssets("t:Shader");
            foreach (var guid in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(p))
                    return p;
            }
            return null;
        }

        /// <summary>Returns a JSON string literal (quoted, backslash-escaped).</summary>
        static string JsonString(string raw)
            => System.Text.Json.JsonSerializer.Serialize(raw);
    }
}
#endif
