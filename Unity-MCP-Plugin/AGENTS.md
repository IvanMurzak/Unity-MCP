# Unity-MCP-Plugin/AGENTS.md

Unity Editor/Runtime side of the MCP bridge: an attribute-based framework that registers and runs
MCP tools, prompts and resources, plus the MCP server manager and AI-client auto-configuration.
Open this folder in Unity (2022.3+ minimum; compiles automatically).

## Layout (`Packages/com.ivanmurzak.unity.mcp/` holds all shipped code)

- `Editor/Scripts/API/{Tool,Prompt,Resource,SystemTool}/` — MCP endpoints; tools are partial
  classes, one operation per file.
- `Editor/Scripts/` — editor singleton, `Startup`, server manager, UI and services.
- `Runtime/` — runtime singleton and shared data/converters (also used in game builds).
- `Tests/Editor` (EditMode), `Tests/Runtime` (PlayMode) — run via Unity Test Runner.
- `Tests~/EngineFree/` — Unity-free xUnit project over selected plugin sources (`dotnet test`).
- `Assets/` — demo setup and bundled NuGet DLLs; `Commands/` — helper scripts
  (`start_mcp_inspector.bat`, needs Node.js).
- `.claude/skills/` — generated from `[AiSkillDescription]`/`[AiSkillBody]` by the
  `unity-skill-generate` tool; regenerate, don't hand-edit.

## Critical invariants

- Editing `.cs` silences MCP during recompile; read `Editor.log` from disk to get compile errors.
- No spaces in the project path (validated on startup).
- Every Unity API call goes through `MainThread.Instance.Run()`.

Detail in `Docs/agents/`: `startup-flow.md` (init, CI detection) · `transport.md` (port hashing,
server binary, domain reload) · `models.md` (`ObjectRef`) · `auto-config.md` (AI client
configurators) · `structure.md` (key classes) · `protocol.md` (MCP attributes, tests, errors).
