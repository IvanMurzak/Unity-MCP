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
using System.IO;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Terrains
{
    using Terrain = UnityEngine.Terrain;
    using TerrainData = UnityEngine.TerrainData;

    /// <summary>
    /// Shared terrain operations behind the RQL surface: region checks, resolution rules, and the Undo / dirty bookkeeping for
    /// TerrainData writes. The engine records the call target (the Terrain) itself; heights, splat maps, layers and trees live
    /// in the TerrainData asset, so every write here records the TerrainData (and the splat textures) first.
    /// </summary>
    internal static class TerrainOps
    {
        public const int MinHeightmapResolution = 33;
        public const int MaxHeightmapResolution = 4097;
        public const int MinAlphamapResolution = 16;
        public const int MaxAlphamapResolution = 4096;

        public static TerrainData Data(Terrain terrain) =>
            terrain.terrainData != null ? terrain.terrainData : throw new InvalidOperationException($"terrain '{terrain.name}' has no TerrainData");

        /// <summary>
        /// Record the TerrainData (complete snapshot: heights, splat maps, trees; with <paramref name="alphamaps"/> the splat
        /// textures too) for Undo, apply <paramref name="write"/>, then mark the data and the terrain dirty.
        /// </summary>
        public static void Write(Terrain terrain, TerrainData data, string action, Action write, bool alphamaps = false)
        {
            Undo.RegisterCompleteObjectUndo(data, "RQL terrain " + action);
            if (alphamaps && data.alphamapTextures.Length > 0) Undo.RegisterCompleteObjectUndo(data.alphamapTextures, "RQL terrain " + action);
            write();
            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(terrain);
        }

        /// <summary>
        /// A rectangle inside a square grid of <paramref name="resolution"/> cells. w / h &lt;= 0 extend to the edge; a rectangle that
        /// would leave the grid is an error (nothing is clamped silently).
        /// </summary>
        public static (int x, int y, int w, int h) Region(int x, int y, int w, int h, int resolution, string grid)
        {
            if (x < 0 || y < 0 || x >= resolution || y >= resolution)
                throw new ArgumentException($"({x}, {y}) is outside the {grid} ({resolution} × {resolution} cells, 0 … {resolution - 1})");
            if (w <= 0) w = resolution - x;
            if (h <= 0) h = resolution - y;
            if (x + w > resolution || y + h > resolution)
                throw new ArgumentException($"region ({x}, {y}) {w} × {h} leaves the {grid} ({resolution} × {resolution}); at most {resolution - x} × {resolution - y} fits there");
            return (x, y, w, h);
        }

        public static bool IsValidHeightmapResolution(int r) =>
            r >= MinHeightmapResolution && r <= MaxHeightmapResolution && IsPowerOfTwo(r - 1);

        public static bool IsValidAlphamapResolution(int r) =>
            r >= MinAlphamapResolution && r <= MaxAlphamapResolution && IsPowerOfTwo(r);

        /// <summary>The nearest valid heightmap resolution (2^n + 1 in [33, 4097]).</summary>
        public static int NearestHeightmapResolution(int requested)
        {
            int best = MinHeightmapResolution;
            for (int r = MinHeightmapResolution; r <= MaxHeightmapResolution; r = (r - 1) * 2 + 1)
                if (Math.Abs(r - requested) < Math.Abs(best - requested)) best = r;
            return best;
        }

        public static string HeightmapResolutionRule(int value) =>
            $"heightmapResolution must be 2^n + 1 between {MinHeightmapResolution} and {MaxHeightmapResolution} " +
            $"(33, 65, 129, 257, 513, 1025, 2049, 4097); nearest to {value}: {NearestHeightmapResolution(value)}";

        private static bool IsPowerOfTwo(int v) => v > 0 && (v & (v - 1)) == 0;

        /// <summary>Validate a new asset path: under Assets/, the expected extension, and not overwriting an existing asset.</summary>
        public static string NewAssetPath(string path, string extension)
        {
            path = path.Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.Ordinal)) throw new ArgumentException($"'{path}' must start with Assets/");
            if (!path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"'{path}' must end with {extension}");
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new ArgumentException($"an asset already exists at '{path}' (choose another path)");
            RqlEditorApi.Instance.CreateFolder(Path.GetDirectoryName(path)!);
            return path;
        }
    }
}
