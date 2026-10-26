/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.TestTools.TestRunner.Api;

namespace com.IvanMurzak.Unity.MCP.Editor.API.TestRunner
{
    /// <summary>
    /// Which tests of Unity's test tree a <see cref="Filter"/> selects, decided as com.unity.test-framework decides it
    /// (RuntimeTestRunnerFilter.BuildNUnitFilter): testNames match the full name exactly, groupNames as a regex,
    /// assemblyNames the test's assembly ignoring case; populated fields are ANDed, values of one field ORed, and a value
    /// starting with '!' excludes. Like NUnit's TestFilter.Pass, a test passes a field when it or one of its ancestors
    /// (parameterized method, fixture, namespace) matches.
    /// </summary>
    static class TestFilterMatch
    {
        public static int Count(ITestAdaptor root, Filter filter)
            => Leaves(root).Count(test => Selects(filter, test));

        public static bool Selects(Filter filter, ITestAdaptor test)
        {
            var path = SelfAndAncestors(test).ToList();
            return Passes(path, filter.testNames, (node, name) => node.FullName == name)
                && Passes(path, filter.groupNames, (node, pattern) => Regex.IsMatch(node.FullName ?? string.Empty, pattern))
                && Passes(path, filter.assemblyNames, (node, assembly) => string.Equals(AssemblyName(node), assembly, StringComparison.OrdinalIgnoreCase));
        }

        static bool Passes(List<ITestAdaptor> path, string[]? values, Func<ITestAdaptor, string, bool> matches)
        {
            if (values == null || values.Length == 0)
                return true;
            var include = values.Where(v => !v.StartsWith("!")).ToList();
            var exclude = values.Where(v => v.StartsWith("!")).Select(v => v.Substring(1)).ToList();
            return (include.Count == 0 || path.Any(node => include.Any(v => matches(node, v))))
                && !path.Any(node => exclude.Any(v => matches(node, v)));
        }

        // Unity's AssemblyNameFilter reads TypeInfo; a tree without it still names the assembly in "Asm.dll/..." unique names.
        static string? AssemblyName(ITestAdaptor node)
        {
            var name = node.TypeInfo?.Assembly?.GetName().Name;
            if (name != null || node.IsSuite)
                return name;
            var dll = node.UniqueName?.IndexOf(".dll", StringComparison.OrdinalIgnoreCase) ?? -1;
            return dll > 0 ? node.UniqueName!.Substring(0, dll) : null;
        }

        static IEnumerable<ITestAdaptor> Leaves(ITestAdaptor node)
            => !node.IsSuite ? new[] { node }
                : node.HasChildren ? node.Children.SelectMany(Leaves)
                : Enumerable.Empty<ITestAdaptor>();

        static IEnumerable<ITestAdaptor> SelfAndAncestors(ITestAdaptor? node)
        {
            for (; node != null; node = node.Parent)
                yield return node;
        }
    }
}
