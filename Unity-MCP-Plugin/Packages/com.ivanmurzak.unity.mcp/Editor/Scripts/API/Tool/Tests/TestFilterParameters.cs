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
using System.Collections.Generic;

namespace com.IvanMurzak.Unity.MCP.Editor.API.TestRunner
{
    public struct TestFilterParameters
    {
        public string? TestAssembly { get; set; }
        public string? TestNamespace { get; set; }
        public string? TestClass { get; set; }
        public string? TestMethod { get; set; }
        /// <summary>Anchored groupNames patterns selecting exact tests (from an RQL query over the <c>tests</c> source).</summary>
        public string[]? Groups { get; set; }
        /// <summary>The RQL selection's test names, checked against Unity's test tree before the run.</summary>
        public IReadOnlyList<string>? SelectedNames { get; set; }
        public bool HasGroups => Groups is { Length: > 0 };

        public TestFilterParameters(string? testAssembly = null, string? testNamespace = null, string? testClass = null, string? testMethod = null)
        {
            TestAssembly = testAssembly;
            TestNamespace = testNamespace;
            TestClass = testClass;
            TestMethod = testMethod;
            Groups = null;
            SelectedNames = null;
        }

        public bool HasAnyFilter =>
            !string.IsNullOrEmpty(TestAssembly) ||
            !string.IsNullOrEmpty(TestNamespace) ||
            !string.IsNullOrEmpty(TestClass) ||
            !string.IsNullOrEmpty(TestMethod) ||
            HasGroups;

        public override string ToString()
        {
            if (!HasAnyFilter)
                return "Test filter: all tests";

            var filters = new List<string>();
            if (!string.IsNullOrEmpty(TestAssembly)) filters.Add($"assembly '{TestAssembly}'");
            if (!string.IsNullOrEmpty(TestNamespace)) filters.Add($"namespace '{TestNamespace}'");
            if (!string.IsNullOrEmpty(TestClass)) filters.Add($"class '{TestClass}'");
            if (!string.IsNullOrEmpty(TestMethod)) filters.Add($"method '{TestMethod}'");
            if (SelectedNames != null) filters.Add($"{SelectedNames.Count} test(s) selected by RQL");
            else if (HasGroups) filters.Add("tests selected by RQL");

            return $"Test filter: {string.Join(", ", filters)}";
        }
    }
}
