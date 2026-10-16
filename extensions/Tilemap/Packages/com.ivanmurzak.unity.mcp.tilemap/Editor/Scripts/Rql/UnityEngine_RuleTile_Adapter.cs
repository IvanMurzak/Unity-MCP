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
using System;
using System.Collections.Generic;
using System.Text;
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Tilemaps
{
    using RuleTile = UnityEngine.RuleTile;
    using Tile = UnityEngine.Tilemaps.Tile;

    /// <summary>
    /// RuleTile (2D Tilemap Extras): AddRule builds a tiling rule from a 3 × 3 neighbor pattern, ClearRules removes them. Both
    /// refresh the tile's neighbor cache, which direct writes to <c>m_TilingRules</c> do not.
    /// </summary>
    public sealed class UnityEngine_RuleTile_Adapter : RqlTypeAdapter<RuleTile>
    {
        public override string Description =>
            "RuleTile: m_DefaultSprite, m_DefaultColliderType, m_TilingRules; call AddRule(sprite, pattern, colliderType?) with a 3 × 3 pattern " +
            "\"TTT/T.T/NNN\" (T = this tile, N = not this tile, . = any; rows top to bottom), ClearRules()";

        // Reading order of a 3 × 3 pattern without its center: top row, middle row (left, right), bottom row.
        private static readonly Vector3Int[] Positions =
        {
            new Vector3Int(-1, 1, 0), new Vector3Int(0, 1, 0), new Vector3Int(1, 1, 0),
            new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0),
            new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0), new Vector3Int(1, -1, 0),
        };

        protected override void Members(MemberTable<RuleTile> m)
        {
            m.Untyped.AddMethod("AddRule",
                new[]
                {
                    new CallableParameter("sprite", typeof(Sprite)),
                    new CallableParameter("pattern", typeof(string), true, "..."),
                    new CallableParameter("colliderType", typeof(Tile.ColliderType), true, Tile.ColliderType.Sprite),
                },
                typeof(int), (o, a) => AddRule((RuleTile)o, (Sprite)a[0]!, (string)a[1]!, (Tile.ColliderType)a[2]!),
                "append a tiling rule that shows sprite where the neighbors match pattern: 9 (or 8, without the center) of " +
                "T = this tile, N = not this tile, . = any, rows top to bottom, '/' and spaces ignored (only dots, e.g. \"...\", = always); " +
                "rules are checked in order, the first match wins; returns the rule index");
            m.Untyped.AddMethod("ClearRules", Array.Empty<CallableParameter>(), typeof(int), (o, a) => ClearRules((RuleTile)o),
                "remove every tiling rule (the default sprite stays); returns how many were removed");
        }

        /// <summary>Parse a neighbor pattern into (position, Neighbor.This / NotThis) pairs; "any" cells are left out.</summary>
        public static List<(Vector3Int position, int neighbor)> ParsePattern(string pattern)
        {
            var cells = new StringBuilder();
            foreach (var c in pattern ?? "")
                if (!char.IsWhiteSpace(c) && c != '/' && c != '|') cells.Append(c);
            var result = new List<(Vector3Int, int)>();
            bool any = true;
            for (int i = 0; i < cells.Length; i++) any &= cells[i] == '.' || cells[i] == '?' || cells[i] == '*';
            if (any) return result; // ".", "...", "........." = matches everywhere
            if (cells.Length == 9) cells.Remove(4, 1); // the center is the tile itself
            else if (cells.Length != 8)
                throw new ArgumentException($"pattern '{pattern}' has {cells.Length} cells; expected 9 (3 rows of 3, e.g. \"TTT/T.T/NNN\") or 8 without the center");
            for (int i = 0; i < 8; i++)
            {
                switch (char.ToUpperInvariant(cells[i]))
                {
                    case 'T': case '1': result.Add((Positions[i], RuleTile.TilingRuleOutput.Neighbor.This)); break;
                    case 'N': case 'X': case '2': result.Add((Positions[i], RuleTile.TilingRuleOutput.Neighbor.NotThis)); break;
                    case '.': case '?': case '*': case '0': break;
                    default: throw new ArgumentException($"pattern '{pattern}': '{cells[i]}' is not T (this), N (not this) or . (any)");
                }
            }
            return result;
        }

        private static int AddRule(RuleTile tile, Sprite sprite, string pattern, Tile.ColliderType colliderType)
        {
            if (sprite == null) throw new ArgumentNullException(nameof(sprite));
            var neighbors = ParsePattern(pattern);
            var rule = new RuleTile.TilingRule
            {
                m_Sprites = new[] { sprite },
                m_ColliderType = colliderType,
                m_Neighbors = new List<int>(),
                m_NeighborPositions = new List<Vector3Int>(),
            };
            int id = 0;
            foreach (var r in tile.m_TilingRules) id = Math.Max(id, r.m_Id + 1);
            rule.m_Id = id;
            foreach (var (position, neighbor) in neighbors)
            {
                rule.m_NeighborPositions.Add(position);
                rule.m_Neighbors.Add(neighbor);
            }
            tile.m_TilingRules.Add(rule);
            tile.UpdateNeighborPositions();
            return tile.m_TilingRules.Count - 1;
        }

        private static int ClearRules(RuleTile tile)
        {
            int removed = tile.m_TilingRules.Count;
            tile.m_TilingRules.Clear();
            tile.UpdateNeighborPositions();
            return removed;
        }
    }
}
