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
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Terrains
{
    using Terrain = UnityEngine.Terrain;
    using TerrainData = UnityEngine.TerrainData;
    using TerrainLayer = UnityEngine.TerrainLayer;

    /// <summary>
    /// Terrain: the authoring operations as callable methods (heights, splat painting, layers, tree / detail prototypes, trees,
    /// neighbors). Each one records the TerrainData for Undo and marks it dirty; settings and TerrainData members (size,
    /// heightmapResolution …) are ordinary writable members.
    /// </summary>
    public sealed class UnityEngine_Terrain_Adapter : RqlTypeAdapter<Terrain>
    {
        public override string Description =>
            "Terrain: members are its settings, terrainData holds size / resolutions / layers / prototypes; call SetHeights, SetHeightGrid, " +
            "PaintLayer, AddLayer, RemoveLayer, AddTreePrototype, AddDetailPrototype, PlaceTrees, PlaceTreesAt, ClearTrees, SetNeighbors";

        private static CallableParameter P<TP>(string name) => new CallableParameter(name, typeof(TP));
        private static CallableParameter P<TP>(string name, TP fallback) => new CallableParameter(name, typeof(TP), true, fallback);

        private static void Add(MemberTable<Terrain> m, string name, CallableParameter[] ps, Type returns, Func<Terrain, object?[], object?> body, string description) =>
            m.Untyped.AddMethod(name, ps, returns, (o, a) => body((Terrain)o, a), description);

        protected override void Members(MemberTable<Terrain> m)
        {
            Add(m, "SetHeights", new[] { P<float>("height"), P("x", 0), P("y", 0), P("w", 0), P("h", 0) }, typeof(int),
                (t, a) => SetHeights(t, (float)a[0]!, (int)a[1]!, (int)a[2]!, (int)a[3]!, (int)a[4]!),
                "fill a heightmap region with one normalized height 0 … 1 (cells: x along world X, y along world Z; w / h <= 0 extend to the edge); returns the cell count");
            Add(m, "SetHeightGrid", new[] { P<float[][]>("rows"), P("x", 0), P("y", 0) }, typeof(int),
                (t, a) => SetHeightGrid(t, (float[][])a[0]!, (int)a[1]!, (int)a[2]!),
                "write normalized heights 0 … 1 from rows [[h, h, …], …] (row = y, column = x) starting at cell (x, y); returns the cell count");
            Add(m, "PaintLayer", new[] { P<int>("layer"), P("strength", 1f), P("x", 0), P("y", 0), P("w", 0), P("h", 0) }, typeof(int),
                (t, a) => PaintLayer(t, (int)a[0]!, (float)a[1]!, (int)a[2]!, (int)a[3]!, (int)a[4]!, (int)a[5]!),
                "paint a terrain layer (index) over an alphamap region with weight strength 0 … 1, the other layers keep their proportions; returns the cell count");
            Add(m, "AddLayer", new[] { P<TerrainLayer>("layer") }, typeof(int), (t, a) => AddLayer(t, (TerrainLayer)a[0]!),
                "append a TerrainLayer asset (ref or unity.asset(\"….terrainlayer\")); returns its index");
            Add(m, "RemoveLayer", new[] { P<int>("index") }, typeof(int), (t, a) => RemoveLayer(t, (int)a[0]!),
                "remove the terrain layer at index; returns the remaining layer count");
            Add(m, "AddTreePrototype", new[] { P<GameObject>("prefab"), P("bendFactor", 0f) }, typeof(int),
                (t, a) => AddTreePrototype(t, (GameObject)a[0]!, (float)a[1]!),
                "add a tree prototype from a prefab asset; returns its prototype index");
            Add(m, "AddDetailPrototype", new[] { P<Object>("asset") }, typeof(int), (t, a) => AddDetailPrototype(t, (Object)a[0]!),
                "add a detail prototype: a Texture2D becomes grass billboards, a prefab a detail mesh; returns its index");
            Add(m, "PlaceTrees", new[] { P<int>("prototype"), P<int>("count"), P("minX", 0f), P("minZ", 0f), P("maxX", 1f), P("maxZ", 1f), P("heightScale", 1f), P("widthScale", 1f) }, typeof(int),
                (t, a) => PlaceTrees(t, (int)a[0]!, Scatter((int)a[1]!, (float)a[2]!, (float)a[3]!, (float)a[4]!, (float)a[5]!), (float)a[6]!, (float)a[7]!),
                "scatter count trees of a prototype at random positions inside the normalized rectangle minX … maxX, minZ … maxZ (0 … 1); returns the number added");
            Add(m, "PlaceTreesAt", new[] { P<Vector2[]>("positions"), P("prototype", 0), P("heightScale", 1f), P("widthScale", 1f) }, typeof(int),
                (t, a) => PlaceTrees(t, (int)a[1]!, (Vector2[])a[0]!, (float)a[2]!, (float)a[3]!),
                "place trees of a prototype at normalized positions [\"(x, z)\", …] (0 … 1 across the terrain); returns the number added");
            Add(m, "ClearTrees", Array.Empty<CallableParameter>(), typeof(int), (t, a) => ClearTrees(t),
                "remove every tree instance; returns how many were removed");
            Add(m, "SetNeighbors", new[] { P<Terrain?>("left", null), P<Terrain?>("top", null), P<Terrain?>("right", null), P<Terrain?>("bottom", null) }, typeof(void),
                (t, a) => { SetNeighbors(t, (Terrain?)a[0], (Terrain?)a[1], (Terrain?)a[2], (Terrain?)a[3]); return null; },
                "connect neighboring terrains for seamless LOD (refs to Terrains or their GameObjects; null = none)");
        }

        // ------------------------------------------------------------ heights

        private static int SetHeights(Terrain t, float height, int x, int y, int w, int h)
        {
            if (height < 0f || height > 1f) throw new ArgumentException($"height {height} must be normalized 0 … 1 (fraction of terrainData.size.y)");
            var data = TerrainOps.Data(t);
            (x, y, w, h) = TerrainOps.Region(x, y, w, h, data.heightmapResolution, "heightmap");
            var heights = new float[h, w];
            for (int row = 0; row < h; row++)
                for (int col = 0; col < w; col++)
                    heights[row, col] = height;
            TerrainOps.Write(t, data, "set heights", () => data.SetHeights(x, y, heights));
            return w * h;
        }

        private static int SetHeightGrid(Terrain t, float[][] rows, int x, int y)
        {
            if (rows.Length == 0 || rows[0] == null || rows[0].Length == 0) throw new ArgumentException("rows is empty");
            int h = rows.Length, w = rows[0].Length;
            var data = TerrainOps.Data(t);
            TerrainOps.Region(x, y, w, h, data.heightmapResolution, "heightmap");
            var heights = new float[h, w];
            for (int row = 0; row < h; row++)
            {
                if (rows[row] == null || rows[row].Length != w) throw new ArgumentException($"row {row} has {rows[row]?.Length ?? 0} values, expected {w} (rows must be rectangular)");
                for (int col = 0; col < w; col++)
                {
                    var v = rows[row][col];
                    if (v < 0f || v > 1f) throw new ArgumentException($"rows[{row}][{col}] = {v} must be normalized 0 … 1");
                    heights[row, col] = v;
                }
            }
            TerrainOps.Write(t, data, "set heights", () => data.SetHeights(x, y, heights));
            return w * h;
        }

        // ------------------------------------------------------------ layers

        private static int PaintLayer(Terrain t, int layer, float strength, int x, int y, int w, int h)
        {
            var data = TerrainOps.Data(t);
            int layers = data.alphamapLayers;
            if (layers == 0) throw new InvalidOperationException("the terrain has no layers: AddLayer first");
            if (layer < 0 || layer >= layers) throw new ArgumentOutOfRangeException(nameof(layer), $"layer {layer} does not exist (0 … {layers - 1})");
            if (strength < 0f || strength > 1f) throw new ArgumentException($"strength {strength} must be 0 … 1");
            (x, y, w, h) = TerrainOps.Region(x, y, w, h, data.alphamapResolution, "alphamap");

            var maps = data.GetAlphamaps(x, y, w, h); // [row, col, layer]
            float rest = 1f - strength;
            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    if (layers == 1) { maps[row, col, 0] = 1f; continue; }
                    float others = 0f;
                    for (int l = 0; l < layers; l++) if (l != layer) others += maps[row, col, l];
                    for (int l = 0; l < layers; l++)
                    {
                        if (l == layer) maps[row, col, l] = strength;
                        else maps[row, col, l] = others > 0f ? maps[row, col, l] * rest / others : rest / (layers - 1);
                    }
                }
            }
            TerrainOps.Write(t, data, "paint layer", () => data.SetAlphamaps(x, y, maps), alphamaps: true);
            return w * h;
        }

        private static int AddLayer(Terrain t, TerrainLayer layer)
        {
            if (layer == null) throw new ArgumentNullException(nameof(layer));
            var data = TerrainOps.Data(t);
            var list = new List<TerrainLayer>(data.terrainLayers ?? Array.Empty<TerrainLayer>());
            if (list.Contains(layer)) throw new ArgumentException($"layer '{layer.name}' is already on the terrain (index {list.IndexOf(layer)})");
            list.Add(layer);
            TerrainOps.Write(t, data, "add layer", () => data.terrainLayers = list.ToArray(), alphamaps: true);
            return list.Count - 1;
        }

        private static int RemoveLayer(Terrain t, int index)
        {
            var data = TerrainOps.Data(t);
            var list = new List<TerrainLayer>(data.terrainLayers ?? Array.Empty<TerrainLayer>());
            if (index < 0 || index >= list.Count) throw new ArgumentOutOfRangeException(nameof(index), $"layer {index} does not exist ({list.Count} layer(s))");
            list.RemoveAt(index);
            var kept = AlphamapsWithout(data, index);
            TerrainOps.Write(t, data, "remove layer", () =>
            {
                data.terrainLayers = list.ToArray();
                if (kept != null) data.SetAlphamaps(0, 0, kept);
            }, alphamaps: true);
            return list.Count;
        }

        /// <summary>
        /// The splat map without the channel of layer <paramref name="index"/> (assigning terrainLayers keeps the channels by
        /// position, so the later layers would take over the removed layer's paint), each cell renormalized; a cell painted
        /// only with the removed layer goes to layer 0, like the Terrain Inspector. Null when no layer remains.
        /// </summary>
        private static float[,,]? AlphamapsWithout(TerrainData data, int index)
        {
            int layers = data.alphamapLayers;
            if (layers <= 1 || index >= layers) return null;
            int w = data.alphamapWidth, h = data.alphamapHeight;
            var maps = data.GetAlphamaps(0, 0, w, h);
            var kept = new float[h, w, layers - 1];
            for (int row = 0; row < h; row++)
                for (int col = 0; col < w; col++)
                {
                    float sum = 0f;
                    for (int l = 0, k = 0; l < layers; l++)
                    {
                        if (l == index) continue;
                        kept[row, col, k] = maps[row, col, l];
                        sum += maps[row, col, l];
                        k++;
                    }
                    if (sum > 1e-4f)
                        for (int k = 0; k < layers - 1; k++) kept[row, col, k] /= sum;
                    else
                        kept[row, col, 0] = 1f;
                }
            return kept;
        }

        // ------------------------------------------------------------ prototypes and trees

        private static int AddTreePrototype(Terrain t, GameObject prefab, float bendFactor)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var data = TerrainOps.Data(t);
            var list = new List<TreePrototype>(data.treePrototypes ?? Array.Empty<TreePrototype>()) { new TreePrototype { prefab = prefab, bendFactor = bendFactor } };
            TerrainOps.Write(t, data, "add tree prototype", () => data.treePrototypes = list.ToArray());
            return list.Count - 1;
        }

        private static int AddDetailPrototype(Terrain t, Object asset)
        {
            var prototype = asset switch
            {
                Texture2D texture when texture != null => new DetailPrototype
                {
                    usePrototypeMesh = false, prototypeTexture = texture, renderMode = DetailRenderMode.GrassBillboard,
                    healthyColor = Color.white, dryColor = new Color(0.8f, 0.7f, 0.4f, 1f),
                },
                GameObject prefab when prefab != null => new DetailPrototype
                {
                    usePrototypeMesh = true, prototype = prefab, renderMode = DetailRenderMode.VertexLit,
                    healthyColor = Color.white, dryColor = new Color(0.8f, 0.7f, 0.4f, 1f),
                },
                _ => throw new ArgumentException($"expected a Texture2D (grass) or a prefab GameObject (mesh), got {(asset == null ? "null" : asset.GetType().Name)}"),
            };
            var data = TerrainOps.Data(t);
            var list = new List<DetailPrototype>(data.detailPrototypes ?? Array.Empty<DetailPrototype>()) { prototype };
            TerrainOps.Write(t, data, "add detail prototype", () => data.detailPrototypes = list.ToArray());
            return list.Count - 1;
        }

        private static Vector2[] Scatter(int count, float minX, float minZ, float maxX, float maxZ)
        {
            if (count < 0) throw new ArgumentException("count must be >= 0");
            if (minX < 0f || minZ < 0f || maxX > 1f || maxZ > 1f || minX > maxX || minZ > maxZ)
                throw new ArgumentException($"the rectangle ({minX}, {minZ}) … ({maxX}, {maxZ}) must lie inside 0 … 1 with min <= max");
            var positions = new Vector2[count];
            for (int i = 0; i < count; i++) positions[i] = new Vector2(UnityEngine.Random.Range(minX, maxX), UnityEngine.Random.Range(minZ, maxZ));
            return positions;
        }

        private static int PlaceTrees(Terrain t, int prototype, Vector2[] positions, float heightScale, float widthScale)
        {
            var data = TerrainOps.Data(t);
            int prototypes = data.treePrototypes?.Length ?? 0;
            if (prototypes == 0) throw new InvalidOperationException("the terrain has no tree prototypes: AddTreePrototype first");
            if (prototype < 0 || prototype >= prototypes) throw new ArgumentOutOfRangeException(nameof(prototype), $"tree prototype {prototype} does not exist (0 … {prototypes - 1})");
            if (heightScale <= 0f || widthScale <= 0f) throw new ArgumentException("heightScale and widthScale must be positive");
            var instances = new List<TreeInstance>(data.treeInstances);
            foreach (var p in positions)
            {
                if (p.x < 0f || p.x > 1f || p.y < 0f || p.y > 1f) throw new ArgumentException($"tree position ({p.x}, {p.y}) must be normalized 0 … 1");
                instances.Add(new TreeInstance
                {
                    prototypeIndex = prototype,
                    position = new Vector3(p.x, 0f, p.y), // y is snapped to the heightmap below
                    heightScale = heightScale,
                    widthScale = widthScale,
                    rotation = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                    color = Color.white,
                    lightmapColor = Color.white,
                });
            }
            TerrainOps.Write(t, data, "place trees", () => data.SetTreeInstances(instances.ToArray(), snapToHeightmap: true));
            return positions.Length;
        }

        private static int ClearTrees(Terrain t)
        {
            var data = TerrainOps.Data(t);
            int removed = data.treeInstanceCount;
            TerrainOps.Write(t, data, "clear trees", () => data.SetTreeInstances(Array.Empty<TreeInstance>(), snapToHeightmap: false));
            return removed;
        }

        // ------------------------------------------------------------ neighbors

        private static void SetNeighbors(Terrain t, Terrain? left, Terrain? top, Terrain? right, Terrain? bottom)
        {
            foreach (var n in new[] { left, top, right, bottom })
                if (n != null && n == t) throw new ArgumentException("a terrain cannot be its own neighbor");
            t.SetNeighbors(left, top, right, bottom);
            Terrain.SetConnectivityDirty();
            t.Flush();
        }
    }
}
