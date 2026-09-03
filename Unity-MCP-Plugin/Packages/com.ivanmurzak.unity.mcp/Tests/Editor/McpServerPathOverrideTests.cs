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
    /// <para>Deterministic and editor-state-free: every fixture file lives in an isolated OS temp
    /// directory, nothing touches the network, and the two override SOURCES are both neutralised in
    /// <c>[SetUp]</c> and restored in <c>[TearDown]</c> — the process env var AND
    /// <c>&lt;projectRoot&gt;/.env</c> — so the unset baseline really is unset and the <c>.env</c>-layer
    /// test really does run with no process env.</para>
    /// </summary>
    public class McpServerPathOverrideTests
    {
        string _tempRoot = string.Empty;
        string? _originalProcessEnv;
        string? _originalProjectEnvFile; // content of <projectRoot>/.env; null when the file was absent

        static string ProjectEnvFilePath
            => Path.Combine(UnityMcpPluginEditor.ProjectRootPath, ".env");

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

            _originalProjectEnvFile = File.Exists(ProjectEnvFilePath)
                ? File.ReadAllText(ProjectEnvFilePath)
                : null;
            if (_originalProjectEnvFile != null)
                File.Delete(ProjectEnvFilePath);
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable(McpServerManager.ServerPathEnvVar, _originalProcessEnv);

            try
            {
                if (_originalProjectEnvFile != null)
                    File.WriteAllText(ProjectEnvFilePath, _originalProjectEnvFile);
                else if (File.Exists(ProjectEnvFilePath))
                    File.Delete(ProjectEnvFilePath);
            }
            catch { /* best effort */ }

            try { if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, recursive: true); }
            catch { /* best effort */ }
        }

        /// <summary>A stand-in for a chain-built server binary. Only its PATH is under test here.</summary>
        string CreateFakeServerBinary()
        {
            var dir = Path.Combine(_tempRoot, "chain-build");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, McpServerManager.ExecutableFullName);
            File.WriteAllText(path, "stand-in for a chain-built gamedev-mcp-server; only its path is asserted");
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
            var marker = File.Exists(McpServerManager.VersionFullPath)
                ? File.ReadAllText(McpServerManager.VersionFullPath)
                : null;
            Assert.AreEqual(marker, McpServerManager.GetBinaryVersion());
            Assert.AreEqual(marker == McpServerManager.ServerVersion, McpServerManager.IsVersionMatches());
            Assert.AreEqual(
                McpServerManager.IsBinaryExists() && McpServerManager.IsVersionMatches(),
                McpServerManager.IsBinaryReadyToStart());
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
