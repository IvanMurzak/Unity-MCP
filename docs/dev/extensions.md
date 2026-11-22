# Unity extension modules

Each module remains an independent UPM package. Its package ID, assembly names, tests and
Unity-generated GUIDs survive the migration. A module version works with exactly one core release,
the one its `package.json` names as the core dependency. Its Editor/Runtime asmdefs carry the core's
gate defines (`UNITY_MCP_READY` and `UNITY_MCP_DEPS_<n>` from `NuGetConfig.cs`; #957,
`.github/scripts/check_nuget_gate.py`) and a `versionDefines` entry on the core package that holds
only for that core version (`UNITY_MCP_CORE_MATCH`). With any other core Unity skips the module (not
broken) and reports nothing: its tools are simply absent. So every core release re-releases all
modules for itself (see "Published versions" below).

Why the core version and not only the gate defines: the defines live in the consumer's
`ProjectSettings` and outlive a core update. Until the new core's resolver has run they still satisfy
a module built for the previous core, so after a core-only update (Package Manager, a manifest edit,
`install-plugin`) Unity compiled that module while the core itself was skipped: `error CS0103` in the
module, a batch-mode editor exits with code 1 on every start, a windowed one stops at the errors, and
no plugin code runs to repair it (measured with core 1.7.1 -> 1.13.0 and Animation 2.1.1). The core
package's version is static data Unity reads before it compiles, so a module that names its core is
skipped in that first compile and the update heals itself.

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
    ├── 6000.0.85f1/                                  separate project/Library
    ├── 6000.3.27f1/                                   separate project/Library
    └── 6000.6.7f1/                                   separate project/Library
```

## Tests and isolation

Before opening a test project run:

```shell
python commands/extensions.py prepare --project Unity-Tests/2022.3.62f3
```

This adds local UPM packages, their `testables` entries and the original installer test assets.
It sets the current NuGet assembly gate after checking the bundled DLLs exist, disables
installer self-installation in the test project, and materializes platform-specific fixture
newlines. It preserves newer Unity package versions already selected by a project. Tests that render
(screenshots, editor views) are marked `[RequiresGraphicsDevice]` (category `Graphics`): with
`-nographics` they are ignored, so run the suite without it to cover them.

The four SDKs, both platform images and the original extension modes EditMode, PlayMode and
Standalone remain covered. The core's 16 existing editor legs stay; eight shared PlayMode legs
restore the coverage present in the old extension repositories. There are 24 editor legs for the
entire monorepo, rather than 16 core legs plus 24 for each extension (256 total). Each leg launches
one shared editor for core and every extension. EditMode legs run with `-nographics`: the runners
render on the CPU, where each new scene costs seconds. One more EditMode leg per Unity version (on
`base`) runs only the `Graphics` category with the renderer. Standalone retains player build/runtime checks.
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
Unity 6.6 additionally requires Input System 1.19.0 (1.8.2 fails there with CS0619), Navigation 2.0.10,
ProBuilder 6.1.3 and Tilemap Extras 7.0.1.
The core's `extensions.compatibility.json` defines these conditional floors; CLI, editor installation,
the four self-contained installers and test preparation select them before Unity imports packages.
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

### Published versions

A published module version never changes, and its assemblies compile only with the core release
they name. `python commands/extensions.py validate` fails when a module's asmdefs do not name the
core floor of its `package.json`. `python commands/extensions.py check-published` reads the release
tags of the remote and fails when:

- a file of a published version (package or installer) differs from that release: the version must move;
- a module's core floor is not the core version of this tree: the modules are tested and published
  with the core they sit beside;
- that core is released with another gate generation. While the generation has moved but no core
  release ships it yet, this part is only a note: the next core release takes the modules along.
  (The change that moves the generation rewrites every module's asmdefs, so it still needs `settle`
  for the new module versions.)

It runs on every pull request and in the core release. Before a publication the check is strict:
the floor must be a released core with the same generation, so a module is never published ahead
of the core it needs.

`python commands/extensions.py settle [--core-version <core>]` repairs all of it: it writes the
core floor into the asmdefs, patch-bumps each published module whose files changed and, given the
core version of this tree (after `bump_version.py`), moves every floor to it. The release train runs
it for every core release, so each core release has its own module versions; the module release
follows the core release (`extensions release --engine unity`).

### Installed extensions follow the plugin

The plugin version is the anchor: every way of changing it leaves the project with the extension
versions built for it, and an extension built for another plugin release moves to the one for the
installed plugin, also when that is an older one.

| The plugin changes through | The extensions |
|---|---|
| the plugin's update notification | move in the same package request to their versions for the new plugin |
| `unity-mcp-cli update` | move in the same manifest edit to their versions for the plugin it installs |
| Package Manager, a manifest edit, a pull, `install-plugin` | are skipped by Unity (never a compile error) and moved by the startup check |

The version built for a plugin is the highest published one whose dependency on the plugin names
exactly that plugin version (`ExtensionCompatibility.VersionFor`; `extensionVersionForPlugin` in the
CLI), never `dist-tags.latest`: that one may be built for another plugin release.

`ExtensionAutoUpdater` is the startup check. On plugin startup, after every domain reload (once a
compile or Play Mode has ended):

- It reads the asmdefs of the installed packages that depend on the plugin (no network). An
  extension whose `UNITY_MCP_DEPS_<n>` or core version does not match the plugin is skipped by Unity.
- A skipped first-party extension installed from the registry moves to the version for this plugin.
  One with no published version for this plugin yet is checked again at the first domain reload
  30 minutes later and in the next editor session.
- A first-party registry extension that compiles but does not name its core release (published
  before the pairing) moves to the version for this plugin that does: otherwise the next plugin
  update would break the compile on it.
- Everything it does not move is named in a console warning with what to do: an extension from
  `file:` or git, a third-party one, one with no published version for this plugin, and every
  skipped extension in a batch-mode editor or in a project that turned update notifications off for
  the team.

Between a plugin release and its extension release no version for the new plugin exists: an
extension stays skipped, is reported, and moves once the matching version is published.

The Extensions section of the plugin window installs and offers the same version
(`UpdateChecker.FetchExtensionVersionAsync`). An installed extension that Unity skips shows
**Update** (to the version for this plugin) or, when none is published, **Inactive** with the reason.
`unity-mcp-cli install-extension` installs the version for the plugin release in the manifest and
fails with the reason when none is published.

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
