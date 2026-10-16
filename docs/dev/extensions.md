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

The four SDKs, both platform images and the original extension modes EditMode, PlayMode and
Standalone remain covered. The core's 16 existing editor legs stay; eight shared PlayMode legs
restore the coverage present in the old extension repositories. There are 24 editor legs for the
entire monorepo, rather than 16 core legs plus 24 for each extension (256 total). Each leg launches
one shared editor for core and every extension. Standalone retains player build/runtime checks.
There is no per-extension editor matrix. The four projects never share a Library directory.

The EditMode isolation test uses the running editor's actual compilation graph, resolved UPM
package graph, source files, defines and bundled Roslyn compiler. It recompiles every extension
assembly with only its declared dependency closure, excluding unrelated package assemblies.
Roslyn is a compiler process, not another Unity Editor. The same planted sibling reference must
fail without the Cinemachine DLL and pass with it. Missing extension editor/test assemblies fail
the test, so disabled compilation gates cannot produce a vacuous success. A static guard also
rejects undeclared sibling asmdef dependencies and lost/changed original migration tests.

Filtering references alone is insufficient when a declared third-party DLL was compiled with
optional features enabled by another package. The compiler guard removes package version defines
outside the consumer's dependency closure and adjusts the Input System symbol. It recompiles
dependencies whose conditional sources or emitted assembly references change, then propagates
those DLLs through the isolated graph. Unused auto-referenced DLLs do not force a dependency
rebuild. Ordinary Unity engine modules remain implicit framework references; optional packages
are required explicitly even when their source is bundled with the editor. Project/installer
fixture assemblies are excluded from native package compilation.

Controls plant an undeclared sibling DLL before the production filter and prove it was removed.
The same Cinemachine `PlayerIndex` access must fail without Input System and compile when Input
System is explicitly allowed. This covers conditional members hidden inside an otherwise declared
DLL, as well as direct sibling references. Compiler responses, diagnostics and package/assembly
ownership snapshots stay under `Library/ExtensionIsolation`; no extra Unity Editor is launched.

Combined compilation exposed a real compatibility issue: Cinemachine 3.1.6 conditionally uses
`InputAction.activeValueType`, added in Input System 1.8.0. The InputSystem module now requires
1.8.2, preserving compatibility when installed together. Existing newer project pins stay intact.
Unity 6.6 additionally requires Navigation 2.0.10, ProBuilder 6.1.3 and Tilemap Extras 7.0.1.
The core's `extensions.compatibility.json` defines these conditional floors; CLI, editor installation,
the three self-contained installers and test preparation select them before Unity imports packages.
Older editors keep their native package floors, and newer/custom project pins are preserved.
Use the current core/CLI when installing into Unity 6.6, or declare these floors in a manual UPM
consumer manifest. Mirror guards and actual installer tests keep the install paths aligned.

Unity 6.6 moved its compiler into `DotNetSdk/sdk/*/Roslyn/bincore`; the isolation runner uses the
matching bundled SDK host. Windows console-clear tests also cover open shared readers for both
plain and buffered log storage; clearing retains a writable empty file if Windows forbids deletion.

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
