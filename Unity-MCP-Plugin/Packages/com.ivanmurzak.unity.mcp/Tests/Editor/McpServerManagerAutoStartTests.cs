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
using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;
using Consts = com.IvanMurzak.McpPlugin.Common.Consts;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Regression tests for issue #695: the local MCP server must NOT be auto-started on
    /// Editor launch (or after a binary update) when the connection mode is Cloud.
    ///
    /// <see cref="McpServerManager.IsAutoStartAllowedForMode"/> is the pure decision used by
    /// every auto-start path. User-driven actions (clicking the Start button in the editor
    /// window) are intentionally not gated by this method.
    /// </summary>
    public class McpServerManagerAutoStartTests
    {
        [Test]
        public void IsAutoStartAllowedForMode_Cloud_ReturnsFalse()
        {
            Assert.IsFalse(
                McpServerManager.IsAutoStartAllowedForMode(ConnectionMode.Cloud),
                "Cloud mode must never auto-start the local MCP server (issue #695).");
        }

        [Test]
        public void IsAutoStartAllowedForMode_Custom_ReturnsTrue()
        {
            Assert.IsTrue(
                McpServerManager.IsAutoStartAllowedForMode(ConnectionMode.Custom),
                "Custom mode targets the local MCP server, so auto-start must be allowed " +
                "(subject to the other gates inside StartServerIfNeeded).");
        }

        [Test]
        public void IsAutoStartAllowedForTransport_Stdio_ReturnsFalse()
        {
            Assert.IsFalse(
                McpServerManager.IsAutoStartAllowedForTransport(Consts.MCP.Server.TransportMethod.stdio),
                "With stdio the AI agent launches the server on the project's port; an Editor-started " +
                "server there makes the agent's fail with 'address already in use'.");
        }

        [Test]
        public void IsAutoStartAllowedForTransport_StreamableHttp_ReturnsTrue()
        {
            Assert.IsTrue(
                McpServerManager.IsAutoStartAllowedForTransport(Consts.MCP.Server.TransportMethod.streamableHttp),
                "With streamableHttp the AI agent connects to the Editor-started local server.");
        }

        // Once the editor has loaded, its auto-start has decided, so a connect waits for nothing.
        [Test]
        public void WhenAutoStartSettled_NoServerStarting_RunsRightAway()
        {
            Assume.That(McpServerManager.IsStarting || McpServerManager.IsAutoStartPending, Is.False,
                "a local server is starting right now");

            var ran = false;
            McpServerManager.WhenAutoStartSettled(() => ran = true);

            Assert.IsTrue(ran);
        }
    }

    /// <summary>
    /// McpPlugin 8.11.0 servers send a tool call once and wait <c>tool-timeout</c> for it; <c>plugin-timeout</c> is
    /// the connection timeout again. The plugin passes both when it starts the server, so an older server (which
    /// ignores <c>tool-timeout</c>) keeps its previous behaviour.
    /// </summary>
    public class McpServerManagerToolTimeoutTests
    {
        [Test]
        public void Config_Defaults_ToTheServerToolTimeout()
        {
            var config = new UnityMcpPlugin.UnityConnectionConfig();

            Assert.AreEqual(Consts.Hub.DefaultToolTimeoutMs, config.ToolTimeoutMs);
            Assert.AreEqual(300_000, config.EffectiveToolTimeoutMs);
            Assert.AreEqual(Consts.Hub.DefaultTimeoutMs, config.TimeoutMs, "plugin-timeout keeps its connection default");
        }

        [TestCase(600_000, 10_000, 600_000)]
        [TestCase(5_000, 10_000, 10_000)]      // never below plugin-timeout (the server's own rule)
        [TestCase(300_000, 900_000, 900_000)]  // a Timeout raised for long tools before this setting existed
        [TestCase(0, 10_000, 300_000)]         // not positive: the default
        [TestCase(-1, 10_000, 300_000)]
        public void EffectiveToolTimeout(int toolTimeoutMs, int timeoutMs, int expected)
        {
            var config = new UnityMcpPlugin.UnityConnectionConfig { ToolTimeoutMs = toolTimeoutMs, TimeoutMs = timeoutMs };

            Assert.AreEqual(expected, config.EffectiveToolTimeoutMs);
        }

        [Test]
        public void Config_PersistsToolTimeout_AndAnOlderConfigGetsTheDefault()
        {
            var config = new UnityMcpPlugin.UnityConnectionConfig { ToolTimeoutMs = 450_000 };
            var json = JsonSerializer.Serialize(config);
            StringAssert.DoesNotContain(nameof(UnityMcpPlugin.UnityConnectionConfig.EffectiveToolTimeoutMs), json);

            Assert.AreEqual(450_000, JsonSerializer.Deserialize<UnityMcpPlugin.UnityConnectionConfig>(json)!.ToolTimeoutMs);

            var older = JsonNode.Parse(json)!.AsObject();
            older.Remove(nameof(UnityMcpPlugin.UnityConnectionConfig.ToolTimeoutMs));
            Assert.AreEqual(Consts.Hub.DefaultToolTimeoutMs,
                JsonSerializer.Deserialize<UnityMcpPlugin.UnityConnectionConfig>(older.ToJsonString())!.ToolTimeoutMs);
        }

        [Test]
        public void WithToolTimeout_AppendsTheArgument()
        {
            Assert.AreEqual("port=1 plugin-timeout=10000 tool-timeout=450000",
                McpServerManager.WithToolTimeout("port=1 plugin-timeout=10000", 450_000));
        }

        [Test]
        public void LaunchArguments_CarryBothTimeouts()
        {
            var arguments = McpServerManager.BuildArguments();

            StringAssert.Contains($"plugin-timeout={UnityMcpPluginEditor.TimeoutMs} ", arguments);
            StringAssert.EndsWith($" tool-timeout={UnityMcpPluginEditor.EffectiveToolTimeoutMs}", arguments);
        }

        [Test]
        public void StdioConfig_CarriesBothTimeouts()
        {
            var json = McpServerManager.RawJsonConfigurationStdio(8080, "mcpServers", 10_000, toolTimeoutMs: 450_000).ToJsonString();

            StringAssert.Contains("\"plugin-timeout=10000\"", json);
            StringAssert.Contains("\"tool-timeout=450000\"", json);
        }

        [TestCase(0, 10_000, 300_000)]        // unset: the server default
        [TestCase(0, 900_000, 900_000)]       // never below a raised plugin-timeout (the server does not floor an explicit tool-timeout)
        [TestCase(5_000, 10_000, 10_000)]
        public void StdioConfig_ToolTimeout_FollowsTheEffectiveRule(int toolTimeoutMs, int timeoutMs, int expected)
        {
            var json = McpServerManager.RawJsonConfigurationStdio(8080, "mcpServers", timeoutMs, toolTimeoutMs: toolTimeoutMs).ToJsonString();

            StringAssert.Contains($"\"tool-timeout={expected}\"", json);
        }

        [Test]
        public void DockerCommand_CarriesTheToolTimeoutEnv()
        {
            StringAssert.Contains($"-e MCP_PLUGIN_TOOL_TIMEOUT={UnityMcpPluginEditor.EffectiveToolTimeoutMs} ",
                McpServerManager.DockerSetupRunCommand());
        }
    }

    /// <summary>The decision to start the local server again after it exited on its own, and the backoff between tries.</summary>
    public class McpServerManagerUnexpectedExitRestartTests
    {

        [Test]
        public void RestartAfterUnexpectedExit_CustomHttpKeepRunning_IsTrue()
            => Assert.IsTrue(McpServerManager.ShouldRestartAfterUnexpectedExit(ConnectionMode.Custom, Consts.MCP.Server.TransportMethod.streamableHttp, keepServerRunning: true));

        [TestCase(ConnectionMode.Cloud, Consts.MCP.Server.TransportMethod.streamableHttp, true)]   // the cloud server is not ours
        [TestCase(ConnectionMode.Custom, Consts.MCP.Server.TransportMethod.stdio, true)]           // the agent owns a stdio server
        [TestCase(ConnectionMode.Custom, Consts.MCP.Server.TransportMethod.streamableHttp, false)] // the user does not keep it running
        public void RestartAfterUnexpectedExit_OtherwiseIsFalse(ConnectionMode mode, Consts.MCP.Server.TransportMethod transport, bool keepServerRunning)
            => Assert.IsFalse(McpServerManager.ShouldRestartAfterUnexpectedExit(mode, transport, keepServerRunning));

        [TestCase(0, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 4)]
        [TestCase(5, 32)]
        [TestCase(6, 60)]
        [TestCase(1000, 60)]
        public void RestartDelay_DoublesUpToAMinute(int attempt, double expectedSeconds)
            => Assert.AreEqual(expectedSeconds, McpServerManager.RestartDelay(attempt).TotalSeconds);
    }
}
