# Release & Versioning

Version is sourced from [Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/package.json](../../Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/package.json). Bump with `python commands/bump_version.py --new-version <version>`.

## CI/CD

See `.github/workflows/` for all pipelines. PRs from untrusted contributors require a `ci-ok` label from a maintainer before CI runs.

## Release dependency guard

`release.yml` (job `check-version-tag`) and `release_extensions.yml` (job `validate`) first run
`python commands/release_dependency_guard.py`. It fails every run, dry runs included, while a shipped pin is a
pre-release (any `-` suffix) or missing from nuget.org: the resolver pins in `NuGetConfig.cs` (consumers restore
them from nuget.org), the vendored Code-Exec release in `Editor/Scripts/CodeExec/VENDORED.md`, and the UPM / npm
dependencies of the core, extension and CLI packages. Switch the pins back to published releases (and move the
gate generation) before releasing. Tests: `commands/tests/test_release_dependency_guard.py`.

## MCP registry listing

`server.json` at the repo root is the one universal registry listing (`io.github.IvanMurzak/Unity-MCP`)
for Unity, Unreal and Godot. To refresh it: bump `version` (one publish per version), merge,
then dispatch `.github/workflows/publish_mcp_registry.yml` on `main` (its ref guard rejects other refs).
It lists the hosted remote only: add `packages` once a GameDev-MCP-Server release carries the matching `mcp-name` and Docker label.
