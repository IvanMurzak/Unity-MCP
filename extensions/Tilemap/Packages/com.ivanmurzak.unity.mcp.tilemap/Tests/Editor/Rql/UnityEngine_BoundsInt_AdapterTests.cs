/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Tilemap)        │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Tilemaps
{
    public class UnityEngine_BoundsInt_AdapterTests : TilemapTestBase
    {
        [Test]
        public void CellBoundsShowsOnlyMinMaxAndSize()
        {
            Ground.SetTile(new Vector3Int(-2, -1, 0), TileAsset("Grass"));
            Ground.SetTile(new Vector3Int(3, 4, 0), TileAsset("Sand"));
            var bounds = Cell(Read(T + " | select cellBounds")).Replace("\\", "");
            StringAssert.Contains("(-2, -1, 0)", bounds);
            StringAssert.Contains("(4, 5, 1)", bounds);
            StringAssert.Contains("(6, 6, 1)", bounds);
            StringAssert.DoesNotContain("allPositionsWithin", bounds);
            StringAssert.DoesNotContain("center", bounds);
            Assert.AreEqual("(6, 6, 1)", Cell(Read(T + " | select cellBounds.size")));
        }
    }
}
