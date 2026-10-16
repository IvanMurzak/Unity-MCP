/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Terrain)        │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Terrains
{
    public class TerrainFunctionsTests : TerrainTestBase
    {
        [SetUp]
        public void Shape()
        {
            var heights = new float[33, 33];
            heights[20, 10] = 0.5f;    // row = y, column = x
            heights[20, 11] = 1f;
            Data.SetHeights(0, 0, heights);
        }

        private static string Compact(string s) => s.Replace(" ", "");

        [Test]
        public void HeightsReturnsRowsOfARegion()
        {
            var r = Q(T + " | select h=(terrain.heights($, 10, 20, 2, 2))");
            StringAssert.Contains("[[0.5,1],[0,0]]", Compact(Cell(r)));
            StringAssert.Contains("[[0.5]]", Compact(Cell(Q(T + " | select h=(terrain.heights(gameObject, 10, 20))"))));
        }

        [Test]
        public void HeightsLimitsTheGridAndTheRegion()
        {
            // In a select cell an error shows its code only; at the source position the message is reported.
            StringAssert.Contains("type-mismatch", Table(Q(T + " | select h=(terrain.heights($, 0, 0, 21, 1))")));
            var terrain = Cell(Q(T + " | select $ref"));
            StringAssert.Contains("use terrain.heightStats", Table(Q($"terrain.heights(ref(\"{terrain}\"), 0, 0, 21, 1)")));
            StringAssert.Contains("outside the heightmap", Table(Q($"terrain.heights(ref(\"{terrain}\"), 40, 0)")));
            StringAssert.Contains("first argument must be a Terrain", Table(Q("terrain.heights(\"Ground\", 0, 0)")));
        }

        [Test]
        public void HeightStatsOverTheWholeMapOrARegion()
        {
            var whole = Compact(Cell(Q(T + " | select s=(terrain.heightStats($))")));
            StringAssert.Contains("\"min\":0", whole);
            StringAssert.Contains("\"max\":1", whole);
            StringAssert.Contains("\"cells\":1089", whole);
            var region = Compact(Cell(Q(T + " | select s=(terrain.heightStats(terrainData, 10, 20, 2, 1))")));
            StringAssert.Contains("\"average\":0.75", region);
            var count = Q(T + " | where terrain.heightStats($).max == 1 | count");
            Assert.IsTrue(count.Ok, Table(count));
            Assert.AreEqual(1L, count.Value!.GetValue<long>());
        }

        [Test]
        public void HeightAtSamplesWorldSpace()
        {
            Ground.transform.position = new Vector3(0f, 10f, 0f);
            Ground.terrainData.SetHeights(0, 0, new float[33, 33]);
            var flat = new float[33, 33];
            for (int y = 0; y < 33; y++) for (int x = 0; x < 33; x++) flat[y, x] = 0.4f;
            Ground.terrainData.SetHeights(0, 0, flat);
            var y0 = double.Parse(Cell(Q(T + " | select y=(terrain.heightAt($, 50, 50))")), System.Globalization.CultureInfo.InvariantCulture);
            Assert.AreEqual(10.0 + 0.4 * 50.0, y0, 0.1);
        }
    }
}
