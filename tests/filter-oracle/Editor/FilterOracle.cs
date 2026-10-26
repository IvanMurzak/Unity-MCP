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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.API.TestRunner;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.FilterOracle
{
    /// <summary>
    /// The oracle for tests-run's filter check: for every scenario Unity runs the tool's own <see cref="Filter"/>, and the
    /// tests it reports are compared with the tool's count of them (<see cref="TestFilterMatch"/>). Started with
    /// <c>-batchmode -nographics -executeMethod com.IvanMurzak.Unity.MCP.FilterOracle.FilterOracle.Run [-filterOracleOut file]</c>
    /// (no <c>-quit</c>); exits 0 when every count matches, 1 on a mismatch, 2 when a scenario gave no result.
    /// </summary>
    public static class FilterOracle
    {
        const string Asm = "FilterOracle.EditModeFixtures";
        const string PlayAsm = "FilterOracle.PlayModeFixtures";
        const string Ns = "FilterOracle.Data";
        const string PlayNs = "FilterOracle.Play";

        sealed class Scenario
        {
            public string Name = "";
            public TestMode Mode = TestMode.EditMode;
            public string? Assembly, Namespace, Class, Method;
            public string[]? Query;
        }

        static Scenario Edit(string name, string? asm = null, string? ns = null, string? cls = null, string? method = null, string[]? query = null)
            => new Scenario { Name = name, Assembly = asm, Namespace = ns, Class = cls, Method = method, Query = query };

        static Scenario Play(string name, string? asm = null, string? ns = null, string? cls = null, string? method = null)
            => new Scenario { Name = name, Mode = TestMode.PlayMode, Assembly = asm, Namespace = ns, Class = cls, Method = method };

        // EditMode first, then PlayMode: each run then needs no mode switch.
        static readonly Scenario[] Scenarios =
        {
            Edit("assembly", Asm),
            Edit("assembly + missing class", Asm, cls: "Missing"),
            Edit("assembly + namespace", Asm, Ns),
            Edit("assembly + method", Asm, method: Ns + ".OracleParam.Plain"),
            Edit("class + another class's method", cls: "OracleParam", method: Ns + ".OracleOther.Plain2"),
            Edit("namespace + missing class", ns: Ns, cls: "Missing"),
            Edit("parameterized method", method: Ns + ".OracleParam.Method"),
            Edit("namespace + parameterized method", ns: Ns, method: Ns + ".OracleParam.Method"),
            Edit("assembly + class", Asm, cls: "OracleParam"),
            Edit("namespace + method", ns: Ns, method: Ns + ".OracleParam.Plain"),
            Edit("method in another case", method: (Ns + ".OracleParam.Plain").ToLowerInvariant()),
            Edit("assembly in another case + class", Asm.ToLowerInvariant(), cls: "OracleOther"),
            Edit("namespace", ns: Ns),
            Edit("namespace prefix", ns: "FilterOracle"),
            Edit("namespace = class full name", ns: Ns + ".OracleParam"),
            Edit("namespace = assembly name", ns: Asm),
            Edit("namespace + class + method", ns: Ns + ".Sub", cls: "OracleLeaf", method: Ns + ".Sub.OracleLeaf.InSub"),
            Edit("class with fixture arguments", cls: "OracleArgs"),
            Edit("method of a fixture case", method: Ns + ".OracleArgs(1).Test"),
            Edit("method of a fixture with arguments", method: Ns + ".OracleArgs.Test"),
            Edit("generic class", cls: "OracleGeneric"),
            Edit("nested class", cls: "Inner"),
            Edit("class = namespace segment", cls: "Widget"),
            Edit("class with an explicit test", cls: "OracleExplicit"),
            Edit("explicit method", method: Ns + ".OracleExplicit.ExplicitTest"),
            Edit("ignored class", cls: "OracleIgnored"),
            Edit("UnityTest class", cls: "OracleCoroutine"),
            Edit("method with duplicate case names", method: Ns + ".OracleDup.Same"),
            Edit("duplicate case name", method: Ns + ".OracleDup.Same(1)"),
            Edit("assembly + excluded method", Asm, method: "!" + Ns + ".OracleOther.Plain2"),
            Edit("query: methods", query: new[] { Ns + ".OracleParam.Method", Ns + ".OracleOther.Plain2" }),
            Edit("query: fixture case", query: new[] { Ns + ".OracleArgs(1).Test" }),
            Play("PlayMode assembly", PlayAsm),
            Play("PlayMode assembly + missing class", PlayAsm, cls: "Missing"),
            Play("PlayMode namespace + method", ns: PlayNs, method: PlayNs + ".OraclePlay.A"),
            Play("PlayMode parameterized method", method: PlayNs + ".OraclePlay.Param"),
        };

        // A scenario takes seconds; one whose run reports nothing in this time is retried once.
        const double ScenarioTimeoutSeconds = 120;
        const double TeardownTimeoutSeconds = 60;
        const int IdleTicksBeforeRun = 3;

        // State survives a domain reload (SessionState); a reload drops the statics below.
        const string Key = "FilterOracle.";
        static int Index { get => SessionState.GetInt(Key + "index", 0); set => SessionState.SetInt(Key + "index", value); }
        static int Attempt { get => SessionState.GetInt(Key + "attempt", 0); set => SessionState.SetInt(Key + "attempt", value); }
        static int Ran { get => SessionState.GetInt(Key + "ran", 0); set => SessionState.SetInt(Key + "ran", value); }
        static bool Active { get => SessionState.GetBool(Key + "active", false); set => SessionState.SetBool(Key + "active", value); }
        static bool Running { get => SessionState.GetBool(Key + "running", false); set => SessionState.SetBool(Key + "running", value); }
        static string Predicted { get => SessionState.GetString(Key + "predicted", ""); set => SessionState.SetString(Key + "predicted", value); }
        static string Rows { get => SessionState.GetString(Key + "rows", ""); set => SessionState.SetString(Key + "rows", value); }
        static string OutPath { get => SessionState.GetString(Key + "out", ""); set => SessionState.SetString(Key + "out", value); }
        static string Scenes { get => SessionState.GetString(Key + "scenes", ""); set => SessionState.SetString(Key + "scenes", value); }

        static TestRunnerApi? api;
        static double lastProgress;
        static int idleTicks;

        public static void Run()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                var outIndex = Array.IndexOf(args, "-filterOracleOut");
                OutPath = Path.GetFullPath(outIndex >= 0 && outIndex + 1 < args.Length ? args[outIndex + 1] : "filter-oracle-results.md");
                Index = 0; Attempt = 0; Rows = ""; Running = false; Scenes = Tool_Tests.OpenScenePaths();
                SessionState.SetInt(Key + "bootstrapScenes", BootstrapScenes());
                DisablePlayModeReload();
                var testRunner = Attach();
                testRunner.RetrieveTestList(TestMode.EditMode, edit => testRunner.RetrieveTestList(TestMode.PlayMode, play =>
                {
                    try
                    {
                        if (!Leaves(edit).Any(t => t.FullName.StartsWith(Ns + ".")) || !Leaves(play).Any(t => t.FullName.StartsWith(PlayNs + ".")))
                            throw new InvalidOperationException("the oracle's fixture tests are not in Unity's test tree");
                        Predicted = string.Join(",", Scenarios.Select(s =>
                            TestFilterMatch.Count(s.Mode == TestMode.PlayMode ? play : edit, CreateFilter(s))));
                        Active = true;
                    }
                    catch (Exception e) { Fail(e); }
                }));
            }
            catch (Exception e) { Fail(e); }
        }

        [InitializeOnLoadMethod]
        static void ResumeAfterDomainReload()
        {
            if (Active)
                Attach();
        }

        static TestRunnerApi Attach()
        {
            var testRunner = ScriptableObject.CreateInstance<TestRunnerApi>();
            testRunner.RegisterCallbacks(new Callbacks());
            api = testRunner;
            lastProgress = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            return testRunner;
        }

        // One loop: start the next run only once Unity has fully left the previous one.
        static void Tick()
        {
            if (!Active)
                return;
            if (Running)
            {
                if (EditorApplication.timeSinceStartup - lastProgress > ScenarioTimeoutSeconds)
                    NoResult();
                return;
            }
            if (!IsIdle())
            {
                idleTicks = 0;
                if (EditorApplication.timeSinceStartup - lastProgress < TeardownTimeoutSeconds)
                    return;
                Debug.LogWarning($"[FilterOracle] Unity's previous run did not finish its teardown in {TeardownTimeoutSeconds:0} s; going on");
            }
            else if (++idleTicks < IdleTicksBeforeRun)
                return;
            idleTicks = 0;
            if (Index >= Scenarios.Length)
            {
                Finish();
                return;
            }
            Ran = 0;
            Running = true;
            Scenes = Tool_Tests.OpenScenePaths();
            lastProgress = EditorApplication.timeSinceStartup;
            api!.Execute(new ExecutionSettings(CreateFilter(Scenarios[Index])));
        }

        // Unity's run is over (tests-run's own rule: out of Play Mode, the run's scenes restored) and its bootstrap scene,
        // deleted by a later teardown task, is gone; otherwise the next run, or the exit, cuts that teardown short.
        static bool IsIdle()
            => Tool_Tests.IsTestRunTeardownComplete(EditorApplication.isPlayingOrWillChangePlaymode, Tool_Tests.OpenScenePaths, Scenes)
                && !EditorApplication.isCompiling && !EditorApplication.isUpdating
                && BootstrapScenes() <= SessionState.GetInt(Key + "bootstrapScenes", 0);

        // Leftovers of an earlier, cut run stay; only this run's must be gone.
        static int BootstrapScenes() => Directory.GetFiles("Assets", "InitTestScene*.unity").Length;

        static Filter CreateFilter(Scenario s)
        {
            var filterParams = new TestFilterParameters(s.Assembly, s.Namespace, s.Class, s.Method);
            if (s.Query != null)
                filterParams.Groups = new[] { Tool_Tests.CreateTestsRegexPattern(s.Query) };
            return Tool_Tests.CreateTestFilter(s.Mode, filterParams);
        }

        sealed class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) => lastProgress = EditorApplication.timeSinceStartup;
            public void TestStarted(ITestAdaptor test) => lastProgress = EditorApplication.timeSinceStartup;

            public void TestFinished(ITestResultAdaptor result)
            {
                lastProgress = EditorApplication.timeSinceStartup;
                if (Running && !result.Test.IsSuite)
                    Ran++;
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                if (Running)
                    Record(Ran.ToString());
            }
        }

        static void NoResult()
        {
            Running = false;
            if (Attempt == 0)
            {
                Attempt = 1;
                Debug.LogWarning($"[FilterOracle] {Scenarios[Index].Name}: no result in {ScenarioTimeoutSeconds:0} s, retrying");
                return;
            }
            Record("no result");
        }

        static void Record(string ran)
        {
            Running = false;
            var predicted = Predicted.Split(',')[Index];
            Rows += $"{Index}\t{ran}\t{predicted}\n";
            Debug.Log($"[FilterOracle] {Scenarios[Index].Name}: Unity ran {ran}, tests-run counted {predicted}");
            Index++;
            Attempt = 0;
        }

        static void Finish()
        {
            Active = false;
            EditorApplication.update -= Tick;
            RestorePlayModeReload();
            var rows = Rows.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Split('\t')).ToList();
            var lost = rows.Count(r => r[1] == "no result");
            var mismatched = rows.Count(r => r[1] != r[2]) - lost;
            var lines = new List<string>
            {
                $"## tests-run filter oracle: Unity {Application.unityVersion}",
                "",
                $"{rows.Count - mismatched - lost} of {rows.Count} scenarios: tests-run counts exactly the tests Unity runs" +
                    (mismatched > 0 ? $"; **{mismatched} mismatched**" : "") + (lost > 0 ? $"; **{lost} gave no result**" : "") + ".",
                "",
                "| Scenario | Filter | Unity ran | tests-run counted | |",
                "|---|---|---|---|---|",
            };
            foreach (var r in rows)
            {
                var s = Scenarios[int.Parse(r[0])];
                lines.Add($"| {s.Mode} {s.Name} | {Describe(s)} | {r[1]} | {r[2]} | {(r[1] == r[2] ? "ok" : "**MISMATCH**")} |");
            }
            File.WriteAllLines(OutPath, lines);
            Debug.Log($"[FilterOracle] {rows.Count - mismatched - lost}/{rows.Count} match; report: {OutPath}");
            EditorApplication.Exit(lost > 0 ? 2 : mismatched > 0 ? 1 : 0);
        }

        static string Describe(Scenario s)
        {
            var parts = new List<string>();
            if (s.Assembly != null) parts.Add($"assembly `{s.Assembly}`");
            if (s.Namespace != null) parts.Add($"namespace `{s.Namespace}`");
            if (s.Class != null) parts.Add($"class `{s.Class}`");
            if (s.Method != null) parts.Add($"method `{s.Method}`");
            if (s.Query != null) parts.Add($"query `{string.Join(", ", s.Query)}`");
            return string.Join(", ", parts);
        }

        // PlayMode runs without a domain reload: faster, and nothing the oracle tracks is lost to a reload.
        static void DisablePlayModeReload()
        {
            SessionState.SetBool(Key + "optionsEnabled", EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt(Key + "options", (int)EditorSettings.enterPlayModeOptions);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
        }

        static void RestorePlayModeReload()
        {
            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Key + "optionsEnabled", false);
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(Key + "options", 0);
        }

        static IEnumerable<ITestAdaptor> Leaves(ITestAdaptor node)
            => !node.IsSuite ? new[] { node }
                : node.HasChildren ? node.Children.SelectMany(Leaves)
                : Enumerable.Empty<ITestAdaptor>();

        static void Fail(Exception e)
        {
            Active = false;
            Debug.LogError("[FilterOracle] " + e);
            try
            {
                RestorePlayModeReload();
                File.WriteAllText(OutPath, $"## tests-run filter oracle: Unity {Application.unityVersion}\n\nThe oracle failed: `{e.Message}`\n");
            }
            finally
            {
                EditorApplication.Exit(2);
            }
        }
    }
}
