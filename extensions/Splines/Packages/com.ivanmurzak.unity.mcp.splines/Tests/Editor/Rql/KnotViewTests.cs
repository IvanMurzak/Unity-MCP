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
using Rql;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Splines
{
    public class KnotViewTests : SplinesTestBase
    {
        private const string K = R + " | get splines | get knots";

        [Test]
        public void KnotsAreReadable()
        {
            var r = Ok(Q(K + " | select index position tangentMode"));
            Assert.AreEqual(3, r.Rows!.Count);
            AreClose(new Vector3(5, 0, 0), Vec(r.Rows[1][1]!.GetValue<string>()));
            Assert.AreEqual("AutoSmooth", r.Rows[1][2]!.GetValue<string>());
            Assert.AreEqual(1L, Count(K + " | where position.x > 4 and position.x < 6 | count"));
            Assert.AreEqual(1L, Count(K + " | where position == \"(10, 0, 0)\" | count"));
        }

        [Test]
        public void PositionIsWrittenBackToTheSpline()
        {
            Ok(M(R + " | set splines[0].knots[2].position = \"(10, 1, 0)\""));
            AreClose(new Vector3(10, 1, 0), Road.Spline[2].Position);
            Ok(M(K + " | where index == 0 | set position.y = 3"));
            AreClose(new Vector3(0, 3, 0), Road.Spline[0].Position);
        }

        [Test]
        public void WorldPositionUsesTheContainerTransform()
        {
            Road.transform.position = new Vector3(0, 0, 100);
            AreClose(new Vector3(5, 0, 100), Vec(Cell(Q(R + " | select splines[0].knots[1].worldPosition"))));
            Ok(M(R + " | set splines[0].knots[1].worldPosition = \"(5, 0, 102)\""));
            AreClose(new Vector3(5, 0, 2), Road.Spline[1].Position);
        }

        [Test]
        public void TangentModeIsWritableByName()
        {
            Ok(M(R + " | set splines[0].knots[0].tangentMode = \"Linear\""));
            Assert.AreEqual(TangentMode.Linear, Road.Spline.GetTangentMode(0));
            Ok(M(K + " | set tangentMode = \"Broken\""));
            for (int i = 0; i < 3; i++) Assert.AreEqual(TangentMode.Broken, Road.Spline.GetTangentMode(i));
            var bad = M(K + " | set tangentMode = \"Wobbly\"");
            Assert.IsFalse(bad.Ok);
        }

        [Test]
        public void ExplicitTangentsStickOnComputedKnots()
        {
            Ok(M(R + " | set splines[0].knots[1].tangentOut = \"(0, 0, 4)\", splines[0].knots[1].tangentIn = \"(0, 0, -2)\""));
            Assert.AreEqual(TangentMode.Broken, Road.Spline.GetTangentMode(1));
            AreClose(new Vector3(0, 0, 4), Road.Spline[1].TangentOut);
            AreClose(new Vector3(0, 0, -2), Road.Spline[1].TangentIn);
        }

        [Test]
        public void RotationAndTension()
        {
            Ok(M(R + " | set splines[0].knots[0].eulerAngles = \"(0, 90, 0)\", splines[0].knots[1].tension = 0.5"));
            AreClose(new Vector3(0, 90, 0), ((Quaternion)Road.Spline[0].Rotation).eulerAngles, 1e-3f);
            Assert.AreEqual(0.5f, Road.Spline.GetAutoSmoothTension(1), 1e-6f);
        }

        [Test]
        public void KnotWritesAreUndoable()
        {
            Undo.ClearAll();
            Ok(M(R + " | set splines[0].knots[2].position = \"(10, 5, 0)\", splines[0].knots[0].tangentMode = \"Linear\""));
            Undo.PerformUndo();
            AreClose(new Vector3(10, 0, 0), Road.Spline[2].Position);
            Assert.AreEqual(TangentMode.AutoSmooth, Road.Spline.GetTangentMode(0));
        }

        [Test]
        public void DryRunChangesNothing()
        {
            var r = DryRun(R + " | set splines[0].knots[2].position = \"(10, 5, 0)\"");
            Assert.IsTrue(r.Ok, Table(r));
            AreClose(new Vector3(10, 0, 0), Road.Spline[2].Position);
        }

        private static QueryResult DryRun(string rql) =>
            RqlUnity.Engine.Execute(rql, null, new QueryOptions { AllowMutation = true, DryRun = true });
    }
}
