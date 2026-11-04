# Disable Unity-MCP in CI

Introduced in core **1.6.2** and first-party extensions **2.1.1**.

These independent opt-outs leave the UPM manifest unchanged. Neither is enabled automatically
for `-batchmode` or `-runTests`: headless MCP sessions and MCP's own tests still need the plugin.

## Option 1: skip initialization (recommended for ordinary CI tests)

Set the process environment variable **before launching Unity**:

```bash
UNITY_MCP_DISABLE=1 "$UNITY_EDITOR" -batchmode -nographics \
  -projectPath "$PROJECT_PATH" -runTests -testPlatform PlayMode \
  -testResults "$PROJECT_PATH/test-results.xml" -logFile -
```

In GitHub Actions, put `UNITY_MCP_DISABLE: '1'` in the `env` mapping of the step that launches
Unity. For a Docker runner, explicitly forward it into the Unity container, for example
`docker run --env UNITY_MCP_DISABLE=1 ...`. Setting it only on the host does not necessarily
pass it through an action's container.

`1` and `true` (case-insensitive, surrounding whitespace ignored) disable MCP. Unset, `0`,
`false`, and unrecognized values preserve normal behavior. Restart Unity after changing it.
This is a process override and is not saved in project settings.

MCP skips its startup, tool registration, test-runner callbacks, background server download/start,
NuGet dependency resolution, log collection and editor/PlayMode hooks. Its sources still compile
and assemblies can still load. Existing project dependencies must therefore be valid; this mode
is not a workaround for missing DLLs or compilation errors. Explicit runtime initialization while
disabled reports an exception. Remove the variable for jobs that intentionally exercise MCP.

## Option 2: exclude MCP source assemblies from compilation

Add **`UNITY_MCP_EXCLUDE_FROM_COMPILATION`** to Unity's **Scripting Define Symbols** under
**Project Settings > Player > Other Settings**, for each build target used by your CI jobs.
Preserve the other symbols. This is a scripting symbol, **not an environment variable**.

For CI, prepare the resulting `ProjectSettings/ProjectSettings.asset` before starting Unity
(for example, use a CI-specific settings copy). Keep the regular development settings without
this symbol if developers need MCP. Do not set it from an `-executeMethod` in the same test run:
that happens after the first compilation and changing defines triggers another compilation.

All core package assemblies (including the dependency resolver, runtime, editor, RQL modules
and package tests), plus the updated first-party extension assemblies, use the negated assembly
constraint `!UNITY_MCP_EXCLUDE_FROM_COMPILATION`. The symbol excludes those assemblies together.
Update every installed MCP extension to a release containing this constraint too; older releases
and third-party extensions may still reference the excluded core assemblies. Custom code that
references MCP types must use matching constraints or `#if !UNITY_MCP_EXCLUDE_FROM_COMPILATION`.

This excludes MCP C# source assemblies. It does not uninstall UPM packages, remove imported assets,
or delete/disable precompiled DLLs shared with other packages. Removing the symbol restores normal
compilation. Switching modes can invalidate Unity's compilation cache; keep separate CI cache keys
for excluded and normal jobs. The environment variable from option 1 is unnecessary in this mode.
