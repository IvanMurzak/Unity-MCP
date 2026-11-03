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
using System.IO;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using Rql;
using Rql.Execution;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Tilemaps
{
    using Tilemap = UnityEngine.Tilemaps.Tilemap;
    using TilemapRenderer = UnityEngine.Tilemaps.TilemapRenderer;
    using Tile = UnityEngine.Tilemaps.Tile;

    /// <summary>
    /// A fresh scene with one tilemap ("Ground" under the Grid "World") per test. Assets a test creates go to <see cref="Folder"/>,
    /// which is deleted afterwards.
    /// </summary>
    public abstract class TilemapTestBase
    {
        protected const string Folder = "Assets/RqlTilemapTests";
        protected const string T = "objects | get components | where $ is Tilemap";

        protected Tilemap Ground = null!;
        protected Grid World = null!;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RqlUnity.Reset();
            var grid = new GameObject("World");
            World = grid.AddComponent<Grid>();
            var go = new GameObject("Ground");
            go.transform.SetParent(grid.transform, false);
            Ground = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>();
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
        }

        protected static QueryResult Q(string rql) => RqlUnity.Engine.Execute(rql);

        protected static QueryResult M(string rql) => RqlUnity.Engine.Execute(rql, null, new QueryOptions { AllowMutation = true });

        /// <summary>A query that must succeed.</summary>
        protected static QueryResult Read(string rql)
        {
            var r = Q(rql);
            Assert.IsTrue(r.Ok, Table(r));
            return r;
        }

        /// <summary>A mutation that must succeed.</summary>
        protected static QueryResult Ok(string rql)
        {
            var r = M(rql);
            Assert.IsTrue(r.Ok, Table(r));
            return r;
        }

        /// <summary>A mutation that must fail with a message containing <paramref name="expected"/>.</summary>
        protected static void Fails(string rql, string expected)
        {
            var r = M(rql);
            Assert.IsFalse(r.Ok, Table(r));
            StringAssert.Contains(expected, Table(r));
        }

        protected static string Table(QueryResult r) => AgentFormat.Render(r, OutputFormat.Table);

        protected static string Cell(QueryResult r, int column = 0)
        {
            Assert.IsTrue(r.Ok, Table(r));
            return r.Rows![0][column]?.ToJsonString().Trim('"') ?? "null";
        }

        /// <summary>The <c>result</c> column of a call receipt.</summary>
        protected static string Result(QueryResult r) => Cell(r, 2);

        protected static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "RqlTilemapTests");
        }

        /// <summary>A Tile asset at <see cref="Folder"/>/<paramref name="name"/>.asset.</summary>
        protected static Tile TileAsset(string name = "Grass")
        {
            EnsureFolder();
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            AssetDatabase.CreateAsset(tile, TilePath(name));
            return tile;
        }

        protected static string TilePath(string name) => $"{Folder}/{name}.asset";

        /// <summary>
        /// A PNG at <see cref="Folder"/>/<paramref name="name"/>.png imported as a Sprite; with <paramref name="sheet"/> it is cut
        /// into two sprites "<paramref name="name"/>_0" and "<paramref name="name"/>_1".
        /// </summary>
        protected static string SpriteTexture(string name = "grass", bool asSprite = true, bool sheet = false)
        {
            EnsureFolder();
            var path = $"{Folder}/{name}.png";
            var texture = new Texture2D(8, 4);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (asSprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                if (sheet)
                {
                    importer.spriteImportMode = SpriteImportMode.Multiple;
#pragma warning disable CS0618 // spritesheet is obsolete in newer Unity versions but still applied by the importer
                    importer.spritesheet = new[]
                    {
                        new SpriteMetaData { name = name + "_0", rect = new Rect(0, 0, 4, 4), pivot = new Vector2(0.5f, 0.5f) },
                        new SpriteMetaData { name = name + "_1", rect = new Rect(4, 0, 4, 4), pivot = new Vector2(0.5f, 0.5f) },
                    };
#pragma warning restore CS0618
                }
                else importer.spriteImportMode = SpriteImportMode.Single;
            }
            else importer.textureType = TextureImporterType.Default;
            importer.SaveAndReimport();
            return path;
        }
    }
}
