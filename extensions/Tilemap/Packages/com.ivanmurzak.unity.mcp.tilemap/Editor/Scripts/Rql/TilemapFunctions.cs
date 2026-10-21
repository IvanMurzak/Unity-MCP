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
using Rql.Execution;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Tilemaps
{
    using Tilemap = UnityEngine.Tilemaps.Tilemap;
    using TileBase = UnityEngine.Tilemaps.TileBase;

    /// <summary>
    /// Read-only cell lookups as expression functions (usable in rql-query, unlike call), e.g.
    /// <c>objects | get components | where $ is Tilemap | select name t=(tilemap.tileAt($, "(0, 0, 0)"))</c>.
    /// The first argument is a Tilemap or its GameObject; cells are "(x, y)" / "(x, y, z)" or [x, y, z].
    /// </summary>
    internal static class TilemapFunctions
    {
        /// <summary>RQL encodes at most 20 items per list, so larger results would come back truncated.</summary>
        public const int MaxList = 20;

        public const string TileAtDescription = "tilemap.tileAt(tilemap, cell): the tile asset at a cell \"(x, y, z)\", or null";
        public const string CellDescription =
            "tilemap.cell(tilemap, cell): {cell, tile, path, sprite, color, colliderType, flags, transform} of one cell (tile null when empty)";
        public const string TilesDescription =
            "tilemap.tiles(tilemap, minX, minY, maxX, maxY, z?): tile names of a region as rows, top row (maxY) first, null = empty (at most 20 × 20)";
        public const string CountDescription =
            "tilemap.count(tilemap, tile?, minX?, minY?, maxX?, maxY?, z?): occupied cells, optionally of one tile (asset, path or name) and inside a region";
        public const string CellsDescription =
            "tilemap.cells(tilemap, tile?): the first 20 occupied cells (optionally of one tile asset, path or name), bottom-left first";
        public const string WorldToCellDescription = "tilemap.worldToCell(tilemap, \"(x, y, z)\"): the cell containing a world position";
        public const string CellToWorldDescription = "tilemap.cellToWorld(tilemap, cell): the world position of a cell's center";

        public static object? TileAt(object?[] args)
        {
            if (args.Length != 2) return Mismatch("tilemap.tileAt(tilemap, cell)");
            if (!(TilemapOps.TilemapOf(args[0]) is Tilemap t)) return NotATilemap("tilemap.tileAt", args[0]);
            if (!TilemapOps.TryCell(args[1], out var cell)) return BadCell("tilemap.tileAt", args[1]);
            var tile = t.GetTile(cell);
            return tile != null ? tile : null;
        }

        public static object? Cell(object?[] args)
        {
            if (args.Length != 2) return Mismatch("tilemap.cell(tilemap, cell)");
            if (!(TilemapOps.TilemapOf(args[0]) is Tilemap t)) return NotATilemap("tilemap.cell", args[0]);
            if (!TilemapOps.TryCell(args[1], out var cell)) return BadCell("tilemap.cell", args[1]);
            var tile = t.GetTile(cell);
            bool has = tile != null;
            var sprite = has ? t.GetSprite(cell) : null;
            var path = has ? AssetDatabase.GetAssetPath(tile) : null;
            return new Dictionary<string, object?>
            {
                ["cell"] = TilemapOps.Format(cell),
                ["tile"] = has ? tile : null,
                ["path"] = string.IsNullOrEmpty(path) ? null : path,
                ["sprite"] = sprite != null ? sprite : null,
                ["color"] = has ? Format(t.GetColor(cell)) : null,
                ["colliderType"] = has ? t.GetColliderType(cell).ToString() : null,
                ["flags"] = has ? t.GetTileFlags(cell).ToString() : null,
                ["transform"] = has ? Describe(t.GetTransformMatrix(cell)) : null,
            };
        }

        public static object? Tiles(object?[] args)
        {
            const string sig = "tilemap.tiles(tilemap, minX, minY, maxX, maxY, z?)";
            if (args.Length != 5 && args.Length != 6) return Mismatch(sig);
            if (!(TilemapOps.TilemapOf(args[0]) is Tilemap t)) return NotATilemap("tilemap.tiles", args[0]);
            if (!Ints(args, 1, out var v)) return Mismatch(sig + ": coordinates are integers");
            if (!TryRegion(v, out var region, out var error)) return error;
            if (region.size.x > MaxList || region.size.y > MaxList)
                return new RqlError("type-mismatch", $"tilemap.tiles reads at most {MaxList} × {MaxList} cells (got {region.size.x} × {region.size.y}); use tilemap.count for larger regions");
            var tiles = t.GetTilesBlock(region); // x fastest, then y (bottom row first)
            var rows = new List<object?>(region.size.y);
            for (int y = region.size.y - 1; y >= 0; y--)
            {
                var row = new List<object?>(region.size.x);
                for (int x = 0; x < region.size.x; x++)
                {
                    var tile = tiles[y * region.size.x + x];
                    row.Add(tile != null ? tile.name : null);
                }
                rows.Add(row);
            }
            return rows;
        }

        public static object? Count(object?[] args)
        {
            const string sig = "tilemap.count(tilemap, tile?, minX?, minY?, maxX?, maxY?, z?)";
            if (args.Length != 1 && args.Length != 2 && args.Length != 6 && args.Length != 7) return Mismatch(sig);
            if (!(TilemapOps.TilemapOf(args[0]) is Tilemap t)) return NotATilemap("tilemap.count", args[0]);
            if (!TilemapOps.TryTileMatcher(args.Length > 1 ? args[1] : null, out var match)) return Mismatch(sig + ": tile is a tile asset, an asset path or a tile name");
            BoundsInt? region = null;
            if (args.Length >= 6)
            {
                if (!Ints(args, 2, out var v)) return Mismatch(sig + ": coordinates are integers");
                if (!TryRegion(v, out var r, out var error)) return error;
                region = r;
            }
            try { return (long)TilemapOps.CountTiles(t, match, region); }
            catch (ArgumentException ex) { return new RqlError("type-mismatch", "tilemap.count: " + ex.Message); }
        }

        public static object? Cells(object?[] args)
        {
            if (args.Length != 1 && args.Length != 2) return Mismatch("tilemap.cells(tilemap, tile?)");
            if (!(TilemapOps.TilemapOf(args[0]) is Tilemap t)) return NotATilemap("tilemap.cells", args[0]);
            if (!TilemapOps.TryTileMatcher(args.Length > 1 ? args[1] : null, out var match)) return Mismatch("tilemap.cells(tilemap, tile?): tile is a tile asset, an asset path or a tile name");
            var cells = new List<object?>();
            try
            {
                TilemapOps.ForEachTile(t, (cell, tile) =>
                {
                    if (match == null || match(tile)) cells.Add(TilemapOps.Format(cell));
                    return cells.Count < MaxList;
                });
            }
            catch (ArgumentException ex) { return new RqlError("type-mismatch", "tilemap.cells: " + ex.Message); }
            return cells;
        }

        public static object? WorldToCell(object?[] args)
        {
            if (args.Length != 2) return Mismatch("tilemap.worldToCell(tilemap, \"(x, y, z)\")");
            if (!(TilemapOps.TilemapOf(args[0]) is Tilemap t)) return NotATilemap("tilemap.worldToCell", args[0]);
            if (!TryVector3(args[1], out var world)) return Mismatch("tilemap.worldToCell(tilemap, position): position is \"(x, y, z)\" or [x, y, z]");
            return TilemapOps.Format(t.WorldToCell(world));
        }

        public static object? CellToWorld(object?[] args)
        {
            if (args.Length != 2) return Mismatch("tilemap.cellToWorld(tilemap, cell)");
            if (!(TilemapOps.TilemapOf(args[0]) is Tilemap t)) return NotATilemap("tilemap.cellToWorld", args[0]);
            if (!TilemapOps.TryCell(args[1], out var cell)) return BadCell("tilemap.cellToWorld", args[1]);
            return t.GetCellCenterWorld(cell);
        }

        // ------------------------------------------------------------ helpers

        private static string Format(Color c) => $"({c.r:0.###}, {c.g:0.###}, {c.b:0.###}, {c.a:0.###})";

        private static object Describe(Matrix4x4 m)
        {
            var s = m.lossyScale;
            var r = m.rotation.eulerAngles.z;
            return new Dictionary<string, object?>
            {
                ["rotation"] = (double)Mathf.Round(r * 1000f) / 1000.0,
                ["scale"] = $"({s.x:0.###}, {s.y:0.###}, {s.z:0.###})",
                // A flip (negative x or y scale) makes the 2D determinant negative.
                ["mirrored"] = m.m00 * m.m11 - m.m01 * m.m10 < 0,
            };
        }

        private static bool TryVector3(object? value, out Vector3 v)
        {
            v = default;
            if (value is Vector3 x) { v = x; return true; }
            if (value is string s)
            {
                var parts = s.Trim().TrimStart('(').TrimEnd(')').Split(',');
                if (parts.Length != 2 && parts.Length != 3) return false;
                var f = new float[3];
                for (int i = 0; i < parts.Length; i++)
                    if (!float.TryParse(parts[i].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f[i])) return false;
                v = new Vector3(f[0], f[1], f[2]);
                return true;
            }
            if (value is IList<object?> list && (list.Count == 2 || list.Count == 3))
            {
                var f = new float[3];
                for (int i = 0; i < list.Count; i++)
                {
                    if (!TilemapOps.Number(list[i], out var d)) return false;
                    f[i] = (float)d;
                }
                v = new Vector3(f[0], f[1], f[2]);
                return true;
            }
            return false;
        }

        // Function errors surface without their message when thrown, so region problems are returned as RqlError.
        private static bool TryRegion(int[] v, out BoundsInt region, out RqlError? error)
        {
            region = default;
            error = null;
            try { region = TilemapOps.Region(v[0], v[1], v[2], v[3], v.Length > 4 ? v[4] : 0); return true; }
            catch (ArgumentException ex) { error = new RqlError("type-mismatch", ex.Message); return false; }
        }

        private static bool Ints(object?[] args, int from, out int[] values)
        {
            values = new int[Math.Max(0, args.Length - from)];
            for (int i = from; i < args.Length; i++)
                if (!TilemapOps.Int(args[i], out values[i - from])) return false;
            return true;
        }

        private static RqlError Mismatch(string signature) => new RqlError("type-mismatch", signature);

        private static RqlError BadCell(string fn, object? value) =>
            new RqlError("type-mismatch", $"{fn}: a cell is \"(x, y)\", \"(x, y, z)\" or [x, y, z] with whole numbers, got {(value == null ? "null" : value.ToString())}");

        private static RqlError NotATilemap(string fn, object? value) =>
            new RqlError("type-mismatch", $"{fn}: the first argument must be a Tilemap or its GameObject, got {(value == null ? "null" : value.GetType().Name)}");
    }
}
