# AGENTS.md — Unity-AI-Cinemachine

Unity package `com.ivanmurzak.unity.mcp.cinemachine` — **Cinemachine** (`com.unity.cinemachine`) for AI agents. It ships
no MCP tools: an RQL module for Unity-MCP's `rql-query` /
`rql-mutate` (the `cinemachine` source with `CreateCamera` / `EnsureBrain`, CinemachineCamera `SetBody` / `SetAim` /
`SetNoise` / `SetTargets`, pipeline members `body` / `aim` / `noise` / `extensions`, and a guide rendered into the
`unity-rql` skill). An extension of [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP)
(`com.ivanmurzak.unity.mcp`), generated from `Unity-AI-Tools-Template`; the former tool → RQL table is in `README.md`.

## Layout and commands

- `Packages/com.ivanmurzak.unity.mcp.cinemachine/` — shipped UPM sources, asmdefs and original tests.
- `Installer/` — preserved installer assets/tests; shared projects materialize them.
- `README.md`, `docs/` — module documentation; `migration-source.json` records provenance.
- Shared commands/workflow: `../../commands/extensions.py`, `../../docs/dev/extensions.md`.
- Use the shared per-version `../../Unity-Tests/` projects; do not create separate extension editor runs.

## Critical invariants

- **RQL surface.** Agents use Unity-MCP's `rql-query` / `rql-mutate`; per-camera operations are
  adapter methods of CinemachineCamera, owner-less creation lives on the `cinemachine` source (`[RqlCallable]`), and every
  example belongs in the module guide (`CinemachineModule.Guide`, rendered into the `unity-rql` skill;
  `EveryGuideExampleRuns` executes it in order).
- **Pipeline stages.** Body / Aim / Noise component types come from `[CameraPipeline(stage)]` via TypeCache, excluding
  `[Obsolete]` CM2 components; `SetBody` / `SetAim` remove every other component of that stage. Components are added /
  removed with `Undo.AddComponent` / `Undo.DestroyObjectImmediate` so one Ctrl+Z reverts a call.
- **Unity fake null.** Never `??` / `?.` on `UnityEngine.Object` results (`GetComponent<T>() ?? throw` throws
  MissingComponentException); compare with `== null`.
- **Hidden internals.** Private `[SerializeField, HideInInspector]` bookkeeping (m_StreamingVersion, m_LegacyPriority,
  m_NoiseOffsets) and runtime caches (*TargetAsGroup / *TargetAsVcam …) are hidden; CameraState members are explicit-only.
- Every C# file starts with the copyright box header and `#nullable enable`.
