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
using System.IO;
using Rql;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Tilemaps
{
    using Tilemap = UnityEngine.Tilemaps.Tilemap;
    using TilemapRenderer = UnityEngine.Tilemaps.TilemapRenderer;
    using Tile = UnityEngine.Tilemaps.Tile;
    using RuleTile = UnityEngine.RuleTile;

    /// <summary>
    /// The <c>tilemap</c> source: tilemap operations without an owner object (creating tilemaps and tile assets), e.g.
    /// <c>tilemap | call CreateTilemap("Ground")</c>. Painting is done with methods of the Tilemap component
    /// (<see cref="UnityEngine_Tilemaps_Tilemap_Adapter"/>).
    /// </summary>
    public sealed class TilemapEditorApi
    {
        public static readonly TilemapEditorApi Instance = new TilemapEditorApi();

        [RqlCallable("create a Tilemap GameObject (Tilemap + TilemapRenderer); when parent has a Grid the tilemap becomes another " +
                     "layer of that Grid, otherwise a new Grid (gridName, cellSize default (1, 1, 0)) is created under parent (or at the " +
                     "scene root); returns the tilemap GameObject (undoable)")]
        public GameObject CreateTilemap(string name = "Tilemap", GameObject? parent = null, Vector3? cellSize = null, string gridName = "Grid")
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name is empty");
            if (cellSize.HasValue && (cellSize.Value.x <= 0f || cellSize.Value.y <= 0f || cellSize.Value.z < 0f))
                throw new ArgumentException($"cellSize must be positive in x and y, got {cellSize.Value}");

            GameObject? createdRoot = null;
            Grid? grid = parent != null ? parent.GetComponent<Grid>() : null;
            if (grid == null)
            {
                if (string.IsNullOrWhiteSpace(gridName)) throw new ArgumentException("gridName is empty");
                var gridGo = new GameObject(gridName);
                if (parent != null) gridGo.transform.SetParent(parent.transform, false);
                grid = gridGo.AddComponent<Grid>();
                grid.cellSize = cellSize ?? new Vector3(1f, 1f, 0f);
                createdRoot = gridGo;
            }
            else if (cellSize.HasValue)
            {
                Undo.RecordObject(grid, "RQL tilemap cell size");
                grid.cellSize = cellSize.Value;
            }

            var go = new GameObject(name);
            go.transform.SetParent(grid.transform, false);
            go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>();
            Undo.RegisterCreatedObjectUndo(createdRoot ?? go, "RQL create tilemap " + name);
            return go;
        }

        [RqlCallable("create a Tile asset at path (\"Assets/…/Grass.asset\") showing sprite (a Sprite, its texture " +
                     "unity.asset(\"Assets/…/grass.png\"), its path, or \"….png#spriteName\"), tinted color, with colliderType None / Sprite / Grid; " +
                     "returns the Tile")]
        public Tile CreateTile(string path, Sprite? sprite = null, Color? color = null, Tile.ColliderType colliderType = Tile.ColliderType.Sprite)
        {
            path = TilemapOps.NewAssetPath(path, ".asset");
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = Path.GetFileNameWithoutExtension(path);
            tile.sprite = sprite;
            tile.color = color ?? Color.white;
            tile.colliderType = colliderType;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        [RqlCallable("create a RuleTile asset at path (\"Assets/…/Ground.asset\") with an optional default sprite; add rules with " +
                     "call AddRule(sprite, \"TTT/T.T/NNN\") on it; returns the RuleTile")]
        public RuleTile CreateRuleTile(string path, Sprite? defaultSprite = null, Tile.ColliderType colliderType = Tile.ColliderType.Sprite)
        {
            path = TilemapOps.NewAssetPath(path, ".asset");
            var tile = ScriptableObject.CreateInstance<RuleTile>();
            tile.name = Path.GetFileNameWithoutExtension(path);
            tile.m_DefaultSprite = defaultSprite;
            tile.m_DefaultColliderType = colliderType;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }
    }
}
