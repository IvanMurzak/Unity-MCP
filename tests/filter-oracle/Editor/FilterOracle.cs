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
    /// (no <c>-quit</c>); exits 0 when every count matches, 3 on a mismatch, 2 when it stopped early (Unity's own
    /// failures, such as a compile error, exit 1).
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
            S("global-namespace class", cls: "OracleGlobal"),
            S("assembly + global-namespace class", Asm, cls: "OracleGlobal"),
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

        // The fixture tests take milliseconds: a run silent for this long, or a teardown or a test tree that takes longer
        // than tests-run itself allows, stops the oracle (exit 2) rather than letting a late result land on another run.
        const double ScenarioTimeoutSeconds = 45;
        const int IdleTicksBeforeRun = 3;

        // State survives a domain reload (SessionState); a reload drops the statics below. Results holds one entry per
        // finished scenario, so its length is the next scenario's index.
        const string Key = "FilterOracle.";
        static readonly SessionStateBool Started = new SessionStateBool(Key + "started");
        static readonly SessionStateString Predicted = new SessionStateString(Key + "predicted");
        static readonly SessionStateString Results = new SessionStateString(Key + "results");
        static readonly SessionStateString OutPath = new SessionStateString(Key + "out");
        static readonly SessionStateString Scenes = new SessionStateString(Key + "scenes");
        static readonly SessionStateBool Running = new SessionStateBool(Key + "running");
        static readonly SessionStateInt Ran = new SessionStateInt(Key + "ran");
        static readonly SessionStateInt BootstrapScenesAtStart = new SessionStateInt(Key + "bootstrapScenes");
        static readonly SessionStateBool PlayModeOptionsSaved = new SessionStateBool(Key + "optionsSaved");
        static readonly SessionStateBool PlayModeOptionsEnabled = new SessionStateBool(Key + "optionsEnabled");
        static readonly SessionStateInt PlayModeOptions = new SessionStateInt(Key + "options");

        static bool Predicting => Predicted.Value.Length == 0;
        static List<string> ResultList => Results.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        static TestRunnerApi? api;
        static double lastProgress;
        static int idleTicks;

        public static void Run()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                var outIndex = Array.IndexOf(args, "-filterOracleOut");
                OutPath.Value = Path.GetFullPath(outIndex >= 0 && outIndex + 1 < args.Length ? args[outIndex + 1] : "Logs/filter-oracle.md");
                Predicted.Value = "";
                Results.Value = "";
                Running.Value = false;
                Scenes.Value = Tool_Tests.OpenScenePaths();
                BootstrapScenesAtStart.Value = BootstrapScenes();
                DisablePlayModeReload();
                Started.Value = true;
                Attach();
                RetrievePredictions();
            }
            catch (Exception e) { Stop("the oracle failed: " + e.Message, e); }
        }

        [InitializeOnLoadMethod]
        static void ResumeAfterDomainReload()
        {
            if (!Started.Value)
                return;
            Attach();
            if (Predicting)
                RetrievePredictions(); // the reload dropped the pending callback
        }

        static void Attach()
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks());
            lastProgress = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void RetrievePredictions()
        {
            var testRunner = api!;
            testRunner.RetrieveTestList(TestMode.EditMode, edit => testRunner.RetrieveTestList(TestMode.PlayMode, play =>
            {
                try
                {
                    if (!Leaves(edit).Any(t => t.FullName.StartsWith(Ns + ".")) || !Leaves(play).Any(t => t.FullName.StartsWith(PlayNs + ".")))
                        throw new InvalidOperationException("the oracle's fixture tests are not in Unity's test tree");
                    Predicted.Value = string.Join(",", Scenarios.Select(s =>
                        TestFilterMatch.Count(s.Mode == TestMode.PlayMode ? play : edit, CreateFilter(s))));
                }
                catch (Exception e) { Stop("the oracle failed: " + e.Message, e); }
            }));
        }

        // One loop: start the next run only once Unity has fully left the previous one.
        static void Tick()
        {
            if (!Started.Value)
                return;
            var silentFor = EditorApplication.timeSinceStartup - lastProgress;
            if (Predicting)
            {
                if (silentFor > Tool_Tests.TestListTimeout.TotalSeconds)
                    Stop($"Unity's test tree was not retrieved in {Tool_Tests.TestListTimeout.TotalSeconds:0} s");
                return;
            }
            var index = ResultList.Count;
            if (Running.Value)
            {
                if (silentFor > ScenarioTimeoutSeconds)
                    Stop($"{Scenarios[index].Mode} {Scenarios[index].Name}: Unity reported nothing for {ScenarioTimeoutSeconds:0} s");
                return;
            }
            if (!IsIdle())
            {
                idleTicks = 0;
                if (silentFor > Tool_Tests.TeardownTimeout.TotalSeconds)
                    Stop($"Unity did not finish a run's teardown in {Tool_Tests.TeardownTimeout.TotalSeconds:0} s");
                return;
            }
            if (++idleTicks < IdleTicksBeforeRun)
                return;
            idleTicks = 0;
            if (index >= Scenarios.Length)
            {
                Finish(null);
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
                if (!Running.Value)
                    return;
                Running.Value = false;
                lastProgress = EditorApplication.timeSinceStartup;
                var index = ResultList.Count;
                Results.Value += Ran.Value + ",";
                Debug.Log($"[FilterOracle] {Scenarios[index].Mode} {Scenarios[index].Name}: Unity ran {Ran.Value}, tests-run counted {Predicted.Value.Split(',')[index]}");
            }
        }

        static void Stop(string reason, Exception? error = null)
        {
            Debug.LogError("[FilterOracle] " + (error?.ToString() ?? reason));
            Finish(reason);
        }

        // Writes the report and exits: 0 when every count matches, 3 on a mismatch, 2 when the oracle stopped early.
        static void Finish(string? stopped)
        {
            var exitCode = 2;
            try
            {
                Started.Value = false;
                EditorApplication.update -= Tick;
                RestorePlayModeReload();
                var predicted = Predicted.Value.Split(',');
                var ran = ResultList;
                var matched = Enumerable.Range(0, ran.Count).Count(i => ran[i] == predicted[i]);
                var lines = new List<string>
                {
                    $"## tests-run filter oracle: Unity {Application.unityVersion}",
                    "",
                    $"{matched} of {Scenarios.Length} scenarios: tests-run counts exactly the tests Unity runs" +
                        (ran.Count > matched ? $"; **{ran.Count - matched} mismatched**" : "") + ".",
                };
                if (stopped != null)
                    lines.Add($"**Stopped after {ran.Count} scenarios:** {stopped}");
                lines.Add("");
                lines.Add("| Scenario | Filter | Unity ran | tests-run counted | |");
                lines.Add("|---|---|---|---|---|");
                for (var i = 0; i < ran.Count; i++)
                {
                    var s = Scenarios[i];
                    lines.Add($"| {s.Mode} {s.Name} | {Describe(s)} | {ran[i]} | {predicted[i]} | {(ran[i] == predicted[i] ? "ok" : "**MISMATCH**")} |");
                }
                File.WriteAllLines(OutPath.Value, lines);
                Debug.Log($"[FilterOracle] {matched}/{Scenarios.Length} match{(stopped != null ? ", stopped: " + stopped : "")}; report: {OutPath.Value}");
                exitCode = stopped != null ? 2 : ran.Count > matched ? 3 : 0;
            }
            catch (Exception e)
            {
                Debug.LogError("[FilterOracle] the report was not written: " + e);
            }
            finally
            {
                Predicted.Value = "";
                EditorApplication.Exit(exitCode);
            }
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
            PlayModeOptionsSaved.Value = true;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
        }

        static void RestorePlayModeReload()
        {
            if (!PlayModeOptionsSaved.Value)
                return;
            EditorSettings.enterPlayModeOptionsEnabled = PlayModeOptionsEnabled.Value;
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)PlayModeOptions.Value;
            PlayModeOptionsSaved.Value = false;
        }

        static IEnumerable<ITestAdaptor> Leaves(ITestAdaptor node)
            => !node.IsSuite ? new[] { node }
                : node.HasChildren ? node.Children.SelectMany(Leaves)
                : Enumerable.Empty<ITestAdaptor>();
    }
}
