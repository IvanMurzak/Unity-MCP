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
    /// How many tests of Unity's test tree a <see cref="Filter"/> selects, decided as com.unity.test-framework decides it
    /// (RuntimeTestRunnerFilter.BuildNUnitFilter): testNames match the full name exactly, groupNames as a regex,
    /// assemblyNames the test's assembly ignoring case; populated fields are ANDed, values of one field ORed, and a value
    /// starting with '!' excludes. Like NUnit's TestFilter.Pass, a test passes a field when it or one of its ancestors
    /// (parameterized method, fixture, namespace) matches.
    /// </summary>
    static class TestFilterMatch
    {
        public static int Count(ITestAdaptor root, Filter filter)
        {
            var fields = new[]
            {
                Field.Of(filter.testNames, name => node => node.FullName == name),
                Field.Of(filter.groupNames, pattern =>
                {
                    var regex = new Regex(pattern);
                    return node => regex.IsMatch(node.FullName ?? string.Empty);
                }),
                Field.Of(filter.assemblyNames, assembly => node => string.Equals(AssemblyName(node), assembly, StringComparison.OrdinalIgnoreCase)),
            }.OfType<Field>().ToArray();
            return Count(root, fields, new bool[fields.Length]);
        }

        // One walk from the root: each node is matched once per field, and a match carries down to the node's tests.
        static int Count(ITestAdaptor node, Field[] fields, bool[] includedAbove)
        {
            var included = new bool[fields.Length];
            for (var i = 0; i < fields.Length; i++)
            {
                if (fields[i].Excludes(node))
                    return 0;
                included[i] = includedAbove[i] || fields[i].Includes(node);
            }
            if (!node.IsSuite)
                return included.All(passed => passed) ? 1 : 0;
            return node.HasChildren ? node.Children.Sum(child => Count(child, fields, included)) : 0;
        }

        sealed class Field
        {
            readonly List<Func<ITestAdaptor, bool>> include;
            readonly List<Func<ITestAdaptor, bool>> exclude;

            Field(List<Func<ITestAdaptor, bool>> include, List<Func<ITestAdaptor, bool>> exclude)
            {
                this.include = include;
                this.exclude = exclude;
            }

            public static Field? Of(string[]? values, Func<string, Func<ITestAdaptor, bool>> matcher)
                => values == null || values.Length == 0 ? null : new Field(
                    values.Where(v => !v.StartsWith("!")).Select(matcher).ToList(),
                    values.Where(v => v.StartsWith("!")).Select(v => matcher(v.Substring(1))).ToList());

            public bool Includes(ITestAdaptor node) => include.Count == 0 || include.Any(matches => matches(node));
            public bool Excludes(ITestAdaptor node) => exclude.Any(matches => matches(node));
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
    }
}
