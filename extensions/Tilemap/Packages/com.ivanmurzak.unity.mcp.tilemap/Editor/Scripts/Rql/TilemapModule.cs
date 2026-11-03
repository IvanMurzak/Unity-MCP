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
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Tilemaps
{
    /// <summary>
    /// The extension's RQL module (discovered through TypeCache when this package is installed): the <c>tilemap</c> source
    /// (create tilemaps, Tile and RuleTile assets), Tilemap painting methods, cell lookup functions, the tile / sprite / bounds
    /// adapters and the agent guide rendered into the unity-rql skill.
    /// </summary>
    public sealed class TilemapModule : IRqlModule
    {
        public const string GuideTopic = "Tilemap";

        private const string T = "objects | get components | where $ is Tilemap";

        private const string Guide =
@"Tilemaps are components on children of a `Grid`: `" + T + @"`. Cells are `""(x, y, z)""` (z = 0 for 2D);
tile arguments take an asset path (`""Assets/Tiles/Grass.asset""`), a ref or `unity.asset(…)`; sprite arguments also take
the texture (`unity.asset(""Assets/Art/grass.png"")`), its path or `""Assets/Art/sheet.png#grass_3""` (one sprite of a sheet).

```
" + T + @" | select $ref gameObject.name tileCount usedTiles cellBounds orientation
tilemap | call CreateTilemap(""Ground"")                                    name, parent?, cellSize?, gridName? (new Grid + Tilemap)
tilemap | call CreateTilemap(""Walls"", ref(""GameObject#…""))                  parent with a Grid: another layer of that Grid
tilemap | call CreateTile(""Assets/Tiles/Grass.asset"", ""Assets/Art/grass.png"", ""#FFFFFF"", ""Grid"")   path, sprite?, color?, colliderType?
tilemap | call CreateRuleTile(""Assets/Tiles/Ground.asset"", ""Assets/Art/ground.png#ground_4"")        path, defaultSprite?, colliderType?
```

Painting (`call` needs rql-mutate; undoable):

```
" + T + @" | where gameObject.name == ""Ground"" | call SetTile(""(0, 0, 0)"", ""Assets/Tiles/Grass.asset"")
" + T + @" | call SetTile(""(3, 1, 0)"", null)                                erase one cell
" + T + @" | call SetTiles([""(0, 0, 0)"", ""(1, 0, 0)"", ""(2, 0, 0)""], ""Assets/Tiles/Grass.asset"")
" + T + @" | call BoxFill(""Assets/Tiles/Grass.asset"", -5, -3, 5, 3)          tile, minX, minY, maxX, maxY, z? (corners inclusive)
" + T + @" | call ClearBox(0, 0, 2, 2)                                       returns how many tiles were removed
" + T + @" | call FloodFill(""(1, 1, 0)"", ""Assets/Tiles/Water.asset"")
" + T + @" | call ClearAllTiles()
" + T + @" | call SetColor(""(0, 0, 0)"", ""#FF8080"")                           tint one painted cell
" + T + @" | call SetTransform(""(0, 0, 0)"", 90, ""(1, 1, 1)"", true, false)     rotation°, scale?, flipX?, flipY?
" + T + @" | call SetColliderType(""(0, 0, 0)"", ""None"")                       None / Sprite / Grid
" + T + @" | set orientation = ""XZ"", tileAnchor = ""(0.5, 0.5, 0)"", color = ""(1, 1, 1, 0.5)""
" + T + @" | call CompressBounds()                                           shrink cellBounds to the painted cells
objects | get components | where $ is Grid | set cellSize = ""(1, 0.5, 0)"", cellLayout = ""Isometric""
objects | get components | where $ is TilemapRenderer | set sortingOrder = 2, mode = ""Individual""
```

Reading cells (functions work in rql-query):

```
" + T + @" | select t=(tilemap.tileAt($, ""(0, 0, 0)""))                     the tile asset or null
" + T + @" | select c=(tilemap.cell($, ""(0, 0, 0)""))                        {cell, tile, path, sprite, color, colliderType, flags, transform}
" + T + @" | select rows=(tilemap.tiles($, 0, 0, 9, 9))                       tile names, top row (maxY) first, at most 20 × 20
" + T + @" | select n=(tilemap.count($, ""Grass""))                            occupied cells of a tile (name, path or asset); region: ($, tile, minX, minY, maxX, maxY)
" + T + @" | select cells=(tilemap.cells($, ""Assets/Tiles/Grass.asset""))      first 20 cells holding that tile
" + T + @" | select c=(tilemap.worldToCell($, ""(2.5, 1.2, 0)"")) w=(tilemap.cellToWorld($, ""(2, 1, 0)""))
```

Tile assets are ordinary objects: `unity.assets(""t:TileBase"") | select $ref name`,
`unity.asset(""Assets/Tiles/Grass.asset"") | set sprite = ""Assets/Art/grass2.png"", colliderType = ""Grid""`. RuleTile rules:

```
unity.asset(""Assets/Tiles/Ground.asset"") | call AddRule(""Assets/Art/ground.png#ground_1"", ""NNN/T.T/TTT"")   sprite, pattern, colliderType?
unity.asset(""Assets/Tiles/Ground.asset"") | call ClearRules()
" + T + @" | call RefreshAllTiles()                                          repaint after editing rules
```

A pattern lists the 8 neighbors in rows top to bottom (center ignored): `T` = this tile, `N` = not this tile, `.` = any;
rules are checked in order and the first match wins (`""...""` = always). Methods on empty cells (SetColor, SetTransform,
SetColliderType), regions over 1 048 576 cells and bad paths fail without changing anything. `cellBounds.max` is exclusive.
Created assets are not removed by Undo.";

        public int Order => 100;

        public void Configure(RqlModuleContext ctx)
        {
            ctx.AddSource("tilemap", () => TilemapEditorApi.Instance,
                   "tilemap operations without an owner: CreateTilemap (Grid + Tilemap GameObject), CreateTile, CreateRuleTile (tile assets)")
               .AddFunction("tilemap.tileAt", TilemapFunctions.TileAt, TilemapFunctions.TileAtDescription)
               .AddFunction("tilemap.cell", TilemapFunctions.Cell, TilemapFunctions.CellDescription)
               .AddFunction("tilemap.tiles", TilemapFunctions.Tiles, TilemapFunctions.TilesDescription)
               .AddFunction("tilemap.count", TilemapFunctions.Count, TilemapFunctions.CountDescription)
               .AddFunction("tilemap.cells", TilemapFunctions.Cells, TilemapFunctions.CellsDescription)
               .AddFunction("tilemap.worldToCell", TilemapFunctions.WorldToCell, TilemapFunctions.WorldToCellDescription)
               .AddFunction("tilemap.cellToWorld", TilemapFunctions.CellToWorld, TilemapFunctions.CellToWorldDescription)
               .AddAdapter(new UnityEngine_Tilemaps_Tilemap_Adapter())
               .AddAdapter(new UnityEngine_RuleTile_Adapter())
               .AddGuide(GuideTopic, Guide);
        }
    }
}
