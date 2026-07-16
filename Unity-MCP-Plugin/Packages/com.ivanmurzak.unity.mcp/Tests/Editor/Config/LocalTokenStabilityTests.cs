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
using System.Text.Json.Serialization;
using NUnit.Framework;
using AuthOption = com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server.AuthOption;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Guards the mcp-authorize i2 local-token lifecycle fix (Unity-MCP #897 / BUG-B): the local server
    /// token (<see cref="UnityMcpPlugin.UnityConnectionConfig.LocalToken"/>) must be seeded by
    /// <c>SetDefault()</c> and must survive a mode re-apply and a serialize/deserialize (domain reload +
    /// save/load) round-trip UNCHANGED. A silently regenerated local token orphans the already-written
    /// client <c>.mcp.json</c> (stale <c>Bearer</c>) and produces a Claude Code 401.
    /// </summary>
    public class LocalTokenStabilityTests
    {
        const string PersistedLocalToken = "PERSISTED_LOCAL_TOKEN";

        // Mirror the production serializer options so this round-trip exercises the SAME JSON shape the
        // plugin persists and reloads across a domain reload:
        //  - Save()            -> WriteIndented + CamelCase + JsonStringEnumConverter
        //  - GetOrCreateConfig -> PropertyNameCaseInsensitive + JsonStringEnumConverter
        // (both in Editor/Scripts/UnityMcpPluginEditor.Config.cs).
        static JsonSerializerOptions SaveOptions() => new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        static JsonSerializerOptions LoadOptions() => new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        static UnityMcpPlugin.UnityConnectionConfig RoundTrip(UnityMcpPlugin.UnityConnectionConfig config)
        {
            var json = JsonSerializer.Serialize(config, SaveOptions());
            return JsonSerializer.Deserialize<UnityMcpPlugin.UnityConnectionConfig>(json, LoadOptions())!;
        }

        static UnityMcpPlugin.UnityConnectionConfig CustomTokenConfig() => new UnityMcpPlugin.UnityConnectionConfig
        {
            ConnectionMode = ConnectionMode.Custom,
            AuthOption = AuthOption.token,
            LocalToken = PersistedLocalToken,
        };

        [Test]
        public void SetDefault_SeedsLocalToken_NotCloudToken()
        {
            // A freshly-constructed config (the ctor calls SetDefault) must carry a non-empty LOCAL token
            // and a null cloud token. The prior bug routed GenerateToken() into CloudToken because
            // SetDefault sets ConnectionMode=Cloud BEFORE assigning the token via the mode-routed setter,
            // leaving LocalToken unseeded (and re-mintable by the generate-if-empty fallback).
            var config = new UnityMcpPlugin.UnityConnectionConfig();

            Assert.IsFalse(string.IsNullOrEmpty(config.LocalToken),
                "SetDefault must seed a non-empty LocalToken so the generate-if-empty fallback never has to mint it.");
            Assert.IsNull(config.CloudToken,
                "SetDefault must leave CloudToken null — the cloud token comes from Cloud sign-in, not GenerateToken().");
        }

        [Test]
        public void LocalToken_StableAcrossModeReapply()
        {
            var config = CustomTokenConfig();

            // Re-apply / toggle the connection mode; the local token must never move.
            config.ConnectionMode = ConnectionMode.Cloud;
            config.ConnectionMode = ConnectionMode.Custom;

            Assert.AreEqual(PersistedLocalToken, config.LocalToken,
                "Re-applying the connection mode must not regenerate the local token.");
            Assert.AreEqual(PersistedLocalToken, config.Token,
                "In Custom mode the effective Token must resolve to the (unchanged) LocalToken.");
        }

        [Test]
        public void LocalToken_SurvivesSaveLoadRoundTrip()
        {
            var reloaded = RoundTrip(CustomTokenConfig());

            Assert.AreEqual(PersistedLocalToken, reloaded.LocalToken,
                "The persisted local token must survive a serialize/deserialize (domain reload / save-load) round-trip.");
            Assert.AreEqual(ConnectionMode.Custom, reloaded.ConnectionMode);
            Assert.AreEqual(AuthOption.token, reloaded.AuthOption);
        }

        [Test]
        public void RoundTrip_LeavesLocalTokenNonEmpty_SoGenerateIfEmptyNeverRegenerates()
        {
            // GetOrCreateConfig re-mints LocalToken only when it is null/empty after load. A persisted
            // token must therefore round-trip NON-EMPTY so that guard stays a no-op and never re-mints it.
            var reloaded = RoundTrip(CustomTokenConfig());

            Assert.IsFalse(string.IsNullOrEmpty(reloaded.LocalToken),
                "A reloaded config must carry a non-empty LocalToken so the generate-if-empty fallback stays a no-op.");
        }

        [Test]
        public void LocalToken_SerializesUnderTokenJsonField()
        {
            var json = JsonSerializer.Serialize(CustomTokenConfig(), SaveOptions());
            using var doc = JsonDocument.Parse(json);

            Assert.IsTrue(doc.RootElement.TryGetProperty("token", out var tokenProp),
                "LocalToken must serialize under the \"token\" JSON field the loader reads back.");
            Assert.AreEqual(PersistedLocalToken, tokenProp.GetString());
        }
    }
}
