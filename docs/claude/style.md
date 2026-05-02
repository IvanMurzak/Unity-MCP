# Key Coding Conventions

These apply across both C# sub-projects:

- `#nullable enable` at the top of every file
- Copyright box comment header in every file
- MCP tool classes are `partial` — one operation per file (e.g., `Tool_GameObject.Create.cs`)
- MCP tools MUST return structured types (data models, `List<T>`, `void`, or `Task`) — avoid raw string returns
- All Unity API calls must use `MainThread.Instance.Run(() => ...)` or `RunAsync()`
- Tool/prompt names use **kebab-case** with category prefix (e.g., `gameobject-create`, `assets-find`)
- Namespace pattern: `com.IvanMurzak.Unity.MCP.[Tier].[Component]`
- **No Reflection for private access** — C# Reflection (`System.Reflection`) MUST NOT be used to access private, internal, or non-public members. Exception: `ReflectorNet` library usage is allowed.
