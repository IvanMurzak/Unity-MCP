"""Independent Unity extension packages; no editor launch or publication on import."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil

ROOT = Path(__file__).resolve().parents[1]
CORE = 'com.ivanmurzak.unity.mcp'


def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def write(path, value):
    Path(path).write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')


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
                if hashlib.sha256((package / name).read_bytes()).hexdigest() != digest:
                    raise ValueError(f'Original extension test changed: {package / name}')
        installer = read(package.parents[1] / 'migration-source.json').get('installerFiles', {})
        for name, digest in installer.items():
            if '/Tests/' in '/' + name and name.endswith(('.cs', '.asmdef')):
                source = package.parents[1] / 'Installer/Assets' / name
                if not source.is_file() or hashlib.sha256(source.read_bytes()).hexdigest() != digest:
                    raise ValueError(f'Original installer test changed or missing: {source}')
    return rows


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
            text = readme.read_text(encoding='utf-8')
            text = text.replace(row['packageId'] + '%2F' + previous, row['packageId'] + '%2F' + version)
            readme.write_text(text, encoding='utf-8')
        print(f'{row["name"]}: {previous} -> {version}')


def prepare(project: Path, root=ROOT):
    """Wire all modules into one *version-specific* project; no shared Library directory."""
    rows = validate(root)
    manifest_path = project / 'Packages/manifest.json'
    manifest = read(manifest_path)
    dependencies = manifest.setdefault('dependencies', {})
    tests = set(manifest.get('testables', []))
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
    config = (root / 'Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/DependencyResolver/NuGetConfig.cs').read_text()
    generation = re.search(r'public const string DependencyGenerationDefine = DependencyGenerationDefinePrefix \+ "(\d+)";', config)
    if not generation:
        raise ValueError('Cannot read current NuGet assembly gate')
    dll_root = project / 'Assets/Plugins/NuGet'
    native = read(dll_root / '.nuget-installed.json')
    for package in native['packages'].values():
        for dll in package['dlls']:
            if not (dll_root / dll).is_file():
                raise ValueError(f'Test project is missing bundled dependency: {dll}')
    defines = {value for value in defines if not value.startswith('UNITY_MCP_DEPS_')}
    defines.update(('IVAN_MURZAK_INSTALLER_PROJECT', 'UNITY_MCP_READY', 'UNITY_MCP_DEPS_' + generation.group(1)))
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


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['validate', 'prepare', 'list', 'bump', 'collect-metas'])
    parser.add_argument('--project', type=Path)
    parser.add_argument('--only', default='all')
    parser.add_argument('--version')
    args = parser.parse_args()
    if args.command == 'bump':
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
