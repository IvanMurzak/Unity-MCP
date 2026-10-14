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
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Adapters
{
    public class UnityEngine_Scene_AdapterTests : AdapterTestBase
    {
        [Test]
        public void ScenesExposeSafeMembersAndTheirRoots()
        {
            new GameObject("RootA");
            new GameObject("RootB");
            Assert.AreEqual("true", Cell(Q("scenes | select isLoaded")));
            Assert.AreEqual("2", Cell(Q("scenes | select rootCount")));
            Assert.AreEqual("2", Cell(Q("scenes | select roots.count()")));
            Assert.AreEqual("RootA", Cell(Q("scenes | get roots | where name == \"RootA\" | select name")));
        }

        [Test]
        public void BoundsIntShowsMinMaxSizeOnly()
        {
            var schema = Schema("BoundsInt");
            StringAssert.Contains("min", schema);
            StringAssert.DoesNotContain("allPositionsWithin", schema);
        }

        [Test]
        public void OnlyWhitelistedMembersAreVisible()
        {
            var schema = Schema("Scene");
            StringAssert.Contains("rootCount", schema);
            StringAssert.DoesNotContain("handle", schema);
            Assert.IsTrue(Warns(Q("scenes | select handle"), "unknown-member"));
        }
    }
}
