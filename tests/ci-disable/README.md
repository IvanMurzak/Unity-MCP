# CI disable regression probes

Prepare `Unity-Tests/6000.3.27f1` with `python commands/extensions.py prepare --project ...`.
Copy `tests/ci-disable/Editor` to the project's `Assets/CiDisableProbe/Editor`.
Use a separate Unity process for each mode, `-batchmode -nographics -projectPath <project>`:

1. With `UNITY_MCP_DISABLE=1` and `UNITY_MCP_PROBE_MODE=disabled`, run `-executeMethod CiDisableProbe.Run`.
   This requires compiled core and extension assemblies, verifies no startup singleton, then checks an
   explicit build cannot bypass the disable flag.
2. With `UNITY_MCP_DISABLE=1`, run `-runTests -testPlatform EditMode -testFilter CiDisableBehaviorTests
   -testResults <xml>`. It verifies startup stays inactive across entering/exiting PlayMode (domain reloads).
3. Add `UNITY_MCP_EXCLUDE_FROM_COMPILATION` to the project's Standalone scripting defines before launch,
   unset `UNITY_MCP_DISABLE`, set `UNITY_MCP_PROBE_MODE=excluded`, and run `CiDisableProbe.Run`.
   No shipped MCP assembly may be in Unity's compilation graph or loaded domain. Standalone installer
   fixtures materialized by `prepare` are outside the shipped UPM packages and intentionally excluded
   from this assertion.
4. Remove the exclusion symbol, leave `UNITY_MCP_DISABLE` unset, set `UNITY_MCP_PROBE_MODE=normal`,
   `UNITY_MCP_CONNECTION_MODE=Custom`, `UNITY_MCP_KEEP_CONNECTED=false`, `UNITY_MCP_START_SERVER=false`,
   and `CI=true`; run `CiDisableProbe.Run`. Core/extension compilation and MCP startup must return.

Use `-logFile <log>` for each run. Every probe must log `CI_DISABLE_PROBE_PASS <mode>` and exit 0;
assert the NUnit XML has exactly two passed tests with no skips. Unity creates new `.meta` files;
copy them back to `tests/ci-disable`. Restore temporary project settings after testing.
