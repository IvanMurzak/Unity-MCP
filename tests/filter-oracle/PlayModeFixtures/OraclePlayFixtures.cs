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
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

// PlayMode tests the filter oracle selects: Unity builds the PlayMode tree separately.
namespace FilterOracle.Play
{
    public class OraclePlay
    {
        [Test] public void A() { }
        [UnityTest] public IEnumerator B() { yield return null; }
        [TestCase(1)] [TestCase(2)] public void Param(int x) { }
    }
}
