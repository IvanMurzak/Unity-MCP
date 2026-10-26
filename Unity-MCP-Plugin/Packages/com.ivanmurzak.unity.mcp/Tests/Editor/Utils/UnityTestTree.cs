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
using System.Collections;
using NUnit.Framework;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>Unity's own EditMode test tree, for tests that check what a filter or selection hits in it.</summary>
    public static class UnityTestTree
    {
        /// <summary>Yield (from a [UnityTest]) until the tree is retrieved, then pass it to <paramref name="use"/>.</summary>
        public static IEnumerator EditMode(Action<ITestAdaptor> use)
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            try
            {
                ITestAdaptor? root = null;
                api.RetrieveTestList(TestMode.EditMode, r => root = r);
                var deadline = DateTime.UtcNow.AddSeconds(60);
                while (root == null && DateTime.UtcNow < deadline)
                    yield return null;
                Assert.IsNotNull(root, "Unity's test tree was not retrieved");
                use(root!);
            }
            finally
            {
                Object.DestroyImmediate(api);
            }
        }
    }
}
