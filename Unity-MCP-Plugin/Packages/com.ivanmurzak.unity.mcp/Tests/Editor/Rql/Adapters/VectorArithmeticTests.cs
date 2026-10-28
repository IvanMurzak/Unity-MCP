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
using com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Adapters
{
    /// <summary>
    /// Component-wise arithmetic (RQL spec §4.2): Vector2/3/4, Vector2Int/3Int and Color opt in, Quaternion does not; struct
    /// keys of distinct / group-by round like the display (spec §5).
    /// </summary>
    public class VectorArithmeticTests : AdapterTestBase
    {
        [Test]
        public void ScaleTimesANumberKeepsTheVectorType()
        {
            var coin = new GameObject("Coin");
            coin.transform.localScale = new Vector3(2, 2, 2);
            AssertOk(M("objects | where name == \"Coin\" | set transform.localScale = transform.localScale * 1.5"));
            Assert.AreEqual(new Vector3(3, 3, 3), coin.transform.localScale);

            AssertOk(M("objects | where name == \"Coin\" | set transform.position = transform.position + [0, 2.5, 0]"));
            Assert.AreEqual(new Vector3(0, 2.5f, 0), coin.transform.position);
        }

        [Test]
        public void EveryOptedInTypeComputes()
        {
            Assert.AreEqual(new[] { 1d, 2d }, new UnityEngine_Vector2_Adapter().GetComponents(new Vector2(1, 2)));
            Assert.AreEqual(new[] { 1d, 2d, 3d, 4d }, new UnityEngine_Vector4_Adapter().GetComponents(new Vector4(1, 2, 3, 4)));
            Assert.AreEqual(new Vector2Int(3, 4), new UnityEngine_Vector2Int_Adapter().MakeFromComponents(typeof(Vector2Int), new[] { 3d, 4d }));
            Assert.AreEqual(new Vector3Int(1, 2, 3), new UnityEngine_Vector3Int_Adapter().MakeFromComponents(typeof(Vector3Int), new[] { 0.9999999d, 2d, 3d }));
            Assert.IsNull(new UnityEngine_Vector3Int_Adapter().MakeFromComponents(typeof(Vector3Int), new[] { 1.5d, 2d, 3d }), "a fraction is not truncated");
            Assert.IsNull(new UnityEngine_Quaternion_Adapter().GetComponents(Quaternion.identity));

            var light = new GameObject("Lamp").AddComponent<Light>();
            light.color = new Color(1, 0.5f, 0.25f, 1);
            AssertOk(M("objects | get components | where $ is Light | set color = color * 0.5"));
            Assert.AreEqual(new Color(0.5f, 0.25f, 0.125f, 0.5f), light.color);

            AssertTuple("(2, 4, 6)", Cell(Q("objects | where name == \"Lamp\" | select v = (transform.localScale * [2, 4, 6])")));
            AssertTuple("(-1, -1, -1)", Cell(Q("objects | where name == \"Lamp\" | select v = (-transform.localScale)")));
        }

        [Test]
        public void QuaternionsAndNumberPlusVectorStayTypeMismatches()
        {
            new GameObject("Turret");
            // In a select the failure is the cell's value (error(type-mismatch)), with the reason in a note.
            StringAssert.Contains("Quaternion has no component-wise arithmetic", Table(Q("objects | where name == \"Turret\" | select r = (transform.rotation * 2)")));
            StringAssert.Contains("error(type-mismatch)", Table(Q("objects | where name == \"Turret\" | select r = (transform.position + 1)")));
        }

        [Test]
        public void DistinctMergesScalesThatPrintAlike()
        {
            new GameObject("A").transform.localScale = new Vector3(1, 1, 1);
            new GameObject("B").transform.localScale = new Vector3(0.99999994f, 1, 1);
            new GameObject("C").transform.localScale = new Vector3(2, 2, 2);
            Assert.AreEqual(2, Count("objects | get transform | get localScale | distinct | count"));
        }

        [Test]
        public void AMissingMapEntryReadsNull()
        {
            Assert.AreEqual(1, Count("[{a: 1}, {b: 2}] | where $[\"a\"] == null | count"));
            Assert.AreEqual(1, Count("[{a: 1}, {b: 2}] | where has($[\"a\"]) | count"));
        }
    }
}
