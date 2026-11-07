/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Text.Json;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// McpPlugin 8.9.0 no longer hard-codes the hint it logs after a SignalR connection error
    /// (<c>ConnectionConfig.ConnectionErrorHint</c>, null by default); Unity supplies its own wording.
    /// </summary>
    public class ConnectionErrorHintTests
    {
        const string ExpectedHint =
            "To stop seeing the error, please <b>Stop</b> the connection to MCP server in <b>AI Game Dev</b> window.";

        static UnityMcpPlugin.UnityConnectionConfig Config(ConnectionMode mode)
            => new UnityMcpPlugin.UnityConnectionConfig { ConnectionMode = mode };

        [Test]
        public void UnityConfig_SuppliesTheWindowHint()
        {
            Assert.AreEqual(ExpectedHint, Config(ConnectionMode.Custom).ConnectionErrorHint);
        }

        [Test]
        public void CloudMode_SaysTheSavedServerUrlIsNotUsed()
        {
            // Issue #1129: a localhost Server URL saved with Cloud mode looked like a local setup.
            Assert.AreEqual(
                UnityMcpPlugin.UnityConnectionConfig.CloudModeConnectionErrorHint + " " + ExpectedHint,
                Config(ConnectionMode.Cloud).ConnectionErrorHint);
            StringAssert.Contains(UnityMcpPlugin.UnityConnectionConfig.CloudServerUrl,
                UnityMcpPlugin.UnityConnectionConfig.CloudModeConnectionErrorHint);
        }

        [Test]
        public void CloudMode_WithoutAWindowHint_StillExplainsTheMode()
        {
            var config = Config(ConnectionMode.Cloud);
            config.ConnectionErrorHint = null;
            Assert.AreEqual(UnityMcpPlugin.UnityConnectionConfig.CloudModeConnectionErrorHint, config.ConnectionErrorHint);

            config.ConnectionMode = ConnectionMode.Custom;
            Assert.IsNull(config.ConnectionErrorHint);
        }

        [Test]
        public void Hint_IsRuntimeOnly_AndSurvivesAReload()
        {
            var json = JsonSerializer.Serialize(Config(ConnectionMode.Custom));
            StringAssert.DoesNotContain("ConnectionErrorHint", json);

            var loaded = JsonSerializer.Deserialize<UnityMcpPlugin.UnityConnectionConfig>(json)!;
            Assert.AreEqual(ExpectedHint, loaded.ConnectionErrorHint);
        }
    }
}
