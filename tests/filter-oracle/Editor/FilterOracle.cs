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
            public TestMode Mode;
            public string? Assembly, Namespace, Class, Method;
            public string[]? Query;
        }

        static Scenario S(string name, string? asm = null, string? ns = null, string? cls = null, string? method = null,
            string[]? query = null, TestMode mode = TestMode.EditMode)
            => new Scenario { Name = name, Mode = mode, Assembly = asm, Namespace = ns, Class = cls, Method = method, Query = query };

        // EditMode first, then PlayMode: each run then needs no mode switch.
        static readonly Scenario[] Scenarios =
        {
            S("assembly", Asm),
            S("assembly + missing class", Asm, cls: "Missing"),
            S("assembly + namespace", Asm, Ns),
            S("assembly + method", Asm, method: Ns + ".OracleParam.Plain"),
            S("class + another class's method", cls: "OracleParam", method: Ns + ".OracleOther.Plain2"),
            S("namespace + missing class", ns: Ns, cls: "Missing"),
            S("parameterized method", method: Ns + ".OracleParam.Method"),
            S("namespace + parameterized method", ns: Ns, method: Ns + ".OracleParam.Method"),
            S("assembly + class", Asm, cls: "OracleParam"),
            S("namespace + method", ns: Ns, method: Ns + ".OracleParam.Plain"),
            S("method in another case", method: (Ns + ".OracleParam.Plain").ToLowerInvariant()),
            S("assembly in another case + class", Asm.ToLowerInvariant(), cls: "OracleOther"),
            S("namespace", ns: Ns),
            S("namespace prefix", ns: "FilterOracle"),
            S("namespace = class full name", ns: Ns + ".OracleParam"),
            S("namespace = assembly name", ns: Asm),
            S("namespace + class + method", ns: Ns + ".Sub", cls: "OracleLeaf", method: Ns + ".Sub.OracleLeaf.InSub"),
            S("class with fixture arguments", cls: "OracleArgs"),
            S("method of a fixture case", method: Ns + ".OracleArgs(1).Test"),
            S("method of a fixture with arguments", method: Ns + ".OracleArgs.Test"),
            S("generic class", cls: "OracleGeneric"),
            S("nested class", cls: "Inner"),
            S("class = namespace segment", cls: "Widget"),
            S("class with an explicit test", cls: "OracleExplicit"),
            S("explicit method", method: Ns + ".OracleExplicit.ExplicitTest"),
            S("ignored class", cls: "OracleIgnored"),
            S("UnityTest class", cls: "OracleCoroutine"),
            S("method with duplicate case names", method: Ns + ".OracleDup.Same"),
            S("duplicate case name", method: Ns + ".OracleDup.Same(1)"),
            S("assembly + excluded method", Asm, method: "!" + Ns + ".OracleOther.Plain2"),
            S("query: methods", query: new[] { Ns + ".OracleParam.Method", Ns + ".OracleOther.Plain2" }),
            S("query: fixture case", query: new[] { Ns + ".OracleArgs(1).Test" }),
            S("assembly", PlayAsm, mode: TestMode.PlayMode),
            S("assembly + missing class", PlayAsm, cls: "Missing", mode: TestMode.PlayMode),
            S("namespace + method", ns: PlayNs, method: PlayNs + ".OraclePlay.A", mode: TestMode.PlayMode),
            S("parameterized method", method: PlayNs + ".OraclePlay.Param", mode: TestMode.PlayMode),
        };

        // The fixture tests take milliseconds: a run that reports nothing for this long is retried once.
        const double ScenarioTimeoutSeconds = 45;
        const int IdleTicksBeforeRun = 3;
        const string NoResult = "no result";

        // State survives a domain reload (SessionState); a reload drops the statics below. Results holds one entry per
        // finished scenario, so its length is the next scenario's index.
        const string Key = "FilterOracle.";
        static readonly SessionStateString Predicted = new SessionStateString(Key + "predicted");
        static readonly SessionStateString Results = new SessionStateString(Key + "results");
        static readonly SessionStateString OutPath = new SessionStateString(Key + "out");
        static readonly SessionStateString Scenes = new SessionStateString(Key + "scenes");
        static readonly SessionStateBool Running = new SessionStateBool(Key + "running");
        static readonly SessionStateBool Retried = new SessionStateBool(Key + "retried");
        static readonly SessionStateInt Ran = new SessionStateInt(Key + "ran");
        static readonly SessionStateInt BootstrapScenesAtStart = new SessionStateInt(Key + "bootstrapScenes");
        static readonly SessionStateBool PlayModeOptionsEnabled = new SessionStateBool(Key + "optionsEnabled");
        static readonly SessionStateInt PlayModeOptions = new SessionStateInt(Key + "options");

        static bool Active => Predicted.Value.Length > 0;
        static List<string> ResultList => Results.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        static string Header => $"## tests-run filter oracle: Unity {Application.unityVersion}";

        static TestRunnerApi? api;
        static double lastProgress;
        static int idleTicks;

        public static void Run()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                var outIndex = Array.IndexOf(args, "-filterOracleOut");
                OutPath.Value = Path.GetFullPath(outIndex >= 0 && outIndex + 1 < args.Length ? args[outIndex + 1] : "filter-oracle-results.md");
                Results.Value = "";
                Running.Value = false;
                Retried.Value = false;
                Scenes.Value = Tool_Tests.OpenScenePaths();
                BootstrapScenesAtStart.Value = BootstrapScenes();
                DisablePlayModeReload();
                var testRunner = Attach();
                testRunner.RetrieveTestList(TestMode.EditMode, edit => testRunner.RetrieveTestList(TestMode.PlayMode, play =>
                {
                    try
                    {
                        if (!Leaves(edit).Any(t => t.FullName.StartsWith(Ns + ".")) || !Leaves(play).Any(t => t.FullName.StartsWith(PlayNs + ".")))
                            throw new InvalidOperationException("the oracle's fixture tests are not in Unity's test tree");
                        Predicted.Value = string.Join(",", Scenarios.Select(s =>
                            TestFilterMatch.Count(s.Mode == TestMode.PlayMode ? play : edit, CreateFilter(s))));
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
            if (Running.Value)
            {
                if (EditorApplication.timeSinceStartup - lastProgress > ScenarioTimeoutSeconds)
                    GiveUpOnRun();
                return;
            }
            if (!IsIdle())
            {
                idleTicks = 0;
                if (EditorApplication.timeSinceStartup - lastProgress < Tool_Tests.TeardownTimeout.TotalSeconds)
                    return;
                Debug.LogWarning($"[FilterOracle] Unity's previous run did not finish its teardown in {Tool_Tests.TeardownTimeout.TotalSeconds:0} s; going on");
            }
            else if (++idleTicks < IdleTicksBeforeRun)
                return;
            idleTicks = 0;
            var index = ResultList.Count;
            if (index >= Scenarios.Length)
            {
                Finish();
                return;
            }
            Ran.Value = 0;
            Running.Value = true;
            Scenes.Value = Tool_Tests.OpenScenePaths();
            lastProgress = EditorApplication.timeSinceStartup;
            api!.Execute(new ExecutionSettings(CreateFilter(Scenarios[index])));
        }

        // Unity's run is over (tests-run's own rule: out of Play Mode, the run's scenes restored) and its bootstrap scene,
        // deleted by a later teardown task, is gone; otherwise the next run, or the exit, cuts that teardown short.
        static bool IsIdle()
            => Tool_Tests.IsTestRunTeardownComplete(EditorApplication.isPlayingOrWillChangePlaymode, Tool_Tests.OpenScenePaths, Scenes.Value)
                && !EditorApplication.isCompiling && !EditorApplication.isUpdating
                && BootstrapScenes() <= BootstrapScenesAtStart.Value;

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
                if (Running.Value && !result.Test.IsSuite)
                    Ran.Value++;
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                if (Running.Value)
                    Record(Ran.Value.ToString());
            }
        }

        static void GiveUpOnRun()
        {
            Running.Value = false;
            if (!Retried.Value)
            {
                Retried.Value = true;
                Debug.LogWarning($"[FilterOracle] {Scenarios[ResultList.Count].Name}: no result in {ScenarioTimeoutSeconds:0} s, retrying");
                return;
            }
            Record(NoResult);
        }

        static void Record(string ran)
        {
            Running.Value = false;
            Retried.Value = false;
            var index = ResultList.Count;
            Results.Value += ran + ",";
            Debug.Log($"[FilterOracle] {Scenarios[index].Mode} {Scenarios[index].Name}: Unity ran {ran}, tests-run counted {Predicted.Value.Split(',')[index]}");
        }

        static void Finish()
        {
            var predicted = Predicted.Value.Split(',');
            var ran = ResultList;
            Predicted.Value = "";
            EditorApplication.update -= Tick;
            RestorePlayModeReload();
            var matched = Enumerable.Range(0, ran.Count).Count(i => ran[i] == predicted[i]);
            var lost = ran.Count(r => r == NoResult);
            var mismatched = ran.Count - matched - lost;
            var lines = new List<string>
            {
                Header,
                "",
                $"{matched} of {ran.Count} scenarios: tests-run counts exactly the tests Unity runs" +
                    (mismatched > 0 ? $"; **{mismatched} mismatched**" : "") + (lost > 0 ? $"; **{lost} gave no result**" : "") + ".",
                "",
                "| Scenario | Filter | Unity ran | tests-run counted | |",
                "|---|---|---|---|---|",
            };
            for (var i = 0; i < ran.Count; i++)
            {
                var s = Scenarios[i];
                lines.Add($"| {s.Mode} {s.Name} | {Describe(s)} | {ran[i]} | {predicted[i]} | {(ran[i] == predicted[i] ? "ok" : "**MISMATCH**")} |");
            }
            File.WriteAllLines(OutPath.Value, lines);
            Debug.Log($"[FilterOracle] {matched}/{ran.Count} match; report: {OutPath.Value}");
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
            PlayModeOptionsEnabled.Value = EditorSettings.enterPlayModeOptionsEnabled;
            PlayModeOptions.Value = (int)EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
        }

        static void RestorePlayModeReload()
        {
            EditorSettings.enterPlayModeOptionsEnabled = PlayModeOptionsEnabled.Value;
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)PlayModeOptions.Value;
        }

        static IEnumerable<ITestAdaptor> Leaves(ITestAdaptor node)
            => !node.IsSuite ? new[] { node }
                : node.HasChildren ? node.Children.SelectMany(Leaves)
                : Enumerable.Empty<ITestAdaptor>();

        static void Fail(Exception e)
        {
            Predicted.Value = "";
            Debug.LogError("[FilterOracle] " + e);
            try
            {
                RestorePlayModeReload();
                File.WriteAllText(OutPath.Value, $"{Header}\n\nThe oracle failed: `{e.Message}`\n");
            }
            finally
            {
                EditorApplication.Exit(2);
            }
        }
    }
}
