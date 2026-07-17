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
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.Services;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Pure coverage of <see cref="UnityTokenRefresher"/>'s refresh-request form and its fail-closed
    /// result mapping — the <c>ITokenRefresher</c> Unity wires into the shared credential provider.
    /// </summary>
    public class UnityTokenRefresherTests
    {
        static string Value(IReadOnlyList<KeyValuePair<string, string>> form, string key)
            => form.First(kv => kv.Key == key).Value;

        [Test]
        public void RefreshForm_Is_RefreshTokenGrant()
        {
            var form = UnityTokenRefresher.BuildRefreshForm("rt-1", "unity-mcp-plugin", "mcp:plugin");
            Assert.AreEqual("refresh_token", Value(form, "grant_type"));
            Assert.AreEqual("rt-1", Value(form, "refresh_token"));
            Assert.AreEqual("unity-mcp-plugin", Value(form, "client_id"));
            Assert.AreEqual("mcp:plugin", Value(form, "scope"));
        }

        [Test]
        public void BuildResult_Success_CarriesTokensAndExpiry()
        {
            var parsed = new DeviceTokenResponse { AccessToken = "new.jwt", RefreshToken = "rt-new", ExpiresIn = 3600 };
            var result = UnityTokenRefresher.BuildResult(isSuccessStatusCode: true, statusCode: 200, parsed);
            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual("new.jwt", result.AccessToken);
            Assert.AreEqual("rt-new", result.RefreshToken);
            Assert.IsNotNull(result.ExpiresAt);
        }

        [Test]
        public void BuildResult_HttpError_FailsClosed_WithReason()
        {
            var parsed = new DeviceTokenResponse { Error = "invalid_grant" };
            var result = UnityTokenRefresher.BuildResult(isSuccessStatusCode: false, statusCode: 400, parsed);
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("invalid_grant", result.FailureReason);
            Assert.IsNull(result.AccessToken);
        }

        [Test]
        public void BuildResult_SuccessStatus_ButNoToken_FailsClosed()
        {
            var parsed = new DeviceTokenResponse { AccessToken = null };
            var result = UnityTokenRefresher.BuildResult(isSuccessStatusCode: true, statusCode: 200, parsed);
            Assert.IsFalse(result.Succeeded);
        }

        [Test]
        public void BuildResult_NullBody_FailsClosed()
        {
            var result = UnityTokenRefresher.BuildResult(isSuccessStatusCode: true, statusCode: 200, parsed: null);
            Assert.IsFalse(result.Succeeded);
        }

        [TestCase("https://ai-game.dev/mcp/", "https://ai-game.dev")]
        [TestCase("https://ai-game.dev/mcp", "https://ai-game.dev")]
        [TestCase("https://ai-game.dev/", "https://ai-game.dev")]
        [TestCase("https://ai-game.dev", "https://ai-game.dev")]
        public void NormalizeBase_TrimsSlash_And_McpHubSegment(string input, string expected)
        {
            Assert.AreEqual(expected, UnityTokenRefresher.NormalizeBase(input));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void NormalizeBase_BlankIsNull(string? input)
        {
            Assert.IsNull(UnityTokenRefresher.NormalizeBase(input));
        }
    }
}
