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

        public TestFilterParameters(string? testAssembly = null, string? testNamespace = null, string? testClass = null, string? testMethod = null)
        {
            TestAssembly = testAssembly;
            TestNamespace = testNamespace;
            TestClass = testClass;
            TestMethod = testMethod;
        }

        public bool HasAnyFilter =>
            !string.IsNullOrEmpty(TestAssembly) ||
            !string.IsNullOrEmpty(TestNamespace) ||
            !string.IsNullOrEmpty(TestClass) ||
            !string.IsNullOrEmpty(TestMethod);

        public override string ToString()
        {
            if (!HasAnyFilter)
                return "Test filter: all tests";

            var filters = new List<string>();
            if (!string.IsNullOrEmpty(TestAssembly)) filters.Add($"assembly '{TestAssembly}'");
            if (!string.IsNullOrEmpty(TestNamespace)) filters.Add($"namespace '{TestNamespace}'");
            if (!string.IsNullOrEmpty(TestClass)) filters.Add($"class '{TestClass}'");
            if (!string.IsNullOrEmpty(TestMethod)) filters.Add($"method '{TestMethod}'");

            return $"Test filter: {string.Join(", ", filters)}";
        }
    }
}
