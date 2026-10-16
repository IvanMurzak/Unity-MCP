# MCP Protocol Implementation

Attribute-based registration for three MCP primitives. All use `[System.ComponentModel.Description]` for AI-readable documentation.

- **Tools**: `[McpPluginToolType]` on class, `[McpPluginTool(Name = "category-action")]` on methods
- **Prompts**: `[McpPluginPromptType]` on class, `[McpPluginPrompt]` on methods
- **Resources**: `[McpPluginResourceType]` on class, `[McpPluginResource]` on methods (e.g., `gameobject://currentScene/{path}`)

## Testing Patterns

- Extend `BaseTest` class — provides `[UnitySetUp]` (initializes singleton, creates logger) and `[UnityTearDown]` (destroys all GameObjects)
- `BaseTest.RunTool(string toolName, string json)` helper — executes a tool and asserts success
- Use `[UnityTest]` with `IEnumerator` return type; call `yield return base.SetUp()` / `base.TearDown()`
- Some tests use standard NUnit `[Test]`/`[SetUp]`/`[TearDown]` when Unity APIs aren't needed

## Error Handling

- Structured error responses for AI consumption; graceful non-blocking cleanup on disconnect/quit
- SIGTERM for Unix (falls back to `Kill()`), immediate `Kill()` on Windows
- Log all exceptions (never silently swallowed)

## Configuration

- **No spaces in project path** — validated on startup with user warning
- **Unity 2022.3+** minimum
- Main UI: `Window/AI Game Developer`
- Config file: `Assets/Resources/AI-Game-Developer-Config.json` (auto-created). Editor mode: file path; Play mode: `Resources.Load<TextAsset>()`
