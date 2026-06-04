# AGENTS.md

## What This Is

Unity-MCP bridges LLMs (Codex, Cursor, Copilot, etc.) with Unity Editor and Runtime via the [Model Context Protocol](https://modelcontextprotocol.io/). Sub-projects: `Unity-MCP-Server/` (ASP.NET Core + MCP SDK), `Unity-MCP-Plugin/` (Unity Editor/Runtime plugin), `cli/`, `Installer/`, `Unity-Tests/`.

## Build / Run

- Bump version: `.\commands\bump-version.ps1 <version>`
- CI/CD pipelines live in `.github/workflows/`.

## Project Constitution

Non-negotiable principles and architecture constraints: [`.specify/memory/constitution.md`](.specify/memory/constitution.md). You MUST read the constitution before performing any code review.

## Find Detail In

- [docs/Codex/architecture.md](docs/Codex/architecture.md) — System architecture: SignalR bridge, main-thread execution, deterministic port hashing
- [docs/Codex/style.md](docs/Codex/style.md) — Coding conventions: `#nullable enable`, no reflection for private access, namespace pattern, copyright headers
- [docs/Codex/release.md](docs/Codex/release.md) — Release, versioning, CI/CD
- [docs/Codex/documentation-sync.md](docs/Codex/documentation-sync.md) — README translation/copy sync requirements
- `Unity-MCP-Server/AGENTS.md`, `Unity-MCP-Plugin/AGENTS.md` — sub-project specifics
