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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.AgentConfig;
using com.IvanMurzak.Unity.MCP.Editor.Services;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// mcp-authorize PR 3 — the Unity-side connection identity seam (<see cref="ProjectInstanceService"/>):
    /// the instance-metadata handshake payload, the per-editor-session instance id, and the project-marker
    /// read/write + server-target / pin resolution. Golden-vector parity for the shared
    /// <see cref="ProjectIdentity"/> derivation lives in <c>ProjectIdentityGoldenVectorTests</c>.
    /// </summary>
    public class ProjectInstanceServiceTests
    {
        const string ProjectRoot = "/home/dev/MyGame";

        string _markerRoot = null!;

        [SetUp]
        public void SetUp()
        {
            _markerRoot = Path.Combine(Path.GetTempPath(), "agd-marker-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_markerRoot);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_markerRoot)) Directory.Delete(_markerRoot, recursive: true); }
            catch { /* best-effort cleanup */ }
        }

        // --- Instance-metadata handshake payload ---

        [Test]
        public void BuildMetadata_ProducesUnityHandshakePayload_WithPinAsHashPrefix()
        {
            var metadata = ProjectInstanceService.BuildMetadata(ProjectRoot, "MyGame", instanceId: "sess-1");

            Assert.AreEqual("unity", metadata.Engine);
            Assert.AreEqual("MyGame", metadata.ProjectName);
            Assert.AreEqual("sess-1", metadata.InstanceId);
            // ProjectPathHash is the full 64-hex SHA-256 of the normalized root; the routing pin is its prefix.
            Assert.AreEqual(ProjectIdentity.DeriveProjectPathHash(ProjectRoot), metadata.ProjectPathHash);
            Assert.AreEqual(64, metadata.ProjectPathHash.Length);
            StringAssert.StartsWith(ProjectIdentity.DerivePin(ProjectRoot), metadata.ProjectPathHash);
        }

        [Test]
        public void BuildMetadata_DefaultsInstanceId_ToSessionInstanceId()
        {
            var metadata = ProjectInstanceService.BuildMetadata(ProjectRoot, "MyGame");

            Assert.AreEqual(ProjectInstanceService.SessionInstanceId, metadata.InstanceId);
            Assert.IsNotEmpty(metadata.InstanceId);
        }

        [Test]
        public void BuildMetadata_NullProjectName_YieldsEmptyName_NotThrow()
        {
            var metadata = ProjectInstanceService.BuildMetadata(ProjectRoot, null!);
            Assert.AreEqual(string.Empty, metadata.ProjectName);
        }

        [Test]
        public void BuildMetadata_NullProjectRoot_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ProjectInstanceService.BuildMetadata(null!, "MyGame"));
        }

        // --- Per-editor-session instance id ---

        [Test]
        public void SessionInstanceId_IsStableAcrossCalls_AndIsAGuid()
        {
            var first = ProjectInstanceService.SessionInstanceId;
            var second = ProjectInstanceService.SessionInstanceId;

            Assert.AreEqual(first, second, "the session instance id must be stable across calls (survives domain reloads)");
            Assert.IsTrue(Guid.TryParse(first, out _), "the session instance id must be a GUID");
        }

        // --- Wiring into the connection config ---

        [Test]
        public void AttachInstanceMetadata_PopulatesConfigWithUnitySessionPayload()
        {
            var config = new UnityMcpPlugin.UnityConnectionConfig();
            Assert.IsNull(config.InstanceMetadata, "precondition: a fresh config carries no handshake payload");

            ProjectInstanceService.AttachInstanceMetadata(config);

            Assert.IsNotNull(config.InstanceMetadata);
            Assert.AreEqual("unity", config.InstanceMetadata!.Engine);
            Assert.AreEqual(ProjectInstanceService.SessionInstanceId, config.InstanceMetadata.InstanceId);
            Assert.IsNotEmpty(config.InstanceMetadata.ProjectPathHash);
        }

        [Test]
        public void AttachInstanceMetadata_NullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ProjectInstanceService.AttachInstanceMetadata(null!));
        }

        // --- Project marker read/write + resolution ---

        [Test]
        public void Marker_RoundTrips_ServerTargetAndPortOverride()
        {
            ProjectInstanceService.WriteMarker(_markerRoot, new ProjectMarker
            {
                ServerTarget = "https://ai-game.dev",
                PortOverride = 26123,
            });

            // The marker lands at the tool-neutral, committable location — and carries no credentials.
            var markerPath = Path.Combine(_markerRoot, ProjectMarker.DirectoryName, ProjectMarker.FileName);
            FileAssert.Exists(markerPath);
            var json = File.ReadAllText(markerPath);
            StringAssert.DoesNotContain("token", json.ToLowerInvariant());
            StringAssert.DoesNotContain("credential", json.ToLowerInvariant());

            var read = ProjectInstanceService.ReadMarker(_markerRoot);
            Assert.IsNotNull(read);
            Assert.AreEqual("https://ai-game.dev", read!.ServerTarget);
            Assert.AreEqual(26123, read.PortOverride);
        }

        [Test]
        public void ReadMarker_ReturnsNull_WhenNoMarkerExists()
        {
            Assert.IsNull(ProjectInstanceService.ReadMarker(_markerRoot));
        }

        [Test]
        public void ResolveServerTarget_ReadsMarkerTarget_NullWhenAbsent()
        {
            Assert.IsNull(ProjectInstanceService.ResolveServerTarget(_markerRoot));

            ProjectInstanceService.WriteMarker(_markerRoot, new ProjectMarker { ServerTarget = "http://localhost:23940" });

            Assert.AreEqual("http://localhost:23940", ProjectInstanceService.ResolveServerTarget(_markerRoot));
        }

        [Test]
        public void ResolveIdentity_UsesMarkerPortOverride_PinUnchanged()
        {
            var pin = ProjectIdentity.DerivePin(_markerRoot);

            // No marker → the derived port; pin is hash-derived.
            var derived = ProjectInstanceService.ResolveIdentity(_markerRoot);
            Assert.AreEqual(ProjectIdentity.DerivePort(_markerRoot), derived.Port);
            Assert.IsFalse(derived.PortIsOverridden);
            Assert.AreEqual(pin, derived.Pin);

            // With an override marker → the override wins for Port, pin is unchanged.
            ProjectInstanceService.WriteMarker(_markerRoot, new ProjectMarker { PortOverride = 27777 });
            var overridden = ProjectInstanceService.ResolveIdentity(_markerRoot);
            Assert.AreEqual(27777, overridden.Port);
            Assert.IsTrue(overridden.PortIsOverridden);
            Assert.AreEqual(pin, overridden.Pin);
        }

        [Test]
        public void ResolvePin_MatchesSharedDerivation()
        {
            Assert.AreEqual(ProjectIdentity.DerivePin(ProjectRoot), ProjectInstanceService.ResolvePin(ProjectRoot));
        }
    }
}
