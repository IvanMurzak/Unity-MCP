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
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql
{
    public class SkillRqlTests
    {
        [Test]
        public void TheSkillTeachesTheLanguageAndListsLiveSources()
        {
            var md = Skill_Rql.Render();
            StringAssert.Contains("## Language", md);
            StringAssert.Contains("rql-mutate", md);
            StringAssert.Contains("| `objects` |", md);
            StringAssert.Contains("| `console` |", md);
            StringAssert.Contains("unity.assets(…)", md);
            StringAssert.Contains("UnityCoreModule", md);
        }
    }
}
