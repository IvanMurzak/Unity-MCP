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
using System.Collections.Generic;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Regression tests for the two public product bugs behind "MCP server is already Running / Failed to
    /// start MCP server after updating binary":
    ///   • Fix A (07 §7.1) — version-aware server restart: on a binary update the running server is STOPPED
    ///     before the binary folder is deleted, then STARTED after publish, so the post-publish StartServer()
    ///     never no-ops with "already Running". Verified at the status-machine level through the pure
    ///     <see cref="McpServerManager.OrchestrateServerBinaryUpdate"/> with a mock (delegate) process layer
    ///     that records call order, plus the pure <see cref="McpServerManager.WasServerRunningForUpdate"/> /
    ///     <see cref="McpServerManager.ShouldRestartAfterUpdate"/> predicates.
    ///   • Fix B (07 §7.2 / T10) — per-project server-PID key: the EditorPrefs PID key is suffixed with a
    ///     stable project-path hash so concurrent editors (even on different Unity versions sharing the single
    ///     per-user EditorPrefs hive) never adopt each other's server PIDs; the legacy shared key is migrated
    ///     once, adopting the old PID only when it owns THIS project's port.
    /// All tests are editor-free (no running Editor, no process, no network): they exercise the pure decision
    /// surface so they compile and pass under every Unity version in the matrix.
    /// </summary>
    public class McpServerRestartAndPidKeyTests
    {
        // --- Fix A: WasServerRunningForUpdate (which states count as "live, must stop first") ---

        [Test]
        public void WasServerRunningForUpdate_Running_IsTrue()
            => Assert.IsTrue(McpServerManager.WasServerRunningForUpdate(McpServerStatus.Running));

        [Test]
        public void WasServerRunningForUpdate_Starting_IsTrue()
            => Assert.IsTrue(McpServerManager.WasServerRunningForUpdate(McpServerStatus.Starting));

        [Test]
        public void WasServerRunningForUpdate_NonLiveStates_AreFalse()
        {
            Assert.IsFalse(McpServerManager.WasServerRunningForUpdate(McpServerStatus.Stopped), "Stopped is not live");
            Assert.IsFalse(McpServerManager.WasServerRunningForUpdate(McpServerStatus.Stopping), "Stopping is not live");
            Assert.IsFalse(McpServerManager.WasServerRunningForUpdate(McpServerStatus.Downloading), "Downloading is not live");
            Assert.IsFalse(McpServerManager.WasServerRunningForUpdate(McpServerStatus.External), "External is not our process");
        }

        // --- Fix A: ShouldRestartAfterUpdate (auto-start decision — the "autostart-false" gate) ---

        [Test]
        public void ShouldRestartAfterUpdate_KeepRunning_Custom_IsTrue()
            => Assert.IsTrue(McpServerManager.ShouldRestartAfterUpdate(previousKeepServerRunning: true, ConnectionMode.Custom));

        [Test]
        public void ShouldRestartAfterUpdate_KeepRunningFalse_Custom_IsFalse()
            => Assert.IsFalse(McpServerManager.ShouldRestartAfterUpdate(previousKeepServerRunning: false, ConnectionMode.Custom));

        [Test]
        public void ShouldRestartAfterUpdate_KeepRunning_Cloud_IsFalse()
            => Assert.IsFalse(McpServerManager.ShouldRestartAfterUpdate(previousKeepServerRunning: true, ConnectionMode.Cloud));

        // --- Fix A: OrchestrateServerBinaryUpdate ordering (mock process layer records call order) ---

        static McpServerManager.ServerUpdateRestartOutcome RunOrchestration(
            bool serverWasRunning,
            bool restartAfterPublish,
            List<string> log,
            bool stopSucceeds = true,
            bool publishSucceeds = true)
            => McpServerManager.OrchestrateServerBinaryUpdate(
                serverWasRunning: serverWasRunning,
                restartAfterPublish: restartAfterPublish,
                stopServerAndAwait: () => { log.Add("stop"); return stopSucceeds; },
                deleteBinaryFolder: () => log.Add("delete"),
                publishAndVerify: () => { log.Add("publish"); return publishSucceeds; },
                startServer: () => log.Add("start"));

        [Test]
        public void Orchestrate_RunningServer_StopsBeforeDelete_ThenPublish_ThenStart()
        {
            var log = new List<string>();
            var outcome = RunOrchestration(serverWasRunning: true, restartAfterPublish: true, log);

            Assert.AreEqual(McpServerManager.ServerUpdateRestartOutcome.Completed, outcome);
            CollectionAssert.AreEqual(new[] { "stop", "delete", "publish", "start" }, log,
                "the running server must be stopped before the binary folder is deleted, and restarted after publish");
            Assert.Less(log.IndexOf("stop"), log.IndexOf("delete"), "stop-before-delete ordering");
            Assert.Less(log.IndexOf("publish"), log.IndexOf("start"), "start-after-publish ordering");
        }

        [Test]
        public void Orchestrate_NotRunning_SkipsStop()
        {
            var log = new List<string>();
            var outcome = RunOrchestration(serverWasRunning: false, restartAfterPublish: true, log);

            Assert.AreEqual(McpServerManager.ServerUpdateRestartOutcome.Completed, outcome);
            CollectionAssert.DoesNotContain(log, "stop", "no stop when the server was not running");
            CollectionAssert.AreEqual(new[] { "delete", "publish", "start" }, log);
        }

        [Test]
        public void Orchestrate_StopFails_AbortsBeforeDelete()
        {
            var log = new List<string>();
            var outcome = RunOrchestration(serverWasRunning: true, restartAfterPublish: true, log, stopSucceeds: false);

            Assert.AreEqual(McpServerManager.ServerUpdateRestartOutcome.StopFailed, outcome);
            CollectionAssert.AreEqual(new[] { "stop" }, log,
                "a failed stop must abort BEFORE delete/publish/start — never publish over a live server");
        }

        [Test]
        public void Orchestrate_AutostartFalse_NeverStarts_SoAlreadyRunningPathUnreachable()
        {
            var log = new List<string>();
            var outcome = RunOrchestration(serverWasRunning: true, restartAfterPublish: false, log);

            Assert.AreEqual(McpServerManager.ServerUpdateRestartOutcome.Completed, outcome);
            CollectionAssert.DoesNotContain(log, "start",
                "under autostart-false StartServer() is never invoked, so its 'already Running' path is unreachable");
            CollectionAssert.AreEqual(new[] { "stop", "delete", "publish" }, log);
        }

        [Test]
        public void Orchestrate_PublishFails_DoesNotStart()
        {
            var log = new List<string>();
            var outcome = RunOrchestration(serverWasRunning: true, restartAfterPublish: true, log, publishSucceeds: false);

            Assert.AreEqual(McpServerManager.ServerUpdateRestartOutcome.PublishFailed, outcome);
            CollectionAssert.DoesNotContain(log, "start", "a failed publish must not restart the server");
            CollectionAssert.AreEqual(new[] { "stop", "delete", "publish" }, log);
        }

        // --- Fix B: per-project PID-key isolation across two simulated project paths ---

        const string KeyBase = "McpServerManager_ProcessId";

        [Test]
        public void ProcessIdKey_DistinctProjects_ProduceDistinctKeys()
        {
            var keyA = McpServerManager.ProcessIdKeyForDirectory(@"C:\Projects\Alpha");
            var keyB = McpServerManager.ProcessIdKeyForDirectory(@"C:\Projects\Beta");

            Assert.AreNotEqual(keyA, keyB,
                "two different project paths must map to different PID keys so concurrent editors do not adopt each other's server PID");
            StringAssert.StartsWith(KeyBase + "_", keyA, "the per-project key is the legacy base plus a hash suffix");
            StringAssert.StartsWith(KeyBase + "_", keyB);
            Assert.AreNotEqual(KeyBase, keyA, "the per-project key is never the bare legacy shared key");
        }

        [Test]
        public void ProcessIdKey_SameProject_IsStable_AndCaseInsensitive()
        {
            var key1 = McpServerManager.ProcessIdKeyForDirectory(@"C:\Projects\Alpha");
            var key2 = McpServerManager.ProcessIdKeyForDirectory(@"C:\Projects\Alpha");
            var keyLower = McpServerManager.ProcessIdKeyForDirectory(@"c:\projects\alpha");

            Assert.AreEqual(key1, key2, "the same project path must always map to the same key (deterministic)");
            Assert.AreEqual(key1, keyLower,
                "the hash is over the lower-cased directory (same convention as GeneratePortFromDirectory), so case does not fork the key");
        }

        [Test]
        public void ProcessIdKey_CrossEditorVersionHive_SameProjectSharesKey_DifferentProjectsDoNot()
        {
            // The per-user EditorPrefs hive (Windows: HKCU\...\Unity Editor 5.x) is shared by ALL projects and
            // ALL modern editor versions. The key depends ONLY on the project path (no editor-version component),
            // so a 2022.3 editor and a 6000.x editor opening the SAME project resolve the SAME key (they track one
            // shared server correctly), while two DIFFERENT projects resolve DIFFERENT keys (they never poison each
            // other across the shared hive) — the 07 §7.2 cross-editor-version scenario.
            var project = @"C:\Projects\Shared";
            var keyFrom2022 = McpServerManager.ProcessIdKeyForDirectory(project);
            var keyFrom6000 = McpServerManager.ProcessIdKeyForDirectory(project);
            Assert.AreEqual(keyFrom2022, keyFrom6000, "same project across editor versions → same key");

            var otherProject = McpServerManager.ProcessIdKeyForDirectory(@"C:\Projects\Other");
            Assert.AreNotEqual(keyFrom2022, otherProject, "different projects sharing the hive → distinct keys");
        }

        // --- Fix B: legacy-key migration decision (T10) — adopt only if the PID owns THIS project's port ---

        [Test]
        public void ShouldAdoptLegacyPid_LegacyPidOwnsThisProjectPort_Adopts()
            => Assert.IsTrue(McpServerManager.ShouldAdoptLegacyPid(legacyPid: 1234, pidListeningOnThisProjectPort: 1234),
                "the legacy PID is our own server when it owns this project's port — adopt it once");

        [Test]
        public void ShouldAdoptLegacyPid_ForeignPidOwnsPort_Ignores()
            => Assert.IsFalse(McpServerManager.ShouldAdoptLegacyPid(legacyPid: 1234, pidListeningOnThisProjectPort: 5678),
                "a legacy PID that does NOT own this project's port belongs to another editor sharing the hive — ignore it (T10)");

        [Test]
        public void ShouldAdoptLegacyPid_NoPortOwner_Ignores()
            => Assert.IsFalse(McpServerManager.ShouldAdoptLegacyPid(legacyPid: 1234, pidListeningOnThisProjectPort: -1),
                "nothing is listening on this project's port — the legacy PID cannot be confirmed as ours, so ignore it");

        [Test]
        public void ShouldAdoptLegacyPid_InvalidLegacyPid_Ignores()
        {
            Assert.IsFalse(McpServerManager.ShouldAdoptLegacyPid(legacyPid: -1, pidListeningOnThisProjectPort: -1), "no legacy PID recorded");
            Assert.IsFalse(McpServerManager.ShouldAdoptLegacyPid(legacyPid: 0, pidListeningOnThisProjectPort: 0), "PID 0 is never a valid server PID");
        }
    }
}
