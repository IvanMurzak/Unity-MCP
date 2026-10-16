# AGENTS.md — Unity-AI-Animation

Unity package `com.ivanmurzak.unity.mcp.animation` — makes Unity `AnimationClip` / `AnimatorController` assets and the
`Animator` component available to Unity-MCP's RQL tools (`rql-query` / `rql-mutate`; no extension MCP
tools): the `animation` source (CreateClip, CreateController, animators), clip curve / key / event / settings views and
methods, controller layer / parameter views and state-machine methods, transition conditions. An extension of
[AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) (`com.ivanmurzak.unity.mcp`), generated from
`Unity-AI-Tools-Template`; the former tool → RQL table is in `README.md`.

## Layout and commands

- `Packages/com.ivanmurzak.unity.mcp.animation/` — shipped UPM sources, asmdefs and original tests.
- `Installer/` — preserved installer assets/tests; shared projects materialize them.
- `README.md`, `docs/` — module documentation; `migration-source.json` records provenance.
- Shared commands/workflow: `../../commands/extensions.py`, `../../docs/dev/extensions.md`.
- Use the shared per-version `../../Unity-Tests/` projects; do not create separate extension editor runs.

## Critical invariants

- **RQL surface.** Everything is registered by `AnimationModule` (`IRqlModule`, Order 100, found through TypeCache) and
  documented in its `AddGuide("Animation", …)` text, which is rendered into the `unity-rql` skill: keep the guide examples
  runnable (`AnimationModuleTests.EveryGuideExampleRuns` executes them in order).
- **Copies.** Unity returns copies for clip curves (`AnimationUtility.GetEditorCurve`), events, clip settings, and the
  controller's `layers` / `parameters` arrays. Never expose them raw (writes would be lost): the views re-read on every get
  and write the whole value back on every set.
- **Undo.** Calls on a clip / controller / transition are recorded by the engine (the asset is the owner). View setters
  have a plain owner, so they record their asset themselves (`AnimationEdit.Edit`). Creating an asset is not undoable.
- **Animator.** Most `Animator` properties describe the playing graph and log when read in Edit Mode: the adapter keeps
  only an allowlist of configuration members (`UnityEngine_Animator_Adapter.Safe`).
- **Name clash.** Inside `com.IvanMurzak.Unity.MCP.*` namespaces short names may resolve to namespaces; alias Unity types
  inside the namespace block (`using AnimatorController = UnityEditor.Animations.AnimatorController;`).
- Every C# file starts with the copyright box header and `#nullable enable`.
