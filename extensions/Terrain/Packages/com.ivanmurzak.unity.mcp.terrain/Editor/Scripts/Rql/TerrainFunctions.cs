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
using System;
using System.Collections.Generic;
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Terrains
{
    using Terrain = UnityEngine.Terrain;
    using TerrainData = UnityEngine.TerrainData;

    /// <summary>
    /// Read-only height sampling as expression functions (usable in rql-query, unlike call), e.g.
    /// <c>objects | get components | where $ is Terrain | select name s=(terrain.heightStats($))</c>.
    /// The first argument is a Terrain, its GameObject or a TerrainData.
    /// </summary>
    internal static class TerrainFunctions
    {
        /// <summary>RQL encodes at most 20 items per list, so larger grids would come back truncated.</summary>
        public const int MaxGridSide = 20;

        public const string HeightsDescription =
            "terrain.heights(terrain, x, y, w?, h?): normalized heights 0 … 1 of a heightmap region as rows (at most 20 × 20 cells)";
        public const string HeightStatsDescription =
            "terrain.heightStats(terrain, x?, y?, w?, h?): {min, max, average, cells} of a heightmap region (default: the whole heightmap)";
        public const string HeightAtDescription =
            "terrain.heightAt(terrain, worldX, worldZ): world-space surface height (Y) at a world X / Z position";

        public static object? Heights(object?[] args)
        {
            if (args.Length < 3 || args.Length > 5) return Mismatch("terrain.heights(terrain, x, y, w?, h?)");
            if (!(DataOf(args[0]) is TerrainData data)) return NotATerrain("terrain.heights", args[0]);
            if (!Ints(args, 1, out var v)) return Mismatch("terrain.heights(terrain, x, y, w?, h?): x, y, w and h are integers");
            int w = args.Length > 3 ? v[2] : 1, h = args.Length > 4 ? v[3] : 1;
            if (w < 1 || h < 1 || w > MaxGridSide || h > MaxGridSide)
                return new RqlError("type-mismatch", $"terrain.heights reads 1 … {MaxGridSide} cells per side (got {w} × {h}); use terrain.heightStats for larger regions");
            var (x, y, rw, rh) = TerrainOps.Region(v[0], v[1], w, h, data.heightmapResolution, "heightmap");
            var heights = data.GetHeights(x, y, rw, rh);
            var rows = new List<object?>(rh);
            for (int row = 0; row < rh; row++)
            {
                var cells = new List<object?>(rw);
                for (int col = 0; col < rw; col++) cells.Add((double)heights[row, col]);
                rows.Add(cells);
            }
            return rows;
        }

        public static object? HeightStats(object?[] args)
        {
            if (args.Length != 1 && args.Length != 3 && args.Length != 5) return Mismatch("terrain.heightStats(terrain, x?, y?, w?, h?)");
            if (!(DataOf(args[0]) is TerrainData data)) return NotATerrain("terrain.heightStats", args[0]);
            if (!Ints(args, 1, out var v)) return Mismatch("terrain.heightStats(terrain, x?, y?, w?, h?): x, y, w and h are integers");
            int Arg(int i) => i < v.Length ? v[i] : 0;
            var (x, y, w, h) = TerrainOps.Region(Arg(0), Arg(1), Arg(2), Arg(3), data.heightmapResolution, "heightmap");
            var heights = data.GetHeights(x, y, w, h);
            float min = float.MaxValue, max = float.MinValue;
            double sum = 0;
            foreach (var value in heights)
            {
                if (value < min) min = value;
                if (value > max) max = value;
                sum += value;
            }
            return new Dictionary<string, object?>
            {
                ["min"] = (double)min,
                ["max"] = (double)max,
                ["average"] = sum / (w * h),
                ["cells"] = (long)(w * h),
            };
        }

        public static object? HeightAt(object?[] args)
        {
            if (args.Length != 3) return Mismatch("terrain.heightAt(terrain, worldX, worldZ)");
            var terrain = TerrainOf(args[0]);
            if (terrain == null || terrain.terrainData == null) return NotATerrain("terrain.heightAt", args[0]);
            if (!Number(args[1], out var wx) || !Number(args[2], out var wz)) return Mismatch("terrain.heightAt(terrain, worldX, worldZ): numbers");
            return (double)(terrain.SampleHeight(new Vector3((float)wx, 0f, (float)wz)) + terrain.transform.position.y);
        }

        // Unity's fake-null objects: compare with == null, never ?. / ??.
        private static Terrain? TerrainOf(object? value)
        {
            var terrain = value is GameObject go && go != null ? go.GetComponent<Terrain>() : value as Terrain;
            return terrain != null ? terrain : null;
        }

        private static TerrainData? DataOf(object? value)
        {
            if (value is TerrainData d) return d != null ? d : null;
            var terrain = TerrainOf(value);
            return terrain != null && terrain.terrainData != null ? terrain.terrainData : null;
        }

        private static bool Number(object? value, out double d)
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

        private static bool Ints(object?[] args, int from, out int[] values)
        {
            values = new int[Math.Max(0, args.Length - from)];
            for (int i = from; i < args.Length; i++)
            {
                if (!Number(args[i], out var d) || Math.Floor(d) != d) return false;
                values[i - from] = (int)d;
            }
            return true;
        }

        private static RqlError Mismatch(string signature) => new RqlError("type-mismatch", signature);

        private static RqlError NotATerrain(string fn, object? value) =>
            new RqlError("type-mismatch", $"{fn}: the first argument must be a Terrain (with TerrainData), its GameObject or a TerrainData, got {(value == null ? "null" : value.GetType().Name)}");
    }
}
