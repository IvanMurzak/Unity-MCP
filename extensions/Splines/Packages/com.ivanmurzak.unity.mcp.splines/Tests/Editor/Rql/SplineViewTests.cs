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
using NUnit.Framework;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Splines
{
    public class SplineViewTests : SplinesTestBase
    {
        [Test]
        public void ReadsCountLengthAndClosed()
        {
            var r = Ok(Q(R + " | get splines | select index knotCount length closed"));
            Assert.AreEqual("0", r.Rows![0][0]!.ToJsonString());
            Assert.AreEqual("3", r.Rows[0][1]!.ToJsonString());
            Assert.AreEqual(Road.CalculateLength(0), double.Parse(r.Rows[0][2]!.ToJsonString(), System.Globalization.CultureInfo.InvariantCulture), 1e-3);
            Assert.AreEqual("false", r.Rows[0][3]!.ToJsonString());
        }

        [Test]
        public void ClosedIsWritableAndUndoable()
        {
            Undo.ClearAll();
            var r = Ok(M(R + " | set splines[0].closed = true"));
            StringAssert.Contains("splines[0].closed", Table(r));
            Assert.IsTrue(Road.Spline.Closed);
            Undo.PerformUndo();
            Assert.IsFalse(Road.Spline.Closed);
        }

        [Test]
        public void BulkWritesThroughGet()
        {
            Ok(M(R + " | call AddSpline()"));
            Ok(M(C + " | get splines | set closed = true"));
            Assert.IsTrue(Road.Splines[0].Closed);
            Assert.IsTrue(Road.Splines[1].Closed);
        }

        [Test]
        public void ReadOnlyMembersRejectWrites()
        {
            var bad = M(R + " | set splines[0].knotCount = 1");
            Assert.IsFalse(bad.Ok);
            StringAssert.Contains("readonly-member", Table(bad));
            Assert.AreEqual(3, Road.Spline.Count);
        }

        [Test]
        public void TheViewIsDescribed()
        {
            var schema = Table(Ok(Q(R + " | get splines | describe")));
            StringAssert.Contains("closed", schema);
            StringAssert.Contains("knots", schema);
        }
    }
}
