# EngineFree — the plugin's licence-free, editor-free test project

A plain `Microsoft.NET.Sdk` xUnit project that compiles a small, deliberately-chosen set of the
**real shipped plugin sources** (`Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/**`) directly
via `<Compile Include>` and runs them on a hosted `ubuntu-latest` runner in a few seconds — no Unity
licence, no Docker, no editor, no `.meta` files.

It exists because the Unity plugin had **zero** engine-free tests: everything was either a Unity Test
Runner asmdef behind game-ci (licence + minutes + editor boot) or a Node test of `cli/`. Nothing gave
the C# side a signal you could get on a fork PR in seconds. The model is
[`Godot-MCP.Tests`](https://github.com/IvanMurzak/Godot-MCP) — same technique, same per-`<Compile>`
comments explaining what may and may not be called.

CI job: `engine-free-tests` in `.github/workflows/test_pull_request.yml`.

```bash
dotnet test Unity-MCP-Plugin/Tests~/EngineFree/EngineFree.csproj -c Release
```

## Why `Tests~/`

Unity ignores any folder whose name ends in `~`, and this one additionally sits **outside** both
`Assets/` and `Packages/`, so the asset pipeline never imports it and never writes a `.meta` file.
That matters twice over: the project root's `Unity-MCP-Plugin/*.csproj` files are Unity-GENERATED and
gitignored (`.gitignore:31`), so a hand-written csproj anywhere near them would be swallowed by that
rule and/or clobbered by the next project regeneration. `.gitignore:35` re-includes exactly this
project's csproj and nothing else, and the UPM package that `release.yml` packs
(`Packages/com.ivanmurzak.unity.mcp`) carries no reference to this folder at all.

Build output (`bin/`, `obj/`, `TestResults/`) is ignored by this folder's own `.gitignore` — the
plugin-root rules are rooted at `Unity-MCP-Plugin/` and do not reach down here.

## What may be compiled in — and the rule for adding more

**"grep finds no `UnityEngine` in the file" is a HYPOTHESIS, not the answer.** A file drags in the
engine through its *transitive* references just as fatally as through its own `using` directives, and
several files in this plugin that are textually clean are not compilable here for exactly that
reason. The rule is therefore mechanical:

1. Add the `<Compile Include>` line.
2. Build.
3. Keep it only if the build is still green — and add a comment at its `<Compile>` line saying what
   it is for and what must not be called.

**Never write a stand-in for a product type to make something compile.** A stub would turn this
suite into a test of the stub — the exact failure mode where a green run means nothing. A file that
does not compile is left out and recorded below instead.

Also: if a type is `partial`, either every file declaring it compiles or the type stays out.
A partial class assembled from a subset of its files presents a surface that **does not exist in
production**, and a reflection test over it is quietly measuring the wrong object.

## Measured NOT compilable (2026-09-04, verified by building, not by reading)

Unity-MCP's engine-free surface is much smaller than Godot-MCP's, and almost all of it funnels
through **two leaves**:

| Leaf | Engine call |
| --- | --- |
| `Runtime/Logger/UnityLogger.cs` | `UnityEngine.Debug.Log/LogError/LogWarning/LogException` |
| `Runtime/Unity/Logs/UnityLogCollector.cs` | `UnityEngine.Application.logMessageReceivedThreaded` |

Everything below is blocked by one of those two, or by another file that is:

| File | First error when compiled here | Chain |
| --- | --- | --- |
| `Runtime/Utils/EnvironmentUtils.cs` | `CS0234: namespace 'Utils' does not exist in 'com.IvanMurzak.Unity.MCP'` | `UnityLoggerFactory` → `UnityLoggerProvider` → `UnityLogger` |
| `Runtime/Logger/UnityLoggerFactory.cs` | `CS0246: UnityLoggerProvider` | same |
| `Runtime/Logger/UnityLoggerProvider.cs` | `CS0246: UnityLogger` | same |
| `Runtime/UnityMcpPlugin.cs` | `CS0246: UnityLogCollector` | `UnityLogCollector` |
| `Runtime/UnityMcpPlugin.Config.cs` | `CS0103: GeneratePortFromDirectory` / `EnvironmentUtils` | needs `UnityMcpPlugin.cs` (above) and `EnvironmentUtils` (above) |
| `Runtime/UnityMcpPluginRuntime.cs` | `CS0246: UnityMcpPlugin` | base type, plus `CreateDefaultReflector`/`ApplyConfigToMcpPlugin` from `UnityMcpPlugin.Build.cs` (`using UnityEngine`) |
| `Runtime/UnityMcpPluginBuilder.cs` | `CS0246: UnityMcpPluginRuntime` | field + return type |
| `Editor/DependencyResolver/NuGetInstallManifest.cs` | `CS0246: UnityEngine` | `Debug.LogWarning`, **not** behind `#if UNITY_EDITOR` |
| `Editor/DependencyResolver/NuGetPackageRestorer.cs` | `CS0246: UnityEngine` | four `Debug.Log*` calls, same |

Consequences worth stating plainly, because they bound what a green run here means:

- **`UnityConnectionConfig` and `EnvironmentUtils.ApplyEnvironmentOverrides` are NOT covered here.**
  The `args > env > disk` precedence ladder stays a game-ci EditMode concern. What this project
  covers of "connection config" is the cloud authorization-server side (`DeviceAuthService`) plus the
  log-level gate — see `ConnectionConfigTests`.
- **Only `Tool_Ping` of the tool families is covered here.** Every other `[AiToolType]` class in the
  plugin (`Tool_Assets`, `Tool_Console`, `Tool_Object`, `Tool_Package`, `Tool_Type`, `Tool_Tool`,
  `Tool_Skills`, …) is spread across partial files of which at least one needs `UnityEngine`, so by
  the partial-class rule above none of them may be compiled in.

Both limits are the *point* rather than a defect: this suite is a fast, licence-free floor under the
pieces that genuinely do not need an engine, not a replacement for the Unity Test Runner suites.

## The two pin locations

`EngineFree.csproj` consumes `com.IvanMurzak.McpPlugin` and `com.IvanMurzak.ReflectorNet` from NuGet
at the **same versions** the resolver installs into a consumer project
(`Editor/DependencyResolver/NuGetConfig.cs`). That makes this csproj a **second pin location** the
release train must bump. Never bump it here by hand and never let it drift:
`NuGetPinGateTests.PinnedPackageVersions_MatchNuGetConfig` compares the declared versions (exported
as assembly metadata from the real `PackageReference` items, so the guard cannot drift from the
csproj) against `NuGetConfig.Packages`, and a missed bump fails loudly instead of leaving this suite
silently testing yesterday's DLLs.

## Workspace-source override compatibility

The project is a NuGet consumer with **no** `UseWorkspaceSources` property of its own, no
`packages.lock.json`, no `RestorePackagesPath` and no `nuget.config`, so a repo-root
`Directory.Build.targets` + `nuget.config` can redirect it at a local workspace feed. Verified
locally against a throwaway feed of repacked `-ws.g<sha8>` packages:

- `obj/project.assets.json` resolves `com.IvanMurzak.McpPlugin/8.3.0-ws.g9c0e11d2`,
  `com.IvanMurzak.McpPlugin.Common/8.3.0-ws.g9c0e11d2`, `com.IvanMurzak.ReflectorNet/5.4.0-ws.g1dff5501`.
- All 44 tests pass against those packages, and again with `-p:Version=8.3.0-ws.g9c0e11d2` — the
  `<Compile Include>` links are unaffected by either.
- The pin-parity test stays GREEN under the override, because the assembly metadata captures the
  DECLARED pin (`Directory.Build.targets` is imported after the ItemGroup that reads it). Intended:
  an override is transient, and must not redden a check about what the release train has to bump.

## Test files

| File | Area |
| --- | --- |
| `NuGetPinGateTests.cs` | NuGet pins + the asmdef generation gate of issue #957, re-implementing `.github/scripts/check_nuget_gate.py`'s rule in C# over the compiled-in constants; plus the csproj pin-parity guard |
| `ConnectionConfigTests.cs` | Cloud authorization-server connection config (`DeviceAuthService`) and the plugin log-level gate |
| `ToolAttributeTests.cs` | `[AiToolType]` / `[AiTool]` plumbing, driven through the real `McpPluginBuilder` registration path |
