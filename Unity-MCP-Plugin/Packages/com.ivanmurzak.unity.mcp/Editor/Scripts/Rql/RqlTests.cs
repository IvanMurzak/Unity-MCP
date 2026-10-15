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
using System.Text.RegularExpressions;
using Rql;
using UnityEditor;
using UnityEditor.Compilation;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    /// <summary>
    /// The <c>tests</c> source: every NUnit / Unity test method in the loaded assemblies, found through TypeCache (instant,
    /// synchronous, no test-tree build), e.g. <c>tests | where fullName contains "Spline" and mode == "EditMode"</c>.
    /// tests-run takes such a query and runs the selection as ONE Test Runner run.
    /// </summary>
    public static class RqlTests
    {
        static readonly string[] TestAttributeNames =
        {
            "NUnit.Framework.TestAttribute, nunit.framework",
            "NUnit.Framework.TestCaseAttribute, nunit.framework",
            "NUnit.Framework.TestCaseSourceAttribute, nunit.framework",
            "NUnit.Framework.TheoryAttribute, nunit.framework",
            "UnityEngine.TestTools.UnityTestAttribute, UnityEngine.TestRunner",
        };

        // TypeCache and the compiled assemblies cannot change within a domain (statics reset on reload).
        static List<RqlTest>? _all;

        /// <summary>The <c>tests</c> source.</summary>
        public static List<RqlTest> All() => new List<RqlTest>(_all ??= Discover());

        /// <summary>The <c>testResults</c> source: the results of the last tests-run (empty before the first one).</summary>
        public static List<RqlTestResult> LastResults() => new List<RqlTestResult>(_lastResults);

        static List<RqlTestResult> _lastResults = new List<RqlTestResult>();

        internal static void SetLastResults(IEnumerable<RqlTestResult> results) => _lastResults = results.ToList();

        /// <summary>
        /// Unity Test Runner <c>groupNames</c> patterns that select exactly these tests (parameterized cases included):
        /// <c>^Namespace\.Fixture\.Method(\(…\))?$</c>.
        /// </summary>
        public static string[] GroupPatterns(IEnumerable<string> fullNames) =>
            fullNames.Distinct(StringComparer.Ordinal).Select(n => "^" + Regex.Escape(n) + @"(\(.*\))?$").ToArray();

        static List<RqlTest> Discover()
        {
            var playerAssemblies = new HashSet<string>(
                CompilationPipeline.GetAssemblies(AssembliesType.Player).Select(a => a.name), StringComparer.Ordinal);
            var category = Type.GetType("NUnit.Framework.CategoryAttribute, nunit.framework");
            var categoryName = category?.GetProperty("Name");
            var explicitAttr = Type.GetType("NUnit.Framework.ExplicitAttribute, nunit.framework");
            var ignoreAttr = Type.GetType("NUnit.Framework.IgnoreAttribute, nunit.framework");
            var parameterized = new HashSet<Type?>
            {
                Type.GetType("NUnit.Framework.TestCaseAttribute, nunit.framework"),
                Type.GetType("NUnit.Framework.TestCaseSourceAttribute, nunit.framework"),
                Type.GetType("NUnit.Framework.TheoryAttribute, nunit.framework"),
            };

            IEnumerable<string> Categories(MemberInfo m) => category == null || categoryName == null
                ? Enumerable.Empty<string>()
                : m.GetCustomAttributes(category, inherit: true).Select(a => categoryName.GetValue(a) as string).Where(s => !string.IsNullOrEmpty(s))!;

            var tests = new Dictionary<string, RqlTest>(StringComparer.Ordinal);
            foreach (var attributeName in TestAttributeNames)
            {
                var attribute = Type.GetType(attributeName);
                if (attribute == null) continue;
                foreach (var method in TypeCache.GetMethodsWithAttribute(attribute))
                {
                    var declaring = method.DeclaringType;
                    if (declaring == null || declaring.ContainsGenericParameters) continue;
                    // A test declared in an abstract base runs once per concrete fixture deriving from it.
                    var fixtures = declaring.IsAbstract
                        ? TypeCache.GetTypesDerivedFrom(declaring).Where(t => !t.IsAbstract && !t.ContainsGenericParameters)
                        : new[] { declaring }.Concat(TypeCache.GetTypesDerivedFrom(declaring).Where(t => !t.IsAbstract && !t.ContainsGenericParameters));
                    foreach (var fixture in fixtures)
                    {
                        var fullName = (fixture.FullName ?? fixture.Name) + "." + method.Name;
                        if (tests.TryGetValue(fullName, out var known))
                        {
                            if (parameterized.Contains(attribute)) known.parameterized = true;
                            continue;
                        }
                        var assembly = fixture.Assembly.GetName().Name ?? "";
                        tests[fullName] = new RqlTest
                        {
                            fullName = fullName,
                            name = method.Name,
                            fixture = fixture.Name,
                            @namespace = fixture.Namespace ?? "",
                            assembly = assembly,
                            mode = playerAssemblies.Contains(assembly) ? "PlayMode" : "EditMode",
                            categories = Categories(method).Concat(Categories(fixture)).Distinct().ToList(),
                            parameterized = parameterized.Contains(attribute),
                            @explicit = explicitAttr != null && (method.IsDefined(explicitAttr, true) || fixture.IsDefined(explicitAttr, true)),
                            ignored = ignoreAttr != null && (method.IsDefined(ignoreAttr, true) || fixture.IsDefined(ignoreAttr, true)),
                        };
                    }
                }
            }
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
        public string mode { get; internal set; } = "EditMode";
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
