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
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class DemoTest
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Debug.Log($"[{nameof(DemoTest)}] SetUp");
            yield return null;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Debug.Log($"[{nameof(DemoTest)}] TearDown");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Always_Valid_Test()
        {
            Assert.IsTrue(true, "This test is a placeholder and should be replaced with actual test logic.");
            yield return null;
        }
    }
}
