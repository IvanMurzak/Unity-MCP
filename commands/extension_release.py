"""Publish only changed, independently signed UPM components from the Unity monorepo."""
from __future__ import annotations

import argparse
import gzip
import hashlib
import io
import json
import os
from pathlib import Path
import re
import subprocess
import tarfile
import urllib.request

from extensions import ROOT, read, validate

SLUG = 'IvanMurzak/Unity-MCP'


def run(args):
    return subprocess.run(args, cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)


def checked(args):
    result = run(args)
    if result.returncode:
        raise RuntimeError(f'{args[0]} {args[1]} failed: {result.stderr[:500]}')
    return result.stdout


def selected(selection='all'):
    rows = validate()
    if selection != 'all':
        names = set(selection.split(','))
        rows = [row for row in rows if names.intersection((row['name'], row['packageId']))]
        found = {name for row in rows for name in (row['name'], row['packageId'])}
        if names - found:
            raise ValueError('Unknown modules: ' + ', '.join(sorted(names - found)))
    if not rows:
        raise ValueError('No extensions selected')
    for row in rows:
        row['version'] = read(ROOT / row['package'] / 'package.json')['version']
        if not re.fullmatch(r'\d+\.\d+\.\d+', row['version']):
            raise ValueError('Component version must be stable semver')
        row['tag'] = row['tagPrefix'] + row['version']
        row['archive'] = row['packageId'] + '-' + row['version'] + '.tgz'
        row['installer'] = 'AI-' + row['name'] + '-Installer.unitypackage'
    return rows


def release(row):
    result = run(['gh', 'release', 'view', row['tag'], '--repo', SLUG, '--json', 'apiUrl'])
    if result.returncode:
        if 'release not found' in result.stderr.lower():
            return None
        raise RuntimeError('Cannot inspect component release: ' + result.stderr[:300])
    url = json.loads(result.stdout)['apiUrl']
    if not re.fullmatch(r'https://api\.github\.com/repos/IvanMurzak/Unity-MCP/releases/\d+', url):
        raise ValueError('Unexpected component release identity')
    return json.loads(checked(['gh', 'api', url]))


def openupm_metadata(package_id):
    url = 'https://raw.githubusercontent.com/openupm/openupm/master/data/packages/' + package_id + '.yml'
    request = urllib.request.Request(url, headers={'User-Agent': 'Unity-MCP-component-release'})
    with urllib.request.urlopen(request, timeout=30) as response:
        text = response.read().decode('utf-8')
    # The registry's routing fields are scalar strings. Do not require a YAML dependency.
    values = {}
    for line in text.splitlines():
        match = re.fullmatch(r'([A-Za-z]+):\s*(.*?)\s*', line)
        if match:
            value = match[2]
            if value.startswith('"'):
                value = json.loads(value)
            elif value.startswith("'") and value.endswith("'"):
                value = value[1:-1].replace("''", "'")
            values[match[1]] = value
    return values


def validate_routes(rows):
    core = openupm_metadata('com.ivanmurzak.unity.mcp')
    ignored = core.get('gitTagIgnore', '')
    if (not ignored or any(not re.search(ignored, row['tag']) for row in rows)
            or re.search(ignored, '1.1.0')):
        raise ValueError('Core OpenUPM registration must exclude extension tags before publication')
    for row in rows:
        metadata = openupm_metadata(row['packageId'])
        if (metadata.get('repoUrl', '').removesuffix('.git').rstrip('/') != 'https://github.com/' + SLUG
                or metadata.get('trackingMode') != 'githubRelease'
                or metadata.get('gitTagPrefix') != row['tagPrefix']
                or metadata.get('githubReleaseAssetName') != row['packageId'] + '-'):
            raise ValueError('OpenUPM registration has not adopted component routing: ' + row['packageId'])


def verify_archive(path, row, *, source_slug=SLUG, source_sha=None):
    package = ROOT / row['package']
    with tarfile.open(path, 'r:gz') as archive:
        members = archive.getmembers()
        names = [member.name.rstrip('/') for member in members]
        if len(names) != len(set(names)):
            raise ValueError('Duplicate UPM archive entries')
        for member in members:
            parts = Path(member.name).parts
            if (member.name.startswith('/') or '..' in parts or member.issym() or member.islnk()
                    or not parts or parts[0] != 'package'):
                raise ValueError('Unsafe UPM entry: ' + member.name)
            if member.isfile() and member.name not in ('package/.attestation.p7m', 'package/package.json'):
                source = package.joinpath(*parts[1:])
                if not source.is_file() or archive.extractfile(member).read() != source.read_bytes():
                    raise ValueError('UPM payload differs from this component: ' + member.name)
        if 'package/.attestation.p7m' not in names or archive.getmember('package/.attestation.p7m').size == 0:
            raise ValueError('Signed UPM attestation is missing')
        metadata = json.load(archive.extractfile('package/package.json'))
        expected = read(package / 'package.json')
        # Unity's real signing CLI injects VCS provenance and reserializes JSON.
        # Bind that generated field to this source; every native manifest field must remain exact.
        if 'repository' not in expected and 'repository' in metadata:
            repository = metadata.pop('repository')
            revision = source_sha or checked(['git', 'rev-parse', 'HEAD']).strip()
            if (not isinstance(repository, dict) or repository.get('type') != 'git'
                    or repository.get('url', '').removesuffix('.git').rstrip('/') != 'https://github.com/' + source_slug
                    or not re.fullmatch(r'[a-f0-9]{40}', revision) or repository.get('revision') != revision
                    or set(repository) - {'type', 'url', 'revision', 'directory'}
                    or ('directory' in repository and repository['directory'] != row['package'])):
                raise ValueError('UPM repository provenance differs from this source')
        if metadata != expected:
            raise ValueError('UPM manifest payload differs from this component')
        if metadata['name'] != row['packageId'] or metadata['version'] != row['version']:
            raise ValueError('UPM coordinates differ')
        required = [p.relative_to(package).as_posix() for folder in ('Editor', 'Runtime')
                    for p in (package / folder).rglob('*') if p.is_file() and p.name != '.gitignore']
        if not required or any('package/' + name not in names for name in required):
            raise ValueError('UPM archive omits native runtime/editor files')


def build_installer(path, row):
    """Unitypackage is a gzip tar of GUID/{asset,asset.meta,pathname}; use original Unity metas.
    No Unity instance is needed to serialize these unchanged text assets and existing GUIDs."""
    asset_root = ROOT / 'extensions' / row['name'] / 'Installer/Assets'
    folders = [p for p in (asset_root / 'com.IvanMurzak').iterdir() if p.is_dir()]
    if len(folders) != 1:
        raise ValueError('Expected exactly one installer asset folder')
    paths = [folders[0], *sorted(folders[0].rglob('*'))]
    guids = set()
    buffer = io.BytesIO()
    with tarfile.open(fileobj=buffer, mode='w') as archive:
        def add(name, data):
            info = tarfile.TarInfo(name)
            info.size = len(data)
            info.mode = 0o644
            archive.addfile(info, io.BytesIO(data))
        for source in paths:
            if source.suffix == '.meta':
                continue
            meta = source.with_name(source.name + '.meta')
            guid = re.search(r'(?m)^guid: ([a-f0-9]{32})$', meta.read_text())
            if not guid or guid[1] in guids:
                raise ValueError('Missing/duplicate original installer GUID: ' + str(meta))
            guids.add(guid[1])
            add(guid[1] + '/asset', b'' if source.is_dir() else source.read_bytes())
            add(guid[1] + '/asset.meta', meta.read_bytes())
            add(guid[1] + '/pathname', ('Assets/' + source.relative_to(asset_root).as_posix()).encode())
    if not guids:
        raise ValueError('Empty installer')
    with path.open('wb') as output, gzip.GzipFile(filename='', mode='wb', fileobj=output, mtime=0) as compressed:
        compressed.write(buffer.getvalue())


def assets_valid(value, row):
    if value.get('draft') or value.get('prerelease'):
        return False
    for name in (row['archive'], row['installer'], 'SHA256SUMS'):
        matches = [a for a in value.get('assets', []) if a['name'] == name]
        if len(matches) != 1 or matches[0].get('state') != 'uploaded' or not re.fullmatch(
                r'sha256:[a-f0-9]{64}', matches[0].get('digest', '')):
            raise ValueError('Published component has unproven asset: ' + name)
    return True


def build(row, dist):
    folder = dist / row['name']
    folder.mkdir(parents=True, exist_ok=True)
    existing = release(row)
    if existing and assets_valid(existing, row):
        print(row['name'] + ': already published; no rebuild')
        return
    archive = folder / row['archive']
    # A retry reuses an existing uploaded, immutable signed archive, including one in a draft.
    uploaded = existing and any(a['name'] == row['archive'] for a in existing.get('assets', []))
    if uploaded and not archive.exists():
        checked(['gh', 'release', 'download', row['tag'], '--repo', SLUG,
                 '--pattern', row['archive'], '--dir', str(folder)])
    elif not archive.exists():
        if any(not os.environ.get(name) for name in ('UPM_SERVICE_ACCOUNT_KEY_ID',
                'UPM_SERVICE_ACCOUNT_KEY_SECRET', 'UPM_ORG_ID')):
            raise ValueError('Existing Unity UPM signing credentials are required')
        checked(['upm', 'pack', './' + row['package'], '--organization-id', os.environ['UPM_ORG_ID'],
                 '--destination', str(folder)])
    verify_archive(archive, row)
    build_installer(folder / row['installer'], row)
    sums = ''.join(hashlib.sha256((folder / name).read_bytes()).hexdigest() + '  ' + name + '\n'
                   for name in (row['archive'], row['installer']))
    (folder / 'SHA256SUMS').write_text(sums)
    print(row['name'] + ': signed native package and installer verified')


def publish(row, dist):
    if os.environ.get('GITHUB_ACTIONS') != 'true' or os.environ.get('GITHUB_REF') != 'refs/heads/main':
        raise ValueError('Publication is restricted to the main GitHub workflow')
    existing = release(row)
    if existing and assets_valid(existing, row):
        return
    if existing is None:
        checked(['gh', 'release', 'create', row['tag'], '--repo', SLUG, '--target', os.environ['GITHUB_SHA'],
                 '--draft', '--title', row['name'] + ' ' + row['version'], '--notes',
                 'Independent signed UPM package. Documentation: https://github.com/' + SLUG
                 + '/blob/main/extensions/' + row['name'] + '/README.md', '--latest=false'])
        existing = release(row)
    folder = dist / row['name']
    verify_archive(folder / row['archive'], row)
    for name in (row['archive'], row['installer'], 'SHA256SUMS'):
        digest = hashlib.sha256((folder / name).read_bytes()).hexdigest()
        matches = [a for a in existing['assets'] if a['name'] == name]
        if matches:
            if len(matches) != 1 or matches[0].get('digest') != 'sha256:' + digest:
                raise ValueError('Refusing to overwrite an immutable component asset: ' + name)
        else:
            checked(['gh', 'release', 'upload', row['tag'], str(folder / name), '--repo', SLUG])
    checked(['gh', 'release', 'edit', row['tag'], '--repo', SLUG, '--draft=false', '--latest=false'])
    if not assets_valid(release(row), row):
        raise ValueError('Component release did not become public')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=('plan', 'build', 'publish', 'routes'))
    parser.add_argument('--only', default='all')
    parser.add_argument('--dist', type=Path, default=ROOT / '.artifacts/extensions')
    args = parser.parse_args()
    rows = selected(args.only)
    if args.command in ('routes', 'publish'):
        validate_routes(rows)
        if args.command == 'routes':
            print('OpenUPM component and core routes verified')
            return
    for row in rows:
        if args.command == 'plan':
            existing = release(row)
            print(row['name'], row['version'], 'published' if existing and assets_valid(existing, row) else 'pending')
        elif args.command == 'build':
            build(row, args.dist.resolve())
        else:
            publish(row, args.dist.resolve())


if __name__ == '__main__':
    main()
