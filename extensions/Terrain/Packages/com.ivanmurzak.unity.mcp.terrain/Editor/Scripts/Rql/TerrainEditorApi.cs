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
using Rql;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Terrains
{
    using Terrain = UnityEngine.Terrain;
    using TerrainData = UnityEngine.TerrainData;
    using TerrainLayer = UnityEngine.TerrainLayer;

    /// <summary>
    /// The <c>terrain</c> source: terrain operations without an owner object (creating terrains and TerrainLayer assets), e.g.
    /// <c>terrain | call Create("Island", null, 257, "(500, 120, 500)")</c>. Operations on an existing terrain are methods of the
    /// Terrain component (<see cref="UnityEngine_Terrain_Adapter"/>).
    /// </summary>
    public sealed class TerrainEditorApi
    {
        public static readonly TerrainEditorApi Instance = new TerrainEditorApi();

        [RqlCallable("create a Terrain GameObject (Terrain + TerrainCollider) backed by a new TerrainData asset; " +
                     "dataPath defaults to Assets/TerrainData_<name>.asset, heightmapResolution must be 2^n + 1 (33 … 4097), " +
                     "size is (width, height, length) in world units; returns the GameObject (undoable; the asset stays)")]
        public GameObject Create(string name = "Terrain", string? dataPath = null, int heightmapResolution = 513, Vector3? size = null, Vector3? position = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name is empty");
            if (!TerrainOps.IsValidHeightmapResolution(heightmapResolution)) throw new ArgumentException(TerrainOps.HeightmapResolutionRule(heightmapResolution));
            var s = size ?? new Vector3(1000f, 600f, 1000f);
            if (s.x <= 0 || s.y <= 0 || s.z <= 0) throw new ArgumentException($"size must be positive, got ({s.x}, {s.y}, {s.z})");

            var path = dataPath == null
                ? AssetDatabase.GenerateUniqueAssetPath($"Assets/TerrainData_{name}.asset")
                : TerrainOps.NewAssetPath(dataPath, ".asset");
            RqlEditorApi.Instance.CreateFolder(Path.GetDirectoryName(path)!);

            // heightmapResolution before size: changing the resolution keeps the size, the other way round it rescales.
            var data = new TerrainData { heightmapResolution = heightmapResolution, size = s, name = Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(data, path);

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = name;
            if (position.HasValue) go.transform.position = position.Value;
            Undo.RegisterCreatedObjectUndo(go, "RQL create terrain " + name);
            return go;
        }

        [RqlCallable("create a TerrainLayer asset at path (\"Assets/…/X.terrainlayer\") from an optional diffuse texture " +
                     "(unity.asset(\"Assets/…/grass.png\")) and tile size \"(x, y)\"; returns it — add it with Terrain.AddLayer")]
        public TerrainLayer CreateLayer(string path, Texture2D? diffuse = null, Vector2? tileSize = null)
        {
            path = TerrainOps.NewAssetPath(path, ".terrainlayer");
            var layer = new TerrainLayer
            {
                diffuseTexture = diffuse,
                tileSize = tileSize ?? new Vector2(15f, 15f),
                name = Path.GetFileNameWithoutExtension(path),
            };
            AssetDatabase.CreateAsset(layer, path);
            return layer;
        }
    }
}
