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
using System.Linq;
using System.Text;
using com.IvanMurzak.Unity.MCP.Editor.Services;
using Xunit;
using PluginLogLevel = com.IvanMurzak.Unity.MCP.Runtime.Utils.LogLevel;
using PluginLogLevelEx = com.IvanMurzak.Unity.MCP.Runtime.Utils.LogLevelEx;

namespace com.IvanMurzak.Unity.MCP.EngineFree.Tests
{
    /// <summary>
    /// Connection configuration, engine-free half.
    ///
    /// <para>SCOPE — read this before adding a test here. The obvious target,
    /// <c>UnityMcpPlugin.UnityConnectionConfig</c> plus <c>EnvironmentUtils.ApplyEnvironmentOverrides</c>
    /// ("args &gt; env &gt; disk"), is NOT reachable from a plain xUnit host: both drag in
    /// <c>UnityEngine</c> transitively and were measured not to compile here (see README.md for the
    /// exact chains). They stay covered by the game-ci EditMode suites. What IS reachable, and is what
    /// this file covers, is the CLOUD side of the connection config — the authorization-server base URL
    /// and the endpoints/scopes/credential derived from it — plus the plugin's own log-level gate, which
    /// is the type of <c>UnityConnectionConfig.LogLevel</c>.</para>
    /// </summary>
    public class ConnectionConfigTests
    {
        const string Base = "https://ai-game.dev";

        // ── Authorization-server base URL → endpoints ──────────────────────────────────────────────

        [Theory]
        [InlineData("https://ai-game.dev")]
        [InlineData("https://ai-game.dev/")]          // a config value pasted with a trailing slash
        [InlineData("https://ai-game.dev///")]
        [InlineData("http://localhost:8080")]         // a worktree / local-dev authorization server
        public void EndpointUrls_AreAppendedToTheTrimmedBase(string configured)
        {
            // A double slash is not cosmetic here: the AS routes are exact, and `//oauth/token` is a
            // different path that 404s — the symptom being a sign-in that can never complete.
            Assert.Equal(configured.TrimEnd('/') + "/oauth/device_authorization",
                DeviceAuthService.DeviceAuthorizeUrl(configured));
            Assert.Equal(configured.TrimEnd('/') + "/oauth/token",
                DeviceAuthService.TokenUrl(configured));
        }

        [Fact]
        public void Constructor_TrimsTheBase_DefaultsTheClientId_AndRejectsAnEmptyBase()
        {
            Assert.Equal(DeviceAuthService.DefaultClientId, new DeviceAuthService(Base).ClientId);
            Assert.Equal("my-client", new DeviceAuthService(Base, clientId: "my-client").ClientId);

            // Whitespace-only overrides fall back to the default rather than being presented on the wire
            // as an empty client_id (which the AS rejects with an opaque 400).
            Assert.Equal(DeviceAuthService.DefaultClientId, new DeviceAuthService(Base, clientId: "   ").ClientId);

            Assert.Throws<ArgumentException>(() => new DeviceAuthService(""));
            Assert.Throws<ArgumentException>(() => new DeviceAuthService("   "));
            Assert.Throws<ArgumentException>(() => new DeviceAuthService(null!));
        }

        // ── The scope the LOGIN presents ───────────────────────────────────────────────────────────

        [Fact]
        public void DeviceLogin_RequestsTheAgentScope_AndIsNeverNarrowedToThePluginScope()
        {
            // A device sign-in mints the AGENT family; the plugin family is DERIVED from it later by
            // RFC 8693 token exchange. If the login were narrowed to `mcp:plugin`, the stored agent
            // family would carry a plugin-only scope and every later exchange would be unauthorised —
            // the P0-3 hazard the source comment names.
            Assert.Equal("mcp:agent", DeviceAuthService.AgentScope);
            Assert.Equal("mcp:plugin", DeviceAuthService.PluginScope);
            Assert.NotEqual(DeviceAuthService.AgentScope, DeviceAuthService.PluginScope);

            Assert.Equal(DeviceAuthService.AgentScope, new DeviceAuthService(Base).Scope);
            Assert.Equal(DeviceAuthService.AgentScope, new DeviceAuthService(Base, scope: "  ").Scope);

            // An explicit scope is still honoured — the default is a default, not a hard-code.
            Assert.Equal("custom:scope", new DeviceAuthService(Base, scope: "custom:scope").Scope);
        }

        // ── The wire forms ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public void DeviceAuthorizeForm_CarriesTheClientIdAndScope_UnderTheRfc8628FieldNames()
        {
            var form = DeviceAuthService.BuildDeviceAuthorizeForm("unity-mcp-plugin", "mcp:agent")
                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

            Assert.Equal(new[] { "client_id", "scope" }, form.Keys.OrderBy(k => k, StringComparer.Ordinal));
            Assert.Equal("unity-mcp-plugin", form["client_id"]);
            Assert.Equal("mcp:agent", form["scope"]);
        }

        [Fact]
        public void DeviceTokenForm_RedeemsTheDeviceCodeUnderTheDeviceCodeGrantUrn()
        {
            var form = DeviceAuthService.BuildDeviceTokenForm("DEV-CODE-1", "unity-mcp-plugin")
                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

            Assert.Equal(new[] { "client_id", "device_code", "grant_type" },
                form.Keys.OrderBy(k => k, StringComparer.Ordinal));

            // The URN is fixed by RFC 8628 §3.4; a typo here is rejected as unsupported_grant_type.
            Assert.Equal("urn:ietf:params:oauth:grant-type:device_code", form["grant_type"]);
            Assert.Equal("urn:ietf:params:oauth:grant-type:device_code", DeviceAuthService.DeviceCodeGrantType);
            Assert.Equal("DEV-CODE-1", form["device_code"]);
            Assert.Equal("unity-mcp-plugin", form["client_id"]);
        }

        // ── Response documents ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void DeviceAuthorizeResponse_ReadsTheSnakeCaseDocument()
        {
            var parsed = DeviceAuthService.ParseDeviceAuthorizeResponse(
                """
                {
                  "device_code": "DEV-1",
                  "user_code": "WXYZ-1234",
                  "verification_uri": "https://ai-game.dev/activate",
                  "verification_uri_complete": "https://ai-game.dev/activate?user_code=WXYZ-1234",
                  "expires_in": 600,
                  "interval": 5
                }
                """);

            Assert.Equal("DEV-1", parsed.DeviceCode);
            Assert.Equal("WXYZ-1234", parsed.UserCode);
            Assert.Equal("https://ai-game.dev/activate", parsed.VerificationUri);
            Assert.Equal("https://ai-game.dev/activate?user_code=WXYZ-1234", parsed.VerificationUriComplete);
            Assert.Equal(600, parsed.ExpiresIn);
            Assert.Equal(5, parsed.Interval);
        }

        [Fact]
        public void DeviceTokenResponse_DistinguishesASuccessFromAnRfc6749PendingError()
        {
            // Pending arrives as HTTP 400 with an error body and is deliberately NOT thrown on — the
            // poll loop reads `error` to decide whether to keep waiting, so a parser that dropped the
            // field would turn "still waiting" into "signed in with a null token".
            var pending = DeviceAuthService.ParseDeviceTokenResponse(
                """{"error":"authorization_pending","error_description":"still waiting"}""");

            Assert.Null(pending.AccessToken);
            Assert.Equal("authorization_pending", pending.Error);
            Assert.Equal("still waiting", pending.ErrorDescription);

            var granted = DeviceAuthService.ParseDeviceTokenResponse(
                """
                {"access_token":"jwt","refresh_token":"rot","token_type":"Bearer",
                 "expires_in":3600,"scope":"mcp:agent"}
                """);

            Assert.Equal("jwt", granted.AccessToken);
            Assert.Equal("rot", granted.RefreshToken);
            Assert.Equal("Bearer", granted.TokenType);
            Assert.Equal(3600, granted.ExpiresIn);
            Assert.Equal("mcp:agent", granted.Scope);
            Assert.Null(granted.Error);
        }

        // ── The credential's account identity ──────────────────────────────────────────────────────

        [Fact]
        public void JwtSubject_IsDecodedFromTheBase64UrlPayload()
        {
            // base64url: '-'/'_' instead of '+'/'/', and padding stripped — the shape an ES256 JWT
            // actually arrives in. A decoder that only handled standard base64 would return null for
            // every real token, and the store's account-switch guard keys on this value.
            //
            // The odd-looking subject is load-bearing and was chosen by search, not for readability:
            // a "nice" one (`user_42`, or even `user_42?/+` — the literal characters!) encodes to
            // base64 containing NEITHER '+' nor '/', so the fixture would exercise neither Replace()
            // and would pass with both of them deleted. Measured: this exact regression scored GREEN
            // under a plant until the payload was replaced. The two asserts below are the guard on
            // the guard — they fail loudly if a future edit makes the fixture toothless again.
            var payload = Base64Url("""{"sub":"auth0|>00?","aud":"ai-game.dev"}""");
            Assert.Contains("-", payload);
            Assert.Contains("_", payload);

            var token = "header." + payload + ".signature";
            Assert.Equal("auth0|>00?", DeviceAuthService.DecodeJwtSubject(token));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-jwt")]                       // no '.' segments
        [InlineData("header..signature")]               // empty payload
        [InlineData("header.!!!not-base64!!!.sig")]     // undecodable payload
        public void JwtSubject_ReturnsNullOnAnythingMalformed(string? token)
        {
            // Never throws: this runs on the sign-in path, and an exception here would surface as a
            // failed login rather than as "no subject recorded".
            Assert.Null(DeviceAuthService.DecodeJwtSubject(token));
        }

        [Fact]
        public void JwtSubject_ReturnsNullWhenTheClaimIsAbsentOrNotAString()
        {
            Assert.Null(DeviceAuthService.DecodeJwtSubject("h." + Base64Url("""{"aud":"ai-game.dev"}""") + ".s"));
            Assert.Null(DeviceAuthService.DecodeJwtSubject("h." + Base64Url("""{"sub":42}""") + ".s"));
            Assert.Null(DeviceAuthService.DecodeJwtSubject("h." + Base64Url("""{"sub":""}""") + ".s"));
        }

        // ── Log level: the one serialized connection-config field reachable engine-free ────────────

        [Theory]
        [InlineData(PluginLogLevel.Trace, PluginLogLevel.Trace, true)]
        [InlineData(PluginLogLevel.Warning, PluginLogLevel.Error, true)]
        [InlineData(PluginLogLevel.Warning, PluginLogLevel.Warning, true)]
        [InlineData(PluginLogLevel.Warning, PluginLogLevel.Info, false)]
        [InlineData(PluginLogLevel.None, PluginLogLevel.Exception, false)]
        public void LogLevelGate_EmitsOnlyAtOrAboveTheConfiguredThreshold(
            PluginLogLevel configured, PluginLogLevel message, bool expected)
        {
            // `UnityConnectionConfig.LogLevel` defaults to Warning, so this comparison decides what a
            // default install prints. It is `configured <= message`, i.e. the ENUM ORDER is load-bearing.
            Assert.Equal(expected, PluginLogLevelEx.IsEnabled(configured, message));
        }

        [Fact]
        public void LogLevelOrder_RunsFromTraceToNone_SoTheGateComparisonMeansWhatItSays()
        {
            // Renumbering or reordering this enum silently inverts every gate above. Pin the ordinals.
            Assert.Equal(new[]
                {
                    PluginLogLevel.Trace, PluginLogLevel.Debug, PluginLogLevel.Info,
                    PluginLogLevel.Warning, PluginLogLevel.Error, PluginLogLevel.Exception,
                    PluginLogLevel.None
                },
                Enum.GetValues(typeof(PluginLogLevel)).Cast<PluginLogLevel>().OrderBy(v => (int)v).ToArray());

            Assert.Equal(0, (int)PluginLogLevel.Trace);
            Assert.Equal(6, (int)PluginLogLevel.None);

            // None must gate out even the loudest level, or "no messages" would still print exceptions.
            Assert.False(PluginLogLevelEx.IsEnabled(PluginLogLevel.None, PluginLogLevel.Error));
        }

        static string Base64Url(string json)
            => Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
