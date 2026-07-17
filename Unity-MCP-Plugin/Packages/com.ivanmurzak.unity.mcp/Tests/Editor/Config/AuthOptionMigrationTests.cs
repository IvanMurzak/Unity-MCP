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
using NUnit.Framework;
using AuthOption = com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server.AuthOption;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Guards the mcp-authorize g5/g6 legacy-auth migration: a persisted
    /// <see cref="AuthOption.required"/> value (the retired shared-token pairing mode whose server
    /// strategy was deleted in b5) must be migrated to the re-added offline <see cref="AuthOption.token"/>
    /// mode on load, so the local server never launches with the crash-inducing
    /// <c>authorization=required</c> value again.
    /// </summary>
    public class AuthOptionMigrationTests
    {
        [Test]
        public void Required_MigratesToToken()
        {
            var config = new UnityMcpPlugin.UnityConnectionConfig { AuthOption = AuthOption.required };

            var migrated = config.MigrateLegacyAuthOption();

            Assert.IsTrue(migrated, "required -> token migration should report a change.");
            Assert.AreEqual(AuthOption.token, config.AuthOption);
        }

        [TestCase(AuthOption.none)]
        [TestCase(AuthOption.oauth)]
        [TestCase(AuthOption.token)]
        public void NonLegacyModes_LeftUnchanged(AuthOption option)
        {
            var config = new UnityMcpPlugin.UnityConnectionConfig { AuthOption = option };

            var migrated = config.MigrateLegacyAuthOption();

            Assert.IsFalse(migrated, $"{option} is a valid target-state mode and must not migrate.");
            Assert.AreEqual(option, config.AuthOption);
        }

        [Test]
        public void Migration_IsIdempotent()
        {
            var config = new UnityMcpPlugin.UnityConnectionConfig { AuthOption = AuthOption.required };

            Assert.IsTrue(config.MigrateLegacyAuthOption(), "first pass migrates.");
            Assert.IsFalse(config.MigrateLegacyAuthOption(), "second pass is a no-op.");
            Assert.AreEqual(AuthOption.token, config.AuthOption);
        }
    }
}
