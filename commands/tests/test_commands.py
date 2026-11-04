"""Cases for the commands/ scripts and the extension template's scripts (hermetic: temp copies, no network)."""
import contextlib
import importlib.util
import io
from pathlib import Path
import sys
import tempfile
import unittest
from unittest import mock

ROOT = Path(__file__).resolve().parents[2]
TEMPLATE = ROOT / "templates/extension"


def load(path: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module  # dataclasses resolve their module through sys.modules
    spec.loader.exec_module(module)
    return module


bump_version = load(ROOT / "commands/bump_version.py", "bump_version")
run_act_test = load(ROOT / "commands/run_act_test.py", "run_act_test")
run_unity_tests = load(ROOT / "commands/run_unity_tests.py", "run_unity_tests")
tpl_bump = load(TEMPLATE / "commands/bump_version.py", "tpl_bump_version")
tpl_update = load(TEMPLATE / "commands/update_ai_game_developer.py", "tpl_update_ai_game_developer")
tpl_init = load(TEMPLATE / "commands/init.py", "tpl_init")

PACKAGE_JSON = """{
  "name": "com.ivanmurzak.unity.mcp",
  "version": "0.70.0",
  "dependencies": {
    "com.unity.test-framework": "1.1.33"
  }
}
"""
CS = """namespace X
{
    public static class C
    {
        public const string Version = "0.70.0";
    }
}
"""
CLI_PACKAGE_JSON = """{
  "name": "unity-mcp-cli",
  "version": "0.70.0-rc.1",
  "dependencies": {
    "@baizor/gamedev-cli-core": "^0.7.2"
  }
}
"""
CLI_LOCK = """{
  "name": "unity-mcp-cli",
  "version": "0.70.0",
  "lockfileVersion": 3,
  "packages": {
    "": {
      "name": "unity-mcp-cli",
      "version": "0.70.0"
    },
    "node_modules/foo": {
      "version": "0.70.0"
    }
  }
}
"""
REPO_FILES = {
    "Installer/Assets/com.IvanMurzak/AI Game Dev Installer/Installer.cs": CS,
    "Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/package.json": PACKAGE_JSON,
    "Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Runtime/UnityMcpPlugin.cs": CS,
    "cli/package.json": CLI_PACKAGE_JSON,
    "cli/package-lock.json": CLI_LOCK,
}


def quiet(fn, *args):
    out = io.StringIO()
    with contextlib.redirect_stdout(out), contextlib.redirect_stderr(out):
        code = fn(*args)
    return code, out.getvalue()


class TempTree(unittest.TestCase):
    files = {}

    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        self.write_tree("\n")

    def tearDown(self):
        self.tmp.cleanup()

    def write_tree(self, newline):
        for rel, text in self.files.items():
            path = self.root / rel
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(text.replace("\n", newline).encode("utf-8"))

    def read(self, rel):
        return (self.root / rel).read_bytes().decode("utf-8")


class BumpVersionTests(TempTree):
    files = REPO_FILES

    def bump(self, *args):
        return quiet(bump_version.main, ["--root", str(self.root), *args])

    def test_bump_rewrites_only_the_version_strings(self):
        code, out = self.bump("--new-version", "0.71.0")
        self.assertEqual(code, 0, out)
        self.assertEqual(self.read("Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/package.json"),
                         PACKAGE_JSON.replace("0.70.0", "0.71.0"))
        for rel in ("Installer/Assets/com.IvanMurzak/AI Game Dev Installer/Installer.cs",
                    "Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Runtime/UnityMcpPlugin.cs"):
            self.assertEqual(self.read(rel), CS.replace("0.70.0", "0.71.0"), rel)
        self.assertEqual(self.read("cli/package.json"), CLI_PACKAGE_JSON.replace("0.70.0-rc.1", "0.71.0"))
        lock = self.read("cli/package-lock.json")
        self.assertEqual(lock.count('"version": "0.71.0"'), 2)
        self.assertIn('"node_modules/foo": {\n      "version": "0.70.0"', lock)
        self.assertIn("CLI package-lock.json updated (2 occurrences)", out)
        self.assertIn("Version: 0.70.0 -> 0.71.0", out)

    def test_preserves_crlf_and_writes_no_bom(self):
        self.write_tree("\r\n")
        rel = "cli/package.json"
        (self.root / rel).write_bytes(b"\xef\xbb\xbf" + CLI_PACKAGE_JSON.replace("\n", "\r\n").encode("utf-8"))
        self.assertEqual(self.bump("--new-version", "1.0.0")[0], 0)
        for rel in REPO_FILES:
            raw = (self.root / rel).read_bytes()
            self.assertFalse(raw.startswith(b"\xef\xbb\xbf"), rel)
            self.assertEqual(raw.count(b"\n"), raw.count(b"\r\n"), rel)

    def test_what_if_writes_nothing(self):
        code, out = self.bump("--new-version", "0.71.0", "--what-if")
        self.assertEqual(code, 0, out)
        self.assertIn("Total replacements: 4", out)
        for rel, text in REPO_FILES.items():
            self.assertEqual(self.read(rel), text, rel)

    def test_invalid_and_same_version(self):
        self.assertEqual(self.bump("--new-version", "1.2")[0], 1)
        code, out = self.bump("--new-version", "0.70.0")
        self.assertEqual(code, 0)
        self.assertIn("same as current version", out)

    def test_bumps_from_a_prerelease_version(self):
        self.assertEqual(self.bump("--new-version", "0.71.0-rc.1")[0], 0)
        code, out = self.bump("--new-version", "0.71.0")
        self.assertEqual(code, 0, out)
        self.assertIn("Version: 0.71.0-rc.1 -> 0.71.0", out)
        for rel in REPO_FILES:
            self.assertNotIn("rc.1", self.read(rel), rel)

    def test_missing_package_json_fails(self):
        (self.root / "Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/package.json").unlink()
        code, out = self.bump("--new-version", "0.71.0")
        self.assertEqual(code, 1)
        self.assertIn("Could not find package.json", out)


TPL_PACKAGE = "Unity-Package/Packages/com.acme.cool/package.json"
TPL_LOCK = "Unity-Tests/2022.3.62f3/Packages/packages-lock.json"
TPL_PACKAGE_JSON = """{
  "name": "com.acme.cool",
  "version": "1.0.0",
  "dependencies": {
    "com.ivanmurzak.unity.mcp": "0.70.0"
  }
}
"""
TPL_LOCK_JSON = """{
  "dependencies": {
    "com.ivanmurzak.unity.mcp": {
      "version": "0.70.0",
      "depth": 0,
      "source": "registry",
      "dependencies": {
        "com.unity.test-framework": "1.1.33"
      }
    },
    "com.acme.other": {
      "version": "2.0.0",
      "dependencies": {
        "com.ivanmurzak.unity.mcp": "0.70.0"
      }
    }
  }
}
"""


class TemplateScriptTests(TempTree):
    files = {TPL_PACKAGE: TPL_PACKAGE_JSON, "Installer/Assets/Cool Installer/Installer.cs": CS,
             TPL_LOCK: TPL_LOCK_JSON}

    def setUp(self):
        super().setUp()
        # The real files only exist after init.py replaced the placeholders.
        patches = [mock.patch.object(tpl_bump, "PACKAGE_JSON", TPL_PACKAGE),
                   mock.patch.object(tpl_bump, "VERSION_FILES", tuple(
                       vf.__class__(vf.path.replace("YOUR_PACKAGE_ID_LOWERCASE", "com.acme.cool")
                                    .replace("YOUR_PACKAGE_NAME_INSTALLER", "Cool Installer"),
                                    vf.pattern, vf.replace, vf.description) for vf in tpl_bump.VERSION_FILES)),
                   mock.patch.object(tpl_update, "TARGET_FILES", (TPL_PACKAGE, TPL_LOCK, "Unity-Tests/missing.json"))]
        for patch in patches:
            patch.start()
            self.addCleanup(patch.stop)

    def test_bump_updates_package_json_and_installer(self):
        code, out = quiet(tpl_bump.main, ["--root", str(self.root), "--new-version", "1.0.1"])
        self.assertEqual(code, 0, out)
        self.assertEqual(self.read(TPL_PACKAGE), TPL_PACKAGE_JSON.replace('"version": "1.0.0"', '"version": "1.0.1"'))
        self.assertEqual(self.read("Installer/Assets/Cool Installer/Installer.cs"), CS.replace("0.70.0", "1.0.1"))

    def test_update_pins_every_core_reference_and_nothing_else(self):
        with mock.patch.object(tpl_update, "get_json", return_value={"tag_name": "v0.71.0"}):
            code, out = quiet(tpl_update.main, ["--root", str(self.root)])
        self.assertEqual(code, 0, out)
        self.assertEqual(self.read(TPL_PACKAGE), TPL_PACKAGE_JSON.replace("0.70.0", "0.71.0"))
        self.assertEqual(self.read(TPL_LOCK), TPL_LOCK_JSON.replace("0.70.0", "0.71.0"))
        self.assertIn('"version": "2.0.0"', self.read(TPL_LOCK))
        self.assertIn("Version: 0.70.0 -> 0.71.0", out)

    def test_update_falls_back_to_tags_and_what_if_writes_nothing(self):
        def fake(url):
            if url.endswith("/releases/latest"):
                raise OSError("404")
            return [{"name": "0.72.0"}]
        with mock.patch.object(tpl_update, "get_json", side_effect=fake):
            code, out = quiet(tpl_update.main, ["--root", str(self.root), "--what-if"])
        self.assertEqual(code, 0, out)
        self.assertIn("Found latest tag: 0.72.0", out)
        self.assertIn("Files to update: 2", out)
        self.assertEqual(self.read(TPL_PACKAGE), TPL_PACKAGE_JSON)

    def test_update_is_a_noop_when_current(self):
        with mock.patch.object(tpl_update, "get_json", return_value={"tag_name": "0.70.0"}):
            code, out = quiet(tpl_update.main, ["--root", str(self.root)])
        self.assertEqual(code, 0, out)
        self.assertIn("Already up to date!", out)


class InitTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        files = {
            "commands/bump_version.py": 'P = "Unity-Package/Packages/YOUR_PACKAGE_ID_LOWERCASE/package.json"\n',
            "Unity-Package/Packages/YOUR_PACKAGE_ID_LOWERCASE/package.json":
                '{\r\n  "name": "YOUR_PACKAGE_ID_LOWERCASE",\r\n  "displayName": "YOUR_PACKAGE_NAME"\r\n}\r\n',
            "Unity-Package/Packages/YOUR_PACKAGE_ID_LOWERCASE/Editor/YOUR_FEATURE_NAMEModule.cs":
                "namespace Rql.YOUR_FEATURE_NAME { // source YOUR_FEATURE_NAME_LOWERCASE\n}\n",
            "Installer/Assets/YOUR_PACKAGE_NAME Installer/Installer.cs":
                'const string Path = "Assets/YOUR_PACKAGE_NAME_INSTALLER"; // YOUR_PACKAGE_NAME_INSTALLER_FILE\n',
            "Unity-Tests/6000.3.1f1/Library/YOUR_PACKAGE_ID.txt": "YOUR_PACKAGE_ID\n",
            ".github/workflows/release.yml": "repo: YOUR_GITHUB_USERNAME_REPOSITORY\n",
        }
        for rel, text in files.items():
            path = self.root / rel
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(text.encode("utf-8"))
        (self.root / "Installer/Assets/YOUR_PACKAGE_NAME Installer/icon.png").write_bytes(b"\x89PNG\xff\xfe\x00")

    def tearDown(self):
        self.tmp.cleanup()

    def read(self, rel):
        return (self.root / rel).read_bytes().decode("utf-8")

    def init(self, *extra):
        return quiet(tpl_init.main, ["--root", str(self.root), "--package-id", "com.Acme.Cool", "--package-name",
                                     "Cool package", "--github-repository", "acme/cool", *extra])

    def test_replaces_content_and_names(self):
        code, out = self.init("--installer-extra-path", "")
        self.assertEqual(code, 0, out)
        self.assertEqual(self.read("commands/bump_version.py"),
                         'P = "Unity-Package/Packages/com.acme.cool/package.json"\n')
        self.assertEqual(self.read("Unity-Package/Packages/com.acme.cool/package.json"),
                         '{\r\n  "name": "com.acme.cool",\r\n  "displayName": "Cool package"\r\n}\r\n')
        self.assertEqual(self.read("Unity-Package/Packages/com.acme.cool/Editor/CoolPackageModule.cs"),
                         "namespace Rql.CoolPackage { // source coolpackage\n}\n")
        self.assertEqual(self.read("Installer/Assets/Cool package Installer/Installer.cs"),
                         'const string Path = "Assets/Cool package Installer"; // Cool-package-Installer\n')
        self.assertEqual((self.root / "Installer/Assets/Cool package Installer/icon.png").read_bytes(),
                         b"\x89PNG\xff\xfe\x00")
        self.assertEqual(self.read(".github/workflows/release.yml"), "repo: acme/cool\n")
        # Build output under a test project is left alone.
        self.assertEqual(self.read("Unity-Tests/6000.3.1f1/Library/YOUR_PACKAGE_ID.txt"), "YOUR_PACKAGE_ID\n")

    def test_installer_extra_path_moves_the_installer(self):
        code, out = self.init("--installer-extra-path", "\\Editor\\", "--feature-name", "Cool")
        self.assertEqual(code, 0, out)
        self.assertEqual(self.read("Installer/Assets/Editor/Cool package Installer/Installer.cs"),
                         'const string Path = "Assets/Editor/Cool package Installer"; // Cool-package-Installer\n')
        self.assertFalse((self.root / "Installer/Assets/Cool package Installer").exists())
        self.assertTrue((self.root / "Unity-Package/Packages/com.acme.cool/Editor/CoolModule.cs").is_file())

    def test_invalid_feature_name_fails(self):
        code, out = self.init("--installer-extra-path", "", "--feature-name", "1bad")
        self.assertEqual(code, 1)
        self.assertIn("is not a valid identifier", out)


class RunActTestTests(unittest.TestCase):
    def test_parse_env(self):
        env = run_act_test.parse_env('# c\n\nUNITY_EMAIL = "a@b.c"\nUNITY_LICENSE=x=y\nBAD\nUNITY_PASSWORD=\'p\'\n')
        self.assertEqual(env, {"UNITY_EMAIL": "a@b.c", "UNITY_LICENSE": "x=y", "UNITY_PASSWORD": "p"})

    def test_act_args(self):
        args = run_act_test.act_args("job", "wf.yml", {"K": "v; rm -rf /"}, True, True, True)
        self.assertEqual(args, ["workflow_dispatch", "-j", "job", "-W", "wf.yml", "-s", "K=v; rm -rf /", "--verbose",
                                "--dryrun", "--env", "ACTIONS_STEP_DEBUG=true", "--artifact-server-path",
                                "./.act-artifacts", "--container-architecture", "linux/amd64"])


class RunUnityTestsTests(unittest.TestCase):
    def parse(self, xml):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "r.xml"
            path.write_text(xml, encoding="utf-8")
            return run_unity_tests.parse_results(path)

    def test_nunit3(self):
        parsed = self.parse('<test-run result="Passed" total="5" passed="4" failed="0" skipped="1"/>')
        self.assertEqual((parsed.success, parsed.total, parsed.passed, parsed.skipped), (True, 5, 4, 1))
        self.assertFalse(self.parse('<test-run result="Failed" total="5" passed="4" failed="1"/>').success)

    def test_nunit2(self):
        parsed = self.parse('<test-results total="10" failures="1" errors="1" not-run="2"/>')
        self.assertEqual((parsed.success, parsed.passed, parsed.failed, parsed.skipped), (False, 6, 2, 2))

    def test_errors(self):
        self.assertEqual(self.parse("<other/>").error, "Unknown XML format")
        self.assertTrue(self.parse("<broken").error)
        self.assertEqual(run_unity_tests.parse_results(Path("/nonexistent.xml")).error, "Results file not found")


if __name__ == "__main__":
    unittest.main()
