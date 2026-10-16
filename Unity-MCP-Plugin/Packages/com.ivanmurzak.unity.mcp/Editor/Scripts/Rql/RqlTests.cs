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
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using Rql;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.TestTools.TestRunner.Api;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    /// <summary>
    /// The <c>tests</c> source: every NUnit / Unity test method in the loaded assemblies, found through TypeCache (instant,
    /// synchronous, no test-tree build), e.g. <c>tests | where fullName contains "Spline" and mode == "EditMode"</c>.
    /// tests-run takes such a query and runs the selection as ONE Test Runner run.
    /// </summary>
    public static class RqlTests
    {
        // The test attributes and whether they mark a parameterized test. Found by name: the Editor assembly has no
        // nunit.framework reference (overrideReferences).
        static readonly (string type, bool parameterized)[] TestAttributes =
        {
            ("NUnit.Framework.TestAttribute, nunit.framework", false),
            ("NUnit.Framework.TestCaseAttribute, nunit.framework", true),
            ("NUnit.Framework.TestCaseSourceAttribute, nunit.framework", true),
            ("NUnit.Framework.TheoryAttribute, nunit.framework", true),
            ("UnityEngine.TestTools.UnityTestAttribute, UnityEngine.TestRunner", false),
        };

        static Type? NUnit(string name) => Type.GetType($"NUnit.Framework.{name}Attribute, nunit.framework");

        // TypeCache and the compiled assemblies cannot change within a domain (statics reset on reload).
        static List<RqlTest>? _all;
        static Dictionary<string, RqlTest>? _byName;

        /// <summary>The <c>tests</c> source.</summary>
        public static List<RqlTest> All() => new List<RqlTest>(_all ??= Discover());

        /// <summary>Every test by its full name.</summary>
        internal static IReadOnlyDictionary<string, RqlTest> ByName =>
            _byName ??= (_all ??= Discover()).ToDictionary(t => t.fullName, StringComparer.Ordinal);

        /// <summary>The <c>testResults</c> source: the results of the last tests-run (empty before the first one).</summary>
        public static List<RqlTestResult> LastResults() => new List<RqlTestResult>(_lastResults ??= LoadLastResults());

        // Kept in SessionState as well: the results outlive the domain reloads of later compiles and Play Mode entries.
        const string LastResultsKey = "Unity_MCP_Rql_TestResults";
        static List<RqlTestResult>? _lastResults;

        internal static void SetLastResults(IEnumerable<RqlTestResult> results)
        {
            _lastResults = results.ToList();
            var json = new JsonArray(_lastResults.Select(r => (JsonNode)new JsonObject
            {
                ["name"] = r.name,
                ["status"] = r.status,
                ["durationMs"] = r.durationMs,
                ["message"] = r.message,
                ["stackTrace"] = r.stackTrace,
            }).ToArray());
            SessionState.SetString(LastResultsKey, json.ToJsonString());
        }

        static List<RqlTestResult> LoadLastResults()
        {
            var raw = SessionState.GetString(LastResultsKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return new List<RqlTestResult>();
            try
            {
                return (JsonNode.Parse(raw) as JsonArray ?? new JsonArray()).OfType<JsonObject>().Select(o => new RqlTestResult
                {
                    name = (string?)o["name"] ?? "",
                    status = (string?)o["status"] ?? "",
                    durationMs = (double?)o["durationMs"] ?? 0,
                    message = (string?)o["message"],
                    stackTrace = (string?)o["stackTrace"],
                }).ToList();
            }
            catch (Exception)
            {
                return new List<RqlTestResult>();
            }
        }

        static List<RqlTest> Discover()
        {
            var playerAssemblies = new HashSet<string>(
                CompilationPipeline.GetAssemblies(AssembliesType.Player).Select(a => a.name), StringComparer.Ordinal);
            var category = NUnit("Category");
            var categoryName = category?.GetProperty("Name");
            var explicitAttr = NUnit("Explicit");
            var ignoreAttr = NUnit("Ignore");

            List<string> Categories(MemberInfo m) => category == null || categoryName == null
                ? new List<string>()
                : m.GetCustomAttributes(category, inherit: true).Select(a => categoryName.GetValue(a) as string).Where(c => !string.IsNullOrEmpty(c)).ToList()!;
            bool Has(MemberInfo m, Type? attribute) => attribute != null && m.IsDefined(attribute, true);

            // Per fixture type, computed once: the concrete fixtures a declaring type's tests run in (an abstract base's tests
            // run once per concrete subclass, named after it, as NUnit does) and the fixture-level attributes.
            var fixturesOf = new Dictionary<Type, Type[]>();
            var fixtureInfo = new Dictionary<Type, (List<string> categories, bool @explicit, bool ignored)>();

            var tests = new Dictionary<string, RqlTest>(StringComparer.Ordinal);
            foreach (var (typeName, parameterized) in TestAttributes)
            {
                var attribute = Type.GetType(typeName);
                if (attribute == null) continue;
                foreach (var method in TypeCache.GetMethodsWithAttribute(attribute))
                {
                    var declaring = method.DeclaringType;
                    if (declaring == null) continue;
                    // A generic base's tests still run in its closed subclasses; static classes (abstract + sealed) are fixtures.
                    if (!fixturesOf.TryGetValue(declaring, out var fixtures))
                        fixturesOf[declaring] = fixtures = TypeCache.GetTypesDerivedFrom(declaring).Prepend(declaring)
                            .Where(t => (!t.IsAbstract || t.IsSealed) && !t.ContainsGenericParameters).ToArray();
                    foreach (var fixture in fixtures)
                    {
                        var fullName = (fixture.FullName ?? fixture.Name) + "." + method.Name;
                        if (tests.TryGetValue(fullName, out var known))
                        {
                            known.parameterized |= parameterized;
                            continue;
                        }
                        if (!fixtureInfo.TryGetValue(fixture, out var info))
                            fixtureInfo[fixture] = info = (Categories(fixture), Has(fixture, explicitAttr), Has(fixture, ignoreAttr));
                        var assembly = fixture.Assembly.GetName().Name ?? "";
                        tests[fullName] = new RqlTest
                        {
                            fullName = fullName,
                            name = method.Name,
                            fixture = fixture.Name,
                            @namespace = fixture.Namespace ?? "",
                            assembly = assembly,
                            mode = playerAssemblies.Contains(assembly) ? nameof(TestMode.PlayMode) : nameof(TestMode.EditMode),
                            categories = Categories(method).Concat(info.categories).Distinct().ToList(),
                            parameterized = parameterized,
                            @explicit = info.@explicit || Has(method, explicitAttr),
                            ignored = info.ignored || Has(method, ignoreAttr),
                        };
                    }
                }
            }
            if (tests.Count == 0 && TestAttributes.All(a => Type.GetType(a.type) == null))
                UnityEngine.Debug.LogWarning("[RQL] tests: no NUnit / Unity test attribute type resolved; the source is empty");
            return tests.Values.OrderBy(t => t.fullName, StringComparer.Ordinal).ToList();
        }
    }

    [RqlDescription("a test method (parameterized: its cases run together)")]
    public sealed class RqlTest
    {
        [RqlDescription("Namespace.Fixture.Method, the name tests-run selects by")]
        public string fullName { get; internal set; } = "";
        public string name { get; internal set; } = "";
        [RqlDescription("the test class")]
        public string fixture { get; internal set; } = "";
        public string @namespace { get; internal set; } = "";
        public string assembly { get; internal set; } = "";
        [RqlDescription("EditMode or PlayMode")]
        public string mode { get; internal set; } = nameof(TestMode.EditMode);
        public List<string> categories { get; internal set; } = new List<string>();
        [RqlDescription("TestCase / TestCaseSource / Theory: several cases")]
        public bool parameterized { get; internal set; }
        [RqlDescription("[Explicit]: runs only when selected by name")]
        public bool @explicit { get; internal set; }
        [RqlDescription("[Ignore]")]
        public bool ignored { get; internal set; }
    }

    [RqlDescription("one test result of the last tests-run")]
    public sealed class RqlTestResult
    {
        [RqlDescription("the test's full name (cases include their arguments)")]
        public string name { get; internal set; } = "";
        [RqlDescription("Passed, Failed or Skipped")]
        public string status { get; internal set; } = "";
        public double durationMs { get; internal set; }
        public string? message { get; internal set; }
        public string? stackTrace { get; internal set; }
    }
}
