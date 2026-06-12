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
using System.IO;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Guards the plugin-version / server-version DECOUPLING: the plugin consumes the shared
    /// GameDev-MCP-Server (https://github.com/IvanMurzak/GameDev-MCP-Server) pinned by
    /// <see cref="McpServerManager.ServerVersion"/>, which diverges from the plugin's own
    /// <see cref="UnityMcpPlugin.Version"/> (plugin 0.x vs server 8.x). The download URL, the
    /// version cache marker, and the Docker image tag must ALL key on ServerVersion — never the
    /// plugin version (a plugin-version URL 404s on the shared repo). Mirrors Godot-MCP's
    /// GodotMcpServerViewTests decoupling suite.
    /// </summary>
    public class ServerVersionDecouplingTests
    {
        [Test]
        public void DownloadUrl_UsesServerVersion_NotPluginVersion()
        {
            var url = McpServerManager.ExecutableZipUrl;

            // The two versions must actually diverge for this test to prove anything.
            Assert.AreNotEqual(UnityMcpPlugin.Version, McpServerManager.ServerVersion,
                "Plugin version and pinned server version are expected to diverge (plugin 0.x, server 8.x).");

            StringAssert.Contains($"/releases/download/v{McpServerManager.ServerVersion}/", url,
                "Download URL must pin the v-prefixed ServerVersion tag.");
            StringAssert.DoesNotContain(UnityMcpPlugin.Version, url,
                "Download URL must NOT be derived from the plugin version.");
        }

        [Test]
        public void DownloadUrl_PointsAtSharedGameDevMcpServerRepo()
        {
            var url = McpServerManager.ExecutableZipUrl;

            StringAssert.StartsWith("https://github.com/IvanMurzak/GameDev-MCP-Server/releases/download/", url,
                "Server binaries are released from the shared GameDev-MCP-Server repo, not Unity-MCP.");
            StringAssert.Contains($"/gamedev-mcp-server-{McpServerManager.PlatformName}.zip", url,
                "Release asset name is gamedev-mcp-server-<rid>.zip.");
        }

        [Test]
        public void ExecutableName_IsSharedServerBinaryName()
        {
            Assert.AreEqual("gamedev-mcp-server", McpServerManager.ExecutableName);
        }

        [TestCase("8.0.0", "v8.0.0")]
        [TestCase("10.2.3", "v10.2.3")]
        public void ServerReleaseTag_PrependsV(string version, string expected)
        {
            Assert.AreEqual(expected, McpServerManager.ServerReleaseTag(version));
        }

        [Test]
        public void ServerReleaseTag_AlreadyVPrefixed_IsNotDoublePrefixed()
        {
            Assert.AreEqual("v8.0.0", McpServerManager.ServerReleaseTag("v8.0.0"));
        }

        [Test]
        public void DockerImage_UsesSharedImagePinnedByServerVersion()
        {
            var command = McpServerManager.DockerSetupRunCommand();

            StringAssert.Contains($"aigamedeveloper/mcp-server:{McpServerManager.ServerVersion}", command,
                "Docker image must be the shared aigamedeveloper/mcp-server tagged by ServerVersion.");
            StringAssert.DoesNotContain("ivanmurzakdev/unity-mcp-server", command,
                "The legacy per-engine Docker image must not be referenced anymore.");
        }

        // --- FindExtractedBinary: the shared release zips are NOT layout-uniform ------------------
        // win zips are FLAT (binary + sidecars at the zip root); osx/linux zips wrap everything in
        // a <rid>/ folder. The staging-dir extraction must find the binary in BOTH layouts.

        [Test]
        public void FindExtractedBinary_FlatLayout_FindsBinaryAtRoot()
        {
            var staging = CreateTempDir();
            try
            {
                var expected = Path.Combine(staging, "gamedev-mcp-server.exe");
                File.WriteAllText(expected, "stub");
                File.WriteAllText(Path.Combine(staging, "appsettings.json"), "{}"); // sidecar

                var found = McpServerManager.FindExtractedBinary(staging, "gamedev-mcp-server.exe");

                Assert.AreEqual(Path.GetFullPath(expected), Path.GetFullPath(found!));
            }
            finally { Directory.Delete(staging, recursive: true); }
        }

        [Test]
        public void FindExtractedBinary_NestedRidLayout_FindsBinaryInsideFolder()
        {
            var staging = CreateTempDir();
            try
            {
                var ridFolder = Path.Combine(staging, "osx-arm64");
                Directory.CreateDirectory(ridFolder);
                var expected = Path.Combine(ridFolder, "gamedev-mcp-server");
                File.WriteAllText(expected, "stub");

                var found = McpServerManager.FindExtractedBinary(staging, "gamedev-mcp-server");

                Assert.AreEqual(Path.GetFullPath(expected), Path.GetFullPath(found!));
            }
            finally { Directory.Delete(staging, recursive: true); }
        }

        [Test]
        public void FindExtractedBinary_PrefersShallowestMatch()
        {
            var staging = CreateTempDir();
            try
            {
                var nested = Path.Combine(staging, "nested", "deeper");
                Directory.CreateDirectory(nested);
                File.WriteAllText(Path.Combine(nested, "gamedev-mcp-server.exe"), "decoy");
                var expected = Path.Combine(staging, "gamedev-mcp-server.exe");
                File.WriteAllText(expected, "stub");

                var found = McpServerManager.FindExtractedBinary(staging, "gamedev-mcp-server.exe");

                Assert.AreEqual(Path.GetFullPath(expected), Path.GetFullPath(found!));
            }
            finally { Directory.Delete(staging, recursive: true); }
        }

        [Test]
        public void FindExtractedBinary_MissingBinary_ReturnsNull()
        {
            var staging = CreateTempDir();
            try
            {
                File.WriteAllText(Path.Combine(staging, "appsettings.json"), "{}");

                var found = McpServerManager.FindExtractedBinary(staging, "gamedev-mcp-server.exe");

                Assert.IsNull(found);
            }
            finally { Directory.Delete(staging, recursive: true); }
        }

        static string CreateTempDir()
        {
            var path = Path.Combine(Path.GetTempPath(), "unity-mcp-test-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
