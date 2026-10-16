# AGENTS.md — Unity-AI-Navigation

Unity package `com.ivanmurzak.unity.mcp.navigation` — wraps Unity's **AI Navigation** package
(`com.unity.ai.navigation`) for AI agents. On branch `experiment/rql` (local experiment) it ships no MCP tools: an
RQL module for Unity-MCP's `rql-query` / `rql-mutate` (surface `Bake()` / `Clear()`, agent `SetDestination`, agent-type
bake settings and areas via the `navigation` source, name views of agent types / areas / masks, and a guide rendered
into the `unity-rql` skill). An
extension of [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) (`com.ivanmurzak.unity.mcp`),
generated from `Unity-AI-Tools-Template`; the user-facing tool list is in `README.md`.

## Layout and commands

- `Packages/com.ivanmurzak.unity.mcp.navigation/` — shipped UPM sources, asmdefs and original tests.
- `Installer/` — preserved installer assets/tests; shared projects materialize them.
- `README.md`, `docs/` — module documentation; `migration-source.json` records provenance.
- Shared commands/workflow: `../../commands/extensions.py`, `../../docs/dev/extensions.md`.
- Use the shared per-version `../../Unity-Tests/` projects; do not create separate extension editor runs.

## Critical invariants

- **RQL surface.** Everything is registered by `NavigationModule` (`IRqlModule`, Order 100, found through TypeCache)
  and documented in its `AddGuide("Navigation", …)` text, which is rendered into the `unity-rql` skill.
- **Bake settings** are project-wide: `navigation | get agentTypes` (`NavMeshAgentType`) and `areas` (`NavMeshArea`)
  edit the `NavMeshProjectSettings` serialized object (`NavMeshNames.Settings()`), not a scene component.
- **Names over ids.** `agentType`, `areaName` / `defaultAreaName`, `areaNames` and `affectedAgents` are name views
  (`NavigationMembers`) over the numeric members; private backing fields that duplicate a public property are hidden
  (`NavigationMembers.HideBackingFields`), because writing them bypasses the property logic.
- Every C# file starts with the copyright box header and `#nullable enable`.
