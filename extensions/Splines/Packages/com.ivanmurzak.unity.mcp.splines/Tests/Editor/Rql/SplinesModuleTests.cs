/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Splines)        │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Linq;
using System.Text.RegularExpressions;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.Unity.MCP.Editor.Rql.Splines;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Splines;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Splines
{
    public class SplinesModuleTests : SplinesTestBase
    {
        [Test]
        public void TheModuleIsDiscovered()
        {
            var modules = RqlUnity.Engine.Modules.Select(m => m.Module).ToList();
            CollectionAssert.Contains(modules, typeof(SplinesModule).FullName);
            Assert.IsTrue(RqlUnity.Engine.Modules.All(m => m.Ok), string.Join("\n", RqlUnity.Engine.Modules));
        }

        [Test]
        public void TheModuleGuideIsPartOfTheRqlSkill()
        {
            var skill = global::com.IvanMurzak.Unity.MCP.Editor.API.Skill_Rql.Render();
            StringAssert.Contains("## Splines", skill);
            StringAssert.Contains("call AddKnot(", skill);
            StringAssert.Contains("splines | call CreateContainer(", skill);
        }

        [Test]
        public void TheSourceIsInTheSchema()
        {
            var schema = RqlUnitySchema.Render(RqlUnity.Engine, null);
            StringAssert.Contains("splines", schema);
            StringAssert.Contains("CreateContainer", RqlUnitySchema.Render(RqlUnity.Engine, "SplinesApi"));
            // View types are not UnityEngine.Objects, so rql-schema cannot look them up by name; describe lists them.
            var knot = Table(Ok(Q(R + " | get splines | get knots | first 1 | describe")));
            StringAssert.Contains("tangentMode", knot);
            StringAssert.Contains("worldPosition", knot);
        }

        /// <summary>Every example query of the guide runs, in order, against a scene that starts without containers.</summary>
        [Test]
        public void EveryGuideExampleRuns()
        {
            UnityEngine.Object.DestroyImmediate(Road.gameObject);
            var block = Regex.Match(SplinesGuide(), "```\\n(.*?)```", RegexOptions.Singleline).Groups[1].Value;
            var queries = block.Split('\n')
                .Select(l => Regex.Split(l, "\\s{3,}")[0].Trim())
                .Where(l => l.Length > 0)
                .ToList();
            Assert.GreaterOrEqual(queries.Count, 15);
            foreach (var q in queries)
            {
                var r = M(q);
                Assert.IsTrue(r.Ok, q + "\n" + Table(r));
            }

            var road = RqlUnity.AllGameObjects().First(go => go.name == "Road").GetComponent<SplineContainer>();
            Assert.AreEqual(1, road.Splines.Count);
            Assert.IsTrue(road.Spline.Closed);
            Assert.AreEqual(4, road.Spline.Count);
            Assert.AreEqual(TangentMode.AutoSmooth, road.Spline.GetTangentMode(0));
            var loop = RqlUnity.AllGameObjects().First(go => go.name == "Loop").GetComponent<SplineContainer>();
            Assert.IsTrue(loop.Spline.Closed);
        }

        private static string SplinesGuide()
        {
            var skill = global::com.IvanMurzak.Unity.MCP.Editor.API.Skill_Rql.Render();
            var start = skill.IndexOf("## Splines", StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0);
            return skill.Substring(start);
        }
    }
}
