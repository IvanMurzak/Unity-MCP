# Release & Versioning

Version is sourced from [Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/package.json](../../Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/package.json). Bump with `.\commands\bump-version.ps1 <version>`.

## CI/CD

See `.github/workflows/` for all pipelines. PRs from untrusted contributors require a `ci-ok` label from a maintainer before CI runs.

## Release dependency guard

`release.yml` (job `check-version-tag`) and `release_extensions.yml` (job `validate`) first run
`python commands/release_dependency_guard.py`. It fails every run, dry runs included, while a shipped pin is a
pre-release (any `-` suffix) or missing from nuget.org: the resolver pins in `NuGetConfig.cs` (consumers restore
them from nuget.org), the vendored Code-Exec release in `Editor/Scripts/CodeExec/VENDORED.md`, and the UPM / npm
dependencies of the core, extension and CLI packages. Switch the pins back to published releases (and move the
gate generation) before releasing. Tests: `commands/tests/test_release_dependency_guard.py`.
