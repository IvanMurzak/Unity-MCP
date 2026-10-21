# AGENTS.md — Unity-AI-Tilemap

Unity package `com.ivanmurzak.unity.mcp.tilemap` — wraps Unity's built-in **Tilemap** module
(`com.unity.modules.tilemap`) plus **2D Tilemap Extras** (`com.unity.2d.tilemap.extras`, for RuleTiles) available
to Unity-MCP's RQL tools (`rql-query` / `rql-mutate`; no extension MCP tools): the `tilemap` source
(`CreateTilemap`, `CreateTile`, `CreateRuleTile`), Tilemap painting methods (SetTile, BoxFill, FloodFill, …),
RuleTile `AddRule` / `ClearRules` and the read-only `tilemap.*` cell functions. An extension of
[AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) (`com.ivanmurzak.unity.mcp`), generated from
`Unity-AI-Tools-Template`; the user-facing tool list is in `README.md`.

## Layout and commands

- `Packages/com.ivanmurzak.unity.mcp.tilemap/` — shipped UPM sources, asmdefs and original tests.
- `Installer/` — preserved installer assets/tests; shared projects materialize them.
- `README.md`, `docs/` — module documentation; `migration-source.json` records provenance.
- Shared commands/workflow: `../../commands/extensions.py`, `../../docs/dev/extensions.md`.
- Use the shared per-version `../../Unity-Tests/` projects; do not create separate extension editor runs.

## Critical invariants

- **RQL surface.** Everything is registered by `TilemapModule` (`IRqlModule`, Order 100, found through
  TypeCache) and documented in its `AddGuide("Tilemap", …)` text, which is rendered into the `unity-rql` skill:
  keep the guide examples in sync with the adapters and functions.
- **Undo.** Every tile write records the whole Tilemap first (`TilemapOps.Record`); created assets are not undone.
- **Limits.** Writes are capped at `TilemapOps.MaxCells` cells per call, scans at `MaxScanCells`; function
  results at `TilemapFunctions.MaxList` items (RQL truncates longer lists). Function errors are returned as
  `RqlError`, not thrown.
- Every C# file starts with the copyright box header and `#nullable enable`.
