/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// EditMode unit tests for the <c>UNITY_MCP_SERVER_PATH</c> dev/CI override
    /// (<see cref="McpServerManager.ServerPathEnvVar"/>): when it points at an EXISTING file that file is
    /// what the editor launches, and both the GitHub-release download and the pinned-version match are
    /// skipped. Mirrors Unreal's <c>UNREAL_MCP_SERVER_PATH</c> rule — including the fall-through when the
    /// override is set but the file is missing.
    ///
    /// <para>Deterministic and editor-state-free: every fixture BINARY lives in an isolated OS temp
    /// directory, nothing touches the network, and the two override SOURCES are both neutralised in
    /// <c>[SetUp]</c> and restored in <c>[TearDown]</c> — the process env var AND
    /// <c>&lt;projectRoot&gt;/.env</c> — so the unset baseline really is unset and the <c>.env</c>-layer
    /// test really does run with no process env.</para>
    ///
    /// <para>That second source is the one piece of REAL project state these tests touch, and it is MOVED
    /// ASIDE rather than deleted: <c>.env</c> is gitignored, user-owned config that this very feature's
    /// documentation tells developers to create, so git holds no copy and a run killed before
    /// <c>[TearDown]</c> must still leave the bytes recoverable on disk rather than only in a field.</para>
    /// </summary>
    public class McpServerPathOverrideTests
    {
        string _tempRoot = string.Empty;
        string? _originalProcessEnv;
        bool _projectEnvFileMovedAside;

        static string ProjectEnvFilePath
            => Path.Combine(UnityMcpPluginEditor.ProjectRootPath, ".env");

        // Where the real <projectRoot>/.env is parked while a test owns that path. An on-disk name rather
        // than an in-memory copy, for the reason the class docblock gives: the file is unrecoverable if a
        // run dies mid-test holding the only copy in a field.
        static string ProjectEnvFileAsidePath
            => ProjectEnvFilePath + ".McpServerPathOverrideTests-aside";

        // The NO-OVERRIDE (pinned release) locations, re-derived here INDEPENDENTLY of the members under
        // test, so the no-override assertions pin `Library/mcp-server/<rid>/` as a positive artifact rather
        // than comparing a value with itself.
        static string ExpectedCacheFolder
            => Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", "Library", "mcp-server", McpServerManager.PlatformName));

        static string ExpectedCacheExecutable
            => Path.GetFullPath(Path.Combine(ExpectedCacheFolder, McpServerManager.ExecutableFullName));

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "mcp-server-path-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);

            _originalProcessEnv = Environment.GetEnvironmentVariable(McpServerManager.ServerPathEnvVar);
            Environment.SetEnvironmentVariable(McpServerManager.ServerPathEnvVar, null);

            // Recover from an earlier run killed before its [TearDown]: the project's own .env is still
            // parked under the aside name, so put it back before this run parks it again.
            if (!File.Exists(ProjectEnvFilePath) && File.Exists(ProjectEnvFileAsidePath))
                File.Move(ProjectEnvFileAsidePath, ProjectEnvFilePath);

            _projectEnvFileMovedAside = false;
            if (File.Exists(ProjectEnvFilePath))
            {
                if (File.Exists(ProjectEnvFileAsidePath))
                    File.Delete(ProjectEnvFileAsidePath);
                File.Move(ProjectEnvFilePath, ProjectEnvFileAsidePath);
                _projectEnvFileMovedAside = true;
            }
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable(McpServerManager.ServerPathEnvVar, _originalProcessEnv);

            try
            {
                // Drop whatever fixture .env a test wrote at the real path, then unpark the developer's own.
                if (File.Exists(ProjectEnvFilePath))
                    File.Delete(ProjectEnvFilePath);
                if (_projectEnvFileMovedAside)
                    File.Move(ProjectEnvFileAsidePath, ProjectEnvFilePath);
            }
            catch (Exception ex)
            {
                // Deliberately NOT best-effort-silent: on failure the project's own .env is still sitting
                // under the aside name and the next Editor launch would read no .env at all. Debug.LogError
                // fails the test, which is the correct loudness for losing a developer's config.
                Debug.LogError(
                    $"{nameof(McpServerPathOverrideTests)}: failed to restore {ProjectEnvFilePath}"
                    + (_projectEnvFileMovedAside ? $" from {ProjectEnvFileAsidePath}" : string.Empty)
                    + $": {ex}");
            }

            try { if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, recursive: true); }
            catch { /* best effort */ }
        }

        /// <summary>
        /// A stand-in for a server binary built from source — the case the override exists for. Only its
        /// PATH is under test here, so the contents are irrelevant.
        /// </summary>
        string CreateFakeServerBinary()
        {
            var dir = Path.Combine(_tempRoot, "built-from-source");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, McpServerManager.ExecutableFullName);
            File.WriteAllText(path, "stand-in for a gamedev-mcp-server built from source; only its path is asserted");
            return path;
        }

        // ── (a) override set to an existing file ────────────────────────────────────

        [Test]
        public void Override_ExistingFile_IsWhatGetsLaunched()
        {
            var exe = CreateFakeServerBinary();
            var exeFolder = Path.GetFullPath(Path.GetDirectoryName(exe)!);
            Environment.SetEnvironmentVariable(McpServerManager.ServerPathEnvVar, exe);

            Assert.AreEqual(Path.GetFullPath(exe), McpServerManager.ResolveServerPathOverride());
            Assert.AreEqual(Path.GetFullPath(exe), McpServerManager.ExecutableFullPath,
                "ExecutableFullPath (StartServer's FileName, and the generated agent configs' `command`) must be the override");
            Assert.AreEqual(exeFolder, Path.GetFullPath(McpServerManager.ExecutableFolderPath),
                "ExecutableFolderPath is StartServer's WorkingDirectory — it must follow the override's own directory");
            Assert.AreEqual(Path.GetFullPath(Path.Combine(exeFolder, "version")),
                Path.GetFullPath(McpServerManager.VersionFullPath),
                "VersionFullPath is derived from ExecutableFolderPath, so it follows the override too");

            // This assertion and the IsBinaryReadyToStart() one below are ENTAILED by the ExecutableFullPath
            // equality above plus the resolver's own File.Exists gate: no mutation of this feature can redden
            // them. They are kept as executable documentation of the contract callers rely on, and must not
            // be counted as independent evidence that the override works.
            Assert.IsTrue(McpServerManager.IsBinaryExists());

            // The override directory carries NO `version` marker, so IsVersionMatches() can only be true via
            // the override short-circuit: delete that short-circuit and GetBinaryVersion() returns null.
            Assert.IsFalse(File.Exists(McpServerManager.VersionFullPath),
                "fixture precondition: no `version` marker sits beside the override");
            Assert.IsNull(McpServerManager.GetBinaryVersion(),
                "fixture precondition: without the short-circuit there is no version to compare");
            Assert.IsTrue(McpServerManager.IsVersionMatches(),
                "the override must skip the pinned-release version match");
            Assert.IsTrue(McpServerManager.IsBinaryReadyToStart(),
                "IsBinaryReadyToStart() gates the Start button and DownloadServerBinaryIfNeeded()");

            Assert.AreNotEqual(ExpectedCacheExecutable, McpServerManager.ExecutableFullPath,
                "the pinned Library/mcp-server binary must NOT be the launch target while the override is active");

            // The other half of that split, and the one nothing else pins: the DOWNLOAD CACHE tier is not
            // redirected by the override. `Download Binaries` must keep publishing into Library/ rather than
            // deleting and replacing the developer's own folder, and the post-publish verification reads this
            // tier precisely because the launch-target members report the override and so cannot fail.
            Assert.AreEqual(ExpectedCacheFolder, Path.GetFullPath(McpServerManager.CachedExecutableFolderPath),
                "an active override must NOT move the download cache");
        }

        [Test]
        public void Override_SkipsVersionMatch_EvenWithAMismatchedVersionMarkerBesideIt()
        {
            var exe = CreateFakeServerBinary();
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(exe)!, "version"), "0.0.0-not-the-pinned-version");
            Environment.SetEnvironmentVariable(McpServerManager.ServerPathEnvVar, exe);

            // Positive artifact: the marker IS readable and DISAGREES with the pin, so a version check that
            // still ran would return false.
            Assert.AreEqual("0.0.0-not-the-pinned-version", McpServerManager.GetBinaryVersion());
            Assert.AreNotEqual(McpServerManager.ServerVersion, McpServerManager.GetBinaryVersion());

            Assert.IsTrue(McpServerManager.IsVersionMatches(),
                "the override short-circuit must win over a mismatched `version` marker");
            Assert.IsTrue(McpServerManager.IsBinaryReadyToStart());

            // The DOWNLOAD path reads a different marker than the LAUNCH path, and this is the contrast that
            // makes DownloadAndUnpackBinary's post-publish verification able to fail at all while an override
            // is active: GetBinaryVersion() above returned the mismatched marker sitting beside the override,
            // so a cache read that followed the override would return it too.
            Assert.AreNotEqual("0.0.0-not-the-pinned-version", McpServerManager.GetCachedBinaryVersion(),
                "the download-cache version read must NOT follow the override");
        }

        // ── (b) override set to a path that does not exist ──────────────────────────

        [Test]
        public void Override_SetButMissingFile_FallsThroughToThePinnedRelease()
        {
            // Baseline captured with the override genuinely unset (SetUp neutralised both sources).
            var unsetExecutable = McpServerManager.ExecutableFullPath;
            var unsetFolder = McpServerManager.ExecutableFolderPath;
            var unsetVersionPath = McpServerManager.VersionFullPath;
            var unsetBinaryExists = McpServerManager.IsBinaryExists();
            var unsetVersionMatches = McpServerManager.IsVersionMatches();
            var unsetReady = McpServerManager.IsBinaryReadyToStart();

            var missing = Path.Combine(_tempRoot, "no-such-dir", McpServerManager.ExecutableFullName);
            Assert.IsFalse(File.Exists(missing), "fixture precondition: the override target must NOT exist");
            Environment.SetEnvironmentVariable(McpServerManager.ServerPathEnvVar, missing);

            Assert.IsNull(McpServerManager.ResolveServerPathOverride(),
                "a set-but-missing override must not resolve (the Unreal rule)");
            Assert.AreEqual(unsetExecutable, McpServerManager.ExecutableFullPath);
            Assert.AreEqual(unsetFolder, McpServerManager.ExecutableFolderPath);
            Assert.AreEqual(unsetVersionPath, McpServerManager.VersionFullPath);
            Assert.AreEqual(unsetBinaryExists, McpServerManager.IsBinaryExists());
            Assert.AreEqual(unsetVersionMatches, McpServerManager.IsVersionMatches());
            Assert.AreEqual(unsetReady, McpServerManager.IsBinaryReadyToStart());

            // Positive artifact: the fall-through target is the pinned per-RID cache, not merely "unchanged".
            Assert.AreEqual(ExpectedCacheExecutable, McpServerManager.ExecutableFullPath);
            Assert.AreNotEqual(Path.GetFullPath(missing), McpServerManager.ExecutableFullPath);
        }

        // ── (c) override unset (pinned-release behaviour, pinned so it cannot regress) ──

        [Test]
        public void NoOverride_KeepsThePinnedLibraryCache_AndTheVersionMarkerCheck()
        {
            Assert.IsNull(McpServerManager.ResolveServerPathOverride(),
                "fixture precondition: neither the process env nor <projectRoot>/.env carries the override");

            Assert.AreEqual(ExpectedCacheFolder, Path.GetFullPath(McpServerManager.ExecutableFolderPath),
                "with no override the launch folder stays Library/mcp-server/<rid>/");
            Assert.AreEqual(ExpectedCacheFolder, Path.GetFullPath(McpServerManager.CachedExecutableFolderPath),
                "the download cache folder is Library/mcp-server/<rid>/ and the override never redirects it");
            Assert.AreEqual(ExpectedCacheExecutable, McpServerManager.ExecutableFullPath);
            Assert.AreEqual(Path.GetFullPath(Path.Combine(ExpectedCacheFolder, "version")),
                Path.GetFullPath(McpServerManager.VersionFullPath));

            // IsVersionMatches() is still driven by the on-disk `version` marker, not short-circuited.
            // Deliberately ONE assertion: re-deriving GetBinaryVersion()'s own body from the same path
            // expression, and restating IsBinaryReadyToStart() as IsBinaryExists() && IsVersionMatches(),
            // are both true by construction. The second is the sharper trap — the only mutation it could
            // catch is `&&` to `||`, and its two operands are EQUAL in both environments this suite runs in
            // (a clean runner: false/false; a box holding the pinned release: true/true), so it stays green
            // even then. Neither was kept, so nothing here reads as coverage it does not provide.
            var marker = File.Exists(McpServerManager.VersionFullPath)
                ? File.ReadAllText(McpServerManager.VersionFullPath)
                : null;
            Assert.AreEqual(marker == McpServerManager.ServerVersion, McpServerManager.IsVersionMatches());
        }

        // ── (d) the <projectRoot>/.env layer ────────────────────────────────────────

        [Test]
        public void Override_ResolvesFromProjectDotEnv_WhenTheProcessEnvIsUnset()
        {
            var exe = CreateFakeServerBinary();

            Assert.IsNull(Environment.GetEnvironmentVariable(McpServerManager.ServerPathEnvVar),
                "fixture precondition: the process env must be EMPTY — this test exercises the .env layer alone");
            Assert.IsNull(McpServerManager.ResolveServerPathOverride(),
                "fixture precondition: nothing resolves before the .env file is written");

            File.WriteAllText(
                ProjectEnvFilePath,
                "# written by McpServerPathOverrideTests\n" +
                McpServerManager.ServerPathEnvVar + "=" + exe + "\n");

            Assert.AreEqual(Path.GetFullPath(exe), McpServerManager.ResolveServerPathOverride(),
                "a GUI/IDE-launched editor inherits no shell exports, so the override MUST also resolve from <projectRoot>/.env");
            Assert.AreEqual(Path.GetFullPath(exe), McpServerManager.ExecutableFullPath);
            Assert.AreEqual(Path.GetFullPath(Path.GetDirectoryName(exe)!),
                Path.GetFullPath(McpServerManager.ExecutableFolderPath));
            Assert.IsTrue(McpServerManager.IsBinaryExists());
            Assert.IsTrue(McpServerManager.IsVersionMatches());
            Assert.IsTrue(McpServerManager.IsBinaryReadyToStart());
            Assert.AreNotEqual(ExpectedCacheExecutable, McpServerManager.ExecutableFullPath);
        }

        /// <summary>
        /// The <c>projectRootPath</c>-scoped overload is PUBLIC and documented as the unit-test seam, so it
        /// gets a test of its own — the sibling test above proves the same layer end-to-end through the real
        /// project root, which is what the wiring needs, but leaves the overload itself unexercised. Reading
        /// an arbitrary root also pins the property the overload exists for: the root is an ARGUMENT, not the
        /// live project, so the resolver has no hidden dependence on <c>UnityMcpPluginEditor</c>.
        /// </summary>
        [Test]
        public void ResolveServerPathOverride_ReadsTheDotEnvOfTheGivenProjectRoot()
        {
            var exe = CreateFakeServerBinary();
            var otherProjectRoot = Path.Combine(_tempRoot, "another-project-root");
            Directory.CreateDirectory(otherProjectRoot);

            Assert.IsNull(Environment.GetEnvironmentVariable(McpServerManager.ServerPathEnvVar),
                "fixture precondition: the process env must be EMPTY — this test exercises the .env layer alone");
            Assert.IsNull(McpServerManager.ResolveServerPathOverride(otherProjectRoot),
                "fixture precondition: that root carries no .env yet");

            File.WriteAllText(
                Path.Combine(otherProjectRoot, ".env"),
                McpServerManager.ServerPathEnvVar + "=" + exe + "\n");

            Assert.AreEqual(Path.GetFullPath(exe), McpServerManager.ResolveServerPathOverride(otherProjectRoot),
                "the overload must read the .env of the root it was HANDED");
            Assert.IsNull(McpServerManager.ResolveServerPathOverride(),
                "and the no-arg overload must still see nothing: that .env belongs to a different root");
        }

        [Test]
        public void ProcessEnv_OutranksProjectDotEnv()
        {
            var fromProcess = CreateFakeServerBinary();
            var envFileDir = Path.Combine(_tempRoot, "from-env-file");
            Directory.CreateDirectory(envFileDir);
            var fromEnvFile = Path.Combine(envFileDir, McpServerManager.ExecutableFullName);
            File.WriteAllText(fromEnvFile, "stand-in");

            File.WriteAllText(
                ProjectEnvFilePath,
                McpServerManager.ServerPathEnvVar + "=" + fromEnvFile + "\n");
            Environment.SetEnvironmentVariable(McpServerManager.ServerPathEnvVar, fromProcess);

            Assert.AreEqual(Path.GetFullPath(fromProcess), McpServerManager.ExecutableFullPath,
                "process env > .env file, per DevControlEnv.Resolve precedence");
            Assert.AreNotEqual(Path.GetFullPath(fromEnvFile), McpServerManager.ExecutableFullPath);
        }
    }
}
