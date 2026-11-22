"""Independent Unity extension packages; no editor launch or publication on import."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[1]
CORE = 'com.ivanmurzak.unity.mcp'
CORE_PACKAGE = 'Unity-MCP-Plugin/Packages/' + CORE
NUGET_CONFIG = CORE_PACKAGE + '/Editor/DependencyResolver/NuGetConfig.cs'
GENERATION_RE = r'public const string DependencyGenerationDefine = DependencyGenerationDefinePrefix \+ "(\d+)";'
VERSION_RE = r'\d+\.\d+\.\d+'


def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def write(path, value):
    # In the newline style the file already has: /extensions/** is `-text` in .gitattributes, so its bytes
    # are committed as written and a host-dependent newline would rewrite every line of a CRLF file on Linux.
    path = Path(path)
    newline = os.linesep if not path.is_file() else '\r\n' if b'\r\n' in path.read_bytes() else '\n'
    path.write_bytes((json.dumps(value, indent=2) + '\n').replace('\n', newline).encode('utf-8'))


def dependency_floor(prior, version):
    if prior is None or prior == version:
        return version
    def parse(value):
        if not re.fullmatch(r'\d+\.\d+\.\d+', value):
            raise ValueError(f'Cannot merge incompatible dependency coordinates: {prior}, {version}')
        return tuple(map(int, value.split('.')))
    return max((prior, version), key=parse)


def inventory(root=ROOT):
    data = read(root / 'extensions/manifest.json')
    if data.get('schemaVersion') != 1 or not data.get('extensions'):
        raise ValueError('Expected a nonempty schema-1 extension inventory')
    rows = data['extensions']
    for key in ('name', 'packageId', 'package', 'tagPrefix'):
        if len({row[key] for row in rows}) != len(rows):
            raise ValueError(f'Duplicate extension {key}')
    for row in rows:
        if (not re.fullmatch(r'[A-Za-z][A-Za-z0-9]*', row['name'])
                or not re.fullmatch(r'com\.ivanmurzak\.unity\.mcp\.[a-z0-9]+', row['packageId'])
                or row['package'] != f'extensions/{row["name"]}/Packages/{row["packageId"]}'
                or row['tagPrefix'] != row['packageId'] + '/'):
            raise ValueError('Invalid component folder, package or tag coordinates')
        package = (root / row['package']).resolve()
        if not package.is_relative_to((root / 'extensions').resolve()):
            raise ValueError('Extension package escapes extensions/')
        info = read(package / 'package.json')
        if info['name'] != row['packageId'] or CORE not in info['dependencies']:
            raise ValueError(f'Invalid package identity/core dependency: {package}')
    return rows


def validate(root=ROOT):
    rows = inventory(root)
    owners, guids = {}, {}
    for row in rows:
        package = root / row['package']
        for path in package.rglob('*.asmdef'):
            # Unity warns on every compilation about an assembly definition without scripts (in every consumer project for
            # shipped assemblies; test assemblies compile only where the package is testable).
            if 'Tests' not in path.relative_to(package).parts and not any(path.parent.rglob('*.cs')):
                raise ValueError(f'{path}: assembly definition has no scripts (add one, e.g. AssemblyInfo.cs)')
            name = read(path)['name']
            if name in owners:
                raise ValueError(f'Duplicate assembly {name}')
            owners[name] = row['packageId']
        for path in package.rglob('*.meta'):
            guid = next(line[6:].strip() for line in path.read_text().splitlines() if line.startswith('guid: '))
            if guid in guids:
                raise ValueError(f'Duplicate Unity GUID: {path} and {guids[guid]}')
            guids[guid] = path
    for row in rows:
        package = root / row['package']
        dependencies = read(package / 'package.json')['dependencies']
        for path in package.rglob('*.asmdef'):
            for reference in read(path).get('references', []):
                owner = owners.get(reference)
                if owner and owner != row['packageId'] and owner not in dependencies:
                    raise ValueError(f'{path}: undeclared sibling dependency {owner}')
        baseline = read(package.parents[1] / 'migration-source.json')['sourceFiles']
        actual = {p.relative_to(package).as_posix() for p in package.rglob('*.cs')}
        expected = {name for name in baseline if name.endswith('.cs')}
        if not expected.issubset(actual):
            raise ValueError(f'Missing original C# files: {expected - actual}')
        # A migration must retain every original test and its assertions unchanged.
        for name, digest in baseline.items():
            if name.startswith('Tests/') and name.endswith(('.cs', '.asmdef')):
                source = (package / name).read_bytes()
                if name.endswith('.asmdef'):
                    # Keep migration provenance immutable. Only the new opt-out constraint is permitted;
                    # removing its exact added bytes must recover the original test assembly definition.
                    source = re.sub(rb',\r?\n[ \t]*"!UNITY_MCP_EXCLUDE_FROM_COMPILATION"', b'', source)
                if hashlib.sha256(source).hexdigest() != digest:
                    raise ValueError(f'Original extension test changed: {package / name}')
        installer = read(package.parents[1] / 'migration-source.json').get('installerFiles', {})
        for name, digest in installer.items():
            if '/Tests/' in '/' + name and name.endswith(('.cs', '.asmdef')):
                source = package.parents[1] / 'Installer/Assets' / name
                if not source.is_file() or hashlib.sha256(source.read_bytes()).hexdigest() != digest:
                    raise ValueError(f'Original installer test changed or missing: {source}')
    policy_path = root / 'Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/extensions.compatibility.json'
    if policy_path.is_file():
        validate_compatibility(root, rows)
    return rows


def validate_compatibility(root=ROOT, rows=None):
    rows = inventory(root) if rows is None else rows
    policy = read(root / 'Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/extensions.compatibility.json')
    if policy.get('schemaVersion') != 1 or policy.get('minimumUnityVersion') != '6000.6':
        raise ValueError('Unsupported Unity compatibility policy')
    editor = (root / 'Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/Scripts/UI/Components/ExtensionPanel.cs').read_text()
    for entry in policy['extensions']:
        row = next(row for row in rows if row['packageId'] == entry['packageId'])
        package = read(root / row['package'] / 'package.json')
        installers = list((root / 'extensions' / row['name'] / 'Installer/Assets').rglob('Installer.cs'))
        if len(installers) != 1:
            raise ValueError('Missing self-contained installer compatibility policy')
        installer = installers[0].read_text()
        enabled = installer.split('#if !IVAN_MURZAK_INSTALLER_PROJECT', 1)[-1].split('#endif', 1)[0]
        if 'EnsureUnityPackageCompatibility(ManifestPath, Application.unityVersion);' not in enabled:
            raise ValueError('Installer does not apply compatibility before importing packages')
        for dependency, version in entry['dependencies'].items():
            if dependency not in package['dependencies']:
                raise ValueError('Compatibility policy may only upgrade an existing native dependency')
            pattern = (r'case "' + re.escape(row['packageId']) + r'":\s*return new Dictionary<string,\s*string>\s*'
                       + r'\{\s*\["' + re.escape(dependency) + r'"\]\s*=\s*"' + re.escape(version) + r'"\s*\}')
            if (not re.search(pattern, editor)
                    or f'const string dependency = "{dependency}";' not in installer
                    or f'const string required = "{version}";' not in installer):
                raise ValueError('C#/installer compatibility mirror drift: ' + row['name'])


def bump(name, version, root=ROOT):
    if not re.fullmatch(r'\d+\.\d+\.\d+', version):
        raise ValueError('Expected a stable component version')
    rows = inventory(root)
    selected = [row for row in rows if name in ('all', row['name'], row['packageId'])]
    if not selected:
        raise ValueError('Unknown extension: ' + name)
    for row in selected:
        package = root / row['package'] / 'package.json'
        info = read(package)
        previous = info['version']
        if previous != version and dependency_floor(previous, version) != version:
            raise ValueError('Component versions may not go backwards')
        info['version'] = version
        write(package, info)
        module = package.parents[2]
        installer = list((module / 'Installer/Assets').rglob('Installer.cs'))
        if len(installer) != 1:
            raise ValueError('Expected one component installer version constant')
        source = installer[0].read_bytes()
        source, count = re.subn(rb'public const string Version = "\d+\.\d+\.\d+";',
                               f'public const string Version = "{version}";'.encode(), source)
        if count != 1:
            raise ValueError('Expected one installer version constant')
        installer[0].write_bytes(source)
        readme = module / 'README.md'
        if readme.is_file():
            # As bytes: every other byte of the file, newlines included, stays as it is.
            readme.write_bytes(readme.read_bytes().replace(f'{row["packageId"]}%2F{previous}'.encode(),
                                                           f'{row["packageId"]}%2F{version}'.encode()))
        print(f'{row["name"]}: {previous} -> {version}')


def prepare(project: Path, root=ROOT):
    """Wire all modules into one *version-specific* project; no shared Library directory."""
    rows = validate(root)
    manifest_path = project / 'Packages/manifest.json'
    manifest = read(manifest_path)
    dependencies = manifest.setdefault('dependencies', {})
    tests = set(manifest.get('testables', []))
    policy = read(root / 'Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/extensions.compatibility.json')
    editor_text = (project / 'ProjectSettings/ProjectVersion.txt').read_text()
    editor_match = re.search(r'(?m)^m_EditorVersion: (\d+)\.(\d+)', editor_text)
    if not editor_match:
        raise ValueError('Cannot read test project editor version')
    editor = tuple(map(int, editor_match.groups()))
    for row in rows:
        package = root / row['package']
        dependencies[row['packageId']] = 'file:' + os.path.relpath(package, project / 'Packages').replace('\\', '/')
        tests.add(row['packageId'])
        # Select the same external package floors an isolated native consumer resolves.
        for name, version in read(package / 'package.json')['dependencies'].items():
            if name == CORE:
                continue
            prior = dependencies.get(name)
            dependencies[name] = dependency_floor(prior, version)
        if editor >= tuple(map(int, policy['minimumUnityVersion'].split('.'))):
            for entry in policy['extensions']:
                if entry['packageId'] == row['packageId']:
                    for name, version in entry['dependencies'].items():
                        dependencies[name] = dependency_floor(dependencies.get(name), version)
        installer = package.parents[1] / 'Installer/Assets/com.IvanMurzak'
        if installer.is_dir():
            for folder in installer.iterdir():
                if folder.is_dir():
                    installed = project / 'Assets/com.IvanMurzak' / folder.name
                    shutil.copytree(folder, installed, dirs_exist_ok=True)
                    folder_meta = folder.with_name(folder.name + '.meta')
                    if folder_meta.is_file():
                        shutil.copy2(folder_meta, installed.with_name(installed.name + '.meta'))
                    # JSONObject serializes using Environment.NewLine. Keep upstream assertions
                    # byte-exact by materializing fixture newlines for this host, not editing tests.
                    for fixture in (installed / 'Tests/Files').rglob('*.json'):
                        fixture.write_bytes(fixture.read_text(encoding='utf-8-sig').replace('\n', os.linesep).encode())
    manifest['testables'] = sorted(tests)
    write(manifest_path, manifest)
    settings = project / 'ProjectSettings/ProjectSettings.asset'
    text = settings.read_text(encoding='utf-8')
    match = re.search(r'(?m)^  scriptingDefineSymbols:\n((?:    .*\n)*)', text)
    if not match:
        raise ValueError(f'{settings}: scriptingDefineSymbols block is missing')
    block = match.group(1)
    standalone = re.search(r'(?m)^    Standalone: *(.*)$', block)
    defines = set(filter(None, standalone.group(1).split(';'))) if standalone else set()
    generation = gate_generation((root / NUGET_CONFIG).read_text(encoding='utf-8'))
    if not generation:
        raise ValueError('Cannot read current NuGet assembly gate')
    dll_root = project / 'Assets/Plugins/NuGet'
    native = read(dll_root / '.nuget-installed.json')
    for package in native['packages'].values():
        for dll in package['dlls']:
            if not (dll_root / dll).is_file():
                raise ValueError(f'Test project is missing bundled dependency: {dll}')
    defines = {value for value in defines if not value.startswith('UNITY_MCP_DEPS_')}
    defines.update(('IVAN_MURZAK_INSTALLER_PROJECT', 'UNITY_MCP_READY', 'UNITY_MCP_DEPS_' + generation))
    value = '    Standalone: ' + ';'.join(sorted(defines))
    block = re.sub(r'(?m)^    Standalone:.*$', value, block) if standalone else block + value + '\n'
    text = text[:match.start(1)] + block + text[match.end(1):]
    text = re.sub(r'(?m)^  activeInputHandler: \d+$', '  activeInputHandler: 2', text)
    settings.write_text(text, encoding='utf-8')
    shutil.copytree(root / 'tests/extensions/Editor', project / 'Assets/ExtensionIsolation', dirs_exist_ok=True)
    print(f'{project.name}: prepared {len(rows)} extension packages and their installer tests')


def collect_metas(project, root=ROOT):
    """Copy only missing installer metadata generated by the shared editor for a NEW module."""
    count = 0
    for row in inventory(root):
        source = root / 'extensions' / row['name'] / 'Installer/Assets/com.IvanMurzak'
        if not source.is_dir():
            continue
        for asset in source.rglob('*'):
            if asset.suffix == '.meta':
                continue
            target = asset.with_name(asset.name + '.meta')
            generated = project / 'Assets/com.IvanMurzak' / asset.relative_to(source)
            generated = generated.with_name(generated.name + '.meta')
            if not target.exists():
                if not generated.is_file():
                    raise ValueError(f'Unity has not generated metadata yet: {generated}')
                shutil.copy2(generated, target)
                count += 1
    print(f'Collected {count} Unity-generated installer metadata files; existing GUIDs untouched')


def gate_generation(config):
    """The NuGet gate generation a core's NuGetConfig.cs declares (None: that core has no generation gate)."""
    match = re.search(GENERATION_RE, config)
    return match[1] if match else None


def next_version(version, policy='patch'):
    major, minor, patch = map(int, version.split('.'))
    return f'{major}.{minor + 1}.0' if policy == 'minor' else f'{major}.{minor}.{patch + 1}'


def set_core_floor(row, core_version, root=ROOT):
    path = root / row['package'] / 'package.json'
    package = read(path)
    package['dependencies'][CORE] = core_version
    write(path, package)


def sync_core(core_version, only='all', policy='patch', dry_run=False, root=ROOT):
    if not re.fullmatch(VERSION_RE, core_version):
        raise ValueError('Expected a stable core dependency version')
    selector = only.removeprefix('Unity-AI-')
    rows = [row for row in inventory(root) if selector in ('all', row['name'], row['packageId'])]
    if not rows:
        raise ValueError('Unknown extension selection: ' + only)
    for row in rows:
        package = read(root / row['package'] / 'package.json')
        previous = package['dependencies'][CORE]
        if previous == core_version or dependency_floor(previous, core_version) != core_version:
            print(row['name'] + ': core floor already current/newer; unchanged')
            continue
        version = next_version(package['version'], policy)
        print(f'{row["name"]}: core {previous} -> {core_version}; component -> {version}')
        if not dry_run:
            set_core_floor(row, core_version, root)
            bump(row['name'], version, root)


def git(root, *args, stdin=None):
    result = subprocess.run(['git', *args], cwd=root, text=True, encoding='utf-8', input=stdin,
                            stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if result.returncode:
        raise ValueError(f'git {args[0]} failed: {result.stderr.strip()[:300]}')
    return result.stdout


class Published:
    """What the remote has released, read from its tags: a core release is `<version>`, a component
    release `<packageId>/<version>`. A remote that cannot be read fails; it is never "unpublished"."""

    def __init__(self, root=ROOT, remote='origin', only=None, core=None):
        """`only`: the component names to read (default all); `core`: one more core version to look up."""
        self.root = root
        self.rows = [row for row in inventory(root) if only is None or row['name'] in only]
        self.core_version = read(root / CORE_PACKAGE / 'package.json')['version']
        self.generation = gate_generation((root / NUGET_CONFIG).read_text(encoding='utf-8'))
        if not self.generation:
            raise ValueError('Cannot read current NuGet assembly gate')
        for row in self.rows:
            package = read(root / row['package'] / 'package.json')
            row['version'], row['floor'] = package['version'], package['dependencies'][CORE]
            row['tag'] = row['tagPrefix'] + row['version']
        tags = {self.core_version, core, *(row[key] for row in self.rows for key in ('tag', 'floor'))} - {None}
        listed = git(root, 'ls-remote', '--tags', remote, *sorted('refs/tags/' + tag for tag in tags))
        self.shas = {ref.removeprefix('refs/tags/'): sha for sha, ref in map(str.split, listed.splitlines())}
        self.gates = {}
        # A CI checkout is shallow and has no tags: fetch the releases it lacks (a full clone stays full).
        tags = sorted(self.shas)
        present = git(root, 'cat-file', '--batch-check', stdin=''.join(self.shas[tag] + '^{tree}\n' for tag in tags))
        absent = [tag for tag, line in zip(tags, present.splitlines()) if line.endswith(' missing')]
        if absent:
            shallow = git(root, 'rev-parse', '--is-shallow-repository').strip() == 'true'
            git(root, 'fetch', '--no-tags', *(['--depth', '1'] if shallow else []), remote,
                *('refs/tags/' + tag for tag in absent))

    def changed(self, row):
        """Files of this component's published version that differ here (none: unpublished or unchanged)."""
        if row['tag'] not in self.shas:
            return []
        paths = (row['package'], f'extensions/{row["name"]}/Installer/Assets')
        # `git diff` leaves out a new file until it is added to the index: list those too.
        tracked = git(self.root, 'diff', '--name-only', self.shas[row['tag']], '--', *paths).splitlines()
        untracked = git(self.root, 'ls-files', '--others', '--exclude-standard', '--', *paths).splitlines()
        return sorted({*tracked, *untracked})

    def floor_sets_gate(self, floor):
        """Does core `floor` set the gate generation the component assemblies here require? On a core with
        another generation Unity skips them without an error. The unreleased core of this tree counts:
        its release ships this generation."""
        if floor not in self.shas:
            return floor == self.core_version
        if floor not in self.gates:
            sha = self.shas[floor]
            # A core older than the resolver has no NuGetConfig.cs, hence no generation.
            exists = git(self.root, 'ls-tree', '--name-only', sha, '--', NUGET_CONFIG).strip()
            self.gates[floor] = gate_generation(git(self.root, 'show', f'{sha}:{NUGET_CONFIG}')) if exists else None
        return self.gates[floor] == self.generation

    def gate_moved_since_release(self):
        """The gate moved after this tree's core version was released: no core release ships it yet."""
        return self.core_version in self.shas and not self.floor_sets_gate(self.core_version)


def check_published(root=ROOT, strict=False, only=None, remote='origin'):
    """Problems that leave a component (all, or the `only` names) stale or unusable for its consumers.
    `strict` (before a publication): every core floor must be a released core that sets this gate generation."""
    state = Published(root, remote, only)
    deferred = not strict and state.gate_moved_since_release()
    problems = []
    for row in state.rows:
        changed = state.changed(row)
        if changed:
            problems.append(f'{row["name"]} {row["version"]} is published, but {len(changed)} of its files changed '
                            f'since (e.g. {changed[0]}): the change reaches nobody until the version moves')
        version = re.fullmatch(VERSION_RE, row['floor'])
        released = version and row['floor'] in state.shas
        if strict and not released:
            problems.append(f'{row["name"]}: core floor {row["floor"]} is not a released core version')
        # Deferred only for a released core: until the next core release no version can replace it.
        elif not version or (not state.floor_sets_gate(row['floor']) and not (deferred and released)):
            problems.append(f'{row["name"]}: core floor {row["floor"]} does not set UNITY_MCP_DEPS_{state.generation}, '
                            'which its assemblies require: on that core Unity skips them without an error')
    if deferred and not problems:
        print(f'Note: the NuGet gate moved to UNITY_MCP_DEPS_{state.generation} after core {state.core_version}. '
              'The next core release must run `extensions.py settle --core-version <it>` (the release train does)')
    return problems


def settle(core_version=None, root=ROOT, dry_run=False, remote='origin'):
    """Make every component publishable again. A core floor that does not set the current gate generation
    moves to `core_version` (the core release that ships it); a published version whose files changed gets
    a patch bump."""
    if core_version and not re.fullmatch(VERSION_RE, core_version):
        raise ValueError('Expected a stable core dependency version')
    state = Published(root, remote, core=core_version)
    if core_version and not state.floor_sets_gate(core_version):
        raise ValueError(f'Core {core_version} does not ship UNITY_MCP_DEPS_{state.generation}: expected a release '
                         f'that sets it, or the unreleased version of this tree ({state.core_version})')
    for row in state.rows:
        refloor = core_version and not state.floor_sets_gate(row['floor'])
        bumped = row['tag'] in state.shas and (refloor or state.changed(row))
        if refloor:
            print(f'{row["name"]}: core floor {row["floor"]} -> {core_version}')
            if not dry_run:
                set_core_floor(row, core_version, root)
        if bumped:
            version = next_version(row['version'])
            if dry_run:
                print(f'{row["name"]}: {row["version"]} -> {version}')
            else:
                bump(row['name'], version, root)
    waiting = [row['name'] for row in state.rows if not core_version and not state.floor_sets_gate(row['floor'])]
    if waiting:
        print(f'Note: core floors left as they are ({", ".join(waiting)}): pass --core-version <the core release '
              f'that ships UNITY_MCP_DEPS_{state.generation}>')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['validate', 'prepare', 'prepare-all', 'list', 'bump', 'collect-metas', 'sync-core',
                                            'check-published', 'settle'])
    parser.add_argument('--project', type=Path)
    parser.add_argument('--only', default='all')
    parser.add_argument('--version')
    parser.add_argument('--core-version')
    parser.add_argument('--bump-policy', choices=['patch', 'minor'], default='patch')
    parser.add_argument('--dry-run', action='store_true')
    args = parser.parse_args()
    if args.command in ('check-published', 'settle') and args.only != 'all':
        parser.error(args.command + ' covers every extension; it takes no --only')
    if args.command == 'check-published':
        problems = check_published()
        if problems:
            raise SystemExit('\n'.join(['FAIL: ' + problem for problem in problems] + [
                'Fix: python commands/extensions.py settle [--core-version <the core release that ships this gate>]']))
        print('Published extension versions match this tree; their core floors set the current NuGet gate')
    elif args.command == 'settle':
        settle(args.core_version, dry_run=args.dry_run)
    elif args.command == 'sync-core':
        if not args.core_version:
            parser.error('sync-core requires --core-version')
        sync_core(args.core_version, args.only, args.bump_policy, args.dry_run)
    elif args.command == 'prepare-all':
        for version in read(ROOT / 'extensions/manifest.json')['testProjects']:
            if not re.fullmatch(r'\d+\.\d+\.\d+[abfp]\d+', version):
                raise ValueError('Unsafe test project editor version')
            prepare(ROOT / 'Unity-Tests' / version)
    elif args.command == 'bump':
        if not args.version:
            parser.error('bump requires --version')
        bump(args.only, args.version)
    elif args.command in ('prepare', 'collect-metas'):
        if not args.project:
            parser.error(args.command + ' requires --project')
        (prepare if args.command == 'prepare' else collect_metas)(args.project.resolve())
    elif args.command == 'validate':
        print(f'Validated {len(validate())} independent extension packages')
    else:
        print(json.dumps(inventory(), indent=2))


if __name__ == '__main__':
    main()
