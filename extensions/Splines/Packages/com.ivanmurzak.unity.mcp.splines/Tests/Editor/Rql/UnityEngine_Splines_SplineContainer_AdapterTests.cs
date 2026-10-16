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
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Splines
{
    public class UnityEngine_Splines_SplineContainer_AdapterTests : SplinesTestBase
    {
        [Test]
        public void RawSplineStorageIsReplacedByTheSplinesViews()
        {
            var schema = RqlUnitySchema.Render(RqlUnity.Engine, "SplineContainer");
            StringAssert.Contains("splines", schema);
            StringAssert.DoesNotContain("m_Spline", schema);
            StringAssert.DoesNotContain("m_Knots", schema);
            StringAssert.Contains("AddKnot(", schema);
            StringAssert.Contains("Evaluate(", schema);
            Assert.AreEqual("1", Cell(Q(R + " | select n=(splines.count())")));
            Assert.AreEqual("3", Cell(Q(R + " | select splines[0].knotCount")));
        }

        [Test]
        public void AddKnotAppendsToSplineZeroOrToTheGivenSpline()
        {
            Assert.AreEqual("3", Result(Ok(M(R + " | call AddKnot(\"(15, 0, 3)\")"))));
            Assert.AreEqual(4, Road.Spline.Count);
            AreClose(new Vector3(15, 0, 3), Road.Spline[3].Position);
            Assert.AreEqual(TangentMode.AutoSmooth, Road.Spline.GetTangentMode(3));

            Assert.AreEqual("1", Result(Ok(M(R + " | call AddSpline()"))));
            Assert.AreEqual("0", Result(Ok(M(R + " | call AddKnot(1, [0, 2, 0])"))));
            Assert.AreEqual(1, Road.Splines[1].Count);
            AreClose(new Vector3(0, 2, 0), Road.Splines[1][0].Position);
        }

        [Test]
        public void AddKnotsAppendsAListOfPositions()
        {
            Assert.AreEqual("6", Result(Ok(M(R + " | call AddKnots([\"(15, 0, 0)\", \"(20, 0, 5)\", [25, 0, 0]])"))));
            AreClose(new Vector3(20, 0, 5), Road.Spline[4].Position);
        }

        [Test]
        public void InsertAndRemoveKnots()
        {
            Assert.AreEqual("1", Result(Ok(M(R + " | call InsertKnot(1, \"(2, 0, 1)\")"))));
            Assert.AreEqual(4, Road.Spline.Count);
            AreClose(new Vector3(2, 0, 1), Road.Spline[1].Position);
            Assert.AreEqual("3", Result(Ok(M(R + " | call RemoveKnot(0)"))));
            AreClose(new Vector3(2, 0, 1), Road.Spline[0].Position);
            // Inserting at the end (index == knotCount) appends.
            Assert.IsTrue(M(R + " | call InsertKnot(3, \"(20, 0, 0)\")").Ok);
            AreClose(new Vector3(20, 0, 0), Road.Spline[3].Position);
        }

        [Test]
        public void AddAndRemoveSplines()
        {
            Assert.AreEqual("1", Result(Ok(M(R + " | call AddSpline(true)"))));
            Assert.AreEqual(2, Road.Splines.Count);
            Assert.IsTrue(Road.Splines[1].Closed);
            Ok(M(R + " | call RemoveSpline(0)"));
            Assert.AreEqual(1, Road.Splines.Count);
            Assert.IsTrue(Road.Spline.Closed);
        }

        [Test]
        public void EvaluateReturnsAPointOnTheSpline()
        {
            Road.transform.position = new Vector3(0, 0, 100);
            var r = Ok(M(R + " | call Evaluate(0.5)"));
            var sample = r.Rows![0][2]!.AsObject();
            AreClose((Vector3)Road.EvaluatePosition(0.5f), Vec(sample["position"]!.GetValue<string>()), 1e-3f);
            AreClose(new Vector3(5, 0, 100), Vec(sample["position"]!.GetValue<string>()), 1e-2f);
            AreClose(new Vector3(5, 0, 0), Vec(sample["localPosition"]!.GetValue<string>()), 1e-2f);
            Assert.AreEqual(0.5, sample["t"]!.GetValue<double>(), 1e-6);
            Assert.IsTrue(M(R + " | call Evaluate(0, 0.5)").Ok, "the spline-index overload resolves");
        }

        [Test]
        public void OutOfRangeIndicesFailWithAReason()
        {
            var bad = M(R + " | call RemoveKnot(7)");
            Assert.IsFalse(bad.Ok);
            StringAssert.Contains("out of range", Table(bad));
            var badSpline = M(R + " | call AddKnot(3, \"(0, 0, 0)\")");
            Assert.IsFalse(badSpline.Ok);
            StringAssert.Contains("spline index 3", Table(badSpline));
            Assert.AreEqual(3, Road.Spline.Count);
        }

        [Test]
        public void KnotOperationsAreUndoable()
        {
            Undo.ClearAll();
            Ok(M(R + " | call AddKnot(\"(15, 0, 0)\")"));
            Assert.AreEqual(4, Road.Spline.Count);
            Undo.PerformUndo();
            Assert.AreEqual(3, Road.Spline.Count);
        }

        [Test]
        public void TheRqlToolsReachTheExtensionThroughTheMcpPipeline()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var args = new System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>
            {
                ["query"] = System.Text.Json.JsonDocument.Parse(
                    "\"objects | get components | where $ is SplineContainer | call AddKnot(\\\"(15, 0, 0)\\\")\"").RootElement.Clone(),
            };
            var response = UnityMcpPluginEditor.Instance.Tools!.RunCallTool(new com.IvanMurzak.McpPlugin.Common.Model.RequestCallTool("rql-mutate", args)).Result;
            Assert.AreEqual(com.IvanMurzak.McpPlugin.Common.Model.ResponseStatus.Success, response.Value!.Status);
            Assert.AreEqual(4, Road.Spline.Count);
        }
    }
}
