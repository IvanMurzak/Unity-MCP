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
using System.Linq;
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Tilemaps
{
    using Tilemap = UnityEngine.Tilemaps.Tilemap;
    using TileBase = UnityEngine.Tilemaps.TileBase;
    using Tile = UnityEngine.Tilemaps.Tile;
    using TileFlags = UnityEngine.Tilemaps.TileFlags;

    /// <summary>
    /// Tilemap: painting as callable methods (SetTile, SetTiles, BoxFill, ClearBox, FloodFill, ClearAllTiles, SetColor,
    /// SetTransform, SetColliderType) and the read-only views <c>tileCount</c> (occupied cells) and <c>usedTiles</c> (distinct
    /// tile assets). Every write records the Tilemap for Undo; settings (orientation, tileAnchor, color …) are ordinary members.
    /// </summary>
    public sealed class UnityEngine_Tilemaps_Tilemap_Adapter : RqlTypeAdapter<Tilemap>
    {
        public override string Description =>
            "Tilemap: members are its settings (orientation, tileAnchor, color, animationFrameRate, cellBounds …), tileCount, usedTiles; " +
            "call SetTile, SetTiles, BoxFill, ClearBox, FloodFill, ClearAllTiles, SetColor, SetTransform, SetColliderType, CompressBounds, RefreshAllTiles; " +
            "cells are \"(x, y, z)\"";

        private static CallableParameter P<TP>(string name) => new CallableParameter(name, typeof(TP));
        private static CallableParameter P<TP>(string name, TP fallback) => new CallableParameter(name, typeof(TP), true, fallback);

        private static void Add(MemberTable<Tilemap> m, string name, CallableParameter[] ps, Type returns, Func<Tilemap, object?[], object?> body, string description) =>
            m.Untyped.AddMethod(name, ps, returns, (o, a) => body((Tilemap)o, a), description);

        protected override void Members(MemberTable<Tilemap> m)
        {
            m.Add("tileCount", t => TilemapOps.CountTiles(t), description: "number of occupied cells (read-only)")
             .Add("usedTiles", UsedTiles, description: "the distinct tile assets painted on the tilemap (read-only)")
             .Reference("layoutGrid")
             .Hide("editorPreviewOrigin", "editorPreviewSize")
             .Expose("CompressBounds", "shrink origin / size (cellBounds) to the painted cells")
             .Expose("RefreshAllTiles", "re-run every tile's GetTileData (after editing a RuleTile's rules)");

            Add(m, "SetTile", new[] { P<Vector3Int>("cell"), P<TileBase?>("tile") }, typeof(void),
                (t, a) => { SetTiles(t, new[] { (Vector3Int)a[0]! }, (TileBase?)a[1]); return null; },
                "paint a tile asset (path \"Assets/…/Grass.asset\", ref or unity.asset(…)) at cell \"(x, y, z)\"; null erases the cell");
            Add(m, "SetTiles", new[] { P<Vector3Int[]>("cells"), P<TileBase?>("tile") }, typeof(int),
                (t, a) => SetTiles(t, (Vector3Int[])a[0]!, (TileBase?)a[1]),
                "paint one tile at every cell of [\"(x, y, z)\", …] (null erases); returns the cell count");
            Add(m, "BoxFill", new[] { P<TileBase?>("tile"), P<int>("minX"), P<int>("minY"), P<int>("maxX"), P<int>("maxY"), P("z", 0) }, typeof(int),
                (t, a) => BoxFill(t, (TileBase?)a[0], TilemapOps.Region((int)a[1]!, (int)a[2]!, (int)a[3]!, (int)a[4]!, (int)a[5]!)),
                "fill the rectangle minX … maxX, minY … maxY (both corners inclusive) on layer z with a tile (null erases); returns the cell count");
            Add(m, "ClearBox", new[] { P<int>("minX"), P<int>("minY"), P<int>("maxX"), P<int>("maxY"), P("z", 0) }, typeof(int),
                (t, a) => ClearBox(t, TilemapOps.Region((int)a[0]!, (int)a[1]!, (int)a[2]!, (int)a[3]!, (int)a[4]!)),
                "erase the tiles of the rectangle minX … maxX, minY … maxY (inclusive) on layer z; returns how many tiles were removed");
            Add(m, "FloodFill", new[] { P<Vector3Int>("cell"), P<TileBase>("tile") }, typeof(void),
                (t, a) => { FloodFill(t, (Vector3Int)a[0]!, (TileBase)a[1]!); return null; },
                "flood-fill the contiguous area of equal tiles (or empty cells) around cell with a tile; an empty area must be enclosed");
            Add(m, "ClearAllTiles", Array.Empty<CallableParameter>(), typeof(int), (t, a) => ClearAll(t),
                "erase every tile; returns how many were removed");
            Add(m, "SetColor", new[] { P<Vector3Int>("cell"), P<Color>("color") }, typeof(void),
                (t, a) => { SetColor(t, (Vector3Int)a[0]!, (Color)a[1]!); return null; },
                "tint the tile at cell (unlocks its LockColor flag), e.g. SetColor(\"(0, 0, 0)\", \"#FF8080\")");
            Add(m, "SetTransform", new[] { P<Vector3Int>("cell"), P("rotation", 0f), P<Vector3?>("scale", null), P("flipX", false), P("flipY", false) }, typeof(void),
                (t, a) => { SetTransform(t, (Vector3Int)a[0]!, (float)a[1]!, (Vector3?)a[2], (bool)a[3]!, (bool)a[4]!); return null; },
                "rotate (degrees around Z), scale and / or mirror the tile at cell (unlocks its LockTransform flag)");
            Add(m, "SetColliderType", new[] { P<Vector3Int>("cell"), P<Tile.ColliderType>("colliderType") }, typeof(void),
                (t, a) => { SetColliderType(t, (Vector3Int)a[0]!, (Tile.ColliderType)a[1]!); return null; },
                "collider shape of the tile at cell: None, Sprite or Grid (used by a TilemapCollider2D)");
        }

        private static TileBase[] UsedTiles(Tilemap t)
        {
            var tiles = new TileBase[t.GetUsedTilesCount()];
            int n = t.GetUsedTilesNonAlloc(tiles);
            return n == tiles.Length ? tiles : tiles.Take(n).ToArray();
        }

        private static int SetTiles(Tilemap t, Vector3Int[] cells, TileBase? tile)
        {
            if (cells.Length > TilemapOps.MaxCells) throw new ArgumentException($"{cells.Length} cells; at most {TilemapOps.MaxCells} per call");
            TilemapOps.Record(t, tile == null ? "erase tiles" : "set tiles");
            t.SetTiles(cells, Enumerable.Repeat(tile, cells.Length).ToArray());
            return cells.Length;
        }

        private static int BoxFill(Tilemap t, TileBase? tile, BoundsInt region)
        {
            int cells = region.size.x * region.size.y;
            TilemapOps.Record(t, "box fill");
            // SetTilesBlock covers exactly the region (Tilemap.BoxFill's pivot semantics vary between Unity versions).
            t.SetTilesBlock(region, Enumerable.Repeat(tile, cells).ToArray());
            return cells;
        }

        private static int ClearBox(Tilemap t, BoundsInt region)
        {
            int removed = TilemapOps.CountTiles(t, null, region);
            BoxFill(t, null, region);
            return removed;
        }

        private static void FloodFill(Tilemap t, Vector3Int cell, TileBase tile)
        {
            if (tile == null) throw new ArgumentNullException(nameof(tile), "FloodFill needs a tile (erase with ClearBox)");
            if (t.GetTile(cell) == null)
            {
                // Unity floods empty cells up to the tilemap's bounds; an open area would silently fill the whole rectangle.
                var b = t.cellBounds;
                if (!b.Contains(cell)) throw new ArgumentException($"{TilemapOps.Format(cell)} is empty and outside the painted area {TilemapOps.Format(b.min)} … {TilemapOps.Format(b.max - Vector3Int.one)}");
            }
            TilemapOps.Record(t, "flood fill");
            t.FloodFill(cell, tile);
        }

        private static int ClearAll(Tilemap t)
        {
            int removed = TilemapOps.CountTiles(t);
            TilemapOps.Record(t, "clear all tiles");
            t.ClearAllTiles();
            return removed;
        }

        private static void SetColor(Tilemap t, Vector3Int cell, Color color)
        {
            TilemapOps.RequireTile(t, cell);
            TilemapOps.Record(t, "set color");
            t.SetTileFlags(cell, t.GetTileFlags(cell) & ~TileFlags.LockColor);
            t.SetColor(cell, color);
        }

        private static void SetTransform(Tilemap t, Vector3Int cell, float rotation, Vector3? scale, bool flipX, bool flipY)
        {
            TilemapOps.RequireTile(t, cell);
            var s = scale ?? Vector3.one;
            if (s.x == 0f || s.y == 0f) throw new ArgumentException("scale x and y must not be 0");
            if (flipX) s.x = -Mathf.Abs(s.x);
            if (flipY) s.y = -Mathf.Abs(s.y);
            TilemapOps.Record(t, "set transform");
            t.SetTileFlags(cell, t.GetTileFlags(cell) & ~TileFlags.LockTransform);
            t.SetTransformMatrix(cell, Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, rotation), s));
        }

        private static void SetColliderType(Tilemap t, Vector3Int cell, Tile.ColliderType type)
        {
            TilemapOps.RequireTile(t, cell);
            TilemapOps.Record(t, "set collider type");
            t.SetColliderType(cell, type);
        }
    }
}
