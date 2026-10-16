# Unity extension modules

Each module remains an independent UPM package. Its package ID, assembly names, tests and
Unity-generated GUIDs survive the migration. Native package dependencies declare the minimum
core/Unity package versions; a newer compatible core does not require republishing every module.

```text
Unity-MCP/
├── Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/  core package
├── cli/                                              core's CLI
├── extensions/
│   ├── manifest.json                                 component inventory
│   ├── Animation/
│   │   ├── Packages/com.ivanmurzak.unity.mcp.animation/
│   │   │   ├── package.json                          independent version/dependencies
│   │   │   ├── Editor/, Runtime/, Tests/             separate asmdefs
│   │   ├── Installer/                                installer sources and tests
│   │   ├── README.md
│   │   └── migration-source.json                     pinned old commit and file hashes
│   └── … nine other modules
├── templates/extension/                              inert authoring scaffold
├── tests/extensions/Editor/                          isolated compiler + sibling canary
└── Unity-Tests/
    ├── 2022.3.62f3/                                  shared core + all extensions
    ├── 2023.2.22f1/                                  separate project/Library
    ├── 6000.3.1f1/                                   separate project/Library
    └── 6000.6.3f1/                                   separate project/Library
```

## Tests and isolation

Before opening a test project run:

```shell
python commands/extensions.py prepare --project Unity-Tests/2022.3.62f3
```

This adds local UPM packages, their `testables` entries and the original installer test assets.
It sets the current NuGet assembly gate after checking the bundled DLLs exist, disables
installer self-installation in the test project, and materializes platform-specific fixture
newlines. It preserves newer Unity package versions already selected by a project. Graphics
tests require a graphics context: do not pass `-nographics` for the full editor suite.

The existing CI version/platform/EditMode/Standalone matrix stays intact. Each existing matrix
leg launches one shared editor for core and all extensions, rather than another project/editor
for each extension. Standalone legs retain their player build/runtime checks. There is no
additional extension-project editor matrix. The four projects never share a Library directory.

The EditMode isolation test uses the running editor's actual compilation graph, resolved UPM
package graph, source files, defines and bundled Roslyn compiler. It recompiles every extension
assembly with only its declared dependency closure, excluding unrelated package assemblies.
Roslyn is a compiler process, not another Unity Editor. The same planted sibling reference must
fail without the Cinemachine DLL and pass with it. Missing extension editor/test assemblies fail
the test, so disabled compilation gates cannot produce a vacuous success. A static guard also
rejects undeclared sibling asmdef dependencies and lost/changed original migration tests.

Combined compilation exposed a real compatibility issue: Cinemachine 3.1.6 conditionally uses
`InputAction.activeValueType`, added in Input System 1.8.0. The InputSystem module now requires
1.8.2, preserving compatibility when installed together. Existing newer project pins stay intact.

## Independent releases

```text
GitHub: IvanMurzak/Unity-MCP
├── 1.1.0                                             core release, core CLI, signed core UPM
├── com.ivanmurzak.unity.mcp.animation/2.0.1
│   ├── com.ivanmurzak.unity.mcp.animation-2.0.1.tgz     signed native UPM archive
│   ├── AI-Animation-Installer.unitypackage
│   └── SHA256SUMS
└── com.ivanmurzak.unity.mcp.terrain/2.0.2
    ├── com.ivanmurzak.unity.mcp.terrain-2.0.2.tgz
    ├── AI-Terrain-Installer.unitypackage
    └── SHA256SUMS
```

These coordinates illustrate independent component versions. A Terrain-only change publishes
only Terrain's new release. It does not rebuild Animation or core. Component releases use
`latest=false`; GitHub's repository-wide latest remains the core release.

```shell
python commands/extensions.py bump --only Terrain --version 2.0.2
python commands/extension_release.py plan --only Terrain
```

The bump updates only that package's version, installer constant and pinned README download.
After green CI and review, publish through the workspace Train's `extensions release --engine
unity` lever, which dispatches `release_extensions.yml` on an exact main commit. A dry run tests
and signs without publishing. Signing uses the existing Unity-MCP UPM credentials and namespace.
Original `.meta` GUIDs and installer asset bytes produce the installer archive without starting
another editor. Native archives must contain an attestation and the exact component sources;
draft retries reuse already uploaded archives and refuse changed existing assets.

OpenUPM registrations point to this repository with each package's literal `gitTagPrefix` and
its signed archive's filename prefix. The core registration excludes extension tags. OpenUPM
continues to serve unchanged historical versions. CLI installation keeps the existing package
IDs and resolves each package's own OpenUPM latest; source code is not cloned to install it.

Add a module from the inert scaffold under `templates/extension/`, register its package in
`extensions/manifest.json` and the shared editor/CLI catalog, and generate new `.meta` files in
Unity. Never duplicate an existing module's GUIDs. Shared test preparation includes the new
module automatically; publish its OpenUPM registration with the matching tag/asset prefixes.
For a new installer, remove metadata only from the new copy before the editor generates unique
GUIDs, then run `python commands/extensions.py collect-metas --project Unity-Tests/2022.3.62f3`
to retain the generated installer metadata in its module. Existing GUIDs are never overwritten.
