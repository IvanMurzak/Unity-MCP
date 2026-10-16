# AGENTS.md — Unity-AI-Splines

Unity package `com.ivanmurzak.unity.mcp.splines` — makes **Unity Splines** (`com.unity.splines`) available to
Unity-MCP's RQL tools (`rql-query` / `rql-mutate`; no extension MCP tools): the `splines`
source (containers, `CreateContainer`), SplineContainer methods (add / insert / remove knots and splines,
evaluate), writable `splines[i].knots[j]` views (position, tangents, rotation, tangent mode, closed), and
Unity.Mathematics literals. An extension of
[AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) (`com.ivanmurzak.unity.mcp`), generated from
`Unity-AI-Tools-Template`; the user-facing tool list is in `README.md`.

## Layout and commands

- `Packages/com.ivanmurzak.unity.mcp.splines/` — shipped UPM sources, asmdefs and original tests.
- `Installer/` — preserved installer assets/tests; shared projects materialize them.
- `README.md`, `docs/` — module documentation; `migration-source.json` records provenance.
- Shared commands/workflow: `../../commands/extensions.py`, `../../docs/dev/extensions.md`.
- Use the shared per-version `../../Unity-Tests/` projects; do not create separate extension editor runs.

## Critical invariants

- **RQL surface.** Everything is registered by `SplinesModule` (`IRqlModule`, Order 100, found through
  TypeCache) and documented in its `AddGuide("Splines", …)` text, which is rendered into the `unity-rql` skill:
  keep the guide examples runnable (`SplinesModuleTests.EveryGuideExampleRuns` executes them in order).
- **Undo.** Calls on a SplineContainer are recorded by the engine (the container is the owner). Writes through
  `SplineView` / `KnotView` have a plain owner, so their setters record the container themselves (`SplineEdit`).
- **Spline data model.** A `SplineContainer` holds one or more `Spline`s; each `Spline` is a list of
  `BezierKnot`s (Position, TangentIn, TangentOut, Rotation) plus a `Closed` flag and per-knot `TangentMode`.
  Edit knots through `Spline`'s `Add` / `Insert` / `RemoveAt` / `SetTangentMode` and the indexer, never by raw
  struct mutation, so the spline rebuilds its cached curve.
- Every C# file starts with the copyright box header and `#nullable enable`.
