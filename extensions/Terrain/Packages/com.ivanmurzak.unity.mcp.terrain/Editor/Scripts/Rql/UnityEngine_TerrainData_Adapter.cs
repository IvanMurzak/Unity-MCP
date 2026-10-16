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
    using TerrainData = UnityEngine.TerrainData;

    /// <summary>
    /// TerrainData: resolutions are validated instead of silently rounded by Unity; the prototype / layer / tree arrays are copies,
    /// so element writes (<c>set terrainData.treeInstances[0].heightScale = 2</c>, <c>set terrainData.treePrototypes[0] = {…}</c>)
    /// are assigned back; the tree instance array
    /// (possibly tens of thousands of entries) and the holes texture (created on first read) are read only when named.
    /// </summary>
    public sealed class UnityEngine_TerrainData_Adapter : RqlTypeAdapter<TerrainData>
    {
        public override string Description =>
            "TerrainData: size (width, height, length), heightmapResolution (2^n + 1), alphamapResolution (power of two), terrainLayers, " +
            "treePrototypes, detailPrototypes, treeInstanceCount; treeInstances only when named; call SetDetailResolution(resolution, perPatch)";

        protected override void Members(MemberTable<TerrainData> m)
        {
            // Unity keeps the cell size when the resolution changes (the world size scales with it); RQL keeps the world size,
            // so `set terrainData.size = …, terrainData.heightmapResolution = …` means the same in either order.
            m.Add("heightmapResolution", d => d.heightmapResolution, (d, v) => { var size = d.size; d.heightmapResolution = v; d.size = size; },
                    (d, v) => TerrainOps.IsValidHeightmapResolution(v) ? null : TerrainOps.HeightmapResolutionRule(v),
                    "heightmap samples per side, 2^n + 1 (33 … 4097); changing it resamples the heights and keeps size")
             .Add("alphamapResolution", d => d.alphamapResolution, (d, v) => d.alphamapResolution = v,
                    (d, v) => TerrainOps.IsValidAlphamapResolution(v) ? null
                        : $"alphamapResolution must be a power of two between {TerrainOps.MinAlphamapResolution} and {TerrainOps.MaxAlphamapResolution}",
                    "splat map texels per side, a power of two (16 … 4096)")
             .CopyOnRead("terrainLayers", "treePrototypes", "detailPrototypes", "treeInstances")
             .ExplicitOnly("treeInstances", "holesTexture")
             .Expose("SetDetailResolution", "detail map resolution and resolution per patch, e.g. SetDetailResolution(1024, 32)");
        }
    }
}
