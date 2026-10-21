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
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Tilemaps
{
    using RuleTile = UnityEngine.RuleTile;
    using Tile = UnityEngine.Tilemaps.Tile;

    public class UnityEngine_RuleTile_AdapterTests : TilemapTestBase
    {
        private RuleTile _rule = null!;
        private string _png = "";
        private string R => $"unity.asset(\"{Folder}/Ground.asset\")";

        [SetUp]
        public void CreateRuleTile()
        {
            _png = SpriteTexture("ground", sheet: true);
            _rule = ScriptableObject.CreateInstance<RuleTile>();
            AssetDatabase.CreateAsset(_rule, $"{Folder}/Ground.asset");
        }

        [Test]
        public void AddRuleBuildsNeighborsFromAPattern()
        {
            Assert.AreEqual("0", Result(Ok(R + $" | call AddRule(\"{_png}#ground_0\", \"NNN/T.T/TTT\")")));
            Assert.AreEqual("1", Result(Ok(R + $" | call AddRule(\"{_png}#ground_1\", \"...\", \"None\")")));
            Assert.AreEqual(2, _rule.m_TilingRules.Count);
            var top = _rule.m_TilingRules[0];
            Assert.AreEqual("ground_0", top.m_Sprites[0].name);
            Assert.AreEqual(8, top.m_Neighbors.Count);
            Assert.AreEqual(RuleTile.TilingRuleOutput.Neighbor.NotThis, top.GetNeighbors()[new Vector3Int(0, 1, 0)]);
            Assert.AreEqual(RuleTile.TilingRuleOutput.Neighbor.This, top.GetNeighbors()[new Vector3Int(-1, 0, 0)]);
            Assert.AreEqual(RuleTile.TilingRuleOutput.Neighbor.This, top.GetNeighbors()[new Vector3Int(1, -1, 0)]);
            var always = _rule.m_TilingRules[1];
            Assert.AreEqual(0, always.m_Neighbors.Count);
            Assert.AreEqual(Tile.ColliderType.None, always.m_ColliderType);
            Assert.AreNotEqual(top.m_Id, always.m_Id);
        }

        [Test]
        public void RulesDecideThePaintedSprite()
        {
            Ok(R + $" | call AddRule(\"{_png}#ground_0\", \"... ..T ...\")"); // a neighbor on the right
            Ok(R + $" | call AddRule(\"{_png}#ground_1\")");
            Ok(T + $" | call SetTiles([\"(0, 0, 0)\", \"(1, 0, 0)\"], \"{Folder}/Ground.asset\")");
            Assert.AreEqual("ground_0", Ground.GetSprite(new Vector3Int(0, 0, 0)).name);
            Assert.AreEqual("ground_1", Ground.GetSprite(new Vector3Int(1, 0, 0)).name);
        }

        [Test]
        public void BadPatternsFailWithoutAddingARule()
        {
            Fails(R + $" | call AddRule(\"{_png}#ground_0\", \"TT/TT\")", "expected 9");
            Fails(R + $" | call AddRule(\"{_png}#ground_0\", \"TTZ/T.T/TTT\")", "'Z' is not");
            Fails(R + " | call AddRule(null)", "sprite");
            Assert.AreEqual(0, _rule.m_TilingRules.Count);
        }

        [Test]
        public void ClearRulesRemovesThemUndoably()
        {
            Ok(R + $" | call AddRule(\"{_png}#ground_0\", \"TTTTTTTT\")");
            Ok(R + $" | call AddRule(\"{_png}#ground_1\")");
            Assert.AreEqual("2", Result(Ok(R + " | call ClearRules()")));
            Assert.AreEqual(0, _rule.m_TilingRules.Count);
            Undo.PerformUndo();
            Assert.AreEqual(2, _rule.m_TilingRules.Count);
        }
    }
}
