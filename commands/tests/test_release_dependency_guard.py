"""Cases for commands/release_dependency_guard.py: which pins a release may ship (hermetic: a fake nuget.org)."""
import json
from pathlib import Path
import sys
import tempfile
import unittest

COMMANDS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(COMMANDS))
import release_dependency_guard as guard

PUBLISHED = {
    "com.ivanmurzak.mcpplugin": ["8.13.1"],
    "com.ivanmurzak.rql": ["0.1.0", "0.2.0"],
    "com.ivanmurzak.codeexec": ["0.3.0", "0.3.1"],
    "r3": ["1.3.0"],
}

NUGET_CONFIG = """namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    public static class NuGetConfig
    {
        public const string NuGetBaseUrl = "%s";
        public static readonly NuGetPackage[] Packages =
        {
            new NuGetPackage("com.IvanMurzak.McpPlugin",  "8.13.1", includeInBuild: true),
            new NuGetPackage("R3",                        "1.3.0",  includeInBuild: true),
            new NuGetPackage("com.IvanMurzak.Rql",        "%s"),
        };
    }
}
"""

VENDORED = """# Vendored: Code-Exec (C#)

`Core/` and `Roslyn/` are copied verbatim from Code-Exec release
**%s** (tag `nuget-v0.3.1`, commit `96c2919`).
"""


def fake_registry(published=PUBLISHED, fail=False):
    def fetch(url):
        if fail:
            raise guard.RegistryError("offline")
        versions = published.get(url.split("/")[-2])
        return None if versions is None else json.dumps({"versions": versions}).encode()
    return guard.NuGetRegistry(fetch)


class GuardTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.fixture()

    def write(self, relative, text):
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")

    def fixture(self, rql="0.2.0", code_exec="0.3.1", base="https://api.nuget.org/v3-flatcontainer",
                extension_core="1.5.0"):
        package = guard.PACKAGE_DIR
        self.write(guard.NUGET_CONFIG, NUGET_CONFIG % (base, rql))
        self.write(guard.CODE_EXEC_VENDORED, VENDORED % code_exec)
        self.write(package / "package.json", json.dumps({"dependencies": {"com.unity.modules.uielements": "1.0.0"}}, indent=2))
        self.write("extensions/Animation/Packages/com.ivanmurzak.unity.mcp.animation/package.json",
                   json.dumps({"dependencies": {"com.ivanmurzak.unity.mcp": extension_core}}, indent=2))
        self.write("cli/package.json", json.dumps({"dependencies": {"commander": "^13.1.0"},
                                                   "devDependencies": {"vitest": "4.0.0-beta.1"}}, indent=2))

    def problems(self, registry=None):
        _, problems = guard.run(self.root, registry or fake_registry())
        return [(guard.rel(p.file, self.root), p.message) for p in problems]

    def test_published_pins_pass(self):
        pins, problems = guard.run(self.root, fake_registry())
        self.assertEqual([], problems)
        self.assertIn(("nuget", "com.IvanMurzak.CodeExec", "0.3.1"), {(p.ecosystem, p.package, p.version) for p in pins})

    def test_prerelease_resolver_pin_fails(self):
        self.fixture(rql="0.3.0-dev.602ce7c")
        self.assertEqual([(guard.NUGET_CONFIG.as_posix(), "com.IvanMurzak.Rql 0.3.0-dev.602ce7c is a pre-release version")],
                         self.problems())

    def test_unpublished_pin_and_unknown_package_fail(self):
        self.fixture(rql="0.9.0")
        self.assertEqual([(guard.NUGET_CONFIG.as_posix(), "com.IvanMurzak.Rql 0.9.0 is not published on nuget.org")],
                         self.problems())
        unknown = self.problems(fake_registry({**PUBLISHED, "r3": None}))
        self.assertIn((guard.NUGET_CONFIG.as_posix(), "R3 is not a package on nuget.org"), unknown)

    def test_registry_outage_fails_closed(self):
        problems = self.problems(fake_registry(fail=True))
        self.assertTrue(problems)
        self.assertTrue(all("cannot verify on nuget.org" in m for _, m in problems))

    def test_local_resolver_feed_fails(self):
        self.fixture(base="file:///C:/prerelease-packages")
        self.assertEqual(1, len(self.problems()))
        self.assertIn("NuGetBaseUrl must be", self.problems()[0][1])

    def test_vendored_code_exec_must_be_a_published_release(self):
        self.fixture(code_exec="0.4.0-dev.1")
        self.assertIn((guard.CODE_EXEC_VENDORED.as_posix(), "com.IvanMurzak.CodeExec 0.4.0-dev.1 is a pre-release version"),
                      self.problems())
        self.fixture(code_exec="0.9.9")
        self.assertIn((guard.CODE_EXEC_VENDORED.as_posix(), "com.IvanMurzak.CodeExec 0.9.9 is not published on nuget.org"),
                      self.problems())
        self.write(guard.CODE_EXEC_VENDORED, "Copied from Code-Exec main (unreleased), commit abc.\n")
        self.assertEqual([(guard.CODE_EXEC_VENDORED.as_posix(), "vendored com.IvanMurzak.CodeExec: no release version "
                           "marker (copy it from a published release)")], self.problems())

    def test_extension_upm_dependencies_are_scanned(self):
        self.fixture(extension_core="1.6.0-preview.1")
        self.assertEqual([("extensions/Animation/Packages/com.ivanmurzak.unity.mcp.animation/package.json",
                           "com.ivanmurzak.unity.mcp 1.6.0-preview.1 is a pre-release version")], self.problems())
        self.fixture(extension_core="file:../../../Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp")
        self.assertEqual(1, len(self.problems()))
        self.assertIn("is not a registry version", self.problems()[0][1])

    def test_missing_shipped_file_fails(self):
        (self.root / guard.CODE_EXEC_VENDORED).unlink()
        self.assertEqual([(guard.CODE_EXEC_VENDORED.as_posix(),
                           "expected file is missing (update release_dependency_guard.py)")], self.problems())


class VersionTests(unittest.TestCase):
    def test_prerelease_and_normalize(self):
        self.assertTrue(guard.is_prerelease("0.3.0-dev.602ce7c"))
        self.assertFalse(guard.is_prerelease("1.0.0+build-7"))
        self.assertEqual("1.0.0", guard.normalize_nuget_version("1.0.0.0"))
        self.assertEqual("1.0.0-rc.1", guard.normalize_nuget_version("1.0-RC.1"))


if __name__ == "__main__":
    unittest.main()
