<!--
Sync Impact Report
==================
- Version change: 1.3.0 → 1.4.0 (mandatory tool input argument added)
- Modified principles: none
- Added sections:
  - IX. `Unity-MCP-Plugin` Mandatory Tool Input Argument
- Removed sections: none
- Templates requiring updates:
  - .specify/templates/plan-template.md — ✅ compatible
  - .specify/templates/spec-template.md — ✅ compatible
  - .specify/templates/tasks-template.md — ✅ compatible
- Follow-up TODOs: none
-->

# Unity-MCP Constitution

## Core Principles

### I. `Unity-MCP-Plugin` Main-Thread Safety

All Unity API calls MUST execute on the main thread via
`MainThread.Instance.Run(() => ...)` (sync) or
`MainThread.Instance.RunAsync()` (async). SignalR callbacks,
background downloads, and MCP tool handlers run off the main
thread — every Unity API touch point MUST be dispatched.

Rationale: Unity's API is not thread-safe. Violating this
causes undefined behavior, crashes, and data corruption that
are extremely difficult to diagnose.

### II. `Unity-MCP-Plugin` Nullable & Type Safety

Every C# file in `Unity-MCP-Plugin` project MUST start with
`#nullable enable`. All public method parameters that accept
optional AI input MUST use nullable types (e.g., `string? filter = null`).
Null checks MUST be explicit — never rely on implicit null propagation
across system boundaries.

Rationale: The MCP protocol passes user-controlled input from
LLMs. Nullable annotations surface missing-value bugs at
compile time and prevent NullReferenceExceptions at runtime.

### III. `Unity-MCP-Plugin` Structured Tool Returns

MCP tools MUST return structured content — typed data models
(e.g., `GameObjectRef`, `SceneDataShallow`, `List<T>`),
`void`, or `Task`/`Task<T>` for async operations. Avoid
returning raw strings. When a tool has no meaningful data
to return, use `void` or `Task`.

Return type categories:
- **Data models**: custom types serialized as JSON for the
  LLM (e.g., `GameObjectRef`, `DestroyGameObjectResult`).
- **Collections**: `List<T>`, `T[]` of structured refs.
- **Async wrappers**: `Task<ResponseCallValueTool<T>>` for
  long-running operations.
- **Void**: for fire-and-forget operations with no output.

Error messages MUST be descriptive enough for an AI agent
to self-correct without human intervention.

Rationale: LLMs parse tool output to decide next steps.
Typed return values enable reliable JSON serialization and
machine-readable feedback without fragile string parsing.

### IV. `Unity-MCP-Plugin` Naming Conventions

- MCP tool and prompt names MUST use **kebab-case** with a
  category prefix (e.g., `assets-find`, `gameobject-create`,
  `scene-open`).
- Tool classes MUST be `partial` — one operation per file
  (e.g., `Tool_GameObject.Create.cs`).
- Namespaces MUST follow the pattern
  `com.IvanMurzak.Unity.MCP.[Tier].[Component]`.
- Every file MUST include the copyright box comment header.

Rationale: Consistent naming enables predictable discovery
by AI agents and maintains codebase navigability at scale.

### V. Test-First Development

New features and bug fixes MUST follow TDD:
1. Write tests first (RED) — tests MUST fail.
2. Implement minimal code to pass (GREEN).
3. Refactor (IMPROVE).
4. Verify minimum 80% coverage on changed code.

Tests MUST extend `BaseTest` and use `[UnityTest]` with
`IEnumerator` return type when Unity APIs are involved.
Use `RunTool(string toolName, string json)` for tool tests.

Rationale: The MCP tool surface area is large (52 tools,
48 prompts). Without test-first discipline, regressions
propagate silently to LLM consumers.

### VI. Immutability

Code MUST create new objects rather than mutating existing
ones wherever practical. Configuration models, data transfer
objects, and reference types (`ObjectRef` hierarchy) MUST
be treated as immutable after construction.

Rationale: Immutable data prevents hidden side effects,
simplifies debugging of multi-threaded dispatch flows,
and enables safe concurrency between the main thread and
SignalR callbacks.

### VII. Security By Default

- No hardcoded secrets (API keys, tokens, passwords) in
  source code — use environment variables or secret managers.
- All user/LLM input MUST be validated at system boundaries.
- Error messages MUST NOT leak internal paths, stack traces,
  or sensitive data to MCP clients.
- Rate limiting and authentication MUST be verified on all
  externally-facing endpoints.

Rationale: MCP servers are network-accessible. A single
leaked secret or injection vector can compromise the entire
Unity project and host machine.

### VIII. No Reflection for Private Access

C# Reflection (`System.Reflection`) MUST NOT be used to access
private, internal, or otherwise non-public fields, properties,
methods, or classes in project code. This includes `Type.GetType`,
`BindingFlags.NonPublic`, `GetMethod`, `GetField`, `GetProperty`,
and similar reflection APIs when used to bypass access modifiers.

Prohibited example:
```csharp
// WRONG: Accessing internal Unity API via reflection
var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor.CoreModule")
                  ?? Type.GetType("UnityEditorInternal.LogEntries, UnityEditor.CoreModule")
                  ?? Type.GetType("UnityEditor.LogEntries, UnityEditor")
                  ?? Type.GetType("UnityEditorInternal.LogEntries, UnityEditor");
logEntriesType?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public)
              ?.Invoke(null, null);
```

This rule does NOT apply to `ReflectorNet` library usage, which is
an external dependency specifically designed for reflection-based
access patterns.

Rationale: Reflection bypasses compile-time type safety, breaks
encapsulation, and creates fragile code that silently fails when
internal APIs change across Unity versions. It also makes code
review harder — non-public members are non-public for a reason.

### IX. `Unity-MCP-Plugin` Mandatory Tool Input Argument

Every MCP tool implemented in `Unity-MCP-Plugin` MUST declare at
least one input argument. If a tool has no meaningful parameters,
it MUST use the following default placeholder argument:

```csharp
string? nothing = null
```

This MUST be the sole input argument for otherwise parameterless
MCP tools.

Rationale: Some AI agents fail when invoking MCP tools that have
zero input arguments. A nullable placeholder argument with a
default value ensures universal agent compatibility without
affecting tool behavior.

## Architecture Constraints

- **Three-tier architecture**: MCP Client ↔ MCP Server
  (ASP.NET Core + SignalR) ↔ Unity Plugin. Changes MUST
  NOT collapse or bypass these tiers.
- **Deterministic port**: Port is derived from SHA256 of the
  project path, mapped to 50000–59999. This algorithm MUST
  NOT change without a migration plan.
- **Server binary lifecycle**: The plugin downloads and
  manages the server binary. Binary path is
  `Library/mcp-server/{platform}/`. The download, start,
  stop, and cleanup flow MUST be non-blocking.
- **Domain reload resilience**: Connection state and server
  PID MUST survive Unity domain reloads via EditorPrefs.
- **File size discipline**: Source files SHOULD stay under
  800 lines. Files exceeding this MUST be split using the
  partial class pattern already established.

## Development Workflow

1. **Research & Reuse** — search for existing implementations
   before writing new code. Prefer battle-tested libraries.
2. **Plan** — use the planner agent for complex features.
   Generate spec, plan, and task artifacts via SpecKit.
3. **TDD** — follow Principle V strictly.
4. **Code Review** — use the code-reviewer agent after
   writing code. Address CRITICAL and HIGH issues before
   commit.
5. **Commit** — use conventional commit format
   (`feat:`, `fix:`, `refactor:`, `docs:`, `test:`,
   `chore:`, `perf:`, `ci:`).
6. **CI Validation** — PRs MUST pass the 18-combination
   Unity test matrix (3 Unity versions × 3 test modes ×
   2 OS) before merge.

## Governance

This constitution is the authoritative source of project
principles. It supersedes ad-hoc practices and individual
preferences when conflicts arise.

**Amendment procedure**:
1. Propose change with rationale in a PR.
2. Document the principle delta (added/modified/removed).
3. Update constitution version per semantic versioning:
   - MAJOR: principle removal or incompatible redefinition.
   - MINOR: new principle or material expansion.
   - PATCH: wording clarification or typo fix.
4. Update dependent templates if principle changes affect
   their structure or gates.

**Compliance review**: All PRs and code reviews MUST verify
adherence to these principles. Complexity that violates a
principle MUST be justified in the PR description.

**Version**: 1.4.0 | **Ratified**: 2026-03-13 | **Last Amended**: 2026-03-14
