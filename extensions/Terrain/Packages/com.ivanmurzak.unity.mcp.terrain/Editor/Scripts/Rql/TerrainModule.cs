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
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Terrains
{
    /// <summary>
    /// The extension's RQL module (discovered through TypeCache when this package is installed): the <c>terrain</c> source
    /// (create terrains and TerrainLayer assets), Terrain authoring methods, validated TerrainData members, height sampling
    /// functions and the agent guide rendered into the unity-rql skill.
    /// </summary>
    public sealed class TerrainModule : IRqlModule
    {
        public const string GuideTopic = "Terrain";

        private const string T = "objects | get components | where $ is Terrain";

        private const string Guide =
@"Terrains are components: `" + T + @"`. Settings are members of the Terrain, the data
(size, resolutions, layers, prototypes) lives in its `terrainData`. Heights are normalized 0 … 1 (fraction of
`terrainData.size.y`); heightmap / alphamap cells are `x` along world X and `y` along world Z.

```
" + T + @" | select $ref gameObject.name terrainData.size terrainData.heightmapResolution terrainData.terrainLayers.name
terrain | call Create(""Island"", ""Assets/Terrain/Island.asset"", 257, ""(500, 120, 500)"", ""(0, 0, 0)"")   name, dataPath, heightmapResolution, size, position
" + T + @" | set terrainData.size = ""(800, 200, 800)""
" + T + @" | set terrainData.heightmapResolution = 513, terrainData.alphamapResolution = 512
" + T + @" | set heightmapPixelError = 2, drawTreesAndFoliage = false, materialTemplate = unity.asset(""Assets/Mats/Terrain.mat"")
```

Heights (`call` needs rql-mutate; sampling functions work in rql-query):

```
" + T + @" | call SetHeights(0.25)                                  flatten everything to 25 %
" + T + @" | call SetHeights(0.6, 100, 100, 32, 32)                 height, x, y, w, h (w / h 0 = to the edge)
" + T + @" | call SetHeightGrid([[0.1, 0.2], [0.3, 0.4]], 10, 20)    explicit rows (row = y) starting at cell (10, 20)
" + T + @" | select h=(terrain.heights($, 10, 20, 2, 2))            normalized rows, at most 20 × 20
" + T + @" | select s=(terrain.heightStats($))                       {min, max, average, cells}; region: ($, x, y, w, h)
" + T + @" | select y=(terrain.heightAt($, 250, 250))                world surface Y at world X / Z
```

Layers (textures), trees and details:

```
terrain | call CreateLayer(""Assets/Terrain/Grass.terrainlayer"", unity.asset(""Assets/Textures/grass.png""), ""(10, 10)"")
" + T + @" | call AddLayer(unity.asset(""Assets/Terrain/Grass.terrainlayer""))   returns the layer index
" + T + @" | call PaintLayer(1, 1.0, 0, 0, 64, 64)                  layer, strength, x, y, w, h on the alphamap
" + T + @" | call RemoveLayer(0)
" + T + @" | set terrainData.terrainLayers[0].tileSize = ""(20, 20)""
" + T + @" | call AddTreePrototype(unity.asset(""Assets/Trees/Oak.prefab""))    returns the prototype index
" + T + @" | call PlaceTrees(0, 200, 0.1, 0.1, 0.5, 0.5)            prototype, count, minX, minZ, maxX, maxZ (normalized), heightScale?, widthScale?
" + T + @" | call PlaceTreesAt([""(0.5, 0.5)"", ""(0.25, 0.75)""], 0)     normalized (x, z) positions, prototype
" + T + @" | call ClearTrees()
" + T + @" | select terrainData.treeInstanceCount terrainData.treePrototypes.prefab
" + T + @" | set terrainData.treePrototypes[0] = {prefab: unity.asset(""Assets/Trees/Oak.prefab""), bendFactor: 0.3}   prototypes are replaced whole
" + T + @" | set terrainData.treeInstances[0].heightScale = 2
" + T + @" | call AddDetailPrototype(unity.asset(""Assets/Textures/grass_blade.png""))   texture = grass billboard, prefab = detail mesh
" + T + @" | get terrainData | call SetDetailResolution(1024, 32)
" + T + @" | where gameObject.name == ""Center"" | call SetNeighbors(ref(""Terrain#…""), null, ref(""Terrain#…""), null)   left, top, right, bottom
```

`terrainData.treeInstances` is read only when named (`select terrainData.treeInstances[0]`). Tree / detail prototype
members are read-only (TerrainData returns copies): replace the element with an object literal. Invalid resolutions, regions
outside the map and out-of-range indices fail without changing anything. Heights, paint, layers and trees are undoable;
created assets (TerrainData, TerrainLayer) are not removed by Undo. Neighbors are not saved with the scene (Unity
reconnects terrains with `allowAutoConnect` and the same `groupingID`).";

        public int Order => 100;

        public void Configure(RqlModuleContext ctx)
        {
            ctx.AddSource("terrain", () => TerrainEditorApi.Instance,
                   "terrain operations without an owner: Create (Terrain GameObject + TerrainData asset), CreateLayer (TerrainLayer asset)")
               .AddFunction("terrain.heights", TerrainFunctions.Heights, TerrainFunctions.HeightsDescription)
               .AddFunction("terrain.heightStats", TerrainFunctions.HeightStats, TerrainFunctions.HeightStatsDescription)
               .AddFunction("terrain.heightAt", TerrainFunctions.HeightAt, TerrainFunctions.HeightAtDescription)
               .AddAdapter(new UnityEngine_Terrain_Adapter())
               .AddAdapter(new UnityEngine_TerrainData_Adapter())
               .AddAdapter(new UnityEngine_TerrainPrototype_Adapter(typeof(UnityEngine.TreePrototype)))
               .AddAdapter(new UnityEngine_TerrainPrototype_Adapter(typeof(UnityEngine.DetailPrototype)))
               .AddGuide(GuideTopic, Guide);
        }
    }
}
