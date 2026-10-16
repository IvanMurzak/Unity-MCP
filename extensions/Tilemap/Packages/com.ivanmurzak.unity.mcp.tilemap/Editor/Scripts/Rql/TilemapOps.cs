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
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Tilemaps
{
    using Tilemap = UnityEngine.Tilemaps.Tilemap;
    using TileBase = UnityEngine.Tilemaps.TileBase;

    /// <summary>
    /// Shared tilemap operations behind the RQL surface: cell / region parsing and checks, tile matching, the Undo bookkeeping
    /// for tile writes and asset path rules.
    /// </summary>
    internal static class TilemapOps
    {
        /// <summary>Largest region one call writes or scans (1024 × 1024 cells).</summary>
        public const int MaxCells = 1 << 20;

        private static readonly Regex Tuple = new Regex(@"^\s*\(([^()]*)\)\s*$", RegexOptions.CultureInvariant);

        public static string Format(Vector3Int c) => $"({c.x}, {c.y}, {c.z})";

        /// <summary>
        /// Record the whole Tilemap for Undo before a tile write. The engine records the call target itself, but a complete
        /// snapshot is what restores the tile, color and transform arrays reliably.
        /// </summary>
        public static void Record(Tilemap tilemap, string action) => Undo.RegisterCompleteObjectUndo(tilemap, "RQL tilemap " + action);

        /// <summary>An inclusive rectangle of cells on layer z; the corners may come in any order. Larger than <see cref="MaxCells"/> fails.</summary>
        public static BoundsInt Region(int minX, int minY, int maxX, int maxY, int z)
        {
            int loX = Math.Min(minX, maxX), hiX = Math.Max(minX, maxX), loY = Math.Min(minY, maxY), hiY = Math.Max(minY, maxY);
            long cells = (long)(hiX - loX + 1) * (hiY - loY + 1);
            if (cells > MaxCells)
                throw new ArgumentException($"region ({loX}, {loY}) … ({hiX}, {hiY}) has {cells} cells; at most {MaxCells} per call (split it)");
            return new BoundsInt(loX, loY, z, hiX - loX + 1, hiY - loY + 1, 1);
        }

        public static TileBase RequireTile(Tilemap tilemap, Vector3Int cell)
        {
            var tile = tilemap.GetTile(cell);
            if (tile == null) throw new ArgumentException($"no tile at {Format(cell)} on '{tilemap.name}' (paint one with SetTile first)");
            return tile;
        }

        /// <summary>Largest area scanned for reads (counts, used cells): 4096 × 4096 cells.</summary>
        public const int MaxScanCells = 1 << 24;

        /// <summary>
        /// Visit every occupied cell inside <paramref name="area"/> (default: the tilemap's cellBounds), one slab of rows at a time,
        /// until <paramref name="visit"/> returns false.
        /// </summary>
        public static void ForEachTile(Tilemap tilemap, Func<Vector3Int, TileBase, bool> visit, BoundsInt? area = null)
        {
            var b = area ?? tilemap.cellBounds;
            if (b.size.x <= 0 || b.size.y <= 0 || b.size.z <= 0) return;
            if ((long)b.size.x * b.size.y * b.size.z > MaxScanCells)
                throw new ArgumentException($"the area {b.size.x} × {b.size.y} × {b.size.z} is larger than {MaxScanCells} cells (CompressBounds, or pass a region)");
            int rowsPerSlab = Math.Max(1, MaxCells / b.size.x);
            for (int z = b.zMin; z < b.zMax; z++)
            {
                for (int y0 = b.yMin; y0 < b.yMax; y0 += rowsPerSlab)
                {
                    int rows = Math.Min(rowsPerSlab, b.yMax - y0);
                    var tiles = tilemap.GetTilesBlock(new BoundsInt(b.xMin, y0, z, b.size.x, rows, 1));
                    int i = 0;
                    for (int y = 0; y < rows; y++)
                        for (int x = 0; x < b.size.x; x++, i++)
                            if (tiles[i] != null && !visit(new Vector3Int(b.xMin + x, y0 + y, z), tiles[i])) return;
                }
            }
        }

        public static int CountTiles(Tilemap tilemap, Func<TileBase, bool>? match = null, BoundsInt? area = null)
        {
            int n = 0;
            ForEachTile(tilemap, (_, t) => { if (match == null || match(t)) n++; return true; }, area);
            return n;
        }

        // ------------------------------------------------------------ raw RQL values (function arguments)

        // Unity's fake-null objects: compare with == null, never ?. / ??.
        public static Tilemap? TilemapOf(object? value)
        {
            var tilemap = value is GameObject go && go != null ? go.GetComponent<Tilemap>() : value as Tilemap;
            return tilemap != null ? tilemap : null;
        }

        public static bool Number(object? value, out double d)
        {
            switch (value)
            {
                case double x: d = x; return true;
                case long l: d = l; return true;
                case int i: d = i; return true;
                case float f: d = f; return true;
                default: d = 0; return false;
            }
        }

        public static bool Int(object? value, out int i)
        {
            i = 0;
            if (!Number(value, out var d) || Math.Floor(d) != d || d < int.MinValue || d > int.MaxValue) return false;
            i = (int)d;
            return true;
        }

        /// <summary>A cell from "(x, y)" / "(x, y, z)", [x, y(, z)], {x, y, z} or a Vector3Int / Vector2Int (z defaults to 0).</summary>
        public static bool TryCell(object? value, out Vector3Int cell)
        {
            cell = default;
            switch (value)
            {
                case Vector3Int v: cell = v; return true;
                case Vector2Int v2: cell = new Vector3Int(v2.x, v2.y, 0); return true;
                case string s:
                {
                    var m = Tuple.Match(s);
                    if (!m.Success) return false;
                    var parts = m.Groups[1].Value.Split(',');
                    var n = new int[parts.Length];
                    for (int i = 0; i < parts.Length; i++)
                        if (!int.TryParse(parts[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out n[i])) return false;
                    return Make(n, out cell);
                }
                case IList<object?> list:
                {
                    var n = new int[list.Count];
                    for (int i = 0; i < n.Length; i++)
                        if (!Int(list[i], out n[i])) return false;
                    return Make(n, out cell);
                }
                case IDictionary<string, object?> map:
                {
                    if (!map.TryGetValue("x", out var x) || !map.TryGetValue("y", out var y) || !Int(x, out var ix) || !Int(y, out var iy)) return false;
                    int iz = 0;
                    if (map.TryGetValue("z", out var z) && !Int(z, out iz)) return false;
                    cell = new Vector3Int(ix, iy, iz);
                    return true;
                }
                default:
                    return false;
            }
        }

        public static bool Make(int[] n, out Vector3Int cell)
        {
            cell = default;
            if (n.Length != 2 && n.Length != 3) return false;
            cell = new Vector3Int(n[0], n[1], n.Length == 3 ? n[2] : 0);
            return true;
        }

        /// <summary>
        /// A tile filter: a TileBase asset (same object), an asset path ("Assets/…/Grass.asset") or a tile name ("Grass"); null =
        /// any tile. Returns false when the value is none of these.
        /// </summary>
        public static bool TryTileMatcher(object? value, out Func<TileBase, bool>? match)
        {
            match = null;
            switch (value)
            {
                case null: return true;
                case TileBase tile when tile != null:
                    match = t => t == tile;
                    return true;
                case string s when s.Replace('\\', '/').StartsWith("Assets/", StringComparison.Ordinal):
                {
                    var path = s.Replace('\\', '/');
                    match = t => AssetDatabase.GetAssetPath(t) == path;
                    return true;
                }
                case string name:
                    match = t => t.name == name;
                    return true;
                default:
                    return false;
            }
        }

        // ------------------------------------------------------------ assets

        /// <summary>Create missing folders of an "Assets/…" path.</summary>
        public static void EnsureFolder(string? folder)
        {
            if (string.IsNullOrEmpty(folder)) return;
            folder = folder!.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent)) throw new ArgumentException($"'{folder}' is not under Assets/");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <summary>Validate a new asset path: under Assets/, the expected extension, and not overwriting an existing asset.</summary>
        public static string NewAssetPath(string path, string extension)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path is empty");
            path = path.Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.Ordinal)) throw new ArgumentException($"'{path}' must start with Assets/");
            if (!path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"'{path}' must end with {extension}");
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new ArgumentException($"an asset already exists at '{path}' (choose another path)");
            EnsureFolder(Path.GetDirectoryName(path));
            return path;
        }
    }
}
