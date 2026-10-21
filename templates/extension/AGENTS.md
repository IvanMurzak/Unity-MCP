# AGENTS.md — Unity-AI-Tools-Template

**Template repo** for Unity extension packages of [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP)
(`com.ivanmurzak.unity.mcp`). `commands/init.ps1` turns its placeholder boilerplate into a real extension,
so every change here propagates to future extensions — keep placeholder tokens consistent.

## Placeholders

`init.ps1` replaces `YOUR_PACKAGE_ID`, `YOUR_PACKAGE_ID_LOWERCASE`,
`YOUR_PACKAGE_NAME`, `YOUR_PACKAGE_NAME_INSTALLER`, `YOUR_PACKAGE_NAME_INSTALLER_FILE`,
`YOUR_GITHUB_USERNAME_REPOSITORY`, `YOUR_FEATURE_NAME` (C# identifier from `-FeatureName`, default: the package name
without spaces) and `YOUR_FEATURE_NAME_LOWERCASE` (the RQL source name) in file contents and file/folder names — but only under its `$TargetPaths`
(`Installer/`, `Unity-Package/`, `Unity-Tests/`, `.github/`, `README.md`, three `commands/` scripts).
Use the tokens in every new file there. This AGENTS.md is not rewritten: a new extension replaces it with
its own (what it wraps, its invariants).

## Layout

- `Unity-Package/` — Unity project hosting the package; only `Packages/YOUR_PACKAGE_ID_LOWERCASE/` ships.
  - `package.json` — the version source of truth and the `com.ivanmurzak.unity.mcp` dependency pin.
  - `Editor/Scripts/Rql/` — the RQL module (`YOUR_FEATURE_NAMEModule`), one adapter per file
    (`UnityEngine_<Type>_Adapter.cs`), the `YOUR_FEATURE_NAME_LOWERCASE` source (`YOUR_FEATURE_NAMEEditorApi`).
  - `Editor/Scripts/` — other Editor code, MCP tools if any (Editor API, Edit + Play Mode, never in game builds).
  - `Runtime/` — runtime tools and data types (no Editor API, ship in game builds).
  - `Tests/Editor/` (EditMode; RQL tests in `Tests/Editor/Rql/`) and `Tests/Runtime/` (PlayMode) — NUnit + Unity Test Framework.
- `Unity-Tests/<unity-version>/` — one test project per supported Unity version, referencing the package via
  `file:`. Adding a version: also add it to both `*.yml-sample` workflows and `open-all-projects-*`.
- `Installer/` — Unity project that exports the `.unitypackage` installer (`Assets/YOUR_PACKAGE_NAME Installer/`).
- `commands/` — the scripts below, plus `open-all-projects-unix.sh` and `check-versions.py`.
- `.github/workflows/` — `bump_version.yml`, `test_unity_plugin.yml` (reusable test job), and
  `release.yml-sample` / `test_pull_request.yml-sample` (inert until renamed after init; see `README.md`).
- `docs/` — user docs: `Deploy-{OpenUPM,GitHub,npmjs}.md`, `Manual-Package-Rename.md`, `openupm-signing.md`.

## Commands (PowerShell, from the repo root)

```powershell
./commands/init.ps1 -PackageId "com.company.package" -PackageName "My Package" -GitHubRepository "user/repo"
./commands/open-all-projects-windows.ps1          # open the Unity projects so .meta files get generated
./commands/bump-version.ps1 -NewVersion "1.0.1"   # -WhatIf to preview
./commands/get-version.ps1
./commands/update-ai-game-developer.ps1           # bump the com.ivanmurzak.unity.mcp pin; -WhatIf to preview
```

- `bump-version.ps1` updates `package.json` and `Installer.cs` in lock-step; add any new version-bearing
  file to its `$VersionFiles`. CI equivalent: dispatch `bump_version.yml` (opens a `release/<version>` PR).
- `release.yml` releases on push to `main` only when the `package.json` version has no tag yet: it builds
  the installer (`INSTALLER_UNITY_VERSION`; change only deliberately), runs the test matrix, then tags and
  publishes the GitHub Release with the `.unitypackage` attached.
- `commands/check-versions.py` is synced in by the release cascade (do not hand-edit); CI runs it as the
  `version-consistency` gate.

## Conventions

- RQL first: agents read and edit data through Unity-MCP's `rql-query` / `rql-mutate` / `rql-schema` tools. An extension
  adds an `IRqlModule` (`Order => 100`, discovered via TypeCache): adapters (`RqlTypeAdapter<T>`: virtual members,
  methods, validation, coercion), sources with `[RqlCallable]` operations for things without an owner object, functions
  for read-only computations, and `ctx.AddGuide(topic, markdown)` (rendered into the `unity-rql` skill). Adapter API:
  RQL `docs/type-adapters.md`. Every guide example must run (`EveryGuideExampleRuns`).
- Namespace `com.IvanMurzak.Unity.MCP.Editor.Rql.YOUR_FEATURE_NAME`; alias the Unity types you use inside the namespace
  block (`using Light = UnityEngine.Light;`): enclosing namespaces of sibling extensions shadow top-level usings.
- RQL tests: a base class (fresh scene, `RqlUnity.Reset()`, `Q()` / `M()`), one test file per adapter / source, module
  tests (discovery, guide in `Skill_Rql.Render()`, guide examples, one `rql-mutate` call through the MCP pipeline). The
  test asmdef lists `Rql.Contracts.dll`, `Rql.Parser.dll`, `Rql.Execution.dll` (it overrides references).
- MCP tools only for what RQL cannot express (binary output, long or asynchronous work, external processes): one
  `[AiToolType]` partial class per area, one `[AiTool(...)]` method per file, with `[Description]` on the
  method and every parameter (that text is what the LLM sees).
- Unity API calls run on the main thread: wrap them in `MainThread.Instance.Run(...)` / `RunAsync(...)`;
  only argument validation runs outside.
- Every C# file starts with the copyright box header and `#nullable enable`.
- High cohesion, low coupling — many small files.
- CI safety: under `pull_request_target`, `test_unity_plugin.yml` runs only with the maintainer-applied
  `ci-ok` label and aborts if the PR touches `.github/workflows/`. Required secrets: see `README.md`.
