# AGENTS.md

Unity-MCP bridges LLMs (Claude, Cursor, Copilot, etc.) with the Unity Editor and Runtime via the
[Model Context Protocol](https://modelcontextprotocol.io/).

The MCP server is NOT in this repo: the plugin consumes the shared
[GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) (binary `gamedev-mcp-server`,
release assets `gamedev-mcp-server-<rid>.zip`, Docker `aigamedeveloper/mcp-server`), one
engine-agnostic server shared by Unity-MCP, Godot-MCP and Unreal-MCP. The plugin downloads the
release pinned by the `ServerVersion` constant in
`Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/Scripts/McpServerManager.cs` (decoupled
from the plugin version). Bumping the consumed server = changing that constant; the pinned
`v<ServerVersion>` release (with all 7 RID zips) must exist BEFORE a plugin release that pins it.

## Build / run

- Bump version: `python commands/bump_version.py --new-version <version>` (`--what-if` to preview).
- Releases happen only by manually dispatching `.github/workflows/release.yml`; merging a version
  bump publishes nothing.
- Unity `.meta` files: never hand-write them; Unity generates them on asset refresh.

## Layout

- `Unity-MCP-Plugin/` — the Unity project holding the UPM package `com.ivanmurzak.unity.mcp`
  (all plugin code and its tests); see its `AGENTS.md`.
- `cli/` — `unity-mcp-cli` npm package (TypeScript); see its `AGENTS.md`.
- `extensions/` — ten optional UPM modules, independent component versions and signed releases.
- `templates/extension/` — inert scaffold; shared tests/releases live in `commands/extensions.py` and `docs/dev/extensions.md`.
- `Installer/` — Unity project that builds the installer `.unitypackage`.
- `Unity-Tests/<unity-version>/` — per-Unity-version test projects referencing the plugin package
  and all extension packages via `file:`; prepare with `commands/extensions.py` before opening.
- `tests/chain-fixtures/` — recorded tool-call fixtures for chain tests, compared byte-for-byte
  (LF); re-recorded by the plugin's `Tests/Editor/Chain/ChainRecordTests.cs`, never hand-edited.
- `tests/filter-oracle/` — checks the `tests-run` filter count against what Unity runs; see its `AGENTS.md`.
- `commands/` — Python utility scripts (stdlib only, 3.9+) (version bump, Unity tests, release notes).
- `docs/` — README translations, dev docs (`docs/dev/`), agent detail docs (`docs/agents/`).
- `memory/` — durable agent lessons (`memory/MEMORY.md` is the index).
- `.github/workflows/`, `.github/scripts/` — CI/CD and its helper scripts.

## Find detail in

- [docs/agents/architecture.md](docs/agents/architecture.md) — SignalR bridge, main-thread execution, deterministic port hashing
- [docs/agents/style.md](docs/agents/style.md) — coding conventions: `#nullable enable`, no reflection for private access, namespaces, copyright headers
- [docs/agents/release.md](docs/agents/release.md) — release, versioning, CI/CD
- [docs/agents/ci-unity-license.md](docs/agents/ci-unity-license.md) — Unity license for CI
- [docs/agents/documentation-sync.md](docs/agents/documentation-sync.md) — README translation/copy sync requirements
