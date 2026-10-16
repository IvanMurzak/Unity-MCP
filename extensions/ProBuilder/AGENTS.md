# AGENTS.md — Unity-AI-ProBuilder

Unity package `com.ivanmurzak.unity.mcp.probuilder` — exposes ProBuilder mesh operations so AI can create and
manipulate 3D geometry through Unity-MCP's RQL tools (no extension MCP tools). An extension of
[AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) (`com.ivanmurzak.unity.mcp`), generated from
`Unity-AI-Tools-Template`; the user-facing tool list is in `README.md`.

## Layout and commands

- `Packages/com.ivanmurzak.unity.mcp.probuilder/` — shipped UPM sources, asmdefs and original tests.
- `Installer/` — preserved installer assets/tests; shared projects materialize them.
- `README.md`, `docs/` — module documentation; `migration-source.json` records provenance.
- Shared commands/workflow: `../../commands/extensions.py`, `../../docs/dev/extensions.md`.
- Use the shared per-version `../../Unity-Tests/` projects; do not create separate extension editor runs.

## Critical invariants

- **Main thread only.** RQL runs on the main thread (Unity-MCP dispatches the RQL tools there).
- **After a mesh edit** call `ProBuilderOps.Rebuild(mesh)` (`ToMesh()` -> `Refresh()` -> `SetDirty`, and the mesh is
  rebuilt again after Undo / Redo — ProBuilder only does that for selected meshes). The engine records the call target
  for Undo; record any other object you change (renderer, transform) with `Undo.RecordObject` first.
- **Validate, never clamp.** Face indices, edges (must be edges of the mesh), amounts and counts are checked before the
  mesh changes; invalid input throws with the valid range.
- **Selections** are face index lists and `[a, b]` vertex pairs; `ProBuilderOps.Facing` (dot-product threshold 0.7,
  local space) backs `probuilder.facing` and the face views' `direction`.
- **Name clash.** Inside `com.IvanMurzak.Unity.MCP.*` namespaces, alias ProBuilder types inside the namespace block
  (`using ProBuilderMesh = UnityEngine.ProBuilder.ProBuilderMesh;`).
- Every C# file starts with the copyright box header and `#nullable enable`.
