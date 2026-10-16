# AGENTS.md — Unity-AI-ParticleSystem

Unity package `com.ivanmurzak.unity.mcp.particlesystem` — makes Unity `ParticleSystem` components available to
Unity-MCP's RQL tools (`rql-query` / `rql-mutate`; no extension MCP tools): writable module structs, MinMaxCurve /
MinMaxGradient literals and playback methods.
An extension of [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) (`com.ivanmurzak.unity.mcp`),
generated from `Unity-AI-Tools-Template`; the user-facing tool list is in `README.md`.

## Layout and commands

- `Packages/com.ivanmurzak.unity.mcp.particlesystem/` — shipped UPM sources, asmdefs and original tests.
- `Installer/` — preserved installer assets/tests; shared projects materialize them.
- `README.md`, `docs/` — module documentation; `migration-source.json` records provenance.
- Shared commands/workflow: `../../commands/extensions.py`, `../../docs/dev/extensions.md`.
- Use the shared per-version `../../Unity-Tests/` projects; do not create separate extension editor runs.

## Critical invariants

- **Main thread only.** RQL runs on the main thread (Unity-MCP dispatches the RQL tools there).
- **RQL surface.** Everything is registered by `ParticleSystemModule` (`IRqlModule`, Order 100, found through
  TypeCache) and documented in its `AddGuide("Particle systems", …)` text, which is rendered into the `unity-rql` skill.
- **Module structs are handles.** `ps.main`, `ps.emission` … are get-only struct properties whose setters write to the
  native particle system; `UnityEngine_ParticleSystem_ModuleStruct_Adapter` marks them writes-through so
  `set main.startLifetime = 3` works.
- **Name clash.** Inside `com.IvanMurzak.Unity.MCP.*` namespaces, alias the type inside the namespace block
  (`using ParticleSystem = UnityEngine.ParticleSystem;`).
- Every C# file starts with the copyright box header and `#nullable enable`.
