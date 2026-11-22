import io
import json
import re
from pathlib import Path
import shutil
import subprocess
import sys
import tarfile
import tempfile
import unittest
from unittest.mock import patch

COMMANDS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(COMMANDS))
import extensions
import extension_release as release


def copy_components(root):
    """Two real components (package, installer, migration record) and their inventory under `root`."""
    rows = extensions.inventory()[:2]
    for row in rows:
        source = extensions.ROOT / row['package']
        destination = root / row['package']
        shutil.copytree(source, destination)
        shutil.copy2(source.parents[1] / 'migration-source.json', destination.parents[1])
        shutil.copytree(source.parents[1] / 'Installer/Assets', destination.parents[1] / 'Installer/Assets')
    extensions.write(root / 'extensions/manifest.json', {'schemaVersion': 1, 'extensions': rows})
    return rows


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
        extensions.write(self.root / 'manifest-unused.json', {})
        self.rows = copy_components(self.root)

    def test_ci_constraint_preserves_migration_checks(self):
        extensions.validate(self.root)
        package = self.root / self.rows[0]['package']
        asmdef = next((package / 'Tests').rglob('*.asmdef'))
        content = asmdef.read_bytes()
        # Adding the CI opt-out is allowed, but changing another test-assembly property is not.
        asmdef.write_bytes(content.replace(b'"autoReferenced": false', b'"autoReferenced": true'))
        with self.assertRaisesRegex(ValueError, 'Original extension test changed'):
            extensions.validate(self.root)

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

    def test_assembly_definition_without_scripts_fails(self):
        runtime = self.root / self.rows[0]['package'] / 'Runtime'
        for script in runtime.rglob('*.cs'):
            script.unlink()
        with self.assertRaisesRegex(ValueError, 'assembly definition has no scripts'):
            extensions.validate(self.root)

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

    def test_bump_keeps_the_newline_style_of_every_file_it_rewrites(self):
        # Both styles: the host's own newline hides one of them.
        row = self.rows[0]
        package = self.root / row['package'] / 'package.json'
        readme = package.parents[2] / 'README.md'
        version = extensions.read(package)['version']
        readme.write_bytes(f'# Module\nlink {row["packageId"]}%2F{version}/file\n'.encode())
        for newline in (b'\n', b'\r\n'):
            for path in (package, readme):
                path.write_bytes(path.read_bytes().replace(b'\r\n', b'\n').replace(b'\n', newline))
            lines = package.read_bytes().count(b'\n')
            version = extensions.next_version(version)
            extensions.bump(row['name'], version, self.root)
            for path in (package, readme):
                content = path.read_bytes()
                self.assertIn(version.encode(), content)
                self.assertEqual(content.count(b'\r\n'), content.count(b'\n') if newline == b'\r\n' else 0, path.name)
            self.assertEqual(lines, package.read_bytes().count(b'\n'))

    def test_sync_core_is_selected_local_idempotent_and_dry_run_writes_nothing(self):
        path = self.root / self.rows[0]['package'] / 'package.json'
        untouched = self.root / self.rows[1]['package'] / 'package.json'
        # Exercise an upgrade independently of the live package's current core floor.
        fixture = extensions.read(path)
        fixture['dependencies'][extensions.CORE] = '1.0.0'
        extensions.write(path, fixture)
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
        extensions.sync_core('1.0.0', 'Animation', root=self.root)
        self.assertEqual(current, path.read_bytes())


class PublishedVersionTests(unittest.TestCase):
    """A real git remote with release tags: core 1.5.0 (gate generation 7) and both components at 2.0.0."""

    @classmethod
    def setUpClass(cls):
        # Built once: git over a few hundred files is most of a test's time. Each test works on its own copy.
        cls.template = tempfile.TemporaryDirectory()
        cls.addClassCleanup(cls.template.cleanup)
        root = Path(cls.template.name) / 'work'
        cls.rows = copy_components(root)
        for row in cls.rows:
            cls.edit_at(root, row, version='2.0.0', floor='1.5.0')
        cls.core_at(root, version='1.5.0', generation=7)
        cls.git_at(cls.template.name, 'init', '--quiet', '--bare', '--initial-branch=main', 'origin.git')
        cls.git_at(root, 'init', '--quiet', '--initial-branch=main')
        cls.git_at(root, 'remote', 'add', 'origin', '../origin.git')  # relative: valid in every copy
        cls.release_at(root, '1.5.0', *(row['tagPrefix'] + '2.0.0' for row in cls.rows))

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        shutil.copytree(self.template.name, self.temp.name, dirs_exist_ok=True)
        self.root = Path(self.temp.name) / 'work'
        self.origin = Path(self.temp.name) / 'origin.git'

    @staticmethod
    def git_at(cwd, *args):
        # Hermetic: a fixed identity, no hooks and no signing of the host machine.
        subprocess.run(['git', '-c', 'user.name=test', '-c', 'user.email=test@example.invalid',
                        '-c', 'core.hooksPath=no-hooks', '-c', 'commit.gpgsign=false', *args],
                       cwd=cwd, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    @classmethod
    def release_at(cls, root, *tags):
        """Commit the tree, push it as main and tag that commit with every name in `tags`."""
        cls.git_at(root, 'add', '--all')
        cls.git_at(root, 'commit', '--quiet', '--message', 'state')
        cls.git_at(root, 'push', '--quiet', '--force', 'origin', 'HEAD:refs/heads/main',
                   *('HEAD:refs/tags/' + tag for tag in tags))

    @staticmethod
    def core_at(root, version, generation):
        config = root / extensions.NUGET_CONFIG
        config.parent.mkdir(parents=True, exist_ok=True)
        extensions.write(root / extensions.CORE_PACKAGE / 'package.json', {'name': extensions.CORE, 'version': version})
        config.write_text(f'public const string DependencyGenerationDefine = DependencyGenerationDefinePrefix + "{generation}";\n')

    @staticmethod
    def edit_at(root, row, version=None, floor=None):
        path = root / row['package'] / 'package.json'
        package = extensions.read(path)
        package['version'] = version or package['version']
        package['dependencies'][extensions.CORE] = floor or package['dependencies'][extensions.CORE]
        extensions.write(path, package)
        if floor and re.fullmatch(extensions.VERSION_RE, floor):
            extensions.set_core_floor(row, floor, root)

    def core(self, version, generation):
        self.core_at(self.root, version, generation)

    def edit(self, row, **coordinates):
        self.edit_at(self.root, row, **coordinates)

    def coordinates(self):
        packages = [extensions.read(self.root / row['package'] / 'package.json') for row in self.rows]
        return [(package['version'], package['dependencies'][extensions.CORE]) for package in packages]

    def check(self, **options):
        return extensions.check_published(self.root, **options)

    def settle(self, core_version=None, **options):
        extensions.settle(core_version, root=self.root, **options)

    def touch(self, row, root=None):
        asmdef = next(((root or self.root) / row['package'] / 'Runtime').rglob('*.asmdef'))
        asmdef.write_bytes(asmdef.read_bytes().replace(b'UNITY_MCP_DEPS_', b'UNITY_MCP_DEPS_9'))

    def installer(self, row):
        return next((self.root / 'extensions' / row['name'] / 'Installer/Assets').rglob('Installer.cs'))

    def test_published_tree_passes_and_settle_changes_nothing(self):
        self.assertEqual([], self.check())
        self.assertEqual([], self.check(strict=True))
        self.settle('1.5.0')
        self.assertEqual([('2.0.0', '1.5.0')] * 2, self.coordinates())

    def test_changed_file_of_a_published_version_fails_until_that_version_moves(self):
        changed, untouched = self.rows
        self.touch(changed)
        problems = self.check()
        self.assertEqual(1, len(problems))
        self.assertRegex(problems[0], changed['name'] + r' 2\.0\.0 is published, but 1 of its files changed')
        self.assertEqual([], self.check(only={untouched['name']}))
        self.settle(dry_run=True)
        self.assertEqual([('2.0.0', '1.5.0')] * 2, self.coordinates())
        self.settle()
        self.assertEqual([('2.0.1', '1.5.0'), ('2.0.0', '1.5.0')], self.coordinates())
        self.assertEqual([], self.check())
        self.assertIn('Version = "2.0.1"', self.installer(changed).read_text(encoding='utf-8'))

    def test_changed_installer_of_a_published_version_fails(self):
        installer = self.installer(self.rows[0])
        installer.write_bytes(installer.read_bytes() + b'// changed\n')
        self.assertRegex(self.check()[0], 'is published, but 1 of its files changed')

    def test_new_file_of_a_published_version_fails_before_it_is_added_to_git(self):
        added = self.root / self.rows[0]['package'] / 'Runtime/Added.cs'
        added.write_text('// new\n')
        self.assertRegex(self.check()[0], r'is published, but 1 of its files changed since \(e\.g\. .*Runtime/Added\.cs\)')
        self.settle()
        self.assertEqual([('2.0.1', '1.5.0'), ('2.0.0', '1.5.0')], self.coordinates())

    def test_unpublished_version_may_change_and_keeps_its_version(self):
        self.edit(self.rows[0], version='2.0.5')
        self.touch(self.rows[0])
        self.assertEqual([], self.check())
        self.settle()
        self.assertEqual([('2.0.5', '1.5.0'), ('2.0.0', '1.5.0')], self.coordinates())

    def test_gate_moved_after_the_core_release_waits_for_the_next_core_version(self):
        self.core(version='1.5.0', generation=8)
        for row in self.rows:
            self.touch(row)  # the change that moves the gate rewrites every module's asmdefs
        # Only the floors wait for the next core release: the changed published files need new versions now.
        problems = self.check()
        self.assertEqual(2, len(problems))
        self.assertRegex(problems[0], 'is published, but 1 of its files changed')
        self.settle()
        self.assertEqual([('2.0.1', '1.5.0')] * 2, self.coordinates())
        # No core release ships generation 8 yet: nothing can be named as the floor, so only a publication fails.
        self.assertEqual([], self.check())
        strict = self.check(strict=True)
        self.assertEqual(2, len(strict))
        self.assertRegex(strict[0], r'core floor 1\.5\.0 does not set UNITY_MCP_DEPS_8')
        with self.assertRaisesRegex(ValueError, r'Core 1\.5\.0 is released and does not ship UNITY_MCP_DEPS_8'):
            self.settle('1.5.0')
        self.assertEqual([('2.0.1', '1.5.0')] * 2, self.coordinates())
        # A floor that is not the core of this tree is not waiting for anything.
        self.edit(self.rows[0], floor='9.9.9')
        self.assertRegex(self.check()[0], r'core floor 9\.9\.9 is not core 1\.5\.0 of this tree')

    def test_core_release_that_moves_the_gate_refloors_and_bumps_every_component(self):
        self.core(version='1.6.0', generation=8)
        self.edit(self.rows[1], version='2.0.5')  # unpublished: takes the floor, keeps its version
        problems = self.check()
        self.assertEqual(2, len(problems))
        self.assertRegex(problems[0], r'core floor 1\.5\.0 is not core 1\.6\.0 of this tree')
        self.settle('1.6.0', dry_run=True)
        self.assertEqual([('2.0.0', '1.5.0'), ('2.0.5', '1.5.0')], self.coordinates())
        self.settle()  # without the core version no floor can move
        self.assertEqual([('2.0.0', '1.5.0'), ('2.0.5', '1.5.0')], self.coordinates())
        self.settle('1.6.0')
        self.assertEqual([('2.0.1', '1.6.0'), ('2.0.5', '1.6.0')], self.coordinates())
        self.assertEqual([], self.check())
        # A component may not be published before the core it requires.
        self.assertRegex(self.check(strict=True)[0], r'core floor 1\.6\.0 is not a released core version')
        self.release_at(self.root, '1.6.0')
        self.assertEqual([], self.check(strict=True))
        self.settle('1.6.0')
        self.assertEqual([('2.0.1', '1.6.0'), ('2.0.5', '1.6.0')], self.coordinates())

    def test_core_release_with_the_same_gate_gets_its_own_component_versions(self):
        # The assemblies name one core release: a later core skips them even when the gate did not move.
        self.core(version='1.6.0', generation=7)
        self.assertEqual(2, len(self.check()))
        with self.assertRaisesRegex(ValueError, r'Core 1\.5\.0 is not the core of this tree \(1\.6\.0\)'):
            self.settle('1.5.0')
        self.settle('1.6.0')
        self.assertEqual([('2.0.1', '1.6.0')] * 2, self.coordinates())
        self.assertEqual([], self.check())
        for row in self.rows:
            asmdefs = extensions.gated_asmdefs(self.root / row['package'])
            self.assertTrue(asmdefs)
            for _, data in asmdefs:
                self.assertIn(extensions.CORE_MATCH_DEFINE, data['defineConstraints'])
                self.assertIn({'name': extensions.CORE, 'expression': '[1.6.0]', 'define': extensions.CORE_MATCH_DEFINE},
                              data['versionDefines'])
        extensions.validate(self.root)

    def test_assemblies_not_constrained_to_their_core_fail_until_settled(self):
        row = self.rows[0]
        asmdef, data = extensions.gated_asmdefs(self.root / row['package'])[0]
        released = asmdef.read_bytes()
        data['defineConstraints'].remove(extensions.CORE_MATCH_DEFINE)
        data['versionDefines'] = [{'name': 'com.unity.other', 'expression': '1.0.0', 'define': 'OTHER'}]
        extensions.write(asmdef, data)
        with self.assertRaisesRegex(ValueError, row['name'] + r': .*\.asmdef must compile only with core 1\.5\.0'):
            extensions.validate(self.root)
        self.settle(dry_run=True)
        self.assertNotIn(extensions.CORE_MATCH_DEFINE, extensions.read(asmdef)['defineConstraints'])
        self.settle()
        extensions.validate(self.root)
        self.assertEqual({'name': 'com.unity.other', 'expression': '1.0.0', 'define': 'OTHER'},
                         extensions.read(asmdef)['versionDefines'][0])
        # What the Inspector appends after the pairing does not unpair it: only its content counts.
        data = extensions.read(asmdef)
        data['defineConstraints'].append('LATER')
        data['versionDefines'].append({'name': 'com.unity.later', 'expression': '2.0.0', 'define': 'LATER'})
        extensions.write(asmdef, data)
        extensions.validate(self.root)
        self.assertEqual([], extensions.unpaired(self.root / row['package'], '1.5.0'))
        # Only the unrelated entry is new: the published file differs, so its version moved.
        self.assertEqual([('2.0.1', '1.5.0'), ('2.0.0', '1.5.0')], self.coordinates())
        # The file keeps its own style: indent, newline and no final newline, as Unity wrote it.
        self.assertEqual(released.endswith(b'\n'), asmdef.read_bytes().endswith(b'\n'))
        self.assertEqual(released.count(b'\r\n') > 0, asmdef.read_bytes().count(b'\r\n') > 0)
        self.assertIn(b'\n    "name"', asmdef.read_bytes().replace(b'\r\n', b'\n'))
        self.edit(row, floor='^1.5.0')
        with self.assertRaisesRegex(ValueError, r'core dependency \^1\.5\.0 must be one core version'):
            extensions.validate(self.root)

    def test_unknown_floor_and_unreadable_remote_fail(self):
        self.edit(self.rows[0], version='2.0.5', floor='9.9.9')
        self.assertRegex(self.check()[0], r'core floor 9\.9\.9 is not core 1\.5\.0 of this tree')
        with self.assertRaisesRegex(ValueError, 'git ls-remote failed'):
            self.check(remote='missing')

    def test_core_without_a_generation_gate_is_a_stale_floor(self):
        config = self.root / extensions.NUGET_CONFIG
        text = config.read_text()
        config.unlink()
        self.release_at(self.root, '1.0.0')  # a core older than the resolver: no NuGetConfig.cs
        config.write_text(text)
        self.core(version='1.0.0', generation=7)
        for row in self.rows:
            self.edit(row, version='2.0.5', floor='1.0.0')
        self.assertRegex(self.check(strict=True)[0], r'core floor 1\.0\.0 does not set UNITY_MCP_DEPS_7')

    def test_only_a_core_version_can_be_a_floor(self):
        component_tag = self.rows[1]['tagPrefix'] + '2.0.0'  # a release whose tree carries the current gate
        with self.assertRaisesRegex(ValueError, 'stable core dependency version'):
            self.settle(component_tag)
        self.edit(self.rows[0], version='2.0.5', floor=component_tag)
        self.assertRegex(self.check()[0], r'is not core 1\.5\.0 of this tree')
        self.assertRegex(self.check(strict=True)[0], r'is not core 1\.5\.0 of this tree')

    def test_shallow_ci_checkout_without_tags_reads_the_remote(self):
        (self.root / 'later.txt').write_text('main moved past the releases\n')
        self.release_at(self.root)  # the checkout below holds this commit only: no release is in it
        clone = Path(self.temp.name) / 'ci'
        self.git_at(self.temp.name, 'clone', '--quiet', '--depth', '1', '--no-tags', self.origin.as_uri(), str(clone))
        self.assertEqual([], extensions.check_published(clone, strict=True))
        self.touch(self.rows[0], clone)
        self.assertEqual(1, len(extensions.check_published(clone)))


class EntryPointTests(unittest.TestCase):
    def run_main(self, module, *arguments):
        with patch.object(sys, 'argv', [module.__name__, *arguments]):
            module.main()

    def test_check_published_command_fails_with_every_problem_and_the_fix(self):
        with patch.object(extensions, 'check_published', return_value=['Animation: stale', 'Terrain: stale']):
            with self.assertRaises(SystemExit) as failure:
                self.run_main(extensions, 'check-published')
        self.assertRegex(str(failure.exception.code),
                         r'FAIL: Animation: stale\nFAIL: Terrain: stale\nFix: python commands/extensions\.py settle')
        with patch.object(extensions, 'check_published', return_value=[]):
            self.run_main(extensions, 'check-published')

    def test_settle_command_passes_the_core_version_and_dry_run(self):
        with patch.object(extensions, 'settle') as settle:
            self.run_main(extensions, 'settle', '--core-version', '1.6.0', '--dry-run')
            self.run_main(extensions, 'settle')
        self.assertEqual([(('1.6.0',), {'dry_run': True}), ((None,), {'dry_run': False})],
                         [tuple(call) for call in settle.call_args_list])

    def test_settle_and_check_published_refuse_a_selection_they_would_ignore(self):
        for command in ('settle', 'check-published'):
            with patch.object(extensions, 'settle') as settle, patch.object(extensions, 'check_published') as check, \
                    patch.object(sys, 'stderr', io.StringIO()), self.assertRaises(SystemExit):
                self.run_main(extensions, command, '--only', 'Animation')
            settle.assert_not_called()
            check.assert_not_called()

    def test_publication_starts_with_the_strict_check_of_the_selection(self):
        for command in ('routes', 'publish'):
            with patch.object(release, 'check_published', return_value=['Animation: core floor']) as check, \
                    patch.object(release, 'validate_routes') as routes, patch.object(release, 'publish') as publish:
                with self.assertRaisesRegex(ValueError, 'Refusing to publish: Animation: core floor'):
                    self.run_main(release, command, '--only', 'Animation')
            check.assert_called_once_with(strict=True, only={'Animation'})
            routes.assert_not_called()
            publish.assert_not_called()
        # A plan only reads: it works before the core this selection needs is released.
        with patch.object(release, 'check_published') as check, patch.object(release, 'release', return_value=None):
            self.run_main(release, 'plan', '--only', 'Animation')
        check.assert_not_called()


class ReleaseArtifactTests(unittest.TestCase):
    def test_route_metadata_ignores_yaml_only_description_escapes(self):
        # OpenUPM ParticleSystem has YAML \\x escapes, which are invalid JSON.
        text = r'''description: "ParticleSystem commands\x80\x94configure emission"
repoUrl: https://github.com/IvanMurzak/Unity-MCP
trackingMode: "githubRelease"
gitTagPrefix: 'com.ivanmurzak.unity.mcp.particlesystem/'
githubReleaseAssetName: 'com.ivanmurzak.unity.mcp.particlesystem-'
gitTagIgnore: ''
'''
        with patch.object(release.urllib.request, 'urlopen', return_value=io.BytesIO(text.encode())):
            metadata = release.openupm_metadata('com.ivanmurzak.unity.mcp.particlesystem')
        self.assertEqual(metadata, {
            'repoUrl': 'https://github.com/IvanMurzak/Unity-MCP',
            'trackingMode': 'githubRelease',
            'gitTagPrefix': 'com.ivanmurzak.unity.mcp.particlesystem/',
            'githubReleaseAssetName': 'com.ivanmurzak.unity.mcp.particlesystem-',
            'gitTagIgnore': '',
        })

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
