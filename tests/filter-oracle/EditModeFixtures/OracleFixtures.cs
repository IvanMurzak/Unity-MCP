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

// EditMode tests the filter oracle selects with tests-run filters. Each shape is one Unity filter rule:
// parameterized methods and fixtures, generic and nested classes, [Explicit], [Ignore], [UnityTest],
// duplicate case names, a namespace segment named like a class, a namespace prefix decoy and a
// global-namespace test (its only ancestor named like a namespace is the assembly itself).

namespace FilterOracle.Data
{
    public class OracleParam
    {
        [TestCase(1)] [TestCase(2)] public void Method(int x) { }
        [Test] public void Plain() { }
    }

    public class OracleOther { [Test] public void Plain2() { } }

    [TestFixture(1)] [TestFixture(2)]
    public class OracleArgs
    {
        public OracleArgs(int a) { }
        [Test] public void Test() { }
    }

    [TestFixture(typeof(int))]
    public class OracleGeneric<T> { [Test] public void Test() { } }

    public class OracleExplicit
    {
        [Test, Explicit] public void ExplicitTest() { }
        [Test] public void Normal() { }
    }

    public class OracleIgnored { [Test, Ignore("filter oracle")] public void Ignored() { } }

    public class OracleOuter { public class Inner { [Test] public void T() { } } }

    public class OracleCoroutine { [UnityTest] public IEnumerator Co() { yield return null; } }

    public class OracleDup { [TestCase(1)] [TestCase(1)] public void Same(int x) { } }
}

namespace FilterOracle.Data.Sub
{
    public class OracleLeaf { [Test] public void InSub() { } }
}

namespace FilterOracle.Data.Widget
{
    public class Thing { [Test] public void T() { } }
}

namespace FilterOracle.DataX
{
    public class OracleDecoy { [Test] public void D() { } }
}

public class OracleGlobal { [Test] public void G() { } }
