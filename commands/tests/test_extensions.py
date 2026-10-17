import io
import json
import re
from pathlib import Path
import shutil
import sys
import tarfile
import tempfile
import unittest
from unittest.mock import patch

COMMANDS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(COMMANDS))
import extensions
import extension_release as release


class PackageBoundaryTests(unittest.TestCase):
    def test_existing_newer_sdk_dependency_is_preserved(self):
        self.assertEqual('1.19.0', extensions.dependency_floor('1.19.0', '1.8.2'))
        self.assertEqual('1.8.2', extensions.dependency_floor('1.7.0', '1.8.2'))
        with self.assertRaisesRegex(ValueError, 'incompatible dependency coordinates'):
            extensions.dependency_floor('file:../unrelated', '1.8.2')

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        rows = extensions.inventory()[:2]
        extensions.write(self.root / 'manifest-unused.json', {})
        for row in rows:
            source = extensions.ROOT / row['package']
            destination = self.root / row['package']
            shutil.copytree(source, destination)
            shutil.copy2(source.parents[1] / 'migration-source.json', destination.parents[1])
            shutil.copytree(source.parents[1] / 'Installer/Assets', destination.parents[1] / 'Installer/Assets')
        extensions.write(self.root / 'extensions/manifest.json', {'schemaVersion': 1, 'extensions': rows})
        self.rows = rows

    def test_original_packages_pass_then_undeclared_sibling_assembly_fails(self):
        self.assertEqual(2, len(extensions.validate(self.root)))
        row = self.rows[0]
        target = next((self.root / row['package'] / 'Editor').rglob('*.asmdef'))
        data = extensions.read(target)
        data['references'].append('com.IvanMurzak.Unity.MCP.Cinemachine.Editor')
        extensions.write(target, data)
        with self.assertRaisesRegex(ValueError, 'undeclared sibling dependency'):
            extensions.validate(self.root)
        package = self.root / row['package'] / 'package.json'
        metadata = extensions.read(package)
        metadata['dependencies'][self.rows[1]['packageId']] = '2.0.0'
        extensions.write(package, metadata)
        self.assertEqual(2, len(extensions.validate(self.root)))

    def test_original_test_deletion_or_assertion_changes_fail(self):
        test = next((self.root / self.rows[0]['package'] / 'Tests').rglob('*.cs'))
        original = test.read_bytes()
        test.write_bytes(original + b'\n// test changed\n')
        with self.assertRaisesRegex(ValueError, 'Original extension test changed'):
            extensions.validate(self.root)
        test.unlink()
        with self.assertRaisesRegex(ValueError, 'Missing original C#'):
            extensions.validate(self.root)

    def test_duplicate_native_guid_fails(self):
        first = next((self.root / self.rows[0]['package']).rglob('*.meta'))
        second = next((self.root / self.rows[1]['package']).rglob('*.meta'))
        second.write_bytes(first.read_bytes())
        with self.assertRaisesRegex(ValueError, 'Duplicate Unity GUID'):
            extensions.validate(self.root)

    def test_sync_core_is_selected_local_idempotent_and_dry_run_writes_nothing(self):
        path = self.root / self.rows[0]['package'] / 'package.json'
        untouched = self.root / self.rows[1]['package'] / 'package.json'
        before, other = path.read_bytes(), untouched.read_bytes()
        previous = extensions.read(path)['version']
        extensions.sync_core('1.1.0', 'Unity-AI-Animation', dry_run=True, root=self.root)
        self.assertEqual(before, path.read_bytes())
        extensions.sync_core('1.1.0', 'Animation', root=self.root)
        info = extensions.read(path)
        self.assertEqual('1.1.0', info['dependencies'][extensions.CORE])
        self.assertNotEqual(previous, info['version'])
        self.assertEqual(other, untouched.read_bytes())
        current = path.read_bytes()
        extensions.sync_core('1.1.0', 'Animation', root=self.root)
        self.assertEqual(current, path.read_bytes())


class ReleaseArtifactTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.folder = Path(self.temp.name)
        self.row = release.selected('Animation')[0]

    def archive(self, name='com.ivanmurzak.unity.mcp.animation', signed=True, extra=None, metadata=None, omit=None):
        target = self.folder / 'native.tgz'
        package = extensions.ROOT / self.row['package']
        with tarfile.open(target, 'w:gz') as archive:
            for source in package.rglob('*'):
                if source.is_file() and source.name != '.gitignore' and source.relative_to(package).as_posix() != omit:
                    if source.name == 'package.json' and metadata is not None:
                        data = json.dumps(metadata).encode()
                        info = tarfile.TarInfo('package/package.json')
                        info.size = len(data)
                        archive.addfile(info, io.BytesIO(data))
                    else:
                        archive.add(source, arcname='package/' + source.relative_to(package).as_posix())
            entries = {'package/.attestation.p7m': b'signature'} if signed else {}
            if extra:
                entries.update(extra)
            for path, content in entries.items():
                info = tarfile.TarInfo(path)
                info.size = len(content)
                archive.addfile(info, io.BytesIO(content))
        return target

    def test_unsigned_archive_rejected_signed_native_bytes_accepted(self):
        with self.assertRaisesRegex(ValueError, 'attestation'):
            release.verify_archive(self.archive(signed=False), self.row)
        release.verify_archive(self.archive(), self.row)

    def test_foreign_component_file_or_path_traversal_rejected(self):
        with self.assertRaisesRegex(ValueError, 'payload differs'):
            release.verify_archive(self.archive(extra={'package/Editor/Foreign.cs': b'foreign'}), self.row)
        with self.assertRaisesRegex(ValueError, 'Unsafe UPM'):
            release.verify_archive(self.archive(extra={'package/../escape.cs': b'escape'}), self.row)

    def test_signer_generated_repository_is_bound_without_loosening_native_metadata(self):
        original = extensions.read(extensions.ROOT / self.row['package'] / 'package.json')
        sha = 'a' * 40
        provenance = {'url': 'https://github.com/IvanMurzak/Unity-MCP', 'type': 'git', 'revision': sha}
        metadata = {**original, 'repository': provenance}
        with patch.object(release, 'checked', return_value=sha):
            release.verify_archive(self.archive(metadata=metadata), self.row)
            for field, value in [('url', 'https://github.com/Other/Repo'), ('revision', 'b' * 40),
                                 ('directory', 'extensions/Other/Packages/foreign')]:
                changed = {**metadata, 'repository': {**provenance, field: value}}
                with self.assertRaisesRegex(ValueError, 'provenance differs'):
                    release.verify_archive(self.archive(metadata=changed), self.row)
            for field, value in [('version', '99.0.0'), ('dependencies', {})]:
                with self.assertRaisesRegex(ValueError, 'manifest payload differs'):
                    release.verify_archive(self.archive(metadata={**metadata, field: value}), self.row)

    def test_native_source_file_may_not_be_omitted(self):
        package = extensions.ROOT / self.row['package']
        source = next((package / 'Editor').rglob('*.cs')).relative_to(package).as_posix()
        with self.assertRaisesRegex(ValueError, 'omits native'):
            release.verify_archive(self.archive(omit=source), self.row)

    def test_installer_is_reproducible_and_contains_original_guid_and_file_bytes(self):
        first, second = self.folder / 'a.unitypackage', self.folder / 'b.unitypackage'
        release.build_installer(first, self.row)
        release.build_installer(second, self.row)
        self.assertEqual(first.read_bytes(), second.read_bytes())
        native = extensions.ROOT / 'extensions/Animation/Installer'
        with tarfile.open(first, 'r:gz') as archive:
            paths = [m for m in archive.getmembers() if m.name.endswith('/pathname')]
            self.assertGreater(len(paths), 10)
            for entry in paths:
                name = archive.extractfile(entry).read().decode()
                source = native / name
                guid = entry.name.split('/')[0]
                self.assertEqual(source.with_name(source.name + '.meta').read_bytes(),
                                 archive.extractfile(guid + '/asset.meta').read())
                self.assertEqual(b'' if source.is_dir() else source.read_bytes(),
                                 archive.extractfile(guid + '/asset').read())

    def test_published_component_is_never_rebuilt(self):
        value = {'draft': False, 'prerelease': False, 'assets': [
            {'name': name, 'state': 'uploaded', 'digest': 'sha256:' + 'a' * 64}
            for name in (self.row['archive'], self.row['installer'], 'SHA256SUMS')]}
        with patch.object(release, 'release', return_value=value), patch.object(release, 'checked') as call:
            release.build(self.row, self.folder)
        call.assert_not_called()
        self.assertFalse((self.folder / self.row['name'] / self.row['archive']).exists())

    def test_route_gate_rejects_old_repository_and_unfiltered_core_tags(self):
        core = {'gitTagIgnore': r'^com\.ivanmurzak\.unity\.mcp\.'}
        component = {'repoUrl': 'https://github.com/IvanMurzak/Unity-MCP',
                     'trackingMode': 'githubRelease', 'gitTagPrefix': self.row['tagPrefix'],
                     'githubReleaseAssetName': self.row['packageId'] + '-'}
        def lookup(name):
            return core if name == 'com.ivanmurzak.unity.mcp' else component
        with patch.object(release, 'openupm_metadata', side_effect=lookup):
            release.validate_routes([self.row])
            component['repoUrl'] = 'https://github.com/IvanMurzak/Unity-AI-Animation'
            with self.assertRaisesRegex(ValueError, 'has not adopted'):
                release.validate_routes([self.row])
            core['gitTagIgnore'] = ''
            with self.assertRaisesRegex(ValueError, 'exclude extension tags'):
                release.validate_routes([self.row])


class WorkflowCoverageTests(unittest.TestCase):
    def test_pr_reporter_receives_xml_including_train_prs(self):
        text = (extensions.ROOT / '.github/workflows/test_unity_plugin.yml').read_text()
        steps = re.split(r'(?m)^      - ', text)
        uploads = [s for s in steps if s.startswith('uses: actions/upload-artifact@')
                   and 'name: Test results for ' in s]
        self.assertEqual(1, len(uploads))
        condition = re.search(r'(?m)^        if: (.+)$', uploads[0]).group(1)

        def evaluate(expression, event, lock, labels):
            values = {'github.event.pull_request.labels.*.name': labels,
                      'github.event_name': event, 'env.CHAIN_LOCK': lock}
            for name, value in values.items():
                expression = expression.replace(name, repr(value))
            expression = expression.replace('&&', ' and ').replace('||', ' or ')
            expression = re.sub(r'!(?!=)', 'not ', expression)
            return eval(expression, {'__builtins__': {}}, {
                'always': lambda: True, 'contains': lambda values, value: value in values,
                'join': lambda values, separator: separator.join(values)})

        # The workflow_run reporter consumes XML for every same-repository PR.
        # Dispatch collection consumes JSON only; retain that storage policy.
        cases = [('pull_request', '', [], True),
                 ('pull_request', '', ['train/example'], True),
                 ('pull_request', '{"lock":true}', ['train/example'], True),
                 ('workflow_dispatch', '{"lock":true}', [], False),
                 ('workflow_dispatch', '', [], True)]
        for event, lock, labels, expected in cases:
            with self.subTest(event=event, lock=bool(lock), labels=labels):
                self.assertEqual(expected, evaluate(condition, event, lock, labels))
        old = "always() && env.CHAIN_LOCK == '' && !contains(join(github.event.pull_request.labels.*.name, ' '), 'train/')"
        self.assertFalse(evaluate(old, 'pull_request', '', ['train/example']))
        self.assertTrue(evaluate(old, 'pull_request', '', []))

    def check_matrix(self, text, filename):
        parts = re.split(r'(?m)^  ([a-z][a-z0-9-]*):\s*$', text)
        jobs = dict(zip(parts[1::2], parts[2::2]))
        versions = extensions.read(extensions.ROOT / 'extensions/manifest.json')['testProjects']
        keys = []
        for version in versions:
            for mode in ('editmode', 'playmode', 'standalone'):
                key = 'test-unity-' + version.replace('.', '-') + '-' + mode
                self.assertIn(key, jobs, f'{filename} lost the original {version}/{mode} coverage')
                body = jobs[key]
                self.assertIn(f'./Unity-Tests/{version}', body)
                self.assertRegex(body, r'testMode: [\'"]' + mode + r'[\'"]')
                if filename == 'test_pull_request.yml':
                    self.assertIn('lock: ${{ inputs.lock }}', body)
                    self.assertIn('lock_hash8: ${{ inputs.lock_hash8 }}', body)
                elif filename == 'test_pull_request_manual.yml':
                    self.assertIn('needs: resolve', body)
                    self.assertIn('ref: ${{ needs.resolve.outputs.ref }}', body)
                keys.append(key)
        if filename == 'release.yml':
            for publisher in ('deploy-cli-to-npm', 'release-unity-plugin'):
                active = '\n'.join(line.split('#', 1)[0] for line in jobs[publisher].splitlines())
                barrier = active.split('needs: [', 1)[1].split(']', 1)[0]
                for key in keys:
                    self.assertIn(key + ',', barrier, f'{publisher} can publish without {key}')

    def test_original_modes_remain_active_and_gate_every_publication(self):
        for filename in ('test_pull_request.yml', 'test_pull_request_manual.yml', 'release.yml'):
            text = (extensions.ROOT / '.github/workflows' / filename).read_text()
            self.check_matrix(text, filename)
            missing_playmode = text.replace('  test-unity-2022-3-62f3-playmode:', '  removed-playmode:')
            with self.assertRaisesRegex(AssertionError, 'lost the original'):
                self.check_matrix(missing_playmode, filename)


if __name__ == '__main__':
    unittest.main()
