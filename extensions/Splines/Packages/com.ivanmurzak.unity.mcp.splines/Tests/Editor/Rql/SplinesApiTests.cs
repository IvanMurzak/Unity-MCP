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
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Splines
{
    public class SplinesApiTests : SplinesTestBase
    {
        private static SplineContainer? Find(string name) =>
            RqlUnity.AllGameObjects().Where(go => go.name == name).Select(go => go.GetComponent<SplineContainer>()).FirstOrDefault(c => c != null);

        [Test]
        public void CreateContainerWithDefaults()
        {
            var r = Ok(M("splines | call CreateContainer()"));
            StringAssert.Contains("SplineContainer#", Table(r));
            var created = Find("Spline");
            Assert.IsNotNull(created);
            Assert.AreEqual(1, created!.Splines.Count);
            Assert.AreEqual(0, created.Spline.Count);
            Assert.IsFalse(created.Spline.Closed);
        }

        [Test]
        public void CreateContainerWithNamePositionRotationAndClosed()
        {
            Ok(M("splines | call CreateContainer(\"Loop\", \"(0, 0, 5)\", \"(0, 90, 0)\", true)"));
            var loop = Find("Loop")!;
            Assert.AreEqual(new Vector3(0, 0, 5), loop.transform.position);
            Assert.AreEqual(90f, loop.transform.eulerAngles.y, 1e-3f);
            Assert.IsTrue(loop.Spline.Closed);
        }

        [Test]
        public void CreateContainerUnderAParent()
        {
            var parent = new GameObject("Track");
            var parentRef = Cell(Q("objects | where name == \"Track\" | select $ref"));
            Ok(M($"splines | call CreateContainer(\"Lane\", \"(1, 0, 0)\", null, false, ref(\"{parentRef}\"))"));
            var lane = Find("Lane")!;
            Assert.AreEqual(parent.transform, lane.transform.parent);
        }

        [Test]
        public void CreatedContainersAreUndoable()
        {
            Undo.ClearAll();
            Ok(M("splines | call CreateContainer(\"Temp\")"));
            Assert.IsNotNull(Find("Temp"));
            Undo.PerformUndo();
            Assert.IsNull(Find("Temp"));
        }

        [Test]
        public void ContainersListsEveryContainer()
        {
            new GameObject("Hidden").AddComponent<SplineContainer>().gameObject.SetActive(false);
            Assert.AreEqual(2L, Count("splines | get containers | count"));
            var r = Ok(Q("splines | get containers | where gameObject.name == \"Road\" | select gameObject.name n=(splines.count()) splines.knotCount"));
            StringAssert.Contains("Road", Table(r));
            StringAssert.Contains("3", Table(r));
        }

        [Test]
        public void CreateThenAddKnotsFollowUp()
        {
            Ok(M("splines | call CreateContainer(\"Path\")"));
            Ok(M(C + " | where gameObject.name == \"Path\" | call AddKnots([\"(0, 0, 0)\", \"(0, 0, 5)\"])"));
            Assert.AreEqual(2, Find("Path")!.Spline.Count);
        }
    }
}
