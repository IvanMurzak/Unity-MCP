# AGENTS.md — Unity-AI-InputSystem

Unity package `com.ivanmurzak.unity.mcp.inputsystem` — makes Unity's **Input System** (`com.unity.inputsystem`)
available to Unity-MCP's RQL tools (`rql-query` / `rql-mutate`; no extension MCP tools): the `input` source
(assets, layouts, interactions, processors, `CreateAsset`), and InputActionAsset maps / actions / bindings / control
schemes as writable views with add / remove methods. An extension of [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP)
(`com.ivanmurzak.unity.mcp`), generated from `Unity-AI-Tools-Template`; the user-facing tool list is in
`README.md`.

## Layout and commands

- `Packages/com.ivanmurzak.unity.mcp.inputsystem/` — shipped UPM sources, asmdefs and original tests.
- `Installer/` — preserved installer assets/tests; shared projects materialize them.
- `README.md`, `docs/` — module documentation; `migration-source.json` records provenance.
- Shared commands/workflow: `../../commands/extensions.py`, `../../docs/dev/extensions.md`.
- Use the shared per-version `../../Unity-Tests/` projects; do not create separate extension editor runs.

## Critical invariants

- **RQL shape (branch `experiment/rql`).** Agents use Unity-MCP's `rql-query` / `rql-mutate`; no `inputsystem-*`
  tools remain. Assets and their parts are views addressed by id (`ActionMapView`, `ActionView`, `BindingView`;
  `ControlSchemeView` by name) because a re-import replaces the map / action objects. Validation lives in
  `InputValidation`; the guide in `InputSystemModule` must keep one runnable example per operation (a test runs them).
- **Asset-backed.** Every write goes through `InputAssetStore.Edit` (Undo record, change, JSON to the
  `.inputactions` file, re-import); Undo / Redo rewrite the file through `InputAssetStore.Resync`. Serialize with
  `InputAssetStore.ToJson`, never raw `ToJson()`: Input System 1.x throws on an asset with no action maps.
- Every C# file starts with the copyright box header and `#nullable enable`.
