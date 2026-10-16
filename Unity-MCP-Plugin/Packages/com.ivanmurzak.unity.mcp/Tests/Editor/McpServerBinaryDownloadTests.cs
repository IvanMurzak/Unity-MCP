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
using com.IvanMurzak.Unity.MCP.Editor.UI;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Regression tests for issue #845 (server binary not downloaded on Linux; UI stuck on "Starting…").
    /// Covers the deterministic, editor-free pieces of the fix:
    ///   • <see cref="McpServerManager.PublishStagedBinary"/> — the ATOMIC same-volume publish that replaces the
    ///     old "pre-create empty rid dir + write files in place" flow, so an interrupted download can never leave
    ///     a half-populated <c>Library/mcp-server/&lt;rid&gt;/</c> behind.
    ///   • <see cref="McpServerManager.FindExtractedBinary"/> — the layout-agnostic binary locator (flat win zip
    ///     vs nested &lt;rid&gt;/ osx-linux zip).
    ///   • The <see cref="McpServerStatus.Downloading"/> UI mappings (button/label/status-class/enabled) that let
    ///     the window show an honest "Downloading…" instead of a misleading "Starting…".
    ///   • <see cref="ServerBinaryUpdateWatcher"/>'s pure package-change filter (re-check on plugin update).
    /// File IO runs in an isolated OS temp directory; nothing here touches a running Editor, the network, or the
    /// real project's Library folder.
    /// </summary>
    public class McpServerBinaryDownloadTests
    {
        string _tempRoot = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "mcp-server-845-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, recursive: true); }
            catch { /* best effort */ }
        }

        // --- PublishStagedBinary: the atomic publish ---

        [Test]
        public void PublishStagedBinary_MovesCompletePayload_AndRemovesStaging()
        {
            var staged = Path.Combine(_tempRoot, "staging", "payload");
            Directory.CreateDirectory(staged);
            File.WriteAllText(Path.Combine(staged, "gamedev-mcp-server.exe"), "binary");
            File.WriteAllText(Path.Combine(staged, "appsettings.json"), "{}");
            File.WriteAllText(Path.Combine(staged, "version"), "8.0.1");

            var dest = Path.Combine(_tempRoot, "cache", "win-x64");

            McpServerManager.PublishStagedBinary(staged, dest);

            // The destination is fully populated...
            Assert.IsTrue(File.Exists(Path.Combine(dest, "gamedev-mcp-server.exe")), "binary must be published");
            Assert.IsTrue(File.Exists(Path.Combine(dest, "appsettings.json")), "sidecar must travel with the binary");
            Assert.AreEqual("8.0.1", File.ReadAllText(Path.Combine(dest, "version")), "version marker must be present");
            // ...and the staged folder is gone (it was renamed, not copied).
            Assert.IsFalse(Directory.Exists(staged), "staged folder must be consumed by the move");
        }

        [Test]
        public void PublishStagedBinary_ReplacesExistingDestination_NoLeftoverFromOldPayload()
        {
            var dest = Path.Combine(_tempRoot, "cache", "win-x64");
            Directory.CreateDirectory(dest);
            // An OLD payload with a stale extra file that must NOT survive the republish.
            File.WriteAllText(Path.Combine(dest, "gamedev-mcp-server.exe"), "OLD");
            File.WriteAllText(Path.Combine(dest, "stale-leftover.txt"), "stale");

            var staged = Path.Combine(_tempRoot, "staging", "payload");
            Directory.CreateDirectory(staged);
            File.WriteAllText(Path.Combine(staged, "gamedev-mcp-server.exe"), "NEW");
            File.WriteAllText(Path.Combine(staged, "version"), "8.0.1");

            McpServerManager.PublishStagedBinary(staged, dest);

            Assert.AreEqual("NEW", File.ReadAllText(Path.Combine(dest, "gamedev-mcp-server.exe")),
                "the new binary must replace the old one");
            Assert.IsFalse(File.Exists(Path.Combine(dest, "stale-leftover.txt")),
                "the previous payload (and any stale files) must be removed before publish");
        }

        [Test]
        public void PublishStagedBinary_CreatesMissingParentDirectory()
        {
            var staged = Path.Combine(_tempRoot, "staging", "payload");
            Directory.CreateDirectory(staged);
            File.WriteAllText(Path.Combine(staged, "gamedev-mcp-server.exe"), "binary");

            // Parent ("cache") does NOT exist yet — mirrors the post-DeleteBinaryFolderIfExists state where the
            // whole mcp-server root was removed.
            var dest = Path.Combine(_tempRoot, "cache", "win-x64");
            Assert.IsFalse(Directory.Exists(Path.Combine(_tempRoot, "cache")));

            McpServerManager.PublishStagedBinary(staged, dest);

            Assert.IsTrue(File.Exists(Path.Combine(dest, "gamedev-mcp-server.exe")));
        }

        // --- FindExtractedBinary: layout-agnostic locator ---

        [Test]
        public void FindExtractedBinary_FlatLayout_FindsBinaryAtRoot()
        {
            // Windows release zips are FLAT: the binary sits at the extract root.
            File.WriteAllText(Path.Combine(_tempRoot, "gamedev-mcp-server.exe"), "binary");

            var found = McpServerManager.FindExtractedBinary(_tempRoot, "gamedev-mcp-server.exe");

            Assert.IsNotNull(found);
            Assert.AreEqual(_tempRoot, Path.GetDirectoryName(found));
        }

        [Test]
        public void FindExtractedBinary_NestedLayout_FindsBinaryInsideRidFolder()
        {
            // osx/linux release zips wrap the payload in a <rid>/ folder.
            var ridFolder = Path.Combine(_tempRoot, "linux-x64");
            Directory.CreateDirectory(ridFolder);
            File.WriteAllText(Path.Combine(ridFolder, "gamedev-mcp-server"), "binary");

            var found = McpServerManager.FindExtractedBinary(_tempRoot, "gamedev-mcp-server");

            Assert.IsNotNull(found);
            Assert.AreEqual(ridFolder, Path.GetDirectoryName(found));
        }

        [Test]
        public void FindExtractedBinary_PrefersShallowestMatch()
        {
            // A root-level binary must win over a deeper duplicate (the deep one cannot shadow the real one).
            File.WriteAllText(Path.Combine(_tempRoot, "gamedev-mcp-server.exe"), "root");
            var deep = Path.Combine(_tempRoot, "nested", "deeper");
            Directory.CreateDirectory(deep);
            File.WriteAllText(Path.Combine(deep, "gamedev-mcp-server.exe"), "deep");

            var found = McpServerManager.FindExtractedBinary(_tempRoot, "gamedev-mcp-server.exe");

            Assert.AreEqual(_tempRoot, Path.GetDirectoryName(found));
        }

        [Test]
        public void FindExtractedBinary_Missing_ReturnsNull()
        {
            File.WriteAllText(Path.Combine(_tempRoot, "some-other-file.txt"), "x");
            Assert.IsNull(McpServerManager.FindExtractedBinary(_tempRoot, "gamedev-mcp-server.exe"));
        }

        // --- Downloading status UI mappings (issue #845: honest UI instead of stuck "Starting…") ---

        [Test]
        public void DownloadingStatus_ButtonText_IsDownloading()
        {
            Assert.AreEqual("Downloading...", MainWindowEditor.GetServerButtonText(McpServerStatus.Downloading));
        }

        [Test]
        public void DownloadingStatus_LabelText_NamesTheDownload()
        {
            var label = MainWindowEditor.GetServerLabelText(McpServerStatus.Downloading, null);
            StringAssert.Contains("Downloading", label);
            // Must NOT masquerade as the "Starting…" state that was the bug's hallmark.
            Assert.AreNotEqual(MainWindowEditor.GetServerLabelText(McpServerStatus.Starting, null), label);
        }

        [Test]
        public void DownloadingStatus_StatusClass_IsConnecting()
        {
            Assert.AreEqual(
                MainWindowEditor.GetServerStatusClass(McpServerStatus.Starting),
                MainWindowEditor.GetServerStatusClass(McpServerStatus.Downloading),
                "Downloading shares the amber 'connecting' indicator with Starting/Stopping.");
        }

        [Test]
        public void DownloadingStatus_ButtonDisabled_DuringTransientDownload()
        {
            Assert.IsFalse(MainWindowEditor.IsServerButtonEnabled(McpServerStatus.Downloading),
                "the start/stop button must stay disabled while the binary is downloading");
        }

        // --- ServerBinaryUpdateWatcher: pure package-change filter (re-check on plugin update) ---

        [Test]
        public void AffectsUnityMcpPackage_True_WhenAddedContainsUnityMcp()
        {
            Assert.IsTrue(ServerBinaryUpdateWatcher.AffectsUnityMcpPackage(
                added: new[] { "com.unity.collab-proxy", ServerBinaryUpdateWatcher.UnityMcpPackageName },
                changedTo: null,
                changedFrom: null));
        }

        [Test]
        public void AffectsUnityMcpPackage_True_WhenChangedContainsUnityMcp_CaseInsensitive()
        {
            Assert.IsTrue(ServerBinaryUpdateWatcher.AffectsUnityMcpPackage(
                added: null,
                changedTo: new[] { "COM.IVANMURZAK.UNITY.MCP" },
                changedFrom: new[] { "com.ivanmurzak.unity.mcp" }));
        }

        [Test]
        public void AffectsUnityMcpPackage_False_ForUnrelatedPackagesOnly()
        {
            Assert.IsFalse(ServerBinaryUpdateWatcher.AffectsUnityMcpPackage(
                added: new[] { "com.unity.textmeshpro" },
                changedTo: new[] { "com.unity.test-framework" },
                changedFrom: new string?[] { null }));
        }

        [Test]
        public void AffectsUnityMcpPackage_False_ForAllNullCollections()
        {
            Assert.IsFalse(ServerBinaryUpdateWatcher.AffectsUnityMcpPackage(null, null, null));
        }
    }
}
