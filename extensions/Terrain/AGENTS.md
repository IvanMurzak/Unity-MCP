# AGENTS.md — Unity-AI-Terrain

Unity package `com.ivanmurzak.unity.mcp.terrain` — wraps Unity's built-in **Terrain** modules
(`com.unity.modules.terrain` + `com.unity.modules.terrainphysics`) available to Unity-MCP's RQL tools (`rql-query` /
`rql-mutate`; no extension MCP tools): the `terrain` source (Create, CreateLayer), Terrain methods for heights, splat
painting, layers, prototypes, trees and neighbors, validated TerrainData members, and `terrain.*` height sampling
functions. An extension of
[AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) (`com.ivanmurzak.unity.mcp`), generated from
`Unity-AI-Tools-Template`; the user-facing tool list is in `README.md`.

## Layout and commands

- `Packages/com.ivanmurzak.unity.mcp.terrain/` — shipped UPM sources, asmdefs and original tests.
- `Installer/` — preserved installer assets/tests; shared projects materialize them.
- `README.md`, `docs/` — module documentation; `migration-source.json` records provenance.
- Shared commands/workflow: `../../commands/extensions.py`, `../../docs/dev/extensions.md`.
- Use the shared per-version `../../Unity-Tests/` projects; do not create separate extension editor runs.

## Critical invariants

- **RQL surface.** Everything is registered by `TerrainModule` (`IRqlModule`, Order 100, found through TypeCache) and
  documented in its `AddGuide("Terrain", …)` text, which is rendered into the `unity-rql` skill.
- **Undo.** The engine records the call target (the Terrain); heights, splat maps, layers and trees live in the
  TerrainData asset, so every Terrain method writes through `TerrainOps.Write` (records the TerrainData, and the splat
  textures when painting, then marks both dirty).
- **Copies.** TerrainData returns copies of its layer / prototype / tree arrays: the TerrainData adapter assigns element
  writes back (`CopyOnRead`), and prototype members are read-only (an element is replaced whole from an object literal).
- **Terrain is a built-in module**: it ships with and version-locks to the Editor; no extra registry package
  or asmdef reference beyond the base UnityEngine assemblies.
- **Alias `UnityEngine.Terrain`** inside the `…Rql.Terrains` namespace block: short names may resolve to namespaces.
- Every C# file starts with the copyright box header and `#nullable enable`.
